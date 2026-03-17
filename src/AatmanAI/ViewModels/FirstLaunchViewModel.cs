using AatmanAI.Data.Models;
using AatmanAI.Services.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AatmanAI.ViewModels;

public partial class FirstLaunchViewModel : BaseViewModel
{
    private readonly IModelService _modelService;
    private readonly IDownloadService _downloadService;

    [ObservableProperty] private string _statusText = "Preparing your AI...";
    [ObservableProperty] private double _progress;
    [ObservableProperty] private string _progressText = "0%";
    [ObservableProperty] private bool _isDownloading;
    [ObservableProperty] private string? _modelName;

    public FirstLaunchViewModel(IModelService modelService, IDownloadService downloadService)
    {
        _modelService = modelService;
        _downloadService = downloadService;

        _downloadService.DownloadProgressChanged += OnDownloadProgress;
        _downloadService.DownloadCompleted += OnDownloadCompleted;
        _downloadService.DownloadFailed += OnDownloadFailed;
    }

    [RelayCommand]
    private async Task StartDownloadAsync()
    {
        if (IsBusy) return;
        IsBusy = true;

        StatusText = "Finding best model for your device...";
        var defaultModel = await _modelService.GetDefaultModelAsync();
        if (defaultModel is null)
        {
            StatusText = "Could not find a suitable model. Please check your connection.";
            IsBusy = false;
            return;
        }

        ModelName = defaultModel.Name;
        StatusText = $"Downloading {defaultModel.Name}...";
        IsDownloading = true;

        await _downloadService.StartDownloadAsync(defaultModel);
    }

    [RelayCommand]
    private async Task SkipAsync()
    {
        await Shell.Current.GoToAsync("//main/home");
    }

    private void OnDownloadProgress(object? sender, Data.Models.DownloadTask task)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            Progress = task.Progress;
            ProgressText = task.ProgressPercent;
            StatusText = $"Downloading {ModelName}... {task.ProgressPercent}";
        });
    }

    private async void OnDownloadCompleted(object? sender, Data.Models.DownloadTask task)
    {
        // Set as active model
        await _modelService.SetActiveModelAsync(task.ModelId);

        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            StatusText = "Ready!";
            Progress = 1.0;
            await Task.Delay(500);
            await Shell.Current.GoToAsync("//main/home");
        });
    }

    private void OnDownloadFailed(object? sender, Data.Models.DownloadTask task)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            StatusText = $"Download failed: {task.ErrorMessage}";
            IsDownloading = false;
            IsBusy = false;
        });
    }
}
