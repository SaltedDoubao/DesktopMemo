using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using DesktopMemo.Core.Contracts;
using Forms = System.Windows.Forms;

namespace DesktopMemo.Infrastructure.Services;

public sealed class TrayService : ITrayService
{
    private Forms.NotifyIcon? _notifyIcon;
    private bool _isDisposed;
    private Font? _regularFont;
    private Font? _boldFont;
    private Forms.ToolStripMenuItem? _topmostNormalItem;
    private Forms.ToolStripMenuItem? _topmostDesktopItem;
    private Forms.ToolStripMenuItem? _topmostAlwaysItem;
    private Forms.ToolStripMenuItem? _trayClickThroughItem;
    private Forms.ToolStripMenuItem? _rememberPositionItem;
    private Forms.ToolStripMenuItem? _restorePositionItem;
    private Forms.ToolStripMenuItem? _showExitPromptItem;
    private Forms.ToolStripMenuItem? _showDeletePromptItem;
    private Forms.ToolStripMenuItem? _exportNotesItem;
    private Forms.ToolStripMenuItem? _importNotesItem;
    private Forms.ToolStripMenuItem? _clearContentItem;
    private Forms.ToolStripMenuItem? _aboutItem;
    private Forms.ToolStripMenuItem? _restartTrayItem;
    private Forms.ToolStripMenuItem? _trayPresetTopLeft;
    private Forms.ToolStripMenuItem? _trayPresetTopCenter;
    private Forms.ToolStripMenuItem? _trayPresetTopRight;
    private Forms.ToolStripMenuItem? _trayPresetCenter;
    private Forms.ToolStripMenuItem? _trayPresetBottomLeft;
    private Forms.ToolStripMenuItem? _trayPresetBottomRight;
    private Forms.ToolStripMenuItem? _toggleSettingsItem;
    private Forms.ToolStripMenuItem? _showHideItem;
    private Forms.ToolStripMenuItem? _newMemoItem;
    private Forms.ToolStripMenuItem? _windowControlGroup;
    private Forms.ToolStripMenuItem? _topmostGroup;
    private Forms.ToolStripMenuItem? _positionGroup;
    private Forms.ToolStripMenuItem? _quickPosGroup;
    private Forms.ToolStripMenuItem? _toolsGroup;
    private Forms.ToolStripMenuItem? _exitItem;
    private Forms.ContextMenuStrip? _contextMenu;
    private static Icon? _cachedTrayIcon;

    public event EventHandler? TrayIconDoubleClick;
    public event EventHandler? ShowHideWindowClick;
    public event EventHandler? NewMemoClick;
    public event EventHandler? SettingsClick;
    public event EventHandler? ExitClick;
    public event EventHandler<string>? MoveToPresetClick;
    public event EventHandler? RememberPositionClick;
    public event EventHandler? RestorePositionClick;
    public event EventHandler? ExportNotesClick;
    public event EventHandler? ImportNotesClick;
    public event EventHandler? ClearContentClick;
    public event EventHandler? AboutClick;
    public event EventHandler? RestartTrayClick;
    public event EventHandler<bool>? ClickThroughToggleClick;
    public event EventHandler? ReenableExitPromptClick;
    public event EventHandler? ReenableDeletePromptClick;
    public event EventHandler<TopmostMode>? TopmostModeChangeClick;

    public bool IsClickThroughEnabled { get; private set; }

