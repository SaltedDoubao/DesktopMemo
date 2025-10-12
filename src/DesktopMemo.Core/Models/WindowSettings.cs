using System;
using DesktopMemo.Core.Constants;

namespace DesktopMemo.Core.Models;

/// <summary>
/// 表示窗口相关的用户设置。
/// </summary>
public sealed record WindowSettings(
    double Width,
    double Height,
    double Left,
    double Top,
    double Transparency,
    bool IsTopMost,
    bool IsDesktopMode,
    bool IsClickThrough,
    bool ShowDeleteConfirmation = true,
    bool ShowExitConfirmation = true,
    bool DefaultExitToTray = true,
    string PreferredLanguage = "zh-CN",
    string CurrentPage = "memo")
{
    public static WindowSettings Default => new(900, 600, double.NaN, double.NaN, WindowConstants.DEFAULT_TRANSPARENCY, false, true, false, true, true, true, "zh-CN", "memo");

    public WindowSettings WithLocation(double left, double top)
        => this with { Left = left, Top = top };

    public WindowSettings WithSize(double width, double height)
        => this with { Width = width, Height = height };

    public WindowSettings WithAppearance(double transparency, bool topMost, bool desktopMode, bool clickThrough)
        => this with
        {
            Transparency = transparency,
            IsTopMost = topMost,
            IsDesktopMode = desktopMode,
            IsClickThrough = clickThrough
        };
}

