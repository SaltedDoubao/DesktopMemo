using System;
using System.Collections.Generic;
using System.Globalization;

namespace DesktopMemo.Core.Interfaces
{
    public interface ILocalizationService
    {
        string GetString(string key);
        string GetString(string key, params object[] args);
        void SetLanguage(string languageCode);
        string GetCurrentLanguage();
        IEnumerable<LanguageInfo> GetAvailableLanguages();
        event EventHandler<LanguageChangedEventArgs>? LanguageChanged;
    }

    public class LanguageChangedEventArgs : EventArgs
    {
        public string LanguageCode { get; }
        public CultureInfo Culture { get; }

        public LanguageChangedEventArgs(string languageCode, CultureInfo culture)
        {
            LanguageCode = languageCode;
            Culture = culture;
        }
    }

    public class LanguageInfo
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string NativeName { get; set; } = string.Empty;
    }
}