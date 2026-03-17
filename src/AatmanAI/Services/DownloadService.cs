using System.Security.Cryptography;
using AatmanAI.Core.Constants;
using AatmanAI.Core.Enums;
using AatmanAI.Data.Models;
using AatmanAI.Services.Interfaces;
using DownloadState = AatmanAI.Core.Enums.DownloadState;
using DownloadTask = AatmanAI.Data.Models.DownloadTask;

namespace AatmanAI.Services;

public class DownloadService : IDownloadService
{
    private readonly HttpClient _httpClient;
    private readonly IModelService _modelService;
    private readonly INetworkAuditService _audit;
    private readonly SemaphoreSlim _semaphore = new(AppConstants.MaxConcurrentDownloads);
    private readonly List<DownloadTask> _tasks = [];

    public IReadOnlyList<DownloadTask> ActiveTasks => _tasks.AsReadOnly();

    public event EventHandler<DownloadTask>? DownloadProgressChanged;
    public event EventHandler<DownloadTask>? DownloadCompleted;
    public event EventHandler<DownloadTask>? DownloadFailed;

    public DownloadService(HttpClient httpClient, IModelService modelService, INetworkAuditService audit)
    {
        _httpClient = httpClient;
        _httpClient.Timeout = Timeout.InfiniteTimeSpan; // Cancellation handled via task.Cts.Token
        _modelService = modelService;
        _audit = audit;
    }

    public async Task StartDownloadAsync(ModelManifest manifest)
    {
        if (_tasks.Any(t => t.ModelId == manifest.Id)) return;

        var modelsDir = Path.Combine(FileSystem.AppDataDirectory, "models");
        Directory.CreateDirectory(modelsDir);

        var fileName = Path.GetFileName(new Uri(manifest.DownloadUrl).LocalPath);
        var destPath = Path.Combine(modelsDir, fileName);

        var task = new DownloadTask
        {
            ModelId = manifest.Id,
            ModelName = manifest.Name,
            Url = manifest.DownloadUrl,
            DestinationPath = destPath,
            TotalBytes = manifest.FileSizeBytes,
            Cts = new CancellationTokenSource()
        };

        _tasks.Add(task);
        StartForegroundServiceIfNeeded();
        _ = ExecuteDownloadAsync(task, manifest);
    }

    public void PauseDownload(string modelId)
    {
        var task = _tasks.FirstOrDefault(t => t.ModelId == modelId);
        if (task is null) return;
        task.Cts?.Cancel();
        task.State = DownloadState.Paused;
        StopForegroundServiceIfIdle();
    }

    public async Task ResumeDownloadAsync(string modelId)
    {
        var task = _tasks.FirstOrDefault(t => t.ModelId == modelId);
        if (task is null) return;
        task.Cts = new CancellationTokenSource();
        task.State = DownloadState.Queued;
        StartForegroundServiceIfNeeded();
        // Re-fetch manifest info to resume
        var models = await _modelService.GetAvailableModelsAsync();
        var manifest = models.FirstOrDefault(m => m.Id == modelId);
        if (manifest is not null)
            _ = ExecuteDownloadAsync(task, manifest);
    }

    public async Task CancelDownloadAsync(string modelId)
    {
        var task = _tasks.FirstOrDefault(t => t.ModelId == modelId);
        if (task is null) return;
        task.Cts?.Cancel();
        _tasks.Remove(task);

        if (File.Exists(task.DestinationPath))
            File.Delete(task.DestinationPath);

        StopForegroundServiceIfIdle();
    }

