using AatmanAI.Data.Models;

namespace AatmanAI.Services.Interfaces;

public interface IDownloadService
{
    IReadOnlyList<DownloadTask> ActiveTasks { get; }

    Task StartDownloadAsync(ModelManifest manifest);
    void PauseDownload(string modelId);
    Task ResumeDownloadAsync(string modelId);
    Task CancelDownloadAsync(string modelId);

    event EventHandler<DownloadTask>? DownloadProgressChanged;
    event EventHandler<DownloadTask>? DownloadCompleted;
    event EventHandler<DownloadTask>? DownloadFailed;
}
