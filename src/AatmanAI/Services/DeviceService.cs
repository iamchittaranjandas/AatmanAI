using AatmanAI.Core.Constants;
using AatmanAI.Data.Models;
using AatmanAI.Services.Interfaces;

namespace AatmanAI.Services;

public class DeviceService : IDeviceService
{
    private DeviceBenchmark? _cached;

    public async Task<DeviceBenchmark> RunBenchmarkAsync()
    {
        // Only cache GPU/tier info; always re-read RAM and storage so values stay accurate
        var cachedTier = _cached?.DeviceTier;
        var cachedGpu = _cached?.GpuInfo;

        var totalRam = GetTotalRamMb();
        var availableRam = GetAvailableRamMb();
        var freeStorage = GetFreeStorageMb();

        // Use cached tier/gpu if available (expensive to recompute)
        if (cachedTier is not null) {
            _cached!.TotalRamMb = totalRam;
            _cached.AvailableRamMb = availableRam;
            _cached.FreeStorageMb = freeStorage;
            return _cached;
        }

        int batteryPercent = 100;
        bool isCharging = false;
        try
        {
            batteryPercent = (int)(Battery.Default.ChargeLevel * 100);
            isCharging = Battery.Default.State is BatteryState.Charging or BatteryState.Full;
        }
        catch { /* battery API unavailable */ }

        var hasVulkan = DeviceInfo.Current.Platform == DevicePlatform.Android;
        var hasMetal = DeviceInfo.Current.Platform == DevicePlatform.iOS;

        var deviceTier = CalculateDeviceTier(totalRam, hasVulkan || hasMetal);
        var recommendedMax = CalculateRecommendedMaxModel(deviceTier);

        _cached = new DeviceBenchmark
        {
            TotalRamMb = totalRam,
            AvailableRamMb = availableRam,
            GpuInfo = hasVulkan ? "Vulkan" : hasMetal ? "Metal" : "CPU only",
            HasVulkan = hasVulkan,
            HasMetal = hasMetal,
            CpuCores = Environment.ProcessorCount,
            FreeStorageMb = freeStorage,
            BatteryPercent = batteryPercent,
            IsCharging = isCharging,
            DeviceTier = deviceTier,
            RecommendedMaxModelMb = recommendedMax
        };

        return _cached;
    }

    public async Task<ModelCompatibility> CheckModelCompatibilityAsync(ModelManifest model)
    {
        var benchmark = await RunBenchmarkAsync();
        var requiredRamWithBuffer = (int)(model.RamRequiredMb * 1.2);
        var requiredStorageMb = model.FileSizeBytes / (1024 * 1024) + 100; // 100MB buffer only

        var ramOk = benchmark.AvailableRamMb >= requiredRamWithBuffer;
        var storageOk = benchmark.FreeStorageMb >= requiredStorageMb;
        var quantOk = AppConstants.AllowedQuantizations.Contains(model.Quantization);

        string message;
        if (!ramOk) message = $"Not enough RAM. Needs {model.RamRequiredMb}MB, only {benchmark.AvailableRamMb}MB available.";
        else if (!storageOk) message = $"Not enough storage. Needs {requiredStorageMb}MB free.";
        else if (!quantOk) message = $"Unsupported quantization: {model.Quantization}";
        else message = "Compatible";

        return new ModelCompatibility
        {
            IsCompatible = ramOk && storageOk && quantOk,
            RamOk = ramOk,
            StorageOk = storageOk,
            QuantizationOk = quantOk,
            Message = message
        };
    }

    public async Task<bool> HasNetworkConnectionAsync()
    {
        try
        {
            var access = Connectivity.Current.NetworkAccess;
            return access == NetworkAccess.Internet;
        }
        catch { return false; }
    }

    public long GetTotalRamMb()
    {
#if ANDROID
        try
        {
            var lines = File.ReadAllLines("/proc/meminfo");
            foreach (var line in lines)
            {
                if (line.StartsWith("MemTotal:"))
                {
                    var match = System.Text.RegularExpressions.Regex.Match(line, @"(\d+)");
                    if (match.Success && long.TryParse(match.Groups[1].Value, out var kb))
                        return kb / 1024;
                }
            }
        }
        catch { }
#endif
        return 4096; // Default 4GB
    }

    public long GetAvailableRamMb()
    {
#if ANDROID
        try
        {
            var lines = File.ReadAllLines("/proc/meminfo");
            foreach (var line in lines)
            {
                if (line.StartsWith("MemAvailable:"))
                {
                    var match = System.Text.RegularExpressions.Regex.Match(line, @"(\d+)");
                    if (match.Success && long.TryParse(match.Groups[1].Value, out var kb))
                        return kb / 1024;
                }
            }
        }
        catch { }
#endif
        return 2048; // Default 2GB
    }

    public long GetFreeStorageMb()
    {
        try
        {
#if ANDROID
            // DriveInfo("/") reads a tiny tmpfs on Android and returns ~1GB.
            // StatFs on the actual app data directory gives the real internal storage free space.
            var stat = new Android.OS.StatFs(FileSystem.AppDataDirectory);
            return (stat.AvailableBlocksLong * stat.BlockSizeLong) / (1024 * 1024);
#else
            var appDir = FileSystem.AppDataDirectory;
            var driveInfo = new DriveInfo(Path.GetPathRoot(appDir) ?? appDir);
            return driveInfo.AvailableFreeSpace / (1024 * 1024);
#endif
        }
        catch
        {
            return 10240; // Default 10GB
        }
    }

    private static string CalculateDeviceTier(long totalRamMb, bool hasGpuAcceleration) =>
        (totalRamMb, hasGpuAcceleration) switch
        {
            ( >= 12288, true) => "high-end",
            ( >= 8192, true) => "high-end",
            ( >= 6144, _) => "mid-range",
            _ => "budget"
        };

    private static int CalculateRecommendedMaxModel(string tier) => tier switch
    {
        "high-end" => 4000,
        "mid-range" => 2500,
        _ => 1500
    };
}
