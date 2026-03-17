using AatmanAI.Data.Models;

namespace AatmanAI.Services.Interfaces;

public interface IVaultService
{
    bool IsInitialized { get; }
    bool IsProcessing { get; }
    string? ProcessingDocumentId { get; }
    double ProcessingProgress { get; }
    int TotalDocuments { get; }
    int TotalChunks { get; }
    long StorageSizeBytes { get; }

    event Action? StateChanged;

    Task InitializeAsync();
    Task<VaultDocument> AddDocumentAsync(string filePath);
    Task<List<VaultDocument>> GetAllDocumentsAsync();
    Task DeleteDocumentAsync(string documentId);
    Task<List<VaultSearchResult>> SearchAsync(string query, int topK = 5);
    Task<List<VaultSearchResult>> GetTopChunksAsync(int topK = 3);
    string BuildContext(List<VaultSearchResult> results);
    string FormatFileSize(long bytes);
}
