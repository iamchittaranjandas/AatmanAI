using System.Text.Json;
using AatmanAI.Core.Constants;
using AatmanAI.Data.Database;
using AatmanAI.Data.Models;
using AatmanAI.Services.Interfaces;

namespace AatmanAI.Services;

public class ModelService : IModelService
{
    private readonly AppDatabase _db;
    private readonly HttpClient _httpClient;
    private readonly INetworkAuditService _audit;

    private ModelManifestResponse? _cachedManifest;
    private DateTime? _manifestCacheTime;

    public ModelService(AppDatabase db, HttpClient httpClient, INetworkAuditService audit)
    {
        _db = db;
        _httpClient = httpClient;
        _audit = audit;
    }

    public async Task<ModelManifestResponse?> FetchManifestAsync(bool forceRefresh = false)
    {
        // Check 24hr cache
        if (!forceRefresh && _cachedManifest is not null && _manifestCacheTime is not null
            && (DateTime.UtcNow - _manifestCacheTime.Value).TotalHours < 24)
        {
            return _cachedManifest;
        }

        ModelManifestResponse? manifest = null;
        var loadedFromSource = false;

        // Try remote (run on background thread to avoid Android NetworkOnMainThreadException)
        try
        {
            var json = await Task.Run(() => _httpClient.GetStringAsync(AppConstants.ManifestUrl));
            await _audit.LogRequestAsync(AppConstants.ManifestUrl, "manifest", json.Length, "success");
            manifest = JsonSerializer.Deserialize<ModelManifestResponse>(json);
            loadedFromSource = true;
        }
        catch
        {
            await _audit.LogRequestAsync(AppConstants.ManifestUrl, "manifest", 0, "failed");
        }

        // Try bundled asset
        if (!loadedFromSource)
        {
            try
            {
                using var stream = await FileSystem.OpenAppPackageFileAsync("model_manifest.json");
                manifest = await JsonSerializer.DeserializeAsync<ModelManifestResponse>(stream);
                loadedFromSource = true;
            }
            catch { /* bundled asset not available */ }
        }

        // Fallback to hardcoded
        if (!loadedFromSource)
        {
            manifest = GetBuiltInManifest();
        }

        if (manifest is not null)
        {
            // Filter to supported models, then brand
            var filtered = manifest.Models.Where(IsSupportedForCurrentRuntime).ToList();
            if (filtered.Count == 0) manifest = GetBuiltInManifest();
            else manifest.Models = filtered;

            manifest.Models = manifest.Models.Select(BrandModel).ToList();
        }

        _cachedManifest = manifest;
        _manifestCacheTime = DateTime.UtcNow;
        return manifest;
    }

    public async Task<List<ModelManifest>> GetAvailableModelsAsync(bool forceRefresh = false)
    {
        var manifest = await FetchManifestAsync(forceRefresh);
        return manifest?.Models ?? [];
    }

    public async Task<ModelManifest?> GetDefaultModelAsync()
    {
        var manifest = await FetchManifestAsync();
        if (manifest is null) return null;

        return manifest.Models.FirstOrDefault(m => m.Id == AppConstants.FirstLaunchDefaultModelId)
            ?? manifest.Models.FirstOrDefault(m => m.IsDefault)
            ?? manifest.Models.Where(m => m.Tier == "free").OrderBy(m => m.FileSizeBytes).FirstOrDefault();
    }

    public async Task<List<DownloadedModel>> GetDownloadedModelsAsync()
    {
        var conn = await _db.GetConnectionAsync();
        var models = await conn.Table<DownloadedModel>().ToListAsync();
        // Brand each model
        foreach (var m in models)
        {
            m.Name = $"AATMAN AI {m.Parameters}";
            m.Provider = AppConstants.AppName.ToUpper();
        }
        return models;
    }

    public async Task<DownloadedModel?> GetActiveModelAsync()
    {
        var models = await GetDownloadedModelsAsync();
        var active = models.FirstOrDefault(m => m.IsActive);
        if (active is not null) return active;

        // Auto-activate first available model if none active
        if (models.Count > 0)
        {
            await SetActiveModelAsync(models[0].ModelId);
            models[0].IsActive = true;
            return models[0];
        }

        return null;
    }

