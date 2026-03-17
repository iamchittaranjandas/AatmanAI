using AatmanAI.Data.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AatmanAI.ViewModels;

public partial class MarketplaceModelItem : ObservableObject
{
    public ModelManifest Manifest { get; }

    [ObservableProperty] private bool _isDownloading;
    [ObservableProperty] private bool _isDownloaded;
    [ObservableProperty] private bool _isActive;
    [ObservableProperty] private double _downloadProgress;
    [ObservableProperty] private string _speedText = string.Empty;
    [ObservableProperty] private string _etaText = string.Empty;

    public string ProgressPercent => $"{DownloadProgress * 100:F0}%";

    // Computed visibility states — updated whenever underlying flags change
    public bool CanDownload => !IsDownloading && !IsDownloaded;
    public bool ShowProgress => IsDownloading;
    public bool CanUse     => IsDownloaded && !IsActive;

    public MarketplaceModelItem(ModelManifest manifest)
    {
        Manifest = manifest;
    }

    partial void OnIsDownloadingChanged(bool value)
    {
        OnPropertyChanged(nameof(CanDownload));
        OnPropertyChanged(nameof(ShowProgress));
        OnPropertyChanged(nameof(CanUse));
    }

    partial void OnIsDownloadedChanged(bool value)
    {
        OnPropertyChanged(nameof(CanDownload));
        OnPropertyChanged(nameof(CanUse));
    }

    partial void OnIsActiveChanged(bool value)
    {
        OnPropertyChanged(nameof(CanUse));
    }

    partial void OnDownloadProgressChanged(double value)
    {
        OnPropertyChanged(nameof(ProgressPercent));
    }
}
