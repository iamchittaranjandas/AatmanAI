using AatmanAI.Data.Database;
using AatmanAI.Data.Models;
using AatmanAI.Services.Interfaces;

namespace AatmanAI.Services;

public class VaultService : IVaultService
{
    private readonly AppDatabase _database;

    // SRS chunking parameters
    private const int ChunkSize = 512;       // tokens (~384 words)
    private const int ChunkOverlap = 64;     // tokens
    private const double RelevanceThreshold = 0.7;
    private const int EmbeddingDimensions = 384;

    public bool IsInitialized { get; private set; }
    public bool IsProcessing { get; private set; }
    public string? ProcessingDocumentId { get; private set; }
    public double ProcessingProgress { get; private set; }
    public int TotalDocuments { get; private set; }
    public int TotalChunks { get; private set; }
    public long StorageSizeBytes { get; private set; }

    public event Action? StateChanged;

    public VaultService(AppDatabase database)
    {
        _database = database;
    }

    public async Task InitializeAsync()
    {
        if (IsInitialized) return;
        await UpdateStatsAsync();
        IsInitialized = true;
        StateChanged?.Invoke();
    }

    private async Task UpdateStatsAsync()
    {
        var db = await _database.GetConnectionAsync();
        TotalDocuments = await db.Table<VaultDocument>().CountAsync();
        TotalChunks = await db.Table<VaultChunk>().CountAsync();

        var docs = await db.Table<VaultDocument>().ToListAsync();
        StorageSizeBytes = docs.Sum(d => (long)d.SizeBytes);
        StateChanged?.Invoke();
    }

    public async Task<VaultDocument> AddDocumentAsync(string filePath)
    {
        if (!IsInitialized) await InitializeAsync();

        var fileName = Path.GetFileName(filePath);
        var fileType = Path.GetExtension(filePath).TrimStart('.').ToLowerInvariant();

        if (!new[] { "pdf", "txt", "docx", "md" }.Contains(fileType))
            throw new ArgumentException($"Unsupported file type: {fileType}");

        // Copy to vault storage
        var vaultDir = Path.Combine(FileSystem.AppDataDirectory, "vault");
        Directory.CreateDirectory(vaultDir);

        var docId = Guid.NewGuid().ToString();
        var destPath = Path.Combine(vaultDir, $"{docId}.{fileType}");
        File.Copy(filePath, destPath);

        var fileInfo = new FileInfo(destPath);

        var document = new VaultDocument
        {
            Id = docId,
            Name = fileName,
            FilePath = destPath,
            FileType = fileType,
            SizeBytes = (int)fileInfo.Length,
            AddedAt = DateTime.UtcNow,
            IsProcessed = false
        };

        var db = await _database.GetConnectionAsync();
        await db.InsertAsync(document);
        await UpdateStatsAsync();

        // Process in background
        _ = ProcessDocumentAsync(document);

        return document;
    }

    private async Task ProcessDocumentAsync(VaultDocument document)
    {
        IsProcessing = true;
        ProcessingDocumentId = document.Id;
        ProcessingProgress = 0;
        StateChanged?.Invoke();

        try
        {
            // Step 1: Extract text (20%)
            ProcessingProgress = 0.1;
            StateChanged?.Invoke();

            var text = await ExtractTextAsync(document);
            ProcessingProgress = 0.2;
            StateChanged?.Invoke();

            // Step 2: Chunk text (40%)
            var chunks = ChunkText(text, document.Id);
            ProcessingProgress = 0.4;
            StateChanged?.Invoke();

            // Step 3: Generate embeddings (40% -> 90%)
            var db = await _database.GetConnectionAsync();
            int processed = 0;

            foreach (var chunk in chunks)
            {
                chunk.Embedding = GenerateEmbedding(chunk.Content);
                await db.InsertAsync(chunk);

                processed++;
                ProcessingProgress = 0.4 + (0.5 * processed / chunks.Count);
                StateChanged?.Invoke();
            }

            // Step 4: Update document as processed
            document.IsProcessed = true;
            document.ChunkCount = chunks.Count;
            await db.UpdateAsync(document);

            ProcessingProgress = 1.0;
            StateChanged?.Invoke();
            await UpdateStatsAsync();
        }
        catch (Exception ex)
        {
            document.IsProcessed = false;
            document.ErrorMessage = ex.Message;
            var db = await _database.GetConnectionAsync();
            await db.UpdateAsync(document);
        }
        finally
        {
            IsProcessing = false;
            ProcessingDocumentId = null;
            ProcessingProgress = 0;
            StateChanged?.Invoke();
        }
    }

