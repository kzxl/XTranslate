using System.Windows.Input;
using XTranslate.Helpers;
using XTranslate.Models;
using XTranslate.Services;

namespace XTranslate.ViewModels;

/// <summary>
/// Lightweight ViewModel for the translation popup overlay.
/// </summary>
public class PopupViewModel : ViewModelBase
{
    private readonly TranslationService _translationService;

    private string _sourceText = "";
    public string SourceText
    {
        get => _sourceText;
        set => SetProperty(ref _sourceText, value);
    }

    private string _translatedText = "";
    public string TranslatedText
    {
        get => _translatedText;
        set => SetProperty(ref _translatedText, value);
    }

    private string _detectedLanguage = "";
    public string DetectedLanguage
    {
        get => _detectedLanguage;
        set => SetProperty(ref _detectedLanguage, value);
    }

    private bool _isTranslating;
    public bool IsTranslating
    {
        get => _isTranslating;
        set => SetProperty(ref _isTranslating, value);
    }

    private bool _hasError;
    public bool HasError
    {
        get => _hasError;
        set => SetProperty(ref _hasError, value);
    }

    public IReadOnlyList<Language> Languages => LanguageDatabase.Languages;

    private Language _sourceLanguage;
    public Language SourceLanguage
    {
        get => _sourceLanguage;
        set
        {
            if (SetProperty(ref _sourceLanguage, value))
            {
                if (!_isInitializing && !string.IsNullOrWhiteSpace(SourceText))
                    _ = TranslateAsync(SourceText, TargetLanguage?.Code ?? "vi", SourceLanguage?.Code ?? "auto");
            }
        }
    }

    private Language _targetLanguage;
    public Language TargetLanguage
    {
        get => _targetLanguage;
        set
        {
            if (SetProperty(ref _targetLanguage, value))
            {
                if (!_isInitializing && !string.IsNullOrWhiteSpace(SourceText))
                    _ = TranslateAsync(SourceText, TargetLanguage?.Code ?? "vi", SourceLanguage?.Code ?? "auto");
            }
        }
    }

    private bool _isInitializing = true;
    public ICommand CopyCommand { get; }
    public ICommand SwapLanguagesCommand { get; }

    public PopupViewModel(TranslationService translationService)
    {
        _translationService = translationService;
        
        _sourceLanguage = Languages.FirstOrDefault(l => l.Code == "auto") ?? Languages[0];
        _targetLanguage = Languages.FirstOrDefault(l => l.Code == "vi") ?? Languages[0];
        _isInitializing = false;

        SwapLanguagesCommand = new RelayCommand(() =>
        {
            if (SourceLanguage?.Code == "auto") return;
            var temp = SourceLanguage;
            SourceLanguage = TargetLanguage;
            TargetLanguage = temp;
            
            // Note: Setting SourceLanguage and TargetLanguage triggers Re-Translate due to Property Setter.
        });

        CopyCommand = new RelayCommand(() =>
        {
            if (!string.IsNullOrEmpty(TranslatedText))
                System.Windows.Clipboard.SetText(TranslatedText);
        });
    }

    /// <summary>
    /// Translates the given text to the target language.
    /// </summary>
    public async Task TranslateAsync(string text, string targetLang = "vi", string sourceLang = "auto")
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            TranslatedText = "";
            return;
        }

        SourceText = text.Trim();
        IsTranslating = true;
        HasError = false;

        try
        {
            var result = await _translationService.TranslateAsync(SourceText, sourceLang, targetLang);

            if (result.IsSuccess)
            {
                TranslatedText = result.TranslatedText;
                var detected = LanguageDatabase.FindByCode(result.DetectedLanguageCode);
                DetectedLanguage = detected?.Name ?? result.DetectedLanguageCode;
            }
            else
            {
                TranslatedText = result.ErrorMessage ?? "Translation failed";
                HasError = true;
            }
        }
        catch (Exception ex)
        {
            TranslatedText = ex.Message;
            HasError = true;
        }
        finally
        {
            IsTranslating = false;
        }
    }
}
