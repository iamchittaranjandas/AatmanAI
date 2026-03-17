using System.Collections.ObjectModel;
using AatmanAI.Data.Models;
using AatmanAI.Services.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AatmanAI.ViewModels;

public partial class VaultViewModel : BaseViewModel
{
    private readonly IVaultService _vaultService;

    [ObservableProperty] private int _capacityPercent;
    [ObservableProperty] private string _usedStorage = "0 MB";
    [ObservableProperty] private string _totalStorage = "1 GB";
    [ObservableProperty] private int _totalDocuments;
    [ObservableProperty] private int _totalChunks;
    [ObservableProperty] private bool _isProcessing;
    [ObservableProperty] private string? _processingDocumentId;
    [ObservableProperty] private double _processingProgress;
    [ObservableProperty] private bool _hasDocuments;

    public ObservableCollection<VaultDocument> Documents { get; } = new();

    public VaultViewModel(IVaultService vaultService)
    {
        _vaultService = vaultService;
        _vaultService.StateChanged += OnServiceStateChanged;
    }

    private void OnServiceStateChanged()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            IsProcessing = _vaultService.IsProcessing;
            ProcessingDocumentId = _vaultService.ProcessingDocumentId;
            ProcessingProgress = _vaultService.ProcessingProgress;
            TotalDocuments = _vaultService.TotalDocuments;
            TotalChunks = _vaultService.TotalChunks;
            UpdateStorageDisplay();
        });
    }

    private void UpdateStorageDisplay()
    {
        var usedMB = _vaultService.StorageSizeBytes / (1024.0 * 1024.0);
        const double totalMB = 1024.0; // 1 GB limit
        CapacityPercent = totalMB > 0 ? Math.Clamp((int)(usedMB / totalMB * 100), 0, 100) : 0;
        UsedStorage = _vaultService.FormatFileSize(_vaultService.StorageSizeBytes);
        TotalStorage = "1 GB";
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        await _vaultService.InitializeAsync();
        var docs = await _vaultService.GetAllDocumentsAsync();
        Documents.Clear();
        foreach (var doc in docs)
            Documents.Add(doc);
        HasDocuments = Documents.Count > 0;
        UpdateStorageDisplay();
    }

    [RelayCommand]
    private async Task BrowseFilesAsync()
    {
        try
        {
            var result = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Select a document",
                FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    { DevicePlatform.Android, new[] { "application/pdf", "text/plain", "text/markdown",
                        "application/vnd.openxmlformats-officedocument.wordprocessingml.document" } },
                    { DevicePlatform.iOS, new[] { "public.pdf", "public.plain-text", "public.text",
                        "org.openxmlformats.wordprocessingml.document" } },
                })
            });

            if (result == null) return;

            await _vaultService.AddDocumentAsync(result.FullPath);

            // Refresh list
            await LoadAsync();

            await Shell.Current.DisplayAlert("Success", "Document added to vault", "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Error", $"Failed to add document: {ex.Message}", "OK");
        }
    }

    [RelayCommand]
    private async Task DeleteDocumentAsync(VaultDocument? document)
    {
        if (document == null) return;

        bool confirm = await Shell.Current.DisplayAlert(
            "Delete Document",
            $"Are you sure you want to delete \"{document.Name}\"?",
            "Delete", "Cancel");

        if (!confirm) return;

        await _vaultService.DeleteDocumentAsync(document.Id);
        Documents.Remove(document);
        HasDocuments = Documents.Count > 0;
        UpdateStorageDisplay();
    }

    public string FormatFileSize(int bytes) => _vaultService.FormatFileSize(bytes);

    public bool IsDocumentProcessing(string docId) =>
        IsProcessing && ProcessingDocumentId == docId;
}