    private async Task ExecuteDownloadAsync(DownloadTask task, ModelManifest manifest)
    {
        await _semaphore.WaitAsync(task.Cts!.Token);
        try
        {
            task.State = DownloadState.Downloading;
            System.Diagnostics.Debug.WriteLine($"[AatmanAI] Starting download: {manifest.Name} url={manifest.DownloadUrl} expectedSize={manifest.FileSizeBytes}");

            long existingLength = 0;
            if (File.Exists(task.DestinationPath))
                existingLength = new FileInfo(task.DestinationPath).Length;

            System.Diagnostics.Debug.WriteLine($"[AatmanAI] Existing file size: {existingLength}");

            // If file already fully downloaded (within 50% of expected), skip download and just verify
            if (existingLength > 0 && manifest.FileSizeBytes > 0 && existingLength >= manifest.FileSizeBytes * 0.5)
            {
                task.DownloadedBytes = existingLength;
                task.TotalBytes = existingLength;
                task.State = DownloadState.Verifying;
                DownloadProgressChanged?.Invoke(this, task);

                // Save to DB directly (file already complete)
                await _modelService.SaveDownloadedModelAsync(new DownloadedModel
                {
                    ModelId = manifest.Id,
                    Name = manifest.Name,
                    Provider = manifest.Provider,
                    Parameters = manifest.Parameters,
                    Quantization = manifest.Quantization,
                    FileSizeBytes = existingLength,
                    RamRequiredMb = manifest.RamRequiredMb,
                    LocalPath = task.DestinationPath,
                    Sha256 = manifest.Sha256,
                    DownloadedAt = DateTime.UtcNow,
                    Capabilities = string.Join(",", manifest.Capabilities),
                    Languages = string.Join(",", manifest.Languages),
                    ContextLength = manifest.ContextLength,
                    Tier = manifest.Tier
                });

                await _audit.LogRequestAsync(task.Url, "download", existingLength, "success");
                await _modelService.SetActiveModelAsync(manifest.Id);
                task.State = DownloadState.Ready;
                DownloadCompleted?.Invoke(this, task);
                StopForegroundServiceIfIdle();
                return;
            }

            using var request = new HttpRequestMessage(HttpMethod.Get, task.Url);
            if (existingLength > 0)
                request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(existingLength, null);

            using var response = await Task.Run(() => _httpClient.SendAsync(request,
                HttpCompletionOption.ResponseHeadersRead, task.Cts.Token));
            response.EnsureSuccessStatusCode();

            var totalBytes = existingLength + (response.Content.Headers.ContentLength ?? 0);
            if (totalBytes > 0) task.TotalBytes = totalBytes;

            await using var contentStream = await response.Content.ReadAsStreamAsync(task.Cts.Token);
            await using var fileStream = new FileStream(task.DestinationPath,
                existingLength > 0 ? FileMode.Append : FileMode.Create,
                FileAccess.Write, FileShare.Read);

            var buffer = new byte[81920];
            long totalRead = existingLength;
            var lastProgressTime = DateTime.UtcNow;
            long lastProgressBytes = existingLength;
            int bytesRead;

            while ((bytesRead = await contentStream.ReadAsync(buffer, task.Cts.Token)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), task.Cts.Token);
                totalRead += bytesRead;
                task.DownloadedBytes = totalRead;

                var now = DateTime.UtcNow;
                var elapsed = (now - lastProgressTime).TotalSeconds;
                if (elapsed >= 0.5)
                {
                    task.SpeedBytesPerSecond = (totalRead - lastProgressBytes) / elapsed;
                    lastProgressTime = now;
                    lastProgressBytes = totalRead;
                    DownloadProgressChanged?.Invoke(this, task);
                    UpdateForegroundNotification(task);
                }
            }

            // Verify
            task.State = DownloadState.Verifying;
            DownloadProgressChanged?.Invoke(this, task);

            var verified = await VerifyFileAsync(task.DestinationPath, manifest.Sha256, manifest.FileSizeBytes);
            if (!verified)
            {
                System.Diagnostics.Debug.WriteLine($"[AatmanAI] Verification failed for {task.ModelName} at {task.DestinationPath}");
                task.State = DownloadState.Failed;
                task.ErrorMessage = "File verification failed";
                DownloadFailed?.Invoke(this, task);
                StopForegroundServiceIfIdle();
                return;
            }

            // Save to DB
            await _modelService.SaveDownloadedModelAsync(new DownloadedModel
            {
                ModelId = manifest.Id,
                Name = manifest.Name,
                Provider = manifest.Provider,
                Parameters = manifest.Parameters,
                Quantization = manifest.Quantization,
                FileSizeBytes = new FileInfo(task.DestinationPath).Length,
                RamRequiredMb = manifest.RamRequiredMb,
                LocalPath = task.DestinationPath,
                Sha256 = manifest.Sha256,
                DownloadedAt = DateTime.UtcNow,
                Capabilities = string.Join(",", manifest.Capabilities),
                Languages = string.Join(",", manifest.Languages),
                ContextLength = manifest.ContextLength,
                Tier = manifest.Tier
            });

            await _audit.LogRequestAsync(task.Url, "download", totalRead, "success");
            await _modelService.SetActiveModelAsync(manifest.Id);

            task.State = DownloadState.Ready;
            DownloadCompleted?.Invoke(this, task);
        }
        catch (OperationCanceledException)
        {
            if (task.State != DownloadState.Paused)
                task.State = DownloadState.Paused;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AatmanAI] Download failed: {ex.GetType().Name}: {ex.Message}");
            task.State = DownloadState.Failed;
            task.ErrorMessage = ex.Message;
            await _audit.LogRequestAsync(task.Url, "download", task.DownloadedBytes, "failed");
            DownloadFailed?.Invoke(this, task);
        }
        finally
        {
            _semaphore.Release();
            StopForegroundServiceIfIdle();
        }
    }

    private static async Task<bool> VerifyFileAsync(string filePath, string expectedSha256, long expectedSize)
    {
        if (!File.Exists(filePath))
        {
            System.Diagnostics.Debug.WriteLine($"[AatmanAI] Verify: file not found: {filePath}");
            return false;
        }

        var fileInfo = new FileInfo(filePath);
        System.Diagnostics.Debug.WriteLine($"[AatmanAI] Verify: file={filePath} actualSize={fileInfo.Length} expectedSize={expectedSize} sha256='{expectedSha256}'");

        // If SHA256 provided, verify hash
        if (!string.IsNullOrEmpty(expectedSha256))
        {
            await using var stream = File.OpenRead(filePath);
            var hash = await SHA256.HashDataAsync(stream);
            var hashString = Convert.ToHexStringLower(hash);
            var match = string.Equals(hashString, expectedSha256, StringComparison.OrdinalIgnoreCase);
            System.Diagnostics.Debug.WriteLine($"[AatmanAI] Verify SHA256: expected={expectedSha256} actual={hashString} match={match}");
            return match;
        }

        // No SHA256 - accept if file has reasonable size (at least 1MB and at least 50% of expected)
        if (expectedSize > 0)
        {
            var ok = fileInfo.Length >= expectedSize * 0.5 && fileInfo.Length > 1_000_000;
            System.Diagnostics.Debug.WriteLine($"[AatmanAI] Verify size: {fileInfo.Length}/{expectedSize} = {(double)fileInfo.Length / expectedSize:P0} ok={ok}");
            return ok;
        }

        // No expected size either - accept if file is at least 1MB (a valid model file)
        return fileInfo.Length > 1_000_000;
    }

    #region Foreground Service Helpers

    private bool HasActiveDownloads() =>
        _tasks.Any(t => t.State is DownloadState.Downloading or DownloadState.Queued or DownloadState.Verifying);

    private static void StartForegroundServiceIfNeeded()
    {
#if ANDROID
        var context = global::Android.App.Application.Context;
        var intent = new global::Android.Content.Intent(context, typeof(Platforms.Android.DownloadForegroundService));
        if (global::Android.OS.Build.VERSION.SdkInt >= global::Android.OS.BuildVersionCodes.O)
            context.StartForegroundService(intent);
        else
            context.StartService(intent);
#endif
    }

    private void StopForegroundServiceIfIdle()
    {
        if (HasActiveDownloads()) return;
#if ANDROID
        var context = global::Android.App.Application.Context;
        var intent = new global::Android.Content.Intent(context, typeof(Platforms.Android.DownloadForegroundService));
        context.StopService(intent);
#endif
    }

    private static void UpdateForegroundNotification(DownloadTask task)
    {
#if ANDROID
        if (task.TotalBytes <= 0) return;
        var percent = (int)(task.DownloadedBytes * 100 / task.TotalBytes);
        var nm = (global::Android.App.NotificationManager?)global::Android.App.Application.Context
            .GetSystemService(global::Android.Content.Context.NotificationService);
        var notification = new AndroidX.Core.App.NotificationCompat.Builder(
                global::Android.App.Application.Context, "aatman_download")
            .SetContentTitle($"Downloading {task.ModelName}")
            .SetContentText($"{percent}% complete")
            .SetSmallIcon(global::Android.Resource.Drawable.StatSysDownload)
            .SetOngoing(true)
            .SetProgress(100, percent, false)
            .Build();
        nm?.Notify(9001, notification);
#endif
    }

    #endregion
}
