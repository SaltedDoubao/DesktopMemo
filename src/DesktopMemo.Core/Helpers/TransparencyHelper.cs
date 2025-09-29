using DesktopMemo.Core.Constants;

namespace DesktopMemo.Core.Helpers;

/// <summary>
/// 透明度相关的辅助方法
/// </summary>
public static class TransparencyHelper
{
    /// <summary>
    /// 将透明度值转换为百分比
    /// </summary>
    /// <param name="transparency">透明度值 (0-0.4)</param>
    /// <returns>百分比值 (0-100)</returns>
    public static double ToPercent(double transparency)
    {
        var clampedTransparency = Math.Max(0, Math.Min(WindowConstants.MAX_TRANSPARENCY, transparency));
        return (clampedTransparency / WindowConstants.MAX_TRANSPARENCY) * WindowConstants.MAX_TRANSPARENCY_PERCENT;
    }

    /// <summary>
    /// 将百分比转换为透明度值
    /// </summary>
    /// <param name="percent">百分比值 (0-100)</param>
    /// <returns>透明度值 (0-0.4)</returns>
    public static double FromPercent(double percent)
    {
        var clampedPercent = Math.Max(WindowConstants.MIN_TRANSPARENCY_PERCENT,
                                     Math.Min(WindowConstants.MAX_TRANSPARENCY_PERCENT, percent));
        return (clampedPercent * WindowConstants.MAX_TRANSPARENCY) / WindowConstants.MAX_TRANSPARENCY_PERCENT;
    }

    /// <summary>
    /// 验证透明度值是否有效
    /// </summary>
    /// <param name="transparency">透明度值</param>
    /// <returns>是否有效</returns>
    public static bool IsValidTransparency(double transparency)
    {
        return !double.IsNaN(transparency) &&
               !double.IsInfinity(transparency) &&
               transparency >= 0 &&
               transparency <= WindowConstants.MAX_TRANSPARENCY;
    }

    /// <summary>
    /// 规范化透明度值到有效范围
    /// </summary>
    /// <param name="transparency">透明度值</param>
    /// <returns>规范化后的透明度值</returns>
    public static double NormalizeTransparency(double transparency)
    {
        if (!IsValidTransparency(transparency))
        {
            return WindowConstants.DEFAULT_TRANSPARENCY;
        }
        return Math.Max(0, Math.Min(WindowConstants.MAX_TRANSPARENCY, transparency));
    }
}