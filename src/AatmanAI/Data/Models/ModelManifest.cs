using System.Text.Json.Serialization;

namespace AatmanAI.Data.Models;

public class ModelManifestResponse
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    [JsonPropertyName("models")]
    public List<ModelManifest> Models { get; set; } = [];
}

public class ModelManifest
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("provider")]
    public string Provider { get; set; } = string.Empty;

    [JsonPropertyName("parameters")]
    public string Parameters { get; set; } = string.Empty;

    [JsonPropertyName("quantization")]
    public string Quantization { get; set; } = string.Empty;

    [JsonPropertyName("format")]
    public string Format { get; set; } = "gguf";

    [JsonPropertyName("fileSizeBytes")]
    public long FileSizeBytes { get; set; }

    [JsonPropertyName("ramRequiredMb")]
    public int RamRequiredMb { get; set; }

    [JsonPropertyName("downloadUrl")]
    public string DownloadUrl { get; set; } = string.Empty;

    [JsonPropertyName("sha256")]
    public string Sha256 { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("capabilities")]
    public List<string> Capabilities { get; set; } = [];

    [JsonPropertyName("languages")]
    public List<string> Languages { get; set; } = [];

    [JsonPropertyName("contextLength")]
    public int ContextLength { get; set; } = 4096;

    [JsonPropertyName("tier")]
    public string Tier { get; set; } = "free";

    [JsonPropertyName("category")]
    public string Category { get; set; } = "general";

    [JsonPropertyName("isDefault")]
    public bool IsDefault { get; set; }

    // Computed
    [JsonIgnore]
    public string FileSizeDisplay => FileSizeBytes switch
    {
        < 1024 * 1024 => $"{FileSizeBytes / 1024.0:F1} KB",
        < 1024 * 1024 * 1024 => $"{FileSizeBytes / (1024.0 * 1024):F1} MB",
        _ => $"{FileSizeBytes / (1024.0 * 1024 * 1024):F2} GB"
    };

    [JsonIgnore]
    public string RamDisplay => RamRequiredMb >= 1024
        ? $"{RamRequiredMb / 1024.0:F1} GB"
        : $"{RamRequiredMb} MB";
}
