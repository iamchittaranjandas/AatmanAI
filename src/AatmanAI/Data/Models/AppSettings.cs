using SQLite;

namespace AatmanAI.Data.Models;

[Table("AppSettings")]
public class AppSettingEntry
{
    [PrimaryKey]
    public string Key { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;
}
