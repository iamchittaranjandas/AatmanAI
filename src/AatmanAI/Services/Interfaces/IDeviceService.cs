using AatmanAI.Data.Models;

namespace AatmanAI.Services.Interfaces;

public interface IDeviceService
{
    Task<DeviceBenchmark> RunBenchmarkAsync();
    Task<ModelCompatibility> CheckModelCompatibilityAsync(ModelManifest model);
    Task<bool> HasNetworkConnectionAsync();
    long GetAvailableRamMb();
    long GetTotalRamMb();
    long GetFreeStorageMb();
}
