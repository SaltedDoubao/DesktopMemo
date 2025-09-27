using System;

namespace DesktopMemo.Core.Contracts;

public interface ITrayService : IDisposable
{
    event EventHandler? TrayIconDoubleClick;
    event EventHandler? ShowHideWindowClick;
    event EventHandler? NewMemoClick;
    event EventHandler? SettingsClick;
    event EventHandler? ExitClick;

    event EventHandler<string>? MoveToPresetClick;
    event EventHandler? RememberPositionClick;
    event EventHandler? RestorePositionClick;
    event EventHandler? ExportNotesClick;
    event EventHandler? ImportNotesClick;
    event EventHandler? ClearContentClick;
    event EventHandler? AboutClick;
    event EventHandler? RestartTrayClick;

    void Initialize();
    void Show();
    void Hide();
    void ShowBalloonTip(string title, string text, int timeout = 2000);
    void UpdateText(string text);
    void UpdateTopmostState(TopmostMode mode);
    void UpdateClickThroughState(bool enabled);
}