using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using DesktopMemo.Core.Contracts;

namespace DesktopMemo.Infrastructure.Services;

public class WindowService : IWindowService, IDisposable
{
    private Window? _window;
    private bool _disposed;
    private TopmostMode _currentTopmostMode = TopmostMode.Desktop;
    private bool _isClickThroughEnabled = false;

    // Win32 API 常量
    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TRANSPARENT = 0x00000020;
    private const int WS_EX_LAYERED = 0x00080000;

    private static readonly IntPtr HWND_BOTTOM = new IntPtr(1);
    private static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);
    private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);

    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOACTIVATE = 0x0010;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    public void Initialize(Window window)
    {
        _window = window ?? throw new ArgumentNullException(nameof(window));
    }

    public void SetTopmostMode(TopmostMode mode)
    {
        if (_window == null)
        {
            return;
        }

        var hwnd = new WindowInteropHelper(_window).Handle;
        if (hwnd == IntPtr.Zero)
        {
            return;
        }

        _currentTopmostMode = mode;

        switch (mode)
        {
            case TopmostMode.Normal:
                _window.Topmost = false;
                SetWindowPos(hwnd, HWND_NOTOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
                break;

            case TopmostMode.Desktop:
                _window.Topmost = false;
                SetWindowPos(hwnd, HWND_NOTOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
                SetWindowPos(hwnd, HWND_BOTTOM, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
                IntPtr progmanHwnd = FindWindow("Progman", "Program Manager");
                if (progmanHwnd != IntPtr.Zero)
                {
                    SetWindowPos(hwnd, progmanHwnd, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
                }
                break;

            case TopmostMode.Always:
                _window.Topmost = true;
                SetWindowPos(hwnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
                break;
        }
    }

    public TopmostMode GetCurrentTopmostMode() => _currentTopmostMode;

    public void SetClickThrough(bool enabled)
    {
        if (_window == null)
        {
            return;
        }

        var hwnd = new WindowInteropHelper(_window).Handle;
        if (hwnd == IntPtr.Zero)
        {
            return;
        }

        int exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);

        if (enabled)
        {
            exStyle |= WS_EX_TRANSPARENT | WS_EX_LAYERED;
        }
        else
        {
            exStyle &= ~WS_EX_TRANSPARENT;
        }

        SetWindowLong(hwnd, GWL_EXSTYLE, exStyle);
        _isClickThroughEnabled = enabled;
    }

    public bool IsClickThroughEnabled => _isClickThroughEnabled;

    public void SetWindowPosition(double x, double y)
    {
        if (_window == null)
        {
            return;
        }

        var workingArea = SystemParameters.WorkArea;
        double minX = workingArea.Left - _window.Width + 50;
        double maxX = workingArea.Right - 50;
        double minY = workingArea.Top;
        double maxY = workingArea.Bottom - _window.Height;

        _window.Left = Math.Max(minX, Math.Min(maxX, x));
        _window.Top = Math.Max(minY, Math.Min(maxY, y));
    }

    public (double X, double Y) GetWindowPosition()
    {
        if (_window == null)
        {
            return (0, 0);
        }

        return (_window.Left, _window.Top);
    }

    public void MoveToPresetPosition(string position)
    {
        if (_window == null)
        {
            return;
        }

        var workingArea = SystemParameters.WorkArea;
        double newX = 0,
            newY = 0;
        double windowWidth = _window.Width;
        double windowHeight = _window.Height;

        switch (position)
        {
            case "TopLeft":
                newX = workingArea.Left + 10;
                newY = workingArea.Top + 10;
                break;
            case "TopCenter":
                newX = workingArea.Left + (workingArea.Width - windowWidth) / 2;
                newY = workingArea.Top + 10;
                break;
            case "TopRight":
                newX = workingArea.Right - windowWidth - 10;
                newY = workingArea.Top + 10;
                break;
            case "MiddleLeft":
                newX = workingArea.Left + 10;
                newY = workingArea.Top + (workingArea.Height - windowHeight) / 2;
                break;
            case "Center":
                newX = workingArea.Left + (workingArea.Width - windowWidth) / 2;
                newY = workingArea.Top + (workingArea.Height - windowHeight) / 2;
                break;
            case "MiddleRight":
                newX = workingArea.Right - windowWidth - 10;
                newY = workingArea.Top + (workingArea.Height - windowHeight) / 2;
                break;
            case "BottomLeft":
                newX = workingArea.Left + 10;
                newY = workingArea.Bottom - windowHeight - 10;
                break;
            case "BottomCenter":
                newX = workingArea.Left + (workingArea.Width - windowWidth) / 2;
                newY = workingArea.Bottom - windowHeight - 10;
                break;
            case "BottomRight":
                newX = workingArea.Right - windowWidth - 10;
                newY = workingArea.Bottom - windowHeight - 10;
                break;
            default:
                return;
        }

        SetWindowPosition(newX, newY);
    }

    public void SetWindowOpacity(double opacity)
    {
        if (_window == null)
        {
            return;
        }

        _window.Opacity = Math.Max(0.1, Math.Min(1.0, opacity));
    }

    public double GetWindowOpacity()
    {
        if (_window == null)
        {
            return 1.0;
        }

        return _window.Opacity;
    }

    public void PlayFadeInAnimation()
    {
        if (_window == null)
        {
            return;
        }

        var animation = new DoubleAnimation
        {
            From = 0,
            To = 1,
            Duration = TimeSpan.FromMilliseconds(350),
            EasingFunction = new ExponentialEase { EasingMode = EasingMode.EaseOut, Exponent = 4 }
        };

        _window.BeginAnimation(Window.OpacityProperty, animation);
    }

    public void ToggleWindowVisibility()
    {
        if (_window == null)
        {
            return;
        }

        if (_window.Visibility == Visibility.Visible)
        {
            _window.Hide();
        }
        else
        {
            _window.Show();
            if (_window.WindowState == WindowState.Minimized)
            {
                _window.WindowState = WindowState.Normal;
            }
            _window.Activate();
        }
    }

    public void MinimizeToTray()
    {
        if (_window == null)
        {
            return;
        }

        _window.Hide();
    }

    public void RestoreFromTray()
    {
        if (_window == null)
        {
            return;
        }

        _window.Show();
        if (_window.WindowState == WindowState.Minimized)
        {
            _window.WindowState = WindowState.Normal;
        }
        _window.Activate();
        _window.Focus();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _window = null;
    }
}