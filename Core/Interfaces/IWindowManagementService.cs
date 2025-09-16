using System;
using System.Windows;

namespace DesktopMemo.Core.Interfaces
{
    public interface IWindowManagementService
    {
        void SetTopmostMode(TopmostMode mode);
        void SetWindowPosition(System.Windows.Point position);
        void SetWindowSize(System.Windows.Size size);
        void SetPresetPosition(string presetName);
        void RememberCurrentPosition();
        void RestoreSavedPosition();
        void SetWindowPinned(bool isPinned);
        bool IsWindowPinned { get; }
        TopmostMode CurrentTopmostMode { get; }
        event EventHandler<WindowStateChangedEventArgs>? WindowStateChanged;
    }

    public class WindowStateChangedEventArgs : EventArgs
    {
        public string PropertyName { get; }
        public object? OldValue { get; }
        public object? NewValue { get; }

        public WindowStateChangedEventArgs(string propertyName, object? oldValue, object? newValue)
        {
            PropertyName = propertyName;
            OldValue = oldValue;
            NewValue = newValue;
        }
    }

    public enum TopmostMode
    {
        Normal,
        Desktop,
        Always
    }
}