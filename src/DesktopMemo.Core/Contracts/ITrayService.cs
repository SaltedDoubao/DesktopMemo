namespace DesktopMemo.Core.Contracts;

public interface ITrayService
{
    /// <summary>
    /// 初始化托盘图标
    /// </summary>
    void Initialize();

    /// <summary>
    /// 显示托盘图标
    /// </summary>
    void Show();

    /// <summary>
    /// 隐藏托盘图标
    /// </summary>
    void Hide();

    /// <summary>
    /// 显示气泡提示
    /// </summary>
    void ShowBalloonTip(string title, string text, int timeout = 2000);

    /// <summary>
    /// 更新托盘图标文本
    /// </summary>
    void UpdateText(string text);

    /// <summary>
    /// 销毁托盘图标
    /// </summary>
    void Dispose();

    /// <summary>
    /// 托盘图标双击事件
    /// </summary>
    event EventHandler? TrayIconDoubleClick;

    /// <summary>
    /// 显示/隐藏窗口菜单项点击事件
    /// </summary>
    event EventHandler? ShowHideWindowClick;

    /// <summary>
    /// 新建备忘录菜单项点击事件
    /// </summary>
    event EventHandler? NewMemoClick;

    /// <summary>
    /// 设置菜单项点击事件
    /// </summary>
    event EventHandler? SettingsClick;

    /// <summary>
    /// 退出菜单项点击事件
    /// </summary>
    event EventHandler? ExitClick;
}