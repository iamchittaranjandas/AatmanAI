using SQLite;

namespace AatmanAI.Data.Models;

[Table("vault_documents")]
public class VaultDocument
{
    [PrimaryKey]
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty; // pdf, txt, docx, md
    public int SizeBytes { get; set; }
    public int ChunkCount { get; set; }
    public DateTime AddedAt { get; set; }
    public DateTime? LastAccessedAt { get; set; }
    public bool IsProcessed { get; set; }
    public string? ErrorMessage { get; set; }
}

[Table("vault_chunks")]
public class VaultChunk
{
    [PrimaryKey]
    public string Id { get; set; } = string.Empty;
    [Indexed]
    public string DocumentId { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public int ChunkIndex { get; set; }
    public int StartOffset { get; set; }
    public int EndOffset { get; set; }
    public byte[]? EmbeddingBlob { get; set; } // serialized double[]
    public string? SourceInfo { get; set; }

    [Ignore]
    public double[]? Embedding
    {
        get
        {
            if (EmbeddingBlob == null || EmbeddingBlob.Length == 0) return null;
            var doubles = new double[EmbeddingBlob.Length / sizeof(double)];
            Buffer.BlockCopy(EmbeddingBlob, 0, doubles, 0, EmbeddingBlob.Length);
            return doubles;
        }
        set
        {
            if (value == null) { EmbeddingBlob = null; return; }
            EmbeddingBlob = new byte[value.Length * sizeof(double)];
            Buffer.BlockCopy(value, 0, EmbeddingBlob, 0, EmbeddingBlob.Length);
        }
    }
}

public class VaultSearchResult
{
    public required VaultChunk Chunk { get; init; }
    public required VaultDocument Document { get; init; }
    public double SimilarityScore { get; init; }
}
