using System;
using System.Diagnostics;
using System.Drawing;
using DesktopMemo.Core.Contracts;
using Forms = System.Windows.Forms;

namespace DesktopMemo.Infrastructure.Services;

public class TrayService : ITrayService
{
    private Forms.NotifyIcon? _notifyIcon;
    private bool _isDisposed;
    private Forms.ToolStripMenuItem? _topmostNormalItem;
    private Forms.ToolStripMenuItem? _topmostDesktopItem;
    private Forms.ToolStripMenuItem? _topmostAlwaysItem;
    private Forms.ToolStripMenuItem? _clickThroughItem;

    public event EventHandler? TrayIconDoubleClick;
    public event EventHandler? ShowHideWindowClick;
    public event EventHandler? NewMemoClick;
    public event EventHandler? SettingsClick;
    public event EventHandler? ExitClick;

    public void Initialize()
    {
        if (_notifyIcon != null)
        {
            return;
        }

        _notifyIcon = new Forms.NotifyIcon
        {
            Text = "DesktopMemo",
            Visible = false,
            Icon = ExtractIconFromExecutable()
        };

        CreateContextMenu();
        _notifyIcon.DoubleClick += (s, e) => TrayIconDoubleClick?.Invoke(s, e);
    }

    private void CreateContextMenu()
    {
        if (_notifyIcon == null)
        {
            return;
        }

        var menu = new Forms.ContextMenuStrip();
        menu.Renderer = new DarkTrayMenuRenderer();
        menu.BackColor = Color.FromArgb(45, 45, 48);
        menu.ForeColor = Color.FromArgb(241, 241, 241);
        menu.Padding = new Forms.Padding(8, 4, 8, 4);

        var bold = new Font("Microsoft YaHei", 9F, FontStyle.Bold);
        var regular = new Font("Microsoft YaHei", 9F, FontStyle.Regular);

        var showHideItem = new Forms.ToolStripMenuItem("🏠 显示/隐藏窗口") { Font = bold };
        showHideItem.Click += (s, e) => ShowHideWindowClick?.Invoke(s, e);

        var newMemoItem = new Forms.ToolStripMenuItem("📝 新建便签") { Font = regular };
        newMemoItem.Click += (s, e) => NewMemoClick?.Invoke(s, e);

        var settingsItem = new Forms.ToolStripMenuItem("⚙️ 设置") { Font = regular };
        settingsItem.Click += (s, e) => SettingsClick?.Invoke(s, e);

        var topmostMenu = new Forms.ToolStripMenuItem("窗口置顶模式") { Font = regular };
        _topmostNormalItem = new Forms.ToolStripMenuItem("普通模式", null, (s, e) => UpdateTopmostState(TopmostMode.Normal));
        _topmostDesktopItem = new Forms.ToolStripMenuItem("桌面置顶", null, (s, e) => UpdateTopmostState(TopmostMode.Desktop));
        _topmostAlwaysItem = new Forms.ToolStripMenuItem("永远置顶", null, (s, e) => UpdateTopmostState(TopmostMode.Always));
        topmostMenu.DropDownItems.AddRange(new Forms.ToolStripItem[]
        {
            _topmostNormalItem,
            _topmostDesktopItem,
            _topmostAlwaysItem
        });

        _clickThroughItem = new Forms.ToolStripMenuItem("穿透模式")
        {
            Checked = false
        };
        _clickThroughItem.Click += (s, e) => UpdateClickThroughState(!_clickThroughItem.Checked);

        var exitItem = new Forms.ToolStripMenuItem("❌ 退出") { Font = bold };
        exitItem.Click += (s, e) => ExitClick?.Invoke(s, e);

        menu.Items.AddRange(new Forms.ToolStripItem[]
        {
            showHideItem,
            newMemoItem,
            settingsItem,
            new Forms.ToolStripSeparator(),
            topmostMenu,
            _clickThroughItem,
            new Forms.ToolStripSeparator(),
            exitItem
        });

        _notifyIcon.ContextMenuStrip = menu;
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
        if (_clickThroughItem != null)
        {
            _clickThroughItem.Checked = enabled;
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

        _isDisposed = true;
        GC.SuppressFinalize(this);
    }

    ~TrayService()
    {
        Dispose();
    }

    private static Icon ExtractIconFromExecutable()
    {
        try
        {
            var process = Process.GetCurrentProcess();
            var module = process.MainModule;
            if (module?.FileName != null)
            {
                var icon = Icon.ExtractAssociatedIcon(module.FileName);
                if (icon != null)
                {
                    return icon;
                }
            }
        }
        catch
        {
            // ignore
        }

        return SystemIcons.Application;
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
    }

    private sealed class DarkColorTable : Forms.ProfessionalColorTable
    {
        public override Color MenuItemSelected => Color.FromArgb(62, 62, 66);
        public override Color MenuItemBorder => Color.FromArgb(63, 63, 70);
        public override Color ToolStripDropDownBackground => Color.FromArgb(45, 45, 48);
    }
}