using AatmanAI.Data.Models;

namespace AatmanAI.Services.Interfaces;

public interface IModelService
{
    Task<ModelManifestResponse?> FetchManifestAsync(bool forceRefresh = false);
    Task<List<ModelManifest>> GetAvailableModelsAsync(bool forceRefresh = false);
    Task<ModelManifest?> GetDefaultModelAsync();
    Task<List<DownloadedModel>> GetDownloadedModelsAsync();
    Task<DownloadedModel?> GetActiveModelAsync();
    Task SetActiveModelAsync(string modelId);
    Task SaveDownloadedModelAsync(DownloadedModel model);
    Task DeleteModelAsync(string modelId);
    bool IsModelCompatible(ModelManifest model, long availableRamMb);
    Task<bool> IsFirstLaunchAsync();
}
