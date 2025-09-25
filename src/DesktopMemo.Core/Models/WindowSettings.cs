using System;

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
    bool IsClickThrough)
{
    public static WindowSettings Default => new(900, 600, double.NaN, double.NaN, 0.85, true, false, false);

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

