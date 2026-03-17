using System.Collections.ObjectModel;
using AatmanAI.Data.Database;
using AatmanAI.Data.Models;
using AatmanAI.Services.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AatmanAI.ViewModels;

public partial class StorageViewModel : BaseViewModel
{
    private readonly IModelService _modelService;
    private readonly IDeviceService _deviceService;
    private readonly AppDatabase _db;

    [ObservableProperty] private ObservableCollection<DownloadedModel> _downloadedModels = [];
    [ObservableProperty] private string _totalModelsSize = "0 MB";
    [ObservableProperty] private string _freeStorage = "Unknown";
    [ObservableProperty] private string _chatHistorySize = "0 MB";
    [ObservableProperty] private int _conversationCount;
    [ObservableProperty] private int _messageCount;

    public StorageViewModel(IModelService modelService, IDeviceService deviceService, AppDatabase db)
    {
        _modelService = modelService;
        _deviceService = deviceService;
        _db = db;
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        var models = await _modelService.GetDownloadedModelsAsync();
        DownloadedModels = new ObservableCollection<DownloadedModel>(models);

        long totalBytes = 0;
        foreach (var model in models)
        {
            if (File.Exists(model.LocalPath))
                totalBytes += new FileInfo(model.LocalPath).Length;
            else
                totalBytes += model.FileSizeBytes;
        }
        TotalModelsSize = FormatBytes(totalBytes);

        var benchmark = await _deviceService.RunBenchmarkAsync();
        FreeStorage = $"{benchmark.FreeStorageMb:N0} MB";

        var conn = await _db.GetConnectionAsync();
        ConversationCount = await conn.Table<Conversation>().CountAsync();
        MessageCount = await conn.Table<ChatMessage>().CountAsync();

        var dbPath = Path.Combine(FileSystem.AppDataDirectory, "aatmanai.db3");
        if (File.Exists(dbPath))
            ChatHistorySize = FormatBytes(new FileInfo(dbPath).Length);
    }

    [RelayCommand]
    private async Task DeleteModelAsync(DownloadedModel model)
    {
        bool confirm = await Shell.Current.DisplayAlert(
            "Delete Model",
            $"Delete {model.Name}? This will free {model.FileSizeDisplay}.",
            "Delete", "Cancel");

        if (!confirm) return;

        await _modelService.DeleteModelAsync(model.ModelId);
        DownloadedModels.Remove(model);
        await LoadAsync();
    }

    [RelayCommand]
    private async Task ClearChatHistoryAsync()
    {
        bool confirm = await Shell.Current.DisplayAlert(
            "Clear Chat History",
            "Delete all conversations and messages? This cannot be undone.",
            "Clear All", "Cancel");

        if (!confirm) return;

        var conn = await _db.GetConnectionAsync();
        await conn.DeleteAllAsync<ChatMessage>();
        await conn.DeleteAllAsync<Conversation>();
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
