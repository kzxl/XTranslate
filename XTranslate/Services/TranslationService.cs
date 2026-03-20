using XTranslate.Models;

namespace XTranslate.Services;

/// <summary>
/// Orchestrator for translation. Manages engine selection and basic caching.
/// </summary>
public class TranslationService
{
    private readonly ITranslationEngine _engine;
    private readonly Dictionary<string, TranslationResult> _cache = new();
    private const int MaxCacheSize = 100;

    public ITranslationEngine CurrentEngine => _engine;

    public TranslationService(ITranslationEngine engine)
    {
        _engine = engine;
    }

    public async Task<TranslationResult> TranslateAsync(
        string text, string sourceLang, string targetLang, CancellationToken ct = default)
    {
        var cacheKey = $"{sourceLang}|{targetLang}|{text}";

        if (_cache.TryGetValue(cacheKey, out var cached))
            return cached;

        var result = await _engine.TranslateAsync(text, sourceLang, targetLang, ct);

        if (result.IsSuccess)
        {
            // Simple LRU: remove oldest if full
            if (_cache.Count >= MaxCacheSize)
            {
                var oldest = _cache.Keys.First();
                _cache.Remove(oldest);
            }
            _cache[cacheKey] = result;
        }

        return result;
    }

    public void ClearCache() => _cache.Clear();
}
