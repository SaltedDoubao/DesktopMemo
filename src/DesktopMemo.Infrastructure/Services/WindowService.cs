using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using DesktopMemo.Core.Contracts;
using DesktopMemo.Core.Constants;
using System.Diagnostics;

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

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool IsWindow(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    private const int SW_HIDE = 0;
    private const int SW_SHOW = 5;

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
                SafeSetWindowPos(hwnd, HWND_NOTOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE, "Normal模式");
                break;

            case TopmostMode.Desktop:
                _window.Topmost = false;
                // 改进的桌面置顶实现
                SetDesktopTopmost(hwnd);
                break;

            case TopmostMode.Always:
                _window.Topmost = true;
                SafeSetWindowPos(hwnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE, "Always模式");
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
        if (exStyle == 0)
        {
            var error = Marshal.GetLastWin32Error();
            Debug.WriteLine($"获取窗口样式失败，错误代码: {error}");
            return;
        }

        if (enabled)
        {
            exStyle |= WS_EX_TRANSPARENT | WS_EX_LAYERED;
        }
        else
        {
            exStyle &= ~WS_EX_TRANSPARENT;
        }

        var result = SetWindowLong(hwnd, GWL_EXSTYLE, exStyle);
        if (result == 0)
        {
            var error = Marshal.GetLastWin32Error();
            Debug.WriteLine($"设置窗口样式失败，错误代码: {error}");
        }
        _isClickThroughEnabled = enabled;
    }

    public bool IsClickThroughEnabled => _isClickThroughEnabled;

    public void SetWindowPosition(double x, double y)
    {
        if (_window == null)
        {
            return;
        }

        // 验证输入值是否有效
        if (double.IsNaN(x) || double.IsInfinity(x) || 
            double.IsNaN(y) || double.IsInfinity(y))
        {
            return;
        }

        try
        {
            var workingArea = SystemParameters.WorkArea;
            double minX = workingArea.Left - _window.Width + 50;
            double maxX = workingArea.Right - 50;
            double minY = workingArea.Top;
            double maxY = workingArea.Bottom - _window.Height;

            _window.Left = Math.Max(minX, Math.Min(maxX, x));
            _window.Top = Math.Max(minY, Math.Min(maxY, y));
        }
        catch
        {
            // 如果设置位置失败，忽略错误
        }
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

    private double _backgroundOpacity = WindowConstants.DEFAULT_TRANSPARENCY; // 存储背景透明度值

    public void SetWindowOpacity(double opacity)
    {
        // 验证透明度值是否有效
        if (double.IsNaN(opacity) || double.IsInfinity(opacity))
        {
            opacity = WindowConstants.DEFAULT_TRANSPARENCY; // 使用默认透明度值
            System.Diagnostics.Debug.WriteLine($"透明度值无效，使用默认值: {opacity}");
        }

        _backgroundOpacity = Math.Max(0, Math.Min(WindowConstants.MAX_TRANSPARENCY, opacity)); // 确保在有效范围内
        System.Diagnostics.Debug.WriteLine($"窗口服务存储背景透明度: {_backgroundOpacity}");
    }

    public double GetWindowOpacity()
    {
        return _backgroundOpacity; // 返回存储的背景透明度值
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

    /// <summary>
    /// 改进的桌面置顶实现，确保窗口始终在桌面背景上方
    /// </summary>
    private void SetDesktopTopmost(IntPtr hwnd)
    {
        try
        {
            // 首先设置为非置顶
            SafeSetWindowPos(hwnd, HWND_NOTOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE, "桌面模式-设置非置顶");
            
            // 找到Program Manager
            IntPtr progmanHwnd = FindWindow("Progman", "Program Manager");
            
            if (progmanHwnd != IntPtr.Zero && IsWindow(progmanHwnd))
            {
                // 发送消息以获取WorkerW窗口
                IntPtr result = IntPtr.Zero;
                SendMessage(progmanHwnd, 0x052C, IntPtr.Zero, IntPtr.Zero);
                
                // 查找WorkerW窗口
                IntPtr workerw = IntPtr.Zero;
                IntPtr shellDllDefView = IntPtr.Zero;
                
                do
                {
                    workerw = FindWindowEx(IntPtr.Zero, workerw, "WorkerW", null!);
                    if (workerw != IntPtr.Zero)
                    {
                        shellDllDefView = FindWindowEx(workerw, IntPtr.Zero, "SHELLDLL_DefView", null!);
                        if (shellDllDefView != IntPtr.Zero)
                        {
                            break;
                        }
                    }
                } while (workerw != IntPtr.Zero);
                
                // 将窗口放置在适当的位置
                if (workerw != IntPtr.Zero)
                {
                    // 放置在WorkerW之上
                    SafeSetWindowPos(hwnd, workerw, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE, "桌面模式-WorkerW上方");
                }
                else
                {
                    // 如果找不到WorkerW，使用传统方法
                    SafeSetWindowPos(hwnd, HWND_BOTTOM, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE, "桌面模式-底部");
                    SafeSetWindowPos(hwnd, progmanHwnd, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE, "桌面模式-Progman上方");
                }
            }
            else
            {
                // 如果找不到Program Manager，直接放置在底部
                SafeSetWindowPos(hwnd, HWND_BOTTOM, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE, "桌面模式-找不到Progman");
            }
        }
        catch
        {
            // 如果任何操作失败，使用最简单的方法
            SafeSetWindowPos(hwnd, HWND_BOTTOM, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE, "桌面模式-异常处理");
        }
    }
    
    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    /// <summary>
    /// 安全的SetWindowPos调用，包含错误检查和日志记录
    /// </summary>
    private static void SafeSetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags, string operation)
    {
        try
        {
            var success = SetWindowPos(hWnd, hWndInsertAfter, x, y, cx, cy, uFlags);
            if (!success)
            {
                var error = Marshal.GetLastWin32Error();
                Debug.WriteLine($"SetWindowPos失败 - 操作: {operation}, 错误代码: {error}");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"SetWindowPos异常 - 操作: {operation}, 异常: {ex.Message}");
        }
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