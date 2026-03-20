using System.Windows.Input;
using XTranslate.Helpers;
using XTranslate.Models;
using XTranslate.Services;

namespace XTranslate.ViewModels;

/// <summary>
/// ViewModel for the main translation window.
/// </summary>
public class MainViewModel : ViewModelBase
{
    private readonly TranslationService _translationService;

    // --- Bindable Properties ---

    private string _sourceText = "";
    public string SourceText
    {
        get => _sourceText;
        set
        {
            if (SetProperty(ref _sourceText, value))
                OnPropertyChanged(nameof(CharacterCount));
        }
    }

    private string _translatedText = "";
    public string TranslatedText
    {
        get => _translatedText;
        set => SetProperty(ref _translatedText, value);
    }

    private Language _sourceLanguage;
    public Language SourceLanguage
    {
        get => _sourceLanguage;
        set => SetProperty(ref _sourceLanguage, value);
    }

    private Language _targetLanguage;
    public Language TargetLanguage
    {
        get => _targetLanguage;
        set => SetProperty(ref _targetLanguage, value);
    }

    private bool _isTranslating;
    public bool IsTranslating
    {
        get => _isTranslating;
        set => SetProperty(ref _isTranslating, value);
    }

    private string _statusText = "Ready";
    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    private string _lastDetectedLangCode = "";

    public int CharacterCount => SourceText?.Length ?? 0;

    public IReadOnlyList<Language> SourceLanguages => LanguageDatabase.SourceLanguages;
    public IReadOnlyList<Language> TargetLanguages => LanguageDatabase.TargetLanguages;

    // --- Commands ---

    public ICommand TranslateCommand { get; }
    public ICommand SwapLanguagesCommand { get; }
    public ICommand CopyResultCommand { get; }
    public ICommand ClearCommand { get; }

    public MainViewModel(TranslationService translationService)
    {
        _translationService = translationService;

        _sourceLanguage = LanguageDatabase.FindByCode("auto") ?? Language.Auto;
        _targetLanguage = LanguageDatabase.FindByCode("vi")
                          ?? LanguageDatabase.TargetLanguages[0];

        TranslateCommand = new AsyncRelayCommand(TranslateAsync, () => !IsTranslating && !string.IsNullOrWhiteSpace(SourceText));
        SwapLanguagesCommand = new RelayCommand(SwapLanguages, () => SourceLanguage.Code != "auto");
        CopyResultCommand = new RelayCommand(CopyResult, () => !string.IsNullOrEmpty(TranslatedText));
        ClearCommand = new RelayCommand(Clear);
    }

    public async Task TranslateAsync()
    {
        if (string.IsNullOrWhiteSpace(SourceText)) return;

        IsTranslating = true;
        StatusText = "Đang dịch...";

        try
        {
            var result = await _translationService.TranslateAsync(
                SourceText, SourceLanguage.Code, TargetLanguage.Code);

            if (result.IsSuccess)
            {
                TranslatedText = result.TranslatedText;

                // Update detected language display
                if (SourceLanguage.Code == "auto" && !string.IsNullOrEmpty(result.DetectedLanguageCode))
                {
                    var detected = LanguageDatabase.FindByCode(result.DetectedLanguageCode);
                    StatusText = detected != null
                        ? $"Phát hiện: {detected.Name} → {TargetLanguage.Name}"
                        : $"Dịch thành công";
                }
                else
                {
                    StatusText = $"{SourceLanguage.Name} → {TargetLanguage.Name}";
                }
            }
            else
            {
                TranslatedText = $"⚠ Lỗi: {result.ErrorMessage}";
                StatusText = "Dịch thất bại";
            }
        }
        catch (Exception ex)
        {
            TranslatedText = $"⚠ Lỗi: {ex.Message}";
            StatusText = "Dịch thất bại";
        }
        finally
        {
            IsTranslating = false;
        }
    }

    private void SwapLanguages()
    {
        if (SourceLanguage.Code == "auto")
        {
            if (string.IsNullOrEmpty(_lastDetectedLangCode) || _lastDetectedLangCode == "auto")
                return;

            var newTarget = TargetLanguages.FirstOrDefault(l => l.Code == _lastDetectedLangCode);
            if (newTarget == null) return;
            
            var oldTarget = TargetLanguage;
            TargetLanguage = newTarget;
            SourceLanguage = SourceLanguages.FirstOrDefault(l => l.Code == oldTarget.Code) ?? SourceLanguages[0];
        }
        else
        {
            var oldTarget = TargetLanguage;
            TargetLanguage = TargetLanguages.FirstOrDefault(l => l.Code == SourceLanguage.Code) ?? TargetLanguages[0];
            SourceLanguage = SourceLanguages.FirstOrDefault(l => l.Code == oldTarget.Code) ?? SourceLanguages[0];
        }

        (SourceText, TranslatedText) = (TranslatedText, SourceText);
        
        // Auto trigger translate after swapping, similar to QTranslate
        if (!string.IsNullOrWhiteSpace(SourceText))
            _ = TranslateAsync();
    }

    private void CopyResult()
    {
        if (!string.IsNullOrEmpty(TranslatedText))
            System.Windows.Clipboard.SetText(TranslatedText);
    }

    private void Clear()
    {
        SourceText = "";
        TranslatedText = "";
        StatusText = "Ready";
    }
}
