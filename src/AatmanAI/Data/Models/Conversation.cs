using SQLite;

namespace AatmanAI.Data.Models;

[Table("Conversations")]
public class Conversation
{
    [PrimaryKey]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public string Title { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string? ModelId { get; set; }
    public string? CustomPromptFolderId { get; set; }
    public string? CustomPromptId { get; set; }
    public int MessageCount { get; set; }
    public bool IsPinned { get; set; }
    public bool IsArchived { get; set; }
    public string? LastMessagePreview { get; set; }
    public string? Icon { get; set; }
    [Ignore] public string? BadgeText { get; set; }
    [Ignore] public bool IsRecent { get; set; }
    [Ignore] public bool IsFaded { get; set; }
}
