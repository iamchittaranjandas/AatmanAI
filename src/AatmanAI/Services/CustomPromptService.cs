using AatmanAI.Data.Database;
using AatmanAI.Data.Models;
using AatmanAI.Services.Interfaces;

namespace AatmanAI.Services;

public class CustomPromptService : ICustomPromptService
{
    private readonly AppDatabase _db;

    public CustomPromptService(AppDatabase db) => _db = db;

    public async Task<List<CustomPromptFolder>> GetFoldersAsync()
    {
        var conn = await _db.GetConnectionAsync();
        return await conn.Table<CustomPromptFolder>().OrderBy(f => f.Name).ToListAsync();
    }

    public async Task<CustomPromptFolder> CreateFolderAsync(string name)
    {
        var folder = new CustomPromptFolder { Name = name };
        var conn = await _db.GetConnectionAsync();
        await conn.InsertAsync(folder);
        return folder;
    }

    public async Task RenameFolderAsync(string folderId, string newName)
    {
        var conn = await _db.GetConnectionAsync();
        var folder = await conn.FindAsync<CustomPromptFolder>(folderId);
        if (folder is not null) { folder.Name = newName; await conn.UpdateAsync(folder); }
    }

    public async Task DeleteFolderAsync(string folderId)
    {
        var conn = await _db.GetConnectionAsync();
        await conn.ExecuteAsync("DELETE FROM CustomPrompts WHERE FolderId = ?", folderId);
        await conn.ExecuteAsync("DELETE FROM CustomPromptFolders WHERE Id = ?", folderId);
    }

    public async Task<List<CustomPrompt>> GetPromptsAsync(string folderId)
    {
        var conn = await _db.GetConnectionAsync();
        return await conn.Table<CustomPrompt>().Where(p => p.FolderId == folderId).ToListAsync();
    }

    public async Task<CustomPrompt> CreatePromptAsync(string folderId, string title, string prompt)
    {
        var p = new CustomPrompt { FolderId = folderId, Title = title, Prompt = prompt };
        var conn = await _db.GetConnectionAsync();
        await conn.InsertAsync(p);
        return p;
    }

    public async Task UpdatePromptAsync(string promptId, string title, string prompt)
    {
        var conn = await _db.GetConnectionAsync();
        var p = await conn.FindAsync<CustomPrompt>(promptId);
        if (p is not null) { p.Title = title; p.Prompt = prompt; p.UpdatedAt = DateTime.UtcNow; await conn.UpdateAsync(p); }
    }

    public async Task DeletePromptAsync(string promptId)
    {
        var conn = await _db.GetConnectionAsync();
        await conn.ExecuteAsync("DELETE FROM CustomPrompts WHERE Id = ?", promptId);
    }

    public async Task<CustomPrompt?> GetPromptAsync(string promptId)
    {
        var conn = await _db.GetConnectionAsync();
        return await conn.FindAsync<CustomPrompt>(promptId);
    }
}
