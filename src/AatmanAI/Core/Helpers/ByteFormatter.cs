namespace AatmanAI.Core.Helpers;

public static class ByteFormatter
{
    public static string Format(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
        < 1024L * 1024 * 1024 => $"{bytes / (1024.0 * 1024):F1} MB",
        _ => $"{bytes / (1024.0 * 1024 * 1024):F2} GB"
    };

    public static string FormatSpeed(double bytesPerSecond) => bytesPerSecond switch
    {
        < 1024 => $"{bytesPerSecond:F0} B/s",
        < 1024 * 1024 => $"{bytesPerSecond / 1024:F1} KB/s",
        _ => $"{bytesPerSecond / (1024 * 1024):F1} MB/s"
    };
}
