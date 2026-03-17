using AatmanAI.Services.Interfaces;

namespace AatmanAI.Services;

/// <summary>Phase 2 stub - Voice service for STT/TTS</summary>
public class VoiceService : IVoiceService
{
    public bool IsListening => false;
    public bool IsSpeaking => false;

    public Task<string?> ListenAsync(CancellationToken ct = default) => Task.FromResult<string?>(null);
    public Task SpeakAsync(string text, CancellationToken ct = default) => Task.CompletedTask;
    public Task StopListeningAsync() => Task.CompletedTask;
    public Task StopSpeakingAsync() => Task.CompletedTask;
}
