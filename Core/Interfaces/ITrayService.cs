using System;
using System.Windows.Forms;

namespace DesktopMemo.Core.Interfaces
{
    public interface ITrayService : IDisposable
    {
        void Initialize();
        void ShowTrayIcon();
        void HideTrayIcon();
        void UpdateTooltip(string text);
        void ShowBalloonTip(string title, string text, ToolTipIcon icon = ToolTipIcon.Info);
        event EventHandler? DoubleClick;
        event EventHandler? ShowHideRequested;
        event EventHandler? ExitRequested;
        event EventHandler<TopmostMode>? TopmostModeChanged;
        event EventHandler? NewMemoRequested;
        event EventHandler? ClickThroughToggled;
        ContextMenuStrip? ContextMenu { get; }
    }
}