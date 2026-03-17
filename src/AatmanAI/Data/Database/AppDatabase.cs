using AatmanAI.Data.Models;
using SQLite;

namespace AatmanAI.Data.Database;

public class AppDatabase
{
    private SQLiteAsyncConnection? _db;
    private readonly string _dbPath;

    public AppDatabase()
    {
        _dbPath = Path.Combine(FileSystem.AppDataDirectory, "aatmanai.db3");
    }

    public async Task<SQLiteAsyncConnection> GetConnectionAsync()
    {
        if (_db is not null) return _db;

        _db = new SQLiteAsyncConnection(_dbPath);
        await _db.CreateTableAsync<Conversation>();
        await _db.CreateTableAsync<ChatMessage>();
        await _db.CreateTableAsync<DownloadedModel>();
        await _db.CreateTableAsync<CustomPromptFolder>();
        await _db.CreateTableAsync<CustomPrompt>();
        await _db.CreateTableAsync<NetworkAuditEntry>();
        await _db.CreateTableAsync<VaultDocument>();
        await _db.CreateTableAsync<VaultChunk>();
        await _db.CreateTableAsync<AppSettingEntry>();
        return _db;
    }

    // Settings helpers
    public async Task<string?> GetSettingAsync(string key)
    {
        var db = await GetConnectionAsync();
        var entry = await db.FindAsync<AppSettingEntry>(key);
        return entry?.Value;
    }

    public async Task SetSettingAsync(string key, string value)
    {
        var db = await GetConnectionAsync();
        await db.InsertOrReplaceAsync(new AppSettingEntry { Key = key, Value = value });
    }

    public async Task<T> GetSettingAsync<T>(string key, T defaultValue)
    {
        var raw = await GetSettingAsync(key);
        if (raw is null) return defaultValue;

        try
        {
            return (T)Convert.ChangeType(raw, typeof(T));
        }
        catch
        {
            return defaultValue;
        }
    }
}
