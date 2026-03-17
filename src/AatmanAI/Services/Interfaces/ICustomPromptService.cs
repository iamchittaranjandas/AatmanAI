using AatmanAI.Data.Models;

namespace AatmanAI.Services.Interfaces;

public interface ICustomPromptService
{
    Task<List<CustomPromptFolder>> GetFoldersAsync();
    Task<CustomPromptFolder> CreateFolderAsync(string name);
    Task RenameFolderAsync(string folderId, string newName);
    Task DeleteFolderAsync(string folderId);

    Task<List<CustomPrompt>> GetPromptsAsync(string folderId);
    Task<CustomPrompt> CreatePromptAsync(string folderId, string title, string prompt);
    Task UpdatePromptAsync(string promptId, string title, string prompt);
    Task DeletePromptAsync(string promptId);
    Task<CustomPrompt?> GetPromptAsync(string promptId);
}