    private static async Task<string> ExtractTextAsync(VaultDocument document)
    {
        return document.FileType switch
        {
            "txt" or "md" => await File.ReadAllTextAsync(document.FilePath),
            "pdf" => await File.ReadAllTextAsync(document.FilePath), // TODO: PdfPig
            "docx" => await File.ReadAllTextAsync(document.FilePath), // TODO: OpenXml
            _ => throw new NotSupportedException($"Unsupported: {document.FileType}")
        };
    }

    private static List<VaultChunk> ChunkText(string text, string documentId)
    {
        var chunks = new List<VaultChunk>();
        var words = text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        var wordsPerChunk = (int)(ChunkSize * 0.75);
        var overlapWords = (int)(ChunkOverlap * 0.75);

        int chunkIndex = 0;
        int startWord = 0;

        while (startWord < words.Length)
        {
            int endWord = Math.Min(startWord + wordsPerChunk, words.Length);
            var chunkWords = words[startWord..endWord];
            var content = string.Join(' ', chunkWords);

            int startOffset = 0;
            for (int i = 0; i < startWord && i < words.Length; i++)
                startOffset += words[i].Length + 1;

            chunks.Add(new VaultChunk
            {
                Id = Guid.NewGuid().ToString(),
                DocumentId = documentId,
                Content = content,
                ChunkIndex = chunkIndex,
                StartOffset = startOffset,
                EndOffset = startOffset + content.Length,
                SourceInfo = $"Chunk {chunkIndex + 1}"
            });

            chunkIndex++;
            startWord += wordsPerChunk - overlapWords;
            if (startWord >= words.Length) break;
        }

        return chunks;
    }

    /// Hash-based pseudo-embedding (placeholder for real MiniLM model)
    private static double[] GenerateEmbedding(string text)
    {
        var random = new Random(text.GetHashCode());
        var embedding = new double[EmbeddingDimensions];
        for (int i = 0; i < EmbeddingDimensions; i++)
            embedding[i] = random.NextDouble() * 2 - 1;
        return embedding;
    }

    public async Task<List<VaultDocument>> GetAllDocumentsAsync()
    {
        var db = await _database.GetConnectionAsync();
        return await db.Table<VaultDocument>().OrderByDescending(d => d.AddedAt).ToListAsync();
    }

    public async Task DeleteDocumentAsync(string documentId)
    {
        var db = await _database.GetConnectionAsync();

        // Delete chunks
        var chunks = await db.Table<VaultChunk>()
            .Where(c => c.DocumentId == documentId).ToListAsync();
        foreach (var chunk in chunks)
            await db.DeleteAsync(chunk);

        // Delete file
        var doc = await db.FindAsync<VaultDocument>(documentId);
        if (doc != null)
        {
            if (File.Exists(doc.FilePath))
                File.Delete(doc.FilePath);
            await db.DeleteAsync(doc);
        }

        await UpdateStatsAsync();
    }

