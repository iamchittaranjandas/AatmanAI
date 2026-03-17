namespace AatmanAI.Data.Models;

public class DeviceBenchmark
{
    public long TotalRamMb { get; set; }
    public long AvailableRamMb { get; set; }
    public string GpuInfo { get; set; } = "Unknown";
    public bool HasVulkan { get; set; }
    public bool HasMetal { get; set; }
    public int CpuCores { get; set; }
    public long FreeStorageMb { get; set; }
    public int BatteryPercent { get; set; }
    public bool IsCharging { get; set; }
    public string DeviceTier { get; set; } = "mid-range"; // budget, mid-range, high-end
    public int RecommendedMaxModelMb { get; set; }
}

public class ModelCompatibility
{
    public bool IsCompatible { get; set; }
    public bool RamOk { get; set; }
    public bool StorageOk { get; set; }
    public bool QuantizationOk { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? SuggestedAlternative { get; set; }
}
