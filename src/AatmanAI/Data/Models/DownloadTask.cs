using AatmanAI.Core.Enums;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AatmanAI.Data.Models;

public partial class DownloadTask : ObservableObject
{
    public string ModelId { get; set; } = string.Empty;
    public string ModelName { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string DestinationPath { get; set; } = string.Empty;
    public long TotalBytes { get; set; }

    [ObservableProperty] private long _downloadedBytes;
    [ObservableProperty] private DownloadState _state = DownloadState.Queued;
    [ObservableProperty] private string? _errorMessage;
    [ObservableProperty] private double _speedBytesPerSecond;

    public double Progress => TotalBytes > 0 ? (double)DownloadedBytes / TotalBytes : 0;
    public string ProgressPercent => $"{Progress * 100:F1}%";

    public string? Eta
    {
        get
        {
            if (SpeedBytesPerSecond <= 0 || TotalBytes <= 0) return null;
            var remaining = TotalBytes - DownloadedBytes;
            var seconds = remaining / SpeedBytesPerSecond;
            return seconds switch
            {
                < 60 => $"{seconds:F0}s",
                < 3600 => $"{seconds / 60:F0}m",
                _ => $"{seconds / 3600:F1}h"
            };
        }
    }

    public CancellationTokenSource? Cts { get; set; }
}
