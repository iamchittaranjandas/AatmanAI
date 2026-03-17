using SQLite;

namespace AatmanAI.Data.Models;

[Table("NetworkAuditEntries")]
public class NetworkAuditEntry
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public string Url { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty;
    public string RequestType { get; set; } = string.Empty; // download, manifest, other
    public long DataSize { get; set; }
    public string Status { get; set; } = string.Empty; // success, failed
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
