using XTranslate.Models;

namespace XTranslate.Services;

/// <summary>
/// Interface for all translation engines. Allows swapping Google/Bing/etc.
/// </summary>
public interface ITranslationEngine
{
    string Name { get; }
    IReadOnlyList<Language> SupportedLanguages { get; }

    Task<TranslationResult> TranslateAsync(string text, string sourceLang, string targetLang, 
        CancellationToken ct = default);

    Task<string> DetectLanguageAsync(string text, CancellationToken ct = default);
}
