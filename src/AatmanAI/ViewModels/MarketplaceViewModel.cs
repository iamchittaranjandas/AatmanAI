using System.Collections.ObjectModel;
using AatmanAI.Data.Models;
using AatmanAI.Services.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AatmanAI.ViewModels;

public partial class MarketplaceViewModel : BaseViewModel
{
    private readonly IModelService _modelService;
    private readonly IDownloadService _downloadService;
    private readonly IDeviceService _deviceService;

    private List<MarketplaceModelItem> _allItems = [];

    [ObservableProperty] private ObservableCollection<MarketplaceModelItem> _models = [];
    [ObservableProperty] private string _selectedTab = "all";

    // Tab active/inactive colors — drive chip style without any XAML converter
    private static readonly Color _accentColor   = Color.FromArgb("#00D9A5");
    private static readonly Color _pageBgColor   = Color.FromArgb("#0A0E14");
    private static readonly Color _cardBgColor   = Color.FromArgb("#1A2530");
    private static readonly Color _inactiveColor = Color.FromArgb("#4B5C6B");

    public Color TabAllBg           => SelectedTab == "all"         ? _accentColor : _cardBgColor;
    public Color TabAllText         => SelectedTab == "all"         ? _pageBgColor : _inactiveColor;
    public Color TabRecommendedBg   => SelectedTab == "recommended" ? _accentColor : _cardBgColor;
    public Color TabRecommendedText => SelectedTab == "recommended" ? _pageBgColor : _inactiveColor;
    public Color TabDownloadedBg    => SelectedTab == "downloaded"  ? _accentColor : _cardBgColor;
    public Color TabDownloadedText  => SelectedTab == "downloaded"  ? _pageBgColor : _inactiveColor;
    public Color TabAvailableBg     => SelectedTab == "available"   ? _accentColor : _cardBgColor;
    public Color TabAvailableText   => SelectedTab == "available"   ? _pageBgColor : _inactiveColor;

    public MarketplaceViewModel(IModelService modelService, IDownloadService downloadService, IDeviceService deviceService)
    {
        _modelService = modelService;
        _downloadService = downloadService;
        _deviceService = deviceService;

        _downloadService.DownloadProgressChanged += OnDownloadProgress;
        _downloadService.DownloadCompleted += OnDownloadCompleted;
        _downloadService.DownloadFailed += OnDownloadFailed;
    }

    partial void OnSelectedTabChanged(string value)
    {
        OnPropertyChanged(nameof(TabAllBg));       OnPropertyChanged(nameof(TabAllText));
        OnPropertyChanged(nameof(TabRecommendedBg)); OnPropertyChanged(nameof(TabRecommendedText));
        OnPropertyChanged(nameof(TabDownloadedBg));  OnPropertyChanged(nameof(TabDownloadedText));
        OnPropertyChanged(nameof(TabAvailableBg));   OnPropertyChanged(nameof(TabAvailableText));
        ApplyFilter();
    }

    [RelayCommand]
    private void SelectTab(string tab) => SelectedTab = tab;

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            var manifests = await _modelService.GetAvailableModelsAsync(forceRefresh: true);
            var downloaded = await _modelService.GetDownloadedModelsAsync();
            var active = await _modelService.GetActiveModelAsync();
            var downloadedIds = downloaded.Select(d => d.ModelId).ToHashSet();

            _allItems = manifests.Select(m => new MarketplaceModelItem(m)
            {
                IsDownloaded = downloadedIds.Contains(m.Id),
                IsActive = active?.ModelId == m.Id,
            }).ToList();

            // Restore any already-running downloads
            foreach (var task in _downloadService.ActiveTasks)
            {
                var item = _allItems.FirstOrDefault(i => i.Manifest.Id == task.ModelId);
                if (item is not null)
                {
                    item.IsDownloading = true;
                    item.DownloadProgress = task.Progress;
                }
            }

            ApplyFilter();
        }
        finally { IsBusy = false; }
    }

    private void ApplyFilter()
    {
        IEnumerable<MarketplaceModelItem> filtered = SelectedTab switch
        {
            "recommended" => _allItems.Where(i => i.Manifest.Tier == "free" && !i.IsDownloaded),
            "downloaded"  => _allItems.Where(i => i.IsDownloaded || i.IsActive),
            "available"   => _allItems.Where(i => !i.IsDownloaded && !i.IsDownloading),
            _             => _allItems,
        };
        Models = new ObservableCollection<MarketplaceModelItem>(filtered);
    }

    [RelayCommand]
    private async Task DownloadModelAsync(MarketplaceModelItem item)
    {
        var compatibility = await _deviceService.CheckModelCompatibilityAsync(item.Manifest);
        if (!compatibility.IsCompatible)
        {
            await Shell.Current.DisplayAlert("Cannot Download", compatibility.Message, "OK");
            return;
        }

        item.IsDownloading = true;
        item.DownloadProgress = 0;
        await _downloadService.StartDownloadAsync(item.Manifest);
    }

    [RelayCommand]
    private async Task UseModelAsync(MarketplaceModelItem item)
    {
        await _modelService.SetActiveModelAsync(item.Manifest.Id);
        foreach (var m in _allItems)
            m.IsActive = m.Manifest.Id == item.Manifest.Id;
        ApplyFilter();
    }

    private void OnDownloadProgress(object? sender, DownloadTask task)
    {
        var item = _allItems.FirstOrDefault(m => m.Manifest.Id == task.ModelId);
        if (item is null) return;
        MainThread.BeginInvokeOnMainThread(() =>
        {
            item.DownloadProgress = task.Progress;
            item.SpeedText = task.SpeedBytesPerSecond > 0
                ? $"{task.SpeedBytesPerSecond / (1024 * 1024):F1} MB/s"
                : string.Empty;
            item.EtaText = task.Eta is not null ? $"~{task.Eta} left" : string.Empty;
        });
    }

    private void OnDownloadCompleted(object? sender, DownloadTask task)
    {
        var item = _allItems.FirstOrDefault(m => m.Manifest.Id == task.ModelId);
        if (item is null) return;
        MainThread.BeginInvokeOnMainThread(() =>
        {
            item.IsDownloading = false;
            item.IsDownloaded = true;
            item.DownloadProgress = 1;
            item.SpeedText = string.Empty;
            item.EtaText = string.Empty;
            ApplyFilter();
        });
    }

    private void OnDownloadFailed(object? sender, DownloadTask task)
    {
        var item = _allItems.FirstOrDefault(m => m.Manifest.Id == task.ModelId);
        if (item is null) return;
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            item.IsDownloading = false;
            item.DownloadProgress = 0;
            ApplyFilter();
            await Shell.Current.DisplayAlert("Download Failed", task.ErrorMessage ?? "Unknown error", "OK");
        });
    }
}
