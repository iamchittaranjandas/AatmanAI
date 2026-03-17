using AatmanAI.Core.Enums;

namespace AatmanAI.Services.Interfaces;

public class InferenceParams
{
    public double Temperature { get; set; } = 0.8;
    public double TopP { get; set; } = 0.95;
    public int MaxTokens { get; set; } = 256;
    public int ContextLength { get; set; } = 1024;
}

public interface IInferenceService
{
    InferenceState State { get; }
    string? LoadedModelId { get; }
    double TokensPerSecond { get; }
    int TotalTokensGenerated { get; }

    Task LoadModelAsync(string modelId, string modelPath, CancellationToken ct = default);
    Task UnloadModelAsync();
    IAsyncEnumerable<string> GenerateAsync(
        string prompt,
        string systemPrompt,
        List<(string role, string content)>? history,
        InferenceParams? parameters,
        CancellationToken ct = default);
    Task StopGenerationAsync();
    void SetPowerMode(PowerMode mode);
    void UpdateSamplingParams(double? temperature, double? topP);

    event EventHandler<InferenceState>? StateChanged;
}