    public async Task SetActiveModelAsync(string modelId)
    {
        var conn = await _db.GetConnectionAsync();
        // Deactivate all
        var all = await conn.Table<DownloadedModel>().ToListAsync();
        foreach (var m in all.Where(m => m.IsActive))
        {
            m.IsActive = false;
            await conn.UpdateAsync(m);
        }
        // Activate selected
        var model = await conn.FindAsync<DownloadedModel>(modelId);
        if (model is not null)
        {
            model.IsActive = true;
            model.LastUsedAt = DateTime.UtcNow;
            model.UseCount++;
            await conn.UpdateAsync(model);
        }
    }

    public async Task SaveDownloadedModelAsync(DownloadedModel model)
    {
        model.Name = $"AATMAN AI {model.Parameters}";
        model.Provider = AppConstants.AppName.ToUpper();
        var conn = await _db.GetConnectionAsync();
        await conn.InsertOrReplaceAsync(model);
    }

    public async Task DeleteModelAsync(string modelId)
    {
        var conn = await _db.GetConnectionAsync();
        var model = await conn.FindAsync<DownloadedModel>(modelId);
        if (model is not null)
        {
            if (File.Exists(model.LocalPath))
                File.Delete(model.LocalPath);
            await conn.DeleteAsync(model);
        }
    }

    public bool IsModelCompatible(ModelManifest model, long availableRamMb)
    {
        var requiredRam = (int)(model.RamRequiredMb * 1.2);
        return availableRamMb >= requiredRam;
    }

    public async Task<bool> IsFirstLaunchAsync()
    {
        var models = await GetDownloadedModelsAsync();
        return models.Count == 0;
    }

    private static bool IsSupportedForCurrentRuntime(ModelManifest model)
    {
        var tier = model.Tier.ToLowerInvariant();
        var isSupportedTier = tier is "free" or "power" or "ultra";
        var isTextModel = model.Category == "text-to-text";
        var isGguf = model.DownloadUrl.Contains(".gguf", StringComparison.OrdinalIgnoreCase);
        var isSupportedQuant = AppConstants.AllowedQuantizations.Contains(model.Quantization);
        return isSupportedTier && isTextModel && isGguf && isSupportedQuant;
    }

    private static ModelManifest BrandModel(ModelManifest model)
    {
        var capacity = model.Capabilities.Count > 0
            ? string.Join(", ", model.Capabilities.Take(3).Select(FormatCapability))
            : "General Assistant";

        return new ModelManifest
        {
            Id = model.Id,
            Name = $"AATMAN AI {model.Parameters}",
            Provider = AppConstants.AppName.ToUpper(),
            Parameters = model.Parameters,
            Quantization = model.Quantization,
            Format = model.Format,
            FileSizeBytes = model.FileSizeBytes,
            RamRequiredMb = model.RamRequiredMb,
            DownloadUrl = model.DownloadUrl,
            Sha256 = model.Sha256,
            Description = $"Capacity: {capacity}. Parameters: {model.Parameters}. Private on-device intelligence.",
            Capabilities = model.Capabilities,
            Languages = model.Languages,
            ContextLength = model.ContextLength,
            Tier = model.Tier,
            Category = model.Category,
            IsDefault = model.IsDefault
        };
    }

    private static string FormatCapability(string cap) => cap.Trim().ToLowerInvariant() switch
    {
        "chat" => "Chat",
        "reasoning" => "Reasoning",
        "coding" => "Coding",
        "creative" => "Creative",
        "multilingual" => "Multilingual",
        "voice" => "Voice",
        _ => string.Concat(cap.Trim()[..1].ToUpper(), cap.Trim().AsSpan(1))
    };

