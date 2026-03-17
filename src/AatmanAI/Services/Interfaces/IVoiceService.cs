namespace AatmanAI.Services.Interfaces;

public interface IVoiceService
{
    bool IsListening { get; }
    bool IsSpeaking { get; }

    Task<string?> ListenAsync(CancellationToken ct = default);
    Task SpeakAsync(string text, CancellationToken ct = default);
    Task StopListeningAsync();
    Task StopSpeakingAsync();
}
