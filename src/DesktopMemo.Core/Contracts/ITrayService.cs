using System;

namespace DesktopMemo.Core.Contracts;

public interface ITrayService : IDisposable
{
    event EventHandler? TrayIconDoubleClick;
    event EventHandler? ShowHideWindowClick;
    event EventHandler? NewMemoClick;
    event EventHandler? SettingsClick;
    event EventHandler? ExitClick;

    void Initialize();
    void Show();
    void Hide();
    void ShowBalloonTip(string title, string text, int timeout = 2000);
    void UpdateText(string text);
    void UpdateTopmostState(TopmostMode mode);
    void UpdateClickThroughState(bool enabled);
}