namespace AatmanAI.Core.Enums;

public enum PowerMode
{
    Efficient,
    Performance,
    Auto
}

public static class PowerModeExtensions
{
    public static string DisplayName(this PowerMode mode) => mode switch
    {
        PowerMode.Efficient => "Efficient",
        PowerMode.Performance => "Performance",
        PowerMode.Auto => "Auto",
        _ => "Unknown"
    };

    public static string Description(this PowerMode mode) => mode switch
    {
        PowerMode.Efficient => "Ghost thinks slower but saves battery. Great for long chats.",
        PowerMode.Performance => "Maximum speed. Best when plugged in or for quick questions.",
        PowerMode.Auto => "Automatically adjusts based on battery and temperature.",
        _ => ""
    };

    public static int ThreadCount(this PowerMode mode) => mode switch
    {
        PowerMode.Efficient => 2,
        PowerMode.Performance => 8,
        PowerMode.Auto => 6,
        _ => 4
    };
}
