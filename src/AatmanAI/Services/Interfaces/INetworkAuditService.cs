using AatmanAI.Data.Models;

namespace AatmanAI.Services.Interfaces;

public interface INetworkAuditService
{
    Task LogRequestAsync(string url, string requestType, long dataSize, string status);
    Task<List<NetworkAuditEntry>> GetEntriesAsync(int limit = 100);
    Task<int> GetTotalRequestCountAsync();
    Task ClearEntriesAsync();
}