    public async Task<List<VaultSearchResult>> SearchAsync(string query, int topK = 5)
    {
        if (!IsInitialized) await InitializeAsync();
        if (TotalDocuments == 0) return [];

        var db = await _database.GetConnectionAsync();
        var allChunks = await db.Table<VaultChunk>().ToListAsync();
        var results = new List<VaultSearchResult>();

        // Keyword-based TF-IDF scoring (works without ML model)
        var queryTerms = Tokenize(query);
        if (queryTerms.Count == 0) return [];

        // Build document frequency map across all chunks
        var docFreq = new Dictionary<string, int>();
        var chunkTermSets = new List<(VaultChunk chunk, HashSet<string> terms)>();

        foreach (var chunk in allChunks)
        {
            var terms = Tokenize(chunk.Content);
            var uniqueTerms = new HashSet<string>(terms);
            chunkTermSets.Add((chunk, uniqueTerms));

            foreach (var term in uniqueTerms)
                docFreq[term] = docFreq.GetValueOrDefault(term, 0) + 1;
        }

        int totalDocs = allChunks.Count;

        foreach (var (chunk, chunkTerms) in chunkTermSets)
        {
            double score = 0;
            var chunkWords = Tokenize(chunk.Content);

            foreach (var queryTerm in queryTerms)
            {
                // Term frequency in chunk
                int tf = chunkWords.Count(w => w == queryTerm);
                if (tf == 0) continue;

                // Inverse document frequency
                int df = docFreq.GetValueOrDefault(queryTerm, 0);
                double idf = Math.Log(1.0 + totalDocs / (1.0 + df));

                // TF-IDF score
                double tfNorm = 1.0 + Math.Log(tf);
                score += tfNorm * idf;
            }

            if (score > 0)
            {
                var doc = await db.FindAsync<VaultDocument>(chunk.DocumentId);
                if (doc != null)
                {
                    results.Add(new VaultSearchResult
                    {
                        Chunk = chunk,
                        Document = doc,
                        SimilarityScore = score
                    });
                }
            }
        }

        return results.OrderByDescending(r => r.SimilarityScore).Take(topK).ToList();
    }

    /// <summary>
    /// If there are any documents at all, return the top chunks by relevance.
    /// Fallback: return first chunks from each document (useful when query doesn't match well).
    /// </summary>
    public async Task<List<VaultSearchResult>> GetTopChunksAsync(int topK = 3)
    {
        if (!IsInitialized) await InitializeAsync();
        if (TotalDocuments == 0) return [];

        var db = await _database.GetConnectionAsync();
        var docs = await db.Table<VaultDocument>().Where(d => d.IsProcessed).ToListAsync();
        var results = new List<VaultSearchResult>();

        foreach (var doc in docs)
        {
            var chunks = await db.Table<VaultChunk>()
                .Where(c => c.DocumentId == doc.Id)
                .OrderBy(c => c.ChunkIndex)
                .ToListAsync();

            var firstChunk = chunks.FirstOrDefault();
            if (firstChunk != null)
            {
                results.Add(new VaultSearchResult
                {
                    Chunk = firstChunk,
                    Document = doc,
                    SimilarityScore = 1.0
                });
            }
        }

        return results.Take(topK).ToList();
    }

    private static List<string> Tokenize(string text)
    {
        return text.ToLowerInvariant()
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
            .Select(w => w.Trim('.', ',', '!', '?', ':', ';', '"', '\'', '(', ')', '[', ']'))
            .Where(w => w.Length > 1)
            .ToList();
    }

    public string BuildContext(List<VaultSearchResult> results)
    {
        if (results.Count == 0) return string.Empty;
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("[DOCUMENT CONTEXT]");
        foreach (var r in results)
        {
            sb.AppendLine($"From \"{r.Document.Name}\" ({r.Chunk.SourceInfo}):");
            sb.AppendLine($"\"{r.Chunk.Content}\"");
            sb.AppendLine();
        }
        sb.AppendLine("[/DOCUMENT CONTEXT]");
        return sb.ToString();
    }

    public string FormatFileSize(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
        return $"{bytes / (1024.0 * 1024.0):F1} MB";
    }
}
