using XTranslate.Models;

namespace XTranslate.Core.Interfaces;

public interface ISettingsService
{
    AppSettings Settings { get; }
    void Load();
    void Save();
}
