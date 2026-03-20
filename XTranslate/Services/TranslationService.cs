using XTranslate.Models;

namespace XTranslate.Services;

/// <summary>
/// Orchestrator for translation. Uses engine registry for multi-engine support
/// and provides in-memory caching.
/// </summary>
public class TranslationService
{
    private readonly TranslationEngineRegistry _registry;
    private readonly Dictionary<string, TranslationResult> _cache = new();
    private const int MaxCacheSize = 200;

    public TranslationEngineRegistry Registry => _registry;

    public TranslationService(TranslationEngineRegistry registry)
    {
        _registry = registry;
    }

    /// <summary>
    /// Translate using the active engine from the registry.
    /// </summary>
    public Task<TranslationResult> TranslateAsync(
        string text, string sourceLang, string targetLang, CancellationToken ct = default)
    {
        return TranslateWithEngineAsync(_registry.ActiveEngine, text, sourceLang, targetLang, ct);
    }

    /// <summary>
    /// Translate using a specific engine by name.
    /// </summary>
    public Task<TranslationResult> TranslateWithEngineAsync(
        string engineName, string text, string sourceLang, string targetLang, CancellationToken ct = default)
    {
        var engine = _registry.GetEngine(engineName) ?? _registry.ActiveEngine;
        return TranslateWithEngineAsync(engine, text, sourceLang, targetLang, ct);
    }

    private async Task<TranslationResult> TranslateWithEngineAsync(
        ITranslationEngine engine, string text, string sourceLang, string targetLang, CancellationToken ct)
    {
        var cacheKey = $"{engine.Name}|{sourceLang}|{targetLang}|{text}";

        if (_cache.TryGetValue(cacheKey, out var cached))
            return cached;

        var result = await engine.TranslateAsync(text, sourceLang, targetLang, ct);

        if (result.IsSuccess)
        {
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
