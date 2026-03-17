using System.Collections.ObjectModel;
using AatmanAI.Data.Models;
using AatmanAI.Services.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AatmanAI.ViewModels;

public partial class NetworkAuditViewModel : BaseViewModel
{
    private readonly INetworkAuditService _auditService;

    [ObservableProperty] private ObservableCollection<NetworkAuditEntry> _entries = [];
    [ObservableProperty] private int _totalRequests;
    [ObservableProperty] private string _totalDataTransferred = "0 B";
    [ObservableProperty] private bool _hasEntries;

    public NetworkAuditViewModel(INetworkAuditService auditService)
    {
        _auditService = auditService;
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        var entries = await _auditService.GetEntriesAsync(200);
        Entries = new ObservableCollection<NetworkAuditEntry>(entries);
        TotalRequests = await _auditService.GetTotalRequestCountAsync();
        HasEntries = TotalRequests > 0;

        long totalBytes = 0;
        foreach (var entry in entries)
            totalBytes += entry.DataSize;
        TotalDataTransferred = FormatBytes(totalBytes);
    }

    [RelayCommand]
    private async Task ClearAsync()
    {
        bool confirm = await Shell.Current.DisplayAlert(
            "Clear Audit Log",
            "Clear all network audit entries?",
            "Clear", "Cancel");

        if (!confirm) return;

        await _auditService.ClearEntriesAsync();
        await LoadAsync();
    }

    private static string FormatBytes(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
        < 1024L * 1024 * 1024 => $"{bytes / (1024.0 * 1024):F1} MB",
        _ => $"{bytes / (1024.0 * 1024 * 1024):F2} GB"
    };
}
