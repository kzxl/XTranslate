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

    private string _lastDetectedLangCode = "";

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
            Language? newSource = TargetLanguage;
            Language? newTarget = SourceLanguage;

            if (SourceLanguage?.Code == "auto")
            {
                if (string.IsNullOrEmpty(_lastDetectedLangCode) || _lastDetectedLangCode == "auto")
                    return;

                newTarget = Languages.FirstOrDefault(l => l.Code == _lastDetectedLangCode);
                if (newTarget == null) return;
            }

            _isInitializing = true;
            SourceLanguage = newSource ?? Languages[0];
            _isInitializing = false;
            
            TargetLanguage = newTarget;
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

        bool autoSwitched = false;
    retry:
        try
        {
            var result = await _translationService.TranslateAsync(SourceText, sourceLang, targetLang);

            if (result.IsSuccess)
            {
                // Logic thông minh: NẾU dịch ra mà Ngôn ngữ Vừa Detect trùng béng luôn với Ngôn ngữ Đích (ví dụ text TV -> Dịch sang TV).
                // TA sẽ tự động tráo ngôn ngữ đích sang Anh (hoặc Việt nếu là TA) và dịch lại thêm 1 phát nữa.
                if (sourceLang == "auto" && !autoSwitched && !string.IsNullOrEmpty(result.DetectedLanguageCode))
                {
                    string detected = result.DetectedLanguageCode.Split('-')[0].ToLower();
                    string currentTarget = targetLang.Split('-')[0].ToLower();

                    if (detected == currentTarget)
                    {
                        string newTargetLang = (detected == "vi") ? "en" : "vi";
                        targetLang = newTargetLang;
                        autoSwitched = true;

                        // Cập nhật âm thầm lên ComboBox UI mà không làm API bắn lần 3
                        _isInitializing = true;
                        TargetLanguage = Languages.FirstOrDefault(l => l.Code == newTargetLang) ?? TargetLanguage;
                        _isInitializing = false;

                        goto retry;
                    }
                }

                TranslatedText = result.TranslatedText;
                _lastDetectedLangCode = result.DetectedLanguageCode;
                var detectedObj = LanguageDatabase.FindByCode(result.DetectedLanguageCode);
                DetectedLanguage = detectedObj?.Name ?? result.DetectedLanguageCode;
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