    public static ModelManifestResponse GetBuiltInManifest() => new()
    {
        Version = "1.0.0",
        Models =
        [
            new() { Id = "smollm2-135m-instruct", Name = "SmolLM2 135M Instruct", Provider = "HuggingFace", Parameters = "135M", Quantization = "Q4_K_M", FileSizeBytes = 89128960, RamRequiredMb = 500, DownloadUrl = "https://huggingface.co/HuggingFaceTB/SmolLM2-135M-Instruct-GGUF/resolve/main/smollm2-135m-instruct-q4_k_m.gguf", Sha256 = "", Description = "Tiny but capable model.", Capabilities = ["chat"], Languages = ["en"], Tier = "free", ContextLength = 2048, Category = "text-to-text" },
            new() { Id = "qwen2.5-0.5b", Name = "Qwen 2.5 0.5B Instruct", Provider = "Alibaba", Parameters = "0.5B", Quantization = "Q4_K_M", FileSizeBytes = 416284672, RamRequiredMb = 512, DownloadUrl = "https://huggingface.co/Qwen/Qwen2.5-0.5B-Instruct-GGUF/resolve/main/qwen2.5-0.5b-instruct-q4_k_m.gguf", Sha256 = "", Description = "Small and capable multilingual model.", Capabilities = ["chat", "reasoning", "multilingual"], Languages = ["en", "zh", "es", "fr", "de", "ja", "ko"], Tier = "free", ContextLength = 32768, IsDefault = true, Category = "text-to-text" },
            new() { Id = "gemma-3-1b-it", Name = "Gemma 3 1B IT", Provider = "Google", Parameters = "1B", Quantization = "Q4_K_M", FileSizeBytes = 826277888, RamRequiredMb = 1500, DownloadUrl = "https://huggingface.co/lmstudio-community/gemma-3-1b-it-GGUF/resolve/main/gemma-3-1b-it-Q4_K_M.gguf", Sha256 = "", Description = "Gemma 3 1B optimized for mobile.", Capabilities = ["chat", "reasoning", "creative"], Languages = ["en"], Tier = "free", ContextLength = 32768, Category = "text-to-text" },
            new() { Id = "bitnet-b1.58-2b-4t", Name = "BitNet b1.58 2B 4T", Provider = "Microsoft", Parameters = "2B", Quantization = "I2_S", FileSizeBytes = 1277752770, RamRequiredMb = 2000, DownloadUrl = "https://huggingface.co/microsoft/bitnet-b1.58-2B-4T-gguf/resolve/main/ggml-model-i2_s.gguf", Sha256 = "", Description = "Efficient BitNet model.", Capabilities = ["chat", "reasoning"], Languages = ["en"], Tier = "free", ContextLength = 4096, Category = "text-to-text" },
            new() { Id = "llama-3.2-1b-instruct", Name = "Llama 3.2 1B Instruct", Provider = "Meta", Parameters = "1B", Quantization = "Q4_K_M", FileSizeBytes = 1073741824, RamRequiredMb = 1500, DownloadUrl = "https://huggingface.co/bartowski/Llama-3.2-1B-Instruct-GGUF/resolve/main/Llama-3.2-1B-Instruct-Q4_K_M.gguf", Sha256 = "", Description = "Great balance of quality and speed.", Capabilities = ["chat", "reasoning"], Languages = ["en"], Tier = "power", ContextLength = 4096, Category = "text-to-text" },
            new() { Id = "phi-3.5-mini-instruct", Name = "Phi-3.5 Mini Instruct", Provider = "Microsoft", Parameters = "3.8B", Quantization = "Q4_K_M", FileSizeBytes = 2362232012, RamRequiredMb = 4000, DownloadUrl = "https://huggingface.co/bartowski/Phi-3.5-mini-instruct-GGUF/resolve/main/Phi-3.5-mini-instruct-Q4_K_M.gguf", Sha256 = "", Description = "Excellent for complex reasoning and coding.", Capabilities = ["chat", "reasoning", "coding", "creative"], Languages = ["en"], Tier = "ultra", ContextLength = 4096, Category = "text-to-text" },
        ]
    };
}
