namespace AatmanAI.Core.Enums;

public enum DownloadState
{
    Queued,
    Downloading,
    Paused,
    Verifying,
    Ready,
    Failed
}

public static class DownloadStateExtensions
{
    public static bool IsActive(this DownloadState state) => state == DownloadState.Downloading;
    public static bool IsPaused(this DownloadState state) => state == DownloadState.Paused;
    public static bool IsComplete(this DownloadState state) => state == DownloadState.Ready;
    public static bool CanResume(this DownloadState state) => state is DownloadState.Paused or DownloadState.Failed;
    public static bool CanPause(this DownloadState state) => state == DownloadState.Downloading;

    public static string DisplayText(this DownloadState state) => state switch
    {
        DownloadState.Queued => "Waiting...",
        DownloadState.Downloading => "Downloading",
        DownloadState.Paused => "Paused",
        DownloadState.Verifying => "Verifying...",
        DownloadState.Ready => "Ready",
        DownloadState.Failed => "Failed",
        _ => "Unknown"
    };
}
