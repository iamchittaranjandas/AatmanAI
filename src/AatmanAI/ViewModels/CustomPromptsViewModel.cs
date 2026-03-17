using System.Collections.ObjectModel;
using AatmanAI.Data.Models;
using AatmanAI.Services.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AatmanAI.ViewModels;

public partial class CustomPromptsViewModel : BaseViewModel
{
    private readonly ICustomPromptService _promptService;

    [ObservableProperty] private ObservableCollection<CustomPromptFolder> _folders = [];
    [ObservableProperty] private ObservableCollection<CustomPrompt> _prompts = [];
    [ObservableProperty] private CustomPromptFolder? _selectedFolder;
    [ObservableProperty] private bool _isFolderSelected;
    [ObservableProperty] private string _newFolderName = string.Empty;
    [ObservableProperty] private string _newPromptTitle = string.Empty;
    [ObservableProperty] private string _newPromptContent = string.Empty;
    [ObservableProperty] private bool _isAddingFolder;
    [ObservableProperty] private bool _isAddingPrompt;
    [ObservableProperty] private bool _isEditingPrompt;
    [ObservableProperty] private string? _editingPromptId;

    public CustomPromptsViewModel(ICustomPromptService promptService)
    {
        _promptService = promptService;
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        var folders = await _promptService.GetFoldersAsync();
        Folders = new ObservableCollection<CustomPromptFolder>(folders);

        if (SelectedFolder is not null)
            await LoadPromptsAsync(SelectedFolder.Id);
    }

    [RelayCommand]
    private async Task SelectFolderAsync(CustomPromptFolder folder)
    {
        SelectedFolder = folder;
        IsFolderSelected = true;
        await LoadPromptsAsync(folder.Id);
    }

    private async Task LoadPromptsAsync(string folderId)
    {
        var prompts = await _promptService.GetPromptsAsync(folderId);
        Prompts = new ObservableCollection<CustomPrompt>(prompts);
    }

    [RelayCommand]
    private void ShowAddFolder()
    {
        IsAddingFolder = true;
        NewFolderName = string.Empty;
    }

    [RelayCommand]
    private async Task CreateFolderAsync()
    {
        if (string.IsNullOrWhiteSpace(NewFolderName)) return;

        var folder = await _promptService.CreateFolderAsync(NewFolderName.Trim());
        Folders.Add(folder);
        IsAddingFolder = false;
        NewFolderName = string.Empty;
    }

    [RelayCommand]
    private async Task DeleteFolderAsync(CustomPromptFolder folder)
    {
        await _promptService.DeleteFolderAsync(folder.Id);
        Folders.Remove(folder);

        if (SelectedFolder?.Id == folder.Id)
        {
            SelectedFolder = null;
            IsFolderSelected = false;
            Prompts.Clear();
        }
    }

    [RelayCommand]
    private void ShowAddPrompt()
    {
        IsAddingPrompt = true;
        IsEditingPrompt = false;
        NewPromptTitle = string.Empty;
        NewPromptContent = string.Empty;
        EditingPromptId = null;
    }

    [RelayCommand]
    private void EditPrompt(CustomPrompt prompt)
    {
        IsAddingPrompt = true;
        IsEditingPrompt = true;
        EditingPromptId = prompt.Id;
        NewPromptTitle = prompt.Title;
        NewPromptContent = prompt.Prompt;
    }

    [RelayCommand]
    private async Task SavePromptAsync()
    {
        if (string.IsNullOrWhiteSpace(NewPromptTitle) || SelectedFolder is null) return;

        if (IsEditingPrompt && EditingPromptId is not null)
        {
            await _promptService.UpdatePromptAsync(EditingPromptId, NewPromptTitle.Trim(), NewPromptContent.Trim());
        }
        else
        {
            await _promptService.CreatePromptAsync(SelectedFolder.Id, NewPromptTitle.Trim(), NewPromptContent.Trim());
        }

        IsAddingPrompt = false;
        IsEditingPrompt = false;
        EditingPromptId = null;
        NewPromptTitle = string.Empty;
        NewPromptContent = string.Empty;

        await LoadPromptsAsync(SelectedFolder.Id);
    }

    [RelayCommand]
    private async Task DeletePromptAsync(CustomPrompt prompt)
    {
        await _promptService.DeletePromptAsync(prompt.Id);
        Prompts.Remove(prompt);
    }

    [RelayCommand]
    private void CancelAdd()
    {
        IsAddingFolder = false;
        IsAddingPrompt = false;
        IsEditingPrompt = false;
        EditingPromptId = null;
    }

    [RelayCommand]
    private void GoBackToFolders()
    {
        SelectedFolder = null;
        IsFolderSelected = false;
        Prompts.Clear();
    }
}
