namespace DesktopMemo.Core.Contracts;

public enum TopmostMode
{
    Normal,     // 普通模式，不置顶
    Desktop,    // 桌面层面置顶
    Always      // 总是置顶
}

public interface IWindowService
{
    /// <summary>
    /// 设置窗口置顶模式
    /// </summary>
    void SetTopmostMode(TopmostMode mode);

    /// <summary>
    /// 获取当前置顶模式
    /// </summary>
    TopmostMode GetCurrentTopmostMode();

    /// <summary>
    /// 启用或禁用穿透模式
    /// </summary>
    void SetClickThrough(bool enabled);

    /// <summary>
    /// 检查是否启用了穿透模式
    /// </summary>
    bool IsClickThroughEnabled { get; }

    /// <summary>
    /// 设置窗口位置
    /// </summary>
    void SetWindowPosition(double x, double y);

    /// <summary>
    /// 获取当前窗口位置
    /// </summary>
    (double X, double Y) GetWindowPosition();

    /// <summary>
    /// 移动到预设位置
    /// </summary>
    void MoveToPresetPosition(string position);

    /// <summary>
    /// 设置窗口透明度
    /// </summary>
    void SetWindowOpacity(double opacity);

    /// <summary>
    /// 获取当前透明度
    /// </summary>
    double GetWindowOpacity();

    /// <summary>
    /// 播放窗口动画
    /// </summary>
    void PlayFadeInAnimation();

    /// <summary>
    /// 显示或隐藏窗口
    /// </summary>
    void ToggleWindowVisibility();

    /// <summary>
    /// 最小化到托盘
    /// </summary>
    void MinimizeToTray();

    /// <summary>
    /// 从托盘恢复
    /// </summary>
    void RestoreFromTray();
}