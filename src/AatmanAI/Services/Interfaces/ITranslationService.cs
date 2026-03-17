namespace AatmanAI.Services.Interfaces;

public interface ITranslationService
{
    Task<string> TranslateToEnglishAsync(string hindiText);
    Task<string> TranslateToHindiAsync(string englishText);
}
