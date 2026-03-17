using System.Collections.ObjectModel;
using AatmanAI.Data.Models;
using AatmanAI.Services.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AatmanAI.ViewModels;

public partial class HomeViewModel : BaseViewModel
{
    private readonly IChatService _chatService;
    private readonly IModelService _modelService;
    private readonly IInferenceService _inferenceService;

    [ObservableProperty] private ObservableCollection<Conversation> _conversations = [];
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private string? _activeModelName;
    [ObservableProperty] private bool _hasNoConversations;

    public HomeViewModel(IChatService chatService, IModelService modelService, IInferenceService inferenceService)
    {
        _chatService = chatService;
        _modelService = modelService;
        _inferenceService = inferenceService;
    }

    private static List<Conversation> GetShowcaseThreads() =>
    [
        new()
        {
            Title = "Project Alpha Strategy",
            LastMessagePreview = "Analyzing local PDFs for insights on ...",
            Icon = "\U0001F4C1", // 📁
            IsPinned = true,
            IsRecent = true,
            BadgeText = "\U0001F4CE 3 Docs",
            UpdatedAt = DateTime.UtcNow.AddMinutes(-2)
        },
        new()
        {
            Title = "Personal Reflection Journal",
            LastMessagePreview = "Today I felt more aligned with my co...",
            Icon = "\u2728", // ✨
            UpdatedAt = DateTime.UtcNow.AddHours(-1)
        },
        new()
        {
            Title = "Financial Planning 2024",
            LastMessagePreview = "Securely processed bank statement...",
            Icon = "\U0001F4B2", // 💲
            BadgeText = "\U0001F512 Vault Access",
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        },
        new()
        {
            Title = "Dream Analysis",
            LastMessagePreview = "The recurring theme of flying sugge...",
            Icon = "\U0001F319", // 🌙
            UpdatedAt = DateTime.UtcNow.AddDays(-2)
        },
        new()
        {
            Title = "Ancient Philosophy Study",
            LastMessagePreview = "Comparing Stoicism with Easter...",
            Icon = "\U0001F553", // 🕓
            IsFaded = true,
            UpdatedAt = new DateTime(2024, 10, 24, 0, 0, 0, DateTimeKind.Utc)
        }
    ];

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsBusy = true;
        try
        {
            var dbConversations = await _chatService.GetConversationsAsync();

            // Enrich DB conversations with icons based on age
            foreach (var c in dbConversations)
            {
                c.Icon ??= "\u2728"; // ✨ default
                var age = DateTime.UtcNow - c.UpdatedAt;
                c.IsRecent = age.TotalMinutes < 30;
            }

            // Combine: real conversations first, then showcase threads to fill the view
            var combined = new List<Conversation>(dbConversations);
            if (combined.Count < 5)
            {
                combined.AddRange(GetShowcaseThreads().Skip(combined.Count));
            }

            Conversations = new ObservableCollection<Conversation>(combined);
            HasNoConversations = Conversations.Count == 0;

            var activeModel = await _modelService.GetActiveModelAsync();
            ActiveModelName = activeModel?.Name ?? "No model loaded";

            if (activeModel is not null && _inferenceService.LoadedModelId != activeModel.ModelId)
            {
                await _inferenceService.LoadModelAsync(activeModel.ModelId, activeModel.LocalPath);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task NewChatAsync()
    {
        var conversation = await _chatService.CreateConversationAsync();
        await Shell.Current.GoToAsync($"chat?conversationId={conversation.Id}");
    }

    [RelayCommand]
    private async Task OpenChatAsync(Conversation conversation)
    {
        await Shell.Current.GoToAsync($"chat?conversationId={conversation.Id}");
    }

    [RelayCommand]
    private async Task DeleteConversationAsync(Conversation conversation)
    {
        await _chatService.DeleteConversationAsync(conversation.Id);
        Conversations.Remove(conversation);
        HasNoConversations = Conversations.Count == 0;
    }

    [RelayCommand]
    private async Task OpenSettingsAsync()
    {
        try
        {
            // Switch to settings tab within the main TabBar
            if (Shell.Current?.CurrentItem is TabBar tabBar)
            {
                var settingsTab = tabBar.Items.FirstOrDefault(i => i.Route == "settings");
                if (settingsTab != null)
                {
                    Shell.Current.CurrentItem = settingsTab;
                    return;
                }
            }
            await Shell.Current.GoToAsync("///main/settings");
        }
        catch
        {
            // Fallback: navigate as registered route
            await Shell.Current.GoToAsync("settings");
        }
    }

    [RelayCommand]
    private async Task OpenMarketplaceAsync()
    {
        await Shell.Current.GoToAsync("marketplace");
    }

    [RelayCommand]
    private async Task OpenVaultAsync()
    {
        await Shell.Current.GoToAsync("vault");
    }

    partial void OnSearchTextChanged(string value)
    {
        // Filter conversations (simple client-side filter)
        _ = FilterConversationsAsync(value);
    }

    private async Task FilterConversationsAsync(string query)
    {
        var all = await _chatService.GetConversationsAsync();
        if (string.IsNullOrWhiteSpace(query))
        {
            Conversations = new ObservableCollection<Conversation>(all);
        }
        else
        {
            var filtered = all.Where(c => c.Title.Contains(query, StringComparison.OrdinalIgnoreCase));
            Conversations = new ObservableCollection<Conversation>(filtered);
        }
        HasNoConversations = Conversations.Count == 0;
    }
}
