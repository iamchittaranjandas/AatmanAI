using AatmanAI.Services.Interfaces;

namespace AatmanAI.Services;

/// <summary>Phase 2 stub - pass-through translation</summary>
public class TranslationService : ITranslationService
{
    public Task<string> TranslateToEnglishAsync(string hindiText) => Task.FromResult(hindiText);
    public Task<string> TranslateToHindiAsync(string englishText) => Task.FromResult(englishText);
}
