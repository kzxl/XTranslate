namespace XTranslate.Core.Interfaces;

public interface IClipboardService
{
    Task<string?> GetSelectedTextAsync();
}
