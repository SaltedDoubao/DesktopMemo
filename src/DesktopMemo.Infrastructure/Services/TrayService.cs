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

    public bool IsClickThroughEnabled { get; private set; }

    public void Initialize()
    {
        if (_notifyIcon != null)
        {
            return;
        }

        _notifyIcon = new Forms.NotifyIcon
        {
            Text = "DesktopMemo 便签 - 桌面便签工具",
            Visible = true
        };

        SetTrayIcon();
        BuildContextMenu();
        _notifyIcon.ContextMenuStrip = _contextMenu;
        _notifyIcon.DoubleClick += (s, e) => TrayIconDoubleClick?.Invoke(s, e);
    }

    public void Show()
    {
        if (_notifyIcon != null)
        {
            _notifyIcon.Visible = true;
        }
    }

    public void Hide()
    {
        if (_notifyIcon != null)
        {
            _notifyIcon.Visible = false;
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
        if (_topmostNormalItem != null) _topmostNormalItem.Checked = mode == TopmostMode.Normal;
        if (_topmostDesktopItem != null) _topmostDesktopItem.Checked = mode == TopmostMode.Desktop;
        if (_topmostAlwaysItem != null) _topmostAlwaysItem.Checked = mode == TopmostMode.Always;
    }

    public void UpdateClickThroughState(bool enabled)
    {
        IsClickThroughEnabled = enabled;
        if (_trayClickThroughItem != null)
        {
            if (_trayClickThroughItem.CheckOnClick)
            {
                _trayClickThroughItem.Checked = enabled;
            }
            else
            {
                _trayClickThroughItem.Checked = enabled;
            }
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

        var showHideItem = new Forms.ToolStripMenuItem("🏠 显示/隐藏窗口") { Font = _boldFont };
        showHideItem.Click += (s, e) => ShowHideWindowClick?.Invoke(s, e);

        var newMemoItem = new Forms.ToolStripMenuItem("📝 新建便签") { Font = _regularFont };
        newMemoItem.Click += (s, e) => NewMemoClick?.Invoke(s, e);

        _toggleSettingsItem = new Forms.ToolStripMenuItem("⚙️ 设置") { Font = _regularFont };
        _toggleSettingsItem.Click += (s, e) => SettingsClick?.Invoke(s, e);

        var windowControlGroup = new Forms.ToolStripMenuItem("🖼️ 窗口控制") { Font = _regularFont };

        var topmostGroup = new Forms.ToolStripMenuItem("📌 置顶模式") { Font = _regularFont };
        _topmostNormalItem = new Forms.ToolStripMenuItem("普通模式", null, (s, e) => UpdateTopmostState(TopmostMode.Normal)) { Font = _regularFont };
        _topmostDesktopItem = new Forms.ToolStripMenuItem("桌面置顶", null, (s, e) => UpdateTopmostState(TopmostMode.Desktop)) { Font = _regularFont };
        _topmostAlwaysItem = new Forms.ToolStripMenuItem("总是置顶", null, (s, e) => UpdateTopmostState(TopmostMode.Always)) { Font = _regularFont };
        topmostGroup.DropDownItems.AddRange(new Forms.ToolStripItem[]
        {
            _topmostNormalItem,
            _topmostDesktopItem,
            _topmostAlwaysItem
        });

        var positionGroup = new Forms.ToolStripMenuItem("📍 窗口位置") { Font = _regularFont };
        var quickPosGroup = new Forms.ToolStripMenuItem("快速定位") { Font = _regularFont };

        _trayPresetTopLeft = new Forms.ToolStripMenuItem("左上角", null, (s, e) => MoveToPresetClick?.Invoke(s, "TopLeft")) { Font = _regularFont };
        _trayPresetTopCenter = new Forms.ToolStripMenuItem("顶部中央", null, (s, e) => MoveToPresetClick?.Invoke(s, "TopCenter")) { Font = _regularFont };
        _trayPresetTopRight = new Forms.ToolStripMenuItem("右上角", null, (s, e) => MoveToPresetClick?.Invoke(s, "TopRight")) { Font = _regularFont };
        _trayPresetCenter = new Forms.ToolStripMenuItem("屏幕中央", null, (s, e) => MoveToPresetClick?.Invoke(s, "Center")) { Font = _regularFont };
        _trayPresetBottomLeft = new Forms.ToolStripMenuItem("左下角", null, (s, e) => MoveToPresetClick?.Invoke(s, "BottomLeft")) { Font = _regularFont };
        _trayPresetBottomRight = new Forms.ToolStripMenuItem("右下角", null, (s, e) => MoveToPresetClick?.Invoke(s, "BottomRight")) { Font = _regularFont };

        quickPosGroup.DropDownItems.AddRange(new Forms.ToolStripItem[]
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

        positionGroup.DropDownItems.AddRange(new Forms.ToolStripItem[]
        {
            quickPosGroup,
            new Forms.ToolStripSeparator(),
            _rememberPositionItem,
            _restorePositionItem
        });

        _trayClickThroughItem = new Forms.ToolStripMenuItem("👻 穿透模式")
        {
            Font = _regularFont,
            CheckOnClick = true
        };
        _trayClickThroughItem.CheckedChanged += (s, e) => UpdateClickThroughState(_trayClickThroughItem.Checked);

        windowControlGroup.DropDownItems.AddRange(new Forms.ToolStripItem[]
        {
            topmostGroup,
            positionGroup,
            _trayClickThroughItem
        });

        var toolsGroup = new Forms.ToolStripMenuItem("🛠️ 工具") { Font = _regularFont };
        _exportNotesItem = new Forms.ToolStripMenuItem("📤 导出便签", null, (s, e) => ExportNotesClick?.Invoke(s, e)) { Font = _regularFont };
        _importNotesItem = new Forms.ToolStripMenuItem("📥 导入便签", null, (s, e) => ImportNotesClick?.Invoke(s, e)) { Font = _regularFont };
        _clearContentItem = new Forms.ToolStripMenuItem("🗑️ 清空内容", null, (s, e) => ClearContentClick?.Invoke(s, e)) { Font = _regularFont };
        toolsGroup.DropDownItems.AddRange(new Forms.ToolStripItem[]
        {
            _exportNotesItem,
            _importNotesItem,
            _clearContentItem
        });

        _aboutItem = new Forms.ToolStripMenuItem("ℹ️ 关于", null, (s, e) => AboutClick?.Invoke(s, e)) { Font = _regularFont };

        _showExitPromptItem = new Forms.ToolStripMenuItem("🔄 重新启用退出提示") { Font = _regularFont };
        _showExitPromptItem.Click += (s, e) => ShowBalloonTip("设置已更新", "已重新启用退出提示");

        _showDeletePromptItem = new Forms.ToolStripMenuItem("🗑️ 重新启用删除提示") { Font = _regularFont };
        _showDeletePromptItem.Click += (s, e) => ShowBalloonTip("设置已更新", "已重新启用删除确认提示");

        _restartTrayItem = new Forms.ToolStripMenuItem("🔁 重启托盘图标", null, (s, e) => RestartTrayClick?.Invoke(s, e)) { Font = _regularFont };

        var exitItem = new Forms.ToolStripMenuItem("❌ 退出") { Font = _boldFont };
        exitItem.Click += (s, e) => ExitClick?.Invoke(s, e);

        _contextMenu.Items.AddRange(new Forms.ToolStripItem[]
        {
            showHideItem,
            newMemoItem,
            _toggleSettingsItem,
            new Forms.ToolStripSeparator(),
            windowControlGroup,
            new Forms.ToolStripSeparator(),
            toolsGroup,
            new Forms.ToolStripSeparator(),
            _aboutItem,
            _restartTrayItem,
            _showExitPromptItem,
            _showDeletePromptItem,
            exitItem
        });
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