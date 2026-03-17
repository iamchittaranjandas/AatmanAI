using System.Collections.ObjectModel;
using AatmanAI.Core.Enums;
using AatmanAI.Data.Models;
using AatmanAI.Services.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Runtime.CompilerServices;

namespace AatmanAI.ViewModels;

[QueryProperty(nameof(ConversationId), "conversationId")]
public partial class ChatViewModel : BaseViewModel
{
    private readonly IChatService _chatService;
    private readonly IInferenceService _inferenceService;
    private readonly IModelService _modelService;
    private readonly ICustomPromptService _promptService;

    private string? _selectedPromptId;

    [ObservableProperty] private string? _conversationId;
    [ObservableProperty] private string _conversationTitle = "New Chat";
    [ObservableProperty] private ObservableCollection<ChatMessage> _messages = [];
    [ObservableProperty] private string _inputText = string.Empty;
    [ObservableProperty] private bool _isGenerating;
    [ObservableProperty] private double _tokensPerSecond;
    [ObservableProperty] private string _modelStatus = "Ready";
    [ObservableProperty] private string _selectedPromptName = "Default";
    [ObservableProperty] private string _selectedLanguage = "English";
    [ObservableProperty] private string _selectedModelName = "No model";

    public string TodayDateText => DateTime.Now.ToString("'TODAY,' h:mm tt").ToUpper();

    private CancellationTokenSource? _cts;

