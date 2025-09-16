using System;
using System.Threading.Tasks;

namespace DesktopMemo.Core.Interfaces
{
    public interface ISettingsService
    {
        T GetSetting<T>(string key, T defaultValue);
        void SetSetting<T>(string key, T value);
        Task SaveSettingsAsync();
        Task LoadSettingsAsync();
        event EventHandler<SettingChangedEventArgs>? SettingChanged;
    }

    public class SettingChangedEventArgs : EventArgs
    {
        public string Key { get; }
        public object? Value { get; }

        public SettingChangedEventArgs(string key, object? value)
        {
            Key = key;
            Value = value;
        }
    }
}