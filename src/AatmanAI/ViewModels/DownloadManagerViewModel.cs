using System.Collections.ObjectModel;
using AatmanAI.Data.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AatmanAI.ViewModels;

public partial class DownloadManagerViewModel : BaseViewModel
{
    [ObservableProperty] private string _availableSpace = "128 GB";
    [ObservableProperty] private string _requiredSpace = "2.4 GB";
    [ObservableProperty] private int _pendingCount = 0;
    [ObservableProperty] private bool _hasActiveDownloads;
    [ObservableProperty] private bool _hasInstalledModels;
    [ObservableProperty] private bool _isLoadingModels;

    public ObservableCollection<DownloadTask> ActiveDownloads { get; } = new();
    public ObservableCollection<DownloadedModel> InstalledModels { get; } = new();

    [RelayCommand]
    private async Task CheckForUpdatesAsync()
    {
        if (!SetBusyAndCheck()) return;
        try
        {
            await Task.Delay(2000);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void TogglePause(DownloadTask? task)
    {
        if (task == null) return;
        if (task.State == Core.Enums.DownloadState.Paused)
            task.State = Core.Enums.DownloadState.Downloading;
        else if (task.State == Core.Enums.DownloadState.Downloading)
            task.State = Core.Enums.DownloadState.Paused;
    }

    [RelayCommand]
    private async Task DeleteModelAsync(DownloadedModel? model)
    {
        if (model == null) return;
        InstalledModels.Remove(model);
        HasInstalledModels = InstalledModels.Count > 0;
        await Task.CompletedTask;
    }
}
