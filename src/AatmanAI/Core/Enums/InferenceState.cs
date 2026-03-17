namespace AatmanAI.Core.Enums;

public enum InferenceState
{
    Idle,
    Loading,
    Loaded,
    Generating,
    Error
}

public static class InferenceStateExtensions
{
    public static bool IsReady(this InferenceState state) => state == InferenceState.Loaded;
    public static bool IsBusy(this InferenceState state) => state is InferenceState.Loading or InferenceState.Generating;
    public static bool CanGenerate(this InferenceState state) => state == InferenceState.Loaded;
    public static bool HasError(this InferenceState state) => state == InferenceState.Error;
}
