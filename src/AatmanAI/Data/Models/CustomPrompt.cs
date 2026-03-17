using SQLite;

namespace AatmanAI.Data.Models;

[Table("CustomPromptFolders")]
public class CustomPromptFolder
{
    [PrimaryKey]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

[Table("CustomPrompts")]
public class CustomPrompt
{
    [PrimaryKey]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Indexed]
    public string FolderId { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;
    public string Prompt { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