    public ChatViewModel(IChatService chatService, IInferenceService inferenceService, IModelService modelService, ICustomPromptService promptService)
    {
        _chatService = chatService;
        _inferenceService = inferenceService;
        _modelService = modelService;
        _promptService = promptService;

        _inferenceService.StateChanged += (_, state) =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                ModelStatus = state switch
                {
                    InferenceState.Idle => "No model",
                    InferenceState.Loading => "Loading...",
                    InferenceState.Loaded => SelectedModelName,
                    InferenceState.Generating => "Generating...",
                    InferenceState.Error => "Error",
                    _ => "Unknown"
                };
            });
        };

        _ = LoadActiveModelNameAsync();
    }

    private async Task LoadActiveModelNameAsync()
    {
        var active = await _modelService.GetActiveModelAsync();
        if (active is not null)
        {
            SelectedModelName = active.Parameters;
            if (_inferenceService.State == InferenceState.Loaded)
                ModelStatus = SelectedModelName;
        }
    }

    partial void OnConversationIdChanged(string? value)
    {
        if (value is not null) _ = LoadConversationAsync(value);
    }

    private async Task LoadConversationAsync(string conversationId)
    {
        var conversation = await _chatService.GetConversationAsync(conversationId);
        if (conversation is not null)
            ConversationTitle = conversation.Title;

        var messages = await _chatService.GetMessagesAsync(conversationId);
        Messages = new ObservableCollection<ChatMessage>(messages.Where(m => m.Role is "user" or "assistant"));
    }

    [RelayCommand]
    private async Task SendAsync()
    {
        if (string.IsNullOrWhiteSpace(InputText) || IsGenerating || ConversationId is null) return;

        var userText = InputText.Trim();
        InputText = string.Empty;
        IsGenerating = true;
        _cts = new CancellationTokenSource();

        // Add user message to UI
        var userMsg = new ChatMessage
        {
            ConversationId = ConversationId,
            ContentText = userText,
            Role = "user"
        };
        Messages.Add(userMsg);

        // Add placeholder assistant message for streaming
        var assistantMsg = new ChatMessage
        {
            ConversationId = ConversationId,
            ContentText = "",
            Role = "assistant"
        };
        Messages.Add(assistantMsg);

        try
        {
            // Ensure model is loaded before generating
            if (_inferenceService.State != InferenceState.Loaded && _inferenceService.State != InferenceState.Generating)
            {
                var activeModel = await _modelService.GetActiveModelAsync();
                if (activeModel is not null)
                {
                    await _inferenceService.LoadModelAsync(activeModel.ModelId, activeModel.LocalPath, _cts.Token);
                }
                if (_inferenceService.State != InferenceState.Loaded)
                {
                    assistantMsg.Content = "[Error: Could not load AI model. Please download a model first.]";
                    return;
                }
            }

            await foreach (var token in _chatService.SendMessageAsync(
                ConversationId, userText,
                customPromptId: _selectedPromptId,
                language: SelectedLanguage,
                ct: _cts.Token))
            {
                assistantMsg.Content += token;
                TokensPerSecond = _inferenceService.TokensPerSecond;
            }

            // Update title from first message
            if (Messages.Count(m => m.Role == "user") == 1)
                ConversationTitle = userText.Length > 30 ? userText[..30] + "..." : userText;
        }
        catch (OperationCanceledException)
        {
            // User stopped generation
        }
        catch (Exception ex)
        {
            assistantMsg.Content += $"\n\n[Error: {ex.Message}]";
        }
        finally
        {
            IsGenerating = false;
            _cts = null;
        }
    }

    [RelayCommand]
    private async Task StopAsync()
    {
        _cts?.Cancel();
        await _chatService.StopGenerationAsync();
        IsGenerating = false;
    }

    [RelayCommand]
    private async Task SelectPromptAsync()
    {
        var folders = await _promptService.GetFoldersAsync();
        var options = new List<string> { "None (Default)" };
        var allPrompts = new Dictionary<string, CustomPrompt>();

        foreach (var folder in folders)
        {
            var prompts = await _promptService.GetPromptsAsync(folder.Id);
            foreach (var p in prompts)
            {
                options.Add($"{folder.Name} › {p.Title}");
                allPrompts[$"{folder.Name} › {p.Title}"] = p;
            }
        }

        if (options.Count == 1)
        {
            await Shell.Current.DisplayAlert("No Custom Prompts", "Add prompts in Settings to use them here.", "OK");
            return;
        }

        var choice = await Shell.Current.DisplayActionSheet("Select Prompt Style", "Cancel", null, [.. options]);
        if (choice is null || choice == "Cancel") return;

        if (choice == "None (Default)")
        {
            _selectedPromptId = null;
            SelectedPromptName = "Default";
        }
        else if (allPrompts.TryGetValue(choice, out var prompt))
        {
            _selectedPromptId = prompt.Id;
            SelectedPromptName = prompt.Title.Length > 14 ? prompt.Title[..14] + "..." : prompt.Title;
        }
    }

    [RelayCommand]
    private async Task SelectLanguageAsync()
    {
        string[] languages = ["English", "Hindi", "Spanish", "French", "German", "Japanese", "Chinese", "Arabic", "Portuguese", "Russian"];
        var choice = await Shell.Current.DisplayActionSheet("Response Language", "Cancel", null, languages);
        if (choice is null || choice == "Cancel") return;
        SelectedLanguage = choice;
    }

    [RelayCommand]
    private async Task SelectModelAsync()
    {
        var models = await _modelService.GetDownloadedModelsAsync();
        if (models.Count == 0)
        {
            await Shell.Current.DisplayAlert("No Models", "No models downloaded yet. Visit the Marketplace to download one.", "OK");
            return;
        }

        var names = models.Select(m => m.Parameters).ToArray();
        var choice = await Shell.Current.DisplayActionSheet("Select Model", "Cancel", null, names);
        if (choice is null || choice == "Cancel") return;

        var selected = models.First(m => m.Parameters == choice);
        await _modelService.SetActiveModelAsync(selected.ModelId);
        await _inferenceService.UnloadModelAsync();
        await _inferenceService.LoadModelAsync(selected.ModelId, selected.LocalPath);
        SelectedModelName = selected.Parameters;
        ModelStatus = selected.Parameters;
    }

    [RelayCommand]
    private async Task RegenerateAsync()
    {
        if (IsGenerating || ConversationId is null) return;

        // Remove last assistant message from UI
        var lastAssistant = Messages.LastOrDefault(m => m.Role == "assistant");
        if (lastAssistant is not null)
            Messages.Remove(lastAssistant);

        IsGenerating = true;
        _cts = new CancellationTokenSource();

        var assistantMsg = new ChatMessage
        {
            ConversationId = ConversationId,
            ContentText = "",
            Role = "assistant"
        };
        Messages.Add(assistantMsg);

        try
        {
            await foreach (var token in _chatService.RegenerateLastMessageAsync(
                ConversationId, _cts.Token))
            {
                assistantMsg.Content += token;
            }
        }
        catch (OperationCanceledException) { }
        finally
        {
            IsGenerating = false;
            _cts = null;
        }
    }
}
