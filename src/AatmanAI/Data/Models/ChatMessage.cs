using SQLite;

namespace AatmanAI.Data.Models;

[Table("ChatMessages")]
public class ChatMessage : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
{
    [PrimaryKey]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Indexed]
    public string ConversationId { get; set; } = string.Empty;

    private string _content = string.Empty;
    [Ignore]
    public string Content
    {
        get => _content;
        set => SetProperty(ref _content, value);
    }

    // SQLite-mapped property (non-observable for DB)
    public string ContentText
    {
        get => _content;
        set => _content = value;
    }

    public string Role { get; set; } = "user"; // user, assistant, system
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int TokenCount { get; set; }
    public int GenerationTimeMs { get; set; }
    public double TokensPerSecond { get; set; }
    public bool IsRegenerated { get; set; }
    public string? ModelId { get; set; }

    // Computed properties for XAML binding
    [Ignore]
    public bool IsUser => Role == "user";
    [Ignore]
    public bool IsAssistant => Role == "assistant";
    [Ignore]
    public bool HasTokenStats => Role == "assistant" && TokensPerSecond > 0;
}
