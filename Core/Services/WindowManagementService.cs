using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Interop;
using DesktopMemo.Core.Interfaces;
using DesktopMemo.Utils;

namespace DesktopMemo.Core.Services
{
    public class WindowManagementService : IWindowManagementService
    {
        private readonly Window _window;
        private TopmostMode _currentTopmostMode = TopmostMode.Desktop;
        private bool _isWindowPinned = false;
        private System.Windows.Point? _savedPosition;
        private System.Windows.Size? _savedSize;

        private readonly Dictionary<string, System.Windows.Point> _presetPositions;

        public event EventHandler<WindowStateChangedEventArgs>? WindowStateChanged;

        public bool IsWindowPinned => _isWindowPinned;
        public TopmostMode CurrentTopmostMode => _currentTopmostMode;

        public WindowManagementService(Window window)
        {
            _window = window ?? throw new ArgumentNullException(nameof(window));

            _presetPositions = InitializePresetPositions();
        }

        private Dictionary<string, System.Windows.Point> InitializePresetPositions()
        {
            var screenWidth = SystemParameters.PrimaryScreenWidth;
            var screenHeight = SystemParameters.PrimaryScreenHeight;
            var windowWidth = _window.Width;
            var windowHeight = _window.Height;

            return new Dictionary<string, System.Windows.Point>
            {
                ["TopLeft"] = new System.Windows.Point(50, 50),
                ["TopCenter"] = new System.Windows.Point((screenWidth - windowWidth) / 2, 50),
                ["TopRight"] = new System.Windows.Point(screenWidth - windowWidth - 50, 50),
                ["MiddleLeft"] = new System.Windows.Point(50, (screenHeight - windowHeight) / 2),
                ["Center"] = new System.Windows.Point((screenWidth - windowWidth) / 2, (screenHeight - windowHeight) / 2),
                ["MiddleRight"] = new System.Windows.Point(screenWidth - windowWidth - 50, (screenHeight - windowHeight) / 2),
                ["BottomLeft"] = new System.Windows.Point(50, screenHeight - windowHeight - 100),
                ["BottomCenter"] = new System.Windows.Point((screenWidth - windowWidth) / 2, screenHeight - windowHeight - 100),
                ["BottomRight"] = new System.Windows.Point(screenWidth - windowWidth - 50, screenHeight - windowHeight - 100)
            };
        }

        public void SetTopmostMode(TopmostMode mode)
        {
            var oldMode = _currentTopmostMode;
            _currentTopmostMode = mode;

            switch (mode)
            {
                case TopmostMode.Normal:
                    _window.Topmost = false;
                    DisableDesktopMode();
                    break;
                case TopmostMode.Desktop:
                    _window.Topmost = false;
                    EnableDesktopMode();
                    break;
                case TopmostMode.Always:
                    _window.Topmost = true;
                    DisableDesktopMode();
                    break;
            }

            WindowStateChanged?.Invoke(this, new WindowStateChangedEventArgs(
                nameof(CurrentTopmostMode), oldMode, mode));
        }

        private void EnableDesktopMode()
        {
            var hwnd = new WindowInteropHelper(_window).Handle;
            if (hwnd != IntPtr.Zero)
            {
                Win32Helper.SetWindowAsDesktopChild(hwnd);
            }
        }

        private void DisableDesktopMode()
        {
            var hwnd = new WindowInteropHelper(_window).Handle;
            if (hwnd != IntPtr.Zero)
            {
                Win32Helper.RestoreNormalWindow(hwnd);
            }
        }

        public void SetWindowPosition(System.Windows.Point position)
        {
            _window.Left = position.X;
            _window.Top = position.Y;
        }

        public void SetWindowSize(System.Windows.Size size)
        {
            _window.Width = size.Width;
            _window.Height = size.Height;
        }

        public void SetPresetPosition(string presetName)
        {
            if (_presetPositions.TryGetValue(presetName, out var position))
            {
                SetWindowPosition(position);
            }
        }

        public void RememberCurrentPosition()
        {
            _savedPosition = new System.Windows.Point(_window.Left, _window.Top);
            _savedSize = new System.Windows.Size(_window.Width, _window.Height);
        }

        public void RestoreSavedPosition()
        {
            if (_savedPosition.HasValue)
            {
                SetWindowPosition(_savedPosition.Value);
            }
            if (_savedSize.HasValue)
            {
                SetWindowSize(_savedSize.Value);
            }
        }

        public void SetWindowPinned(bool isPinned)
        {
            var oldValue = _isWindowPinned;
            _isWindowPinned = isPinned;

            WindowStateChanged?.Invoke(this, new WindowStateChangedEventArgs(
                nameof(IsWindowPinned), oldValue, isPinned));
        }
    }
}