    public void Initialize()
    {
        if (_notifyIcon != null)
        {
            return;
        }

        try
        {
            _notifyIcon = new Forms.NotifyIcon
            {
                Text = "DesktopMemo 便签 - 桌面便签工具",
                Visible = false // 先设为false，避免在未完全初始化时显示
            };

            SetTrayIcon();
            BuildContextMenu();
            _notifyIcon.ContextMenuStrip = _contextMenu;
            _notifyIcon.DoubleClick += (s, e) => TrayIconDoubleClick?.Invoke(s, e);
        }
        catch (Exception)
        {
            // 如果托盘图标初始化失败，创建一个最简单的版本
            try
            {
                _notifyIcon = new Forms.NotifyIcon
                {
                    Text = "DesktopMemo",
                    Icon = SystemIcons.Application,
                    Visible = false
                };
                _notifyIcon.DoubleClick += (s, e) => TrayIconDoubleClick?.Invoke(s, e);
            }
            catch
            {
                // 如果连简单版本都失败，则忽略托盘功能
                _notifyIcon = null;
            }
        }
    }

    public void Show()
    {
        try
        {
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = true;
            }
        }
        catch
        {
            // 如果显示托盘图标失败，忽略错误
        }
    }

    public void Hide()
    {
        try
        {
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
            }
        }
        catch
        {
            // 如果隐藏托盘图标失败，忽略错误
        }
    }

    public void ShowBalloonTip(string title, string text, int timeout = 2000)
    {
        _notifyIcon?.ShowBalloonTip(timeout, title, text, Forms.ToolTipIcon.Info);
    }

    public void UpdateText(string text)
    {
        if (_notifyIcon != null)
        {
            _notifyIcon.Text = text;
        }
    }

    public void UpdateTopmostState(TopmostMode mode)
    {
        try
        {
            if (_topmostNormalItem != null) _topmostNormalItem.Checked = mode == TopmostMode.Normal;
            if (_topmostDesktopItem != null) _topmostDesktopItem.Checked = mode == TopmostMode.Desktop;
            if (_topmostAlwaysItem != null) _topmostAlwaysItem.Checked = mode == TopmostMode.Always;
        }
        catch
        {
            // 更新托盘菜单状态失败，忽略错误
        }
    }

    private void OnTopmostModeChanged(TopmostMode mode)
    {
        try
        {
            TopmostModeChangeClick?.Invoke(this, mode);
        }
        catch
        {
            // 事件处理失败，忽略错误
        }
    }

    public void UpdateClickThroughState(bool enabled)
    {
        try
        {
            IsClickThroughEnabled = enabled;
            if (_trayClickThroughItem != null)
            {
                _trayClickThroughItem.Checked = enabled;
            }
        }
        catch
        {
            // 更新托盘菜单状态失败，忽略错误
        }
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        if (_notifyIcon != null)
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            _notifyIcon = null;
        }

        _contextMenu?.Dispose();
        _regularFont?.Dispose();
        _regularFont = null;
        _boldFont?.Dispose();
        _boldFont = null;
        _contextMenu = null;
        _isDisposed = true;
        GC.SuppressFinalize(this);
    }

    ~TrayService()
    {
        Dispose();
    }

    private void SetTrayIcon()
    {
        if (_notifyIcon == null)
        {
            return;
        }

        try
        {
            if (_cachedTrayIcon != null)
            {
                _notifyIcon.Icon = _cachedTrayIcon;
                return;
            }

            var exePath = Environment.ProcessPath;
            if (!string.IsNullOrWhiteSpace(exePath) && File.Exists(exePath))
            {
                var icon = Icon.ExtractAssociatedIcon(exePath);
                if (icon != null)
                {
                    _cachedTrayIcon = icon;
                    _notifyIcon.Icon = icon;
                    return;
                }
            }

            var currentProcess = Process.GetCurrentProcess();
            var mainModule = currentProcess.MainModule;
            if (mainModule?.FileName != null)
            {
                var icon = Icon.ExtractAssociatedIcon(mainModule.FileName);
                if (icon != null)
                {
                    _cachedTrayIcon = icon;
                    _notifyIcon.Icon = icon;
                    return;
                }
            }

            _notifyIcon.Icon = SystemIcons.Application;
        }
        catch
        {
            _notifyIcon.Icon = SystemIcons.Application;
        }
    }

    private void BuildContextMenu()
    {
        if (_notifyIcon == null)
        {
            return;
        }

        _contextMenu = new Forms.ContextMenuStrip
        {
            Renderer = new DarkTrayMenuRenderer(),
            ShowImageMargin = false,
            ShowCheckMargin = true,
            AutoSize = true,
            Padding = new Forms.Padding(8, 4, 8, 4),
            BackColor = Color.FromArgb(45, 45, 48),
            ForeColor = Color.FromArgb(241, 241, 241)
        };

        typeof(Forms.ToolStripDropDownMenu).InvokeMember("DoubleBuffered",
            System.Reflection.BindingFlags.SetProperty | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
            null, _contextMenu, new object[] { true });

        _regularFont?.Dispose();
        _regularFont = CreateFont("Microsoft YaHei", 9F, FontStyle.Regular);
        _boldFont?.Dispose();
        _boldFont = CreateFont("Microsoft YaHei", 9F, FontStyle.Bold);

        _showHideItem = new Forms.ToolStripMenuItem("🏠 显示/隐藏窗口") { Font = _boldFont };
        _showHideItem.Click += (s, e) => ShowHideWindowClick?.Invoke(s, e);

        _newMemoItem = new Forms.ToolStripMenuItem("📝 新建便签") { Font = _regularFont };
        _newMemoItem.Click += (s, e) => NewMemoClick?.Invoke(s, e);

        _toggleSettingsItem = new Forms.ToolStripMenuItem("⚙️ 设置") { Font = _regularFont };
        _toggleSettingsItem.Click += (s, e) => SettingsClick?.Invoke(s, e);

        _windowControlGroup = new Forms.ToolStripMenuItem("🖼️ 窗口控制") { Font = _regularFont };

        _topmostGroup = new Forms.ToolStripMenuItem("📌 置顶模式") { Font = _regularFont };
        _topmostNormalItem = new Forms.ToolStripMenuItem("普通模式") { Font = _regularFont };
        _topmostDesktopItem = new Forms.ToolStripMenuItem("桌面置顶") { Font = _regularFont };
        _topmostAlwaysItem = new Forms.ToolStripMenuItem("总是置顶") { Font = _regularFont };
        
        // 添加事件处理，避免在点击时调用UpdateTopmostState（那个是用来更新UI状态的）
        _topmostNormalItem.Click += (s, e) => OnTopmostModeChanged(TopmostMode.Normal);
        _topmostDesktopItem.Click += (s, e) => OnTopmostModeChanged(TopmostMode.Desktop);
        _topmostAlwaysItem.Click += (s, e) => OnTopmostModeChanged(TopmostMode.Always);
        _topmostGroup.DropDownItems.AddRange(new Forms.ToolStripItem[]
        {
            _topmostNormalItem,
            _topmostDesktopItem,
            _topmostAlwaysItem
        });

        _positionGroup = new Forms.ToolStripMenuItem("📍 窗口位置") { Font = _regularFont };
        _quickPosGroup = new Forms.ToolStripMenuItem("快速定位") { Font = _regularFont };

        _trayPresetTopLeft = new Forms.ToolStripMenuItem("左上角", null, (s, e) => MoveToPresetClick?.Invoke(s, "TopLeft")) { Font = _regularFont };
        _trayPresetTopCenter = new Forms.ToolStripMenuItem("顶部中央", null, (s, e) => MoveToPresetClick?.Invoke(s, "TopCenter")) { Font = _regularFont };
        _trayPresetTopRight = new Forms.ToolStripMenuItem("右上角", null, (s, e) => MoveToPresetClick?.Invoke(s, "TopRight")) { Font = _regularFont };
        _trayPresetCenter = new Forms.ToolStripMenuItem("屏幕中央", null, (s, e) => MoveToPresetClick?.Invoke(s, "Center")) { Font = _regularFont };
        _trayPresetBottomLeft = new Forms.ToolStripMenuItem("左下角", null, (s, e) => MoveToPresetClick?.Invoke(s, "BottomLeft")) { Font = _regularFont };
        _trayPresetBottomRight = new Forms.ToolStripMenuItem("右下角", null, (s, e) => MoveToPresetClick?.Invoke(s, "BottomRight")) { Font = _regularFont };

        _quickPosGroup.DropDownItems.AddRange(new Forms.ToolStripItem[]
        {
            _trayPresetTopLeft,
            _trayPresetTopCenter,
            _trayPresetTopRight,
            new Forms.ToolStripSeparator(),
            _trayPresetCenter,
            new Forms.ToolStripSeparator(),
            _trayPresetBottomLeft,
            _trayPresetBottomRight
        });

        _rememberPositionItem = new Forms.ToolStripMenuItem("记住当前位置") { Font = _regularFont };
        _rememberPositionItem.Click += (s, e) => RememberPositionClick?.Invoke(s, e);

        _restorePositionItem = new Forms.ToolStripMenuItem("恢复保存位置") { Font = _regularFont };
        _restorePositionItem.Click += (s, e) => RestorePositionClick?.Invoke(s, e);

        _positionGroup.DropDownItems.AddRange(new Forms.ToolStripItem[]
        {
            _quickPosGroup,
            new Forms.ToolStripSeparator(),
            _rememberPositionItem,
            _restorePositionItem
        });

        _trayClickThroughItem = new Forms.ToolStripMenuItem("👻 穿透模式")
        {
            Font = _regularFont,
            CheckOnClick = true
        };
        _trayClickThroughItem.Click += (s, e) => ClickThroughToggleClick?.Invoke(s, _trayClickThroughItem.Checked);

        _windowControlGroup.DropDownItems.AddRange(new Forms.ToolStripItem[]
        {
            _topmostGroup,
            _positionGroup,
            _trayClickThroughItem
        });

        _toolsGroup = new Forms.ToolStripMenuItem("🛠️ 工具") { Font = _regularFont };
        _exportNotesItem = new Forms.ToolStripMenuItem("📤 导出便签", null, (s, e) => ExportNotesClick?.Invoke(s, e)) { Font = _regularFont };
        _importNotesItem = new Forms.ToolStripMenuItem("📥 导入便签", null, (s, e) => ImportNotesClick?.Invoke(s, e)) { Font = _regularFont };
        _clearContentItem = new Forms.ToolStripMenuItem("🗑️ 清空内容", null, (s, e) => ClearContentClick?.Invoke(s, e)) { Font = _regularFont };
        _toolsGroup.DropDownItems.AddRange(new Forms.ToolStripItem[]
        {
            _exportNotesItem,
            _importNotesItem,
            _clearContentItem
        });

        _aboutItem = new Forms.ToolStripMenuItem("ℹ️ 关于", null, (s, e) => AboutClick?.Invoke(s, e)) { Font = _regularFont };

        _showExitPromptItem = new Forms.ToolStripMenuItem("🔄 重新启用退出提示") { Font = _regularFont };
        _showExitPromptItem.Click += (s, e) => ReenableExitPromptClick?.Invoke(s, e);

        _showDeletePromptItem = new Forms.ToolStripMenuItem("🗑️ 重新启用删除提示") { Font = _regularFont };
        _showDeletePromptItem.Click += (s, e) => ReenableDeletePromptClick?.Invoke(s, e);

        _restartTrayItem = new Forms.ToolStripMenuItem("🔁 重启托盘图标", null, (s, e) => RestartTrayClick?.Invoke(s, e)) { Font = _regularFont };

        _exitItem = new Forms.ToolStripMenuItem("❌ 退出") { Font = _boldFont };
        _exitItem.Click += (s, e) => ExitClick?.Invoke(s, e);

        _contextMenu.Items.AddRange(new Forms.ToolStripItem[]
        {
            _showHideItem,
            _newMemoItem,
            _toggleSettingsItem,
            new Forms.ToolStripSeparator(),
            _windowControlGroup,
            new Forms.ToolStripSeparator(),
            _toolsGroup,
            new Forms.ToolStripSeparator(),
            _aboutItem,
            _restartTrayItem,
            _showExitPromptItem,
            _showDeletePromptItem,
            _exitItem
        });
    }

    public void UpdateMenuTexts(Func<string, string> getLocalizedString)
    {
        try
        {
            if (_showHideItem != null) _showHideItem.Text = "🏠 " + getLocalizedString("Tray_ShowHide");
            if (_newMemoItem != null) _newMemoItem.Text = "📝 " + getLocalizedString("Tray_NewMemo");
            if (_toggleSettingsItem != null) _toggleSettingsItem.Text = "⚙️ " + getLocalizedString("Tray_Settings");
            if (_windowControlGroup != null) _windowControlGroup.Text = "🖼️ " + getLocalizedString("Tray_WindowControl");
            if (_topmostGroup != null) _topmostGroup.Text = "📌 " + getLocalizedString("Tray_TopmostMode");
            if (_topmostNormalItem != null) _topmostNormalItem.Text = getLocalizedString("Tray_TopmostNormal");
            if (_topmostDesktopItem != null) _topmostDesktopItem.Text = getLocalizedString("Tray_TopmostDesktop");
            if (_topmostAlwaysItem != null) _topmostAlwaysItem.Text = getLocalizedString("Tray_TopmostAlways");
            if (_positionGroup != null) _positionGroup.Text = "📍 " + getLocalizedString("Tray_Position");
            if (_quickPosGroup != null) _quickPosGroup.Text = getLocalizedString("Tray_QuickPosition");
            if (_trayPresetTopLeft != null) _trayPresetTopLeft.Text = getLocalizedString("Tray_Position_TopLeft");
            if (_trayPresetTopCenter != null) _trayPresetTopCenter.Text = getLocalizedString("Tray_Position_TopCenter");
            if (_trayPresetTopRight != null) _trayPresetTopRight.Text = getLocalizedString("Tray_Position_TopRight");
            if (_trayPresetCenter != null) _trayPresetCenter.Text = getLocalizedString("Tray_Position_Center");
            if (_trayPresetBottomLeft != null) _trayPresetBottomLeft.Text = getLocalizedString("Tray_Position_BottomLeft");
            if (_trayPresetBottomRight != null) _trayPresetBottomRight.Text = getLocalizedString("Tray_Position_BottomRight");
            if (_rememberPositionItem != null) _rememberPositionItem.Text = getLocalizedString("Tray_RememberPosition");
            if (_restorePositionItem != null) _restorePositionItem.Text = getLocalizedString("Tray_RestorePosition");
            if (_trayClickThroughItem != null) _trayClickThroughItem.Text = "👻 " + getLocalizedString("Tray_ClickThrough");
            if (_toolsGroup != null) _toolsGroup.Text = "🛠️ " + getLocalizedString("Tray_Tools");
            if (_exportNotesItem != null) _exportNotesItem.Text = "📤 " + getLocalizedString("Tray_Export");
            if (_importNotesItem != null) _importNotesItem.Text = "📥 " + getLocalizedString("Tray_Import");
            if (_clearContentItem != null) _clearContentItem.Text = "🗑️ " + getLocalizedString("Tray_Clear");
            if (_aboutItem != null) _aboutItem.Text = "ℹ️ " + getLocalizedString("Tray_About");
            if (_restartTrayItem != null) _restartTrayItem.Text = "🔁 " + getLocalizedString("Tray_Restart");
            if (_showExitPromptItem != null) _showExitPromptItem.Text = "🔄 " + getLocalizedString("Tray_ReenableExit");
            if (_showDeletePromptItem != null) _showDeletePromptItem.Text = "🗑️ " + getLocalizedString("Tray_ReenableDelete");
            if (_exitItem != null) _exitItem.Text = "❌ " + getLocalizedString("Tray_Exit");
        }
        catch
        {
            // 更新菜单文本失败，忽略错误
        }
    }

    private sealed class DarkTrayMenuRenderer : Forms.ToolStripProfessionalRenderer
    {
        public DarkTrayMenuRenderer() : base(new DarkColorTable())
        {
        }

        protected override void OnRenderMenuItemBackground(Forms.ToolStripItemRenderEventArgs e)
        {
            if (!e.Item.Selected && !e.Item.Pressed)
            {
                return;
            }

            var rect = new Rectangle(2, 0, e.Item.Width - 4, e.Item.Height);
            var color = e.Item.Pressed ? Color.FromArgb(0, 122, 204) : Color.FromArgb(62, 62, 66);

            using var brush = new SolidBrush(color);
            e.Graphics.FillRectangle(brush, rect);
        }

        protected override void OnRenderToolStripBackground(Forms.ToolStripRenderEventArgs e)
        {
            using var brush = new SolidBrush(Color.FromArgb(45, 45, 48));
            e.Graphics.FillRectangle(brush, 0, 0, e.ToolStrip.Width, e.ToolStrip.Height);
        }

        protected override void OnRenderToolStripBorder(Forms.ToolStripRenderEventArgs e)
        {
            using var pen = new Pen(Color.FromArgb(63, 63, 70), 1);
            var rect = new Rectangle(0, 0, e.ToolStrip.Width - 1, e.ToolStrip.Height - 1);
            e.Graphics.DrawRectangle(pen, rect);
        }

        protected override void OnRenderItemText(Forms.ToolStripItemTextRenderEventArgs e)
        {
            e.TextColor = Color.FromArgb(241, 241, 241);
            base.OnRenderItemText(e);
        }

        protected override void OnRenderSeparator(Forms.ToolStripSeparatorRenderEventArgs e)
        {
            var rect = new Rectangle(10, e.Item.Height / 2, e.Item.Width - 20, 1);
            using var brush = new SolidBrush(Color.FromArgb(63, 63, 70));
            e.Graphics.FillRectangle(brush, rect);
        }

        protected override void OnRenderItemCheck(Forms.ToolStripItemImageRenderEventArgs e)
        {
            if (e.Item is not Forms.ToolStripMenuItem menuItem)
            {
                base.OnRenderItemCheck(e);
                return;
            }

            var rect = new Rectangle(e.ImageRectangle.X - 2, e.ImageRectangle.Y - 2,
                e.ImageRectangle.Width + 4, e.ImageRectangle.Height + 4);

            using (var brush = new SolidBrush(Color.FromArgb(0, 122, 204)))
            {
                e.Graphics.FillRectangle(brush, rect);
            }

            using (var pen = new Pen(Color.White, 2))
            {
                var checkRect = e.ImageRectangle;
                var points = new[]
                {
                    new Point(checkRect.X + 3, checkRect.Y + checkRect.Height / 2),
                    new Point(checkRect.X + checkRect.Width / 2, checkRect.Y + checkRect.Height - 4),
                    new Point(checkRect.X + checkRect.Width - 3, checkRect.Y + 3)
                };
                e.Graphics.DrawLines(pen, points);
            }

            menuItem.Image = null;
        }
    }

    private sealed class DarkColorTable : Forms.ProfessionalColorTable
    {
        public override Color MenuItemSelected => Color.FromArgb(62, 62, 66);
        public override Color MenuItemBorder => Color.FromArgb(63, 63, 70);
        public override Color ToolStripDropDownBackground => Color.FromArgb(45, 45, 48);
    }

    private static Font CreateFont(string family, float size, FontStyle style)
    {
        try
        {
            return new Font(family, size, style);
        }
        catch
        {
            return new Font(FontFamily.GenericSansSerif, size, style);
        }
    }
}