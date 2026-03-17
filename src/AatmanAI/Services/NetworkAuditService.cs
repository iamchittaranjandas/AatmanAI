using AatmanAI.Data.Database;
using AatmanAI.Data.Models;
using AatmanAI.Services.Interfaces;

namespace AatmanAI.Services;

public class NetworkAuditService : INetworkAuditService
{
    private readonly AppDatabase _db;

    public NetworkAuditService(AppDatabase db)
    {
        _db = db;
    }

    public async Task LogRequestAsync(string url, string requestType, long dataSize, string status)
    {
        var conn = await _db.GetConnectionAsync();
        var entry = new NetworkAuditEntry
        {
            Url = url,
            Domain = ExtractDomain(url),
            RequestType = requestType,
            DataSize = dataSize,
            Status = status,
            Timestamp = DateTime.UtcNow
        };
        await conn.InsertAsync(entry);
    }

    public async Task<List<NetworkAuditEntry>> GetEntriesAsync(int limit = 100)
    {
        var conn = await _db.GetConnectionAsync();
        return await conn.Table<NetworkAuditEntry>()
            .OrderByDescending(e => e.Timestamp)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<int> GetTotalRequestCountAsync()
    {
        var conn = await _db.GetConnectionAsync();
        return await conn.Table<NetworkAuditEntry>().CountAsync();
    }

    public async Task ClearEntriesAsync()
    {
        var conn = await _db.GetConnectionAsync();
        await conn.DeleteAllAsync<NetworkAuditEntry>();
    }

    private static string ExtractDomain(string url)
    {
        try { return new Uri(url).Host; }
        catch { return "unknown"; }
    }
}
