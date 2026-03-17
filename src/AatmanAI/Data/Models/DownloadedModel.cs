using SQLite;

namespace AatmanAI.Data.Models;

[Table("DownloadedModels")]
public class DownloadedModel
{
    [PrimaryKey]
    public string ModelId { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string Parameters { get; set; } = string.Empty;
    public string Quantization { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public int RamRequiredMb { get; set; }
    public string LocalPath { get; set; } = string.Empty;
    public string Sha256 { get; set; } = string.Empty;
    public DateTime DownloadedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastUsedAt { get; set; }
    public int UseCount { get; set; }
    public string? Capabilities { get; set; } // comma-separated
    public string? Languages { get; set; } // comma-separated
    public int ContextLength { get; set; } = 4096;
    public string Tier { get; set; } = "free";
    public bool IsActive { get; set; }

    // Computed
    [Ignore]
    public string FileSizeDisplay => FileSizeBytes switch
    {
        < 1024 * 1024 => $"{FileSizeBytes / 1024.0:F1} KB",
        < 1024 * 1024 * 1024 => $"{FileSizeBytes / (1024.0 * 1024):F1} MB",
        _ => $"{FileSizeBytes / (1024.0 * 1024 * 1024):F2} GB"
    };
}
