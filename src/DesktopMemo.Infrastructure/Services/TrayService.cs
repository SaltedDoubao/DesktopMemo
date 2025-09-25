using System;
using System.Diagnostics;
using System.Drawing;
using DesktopMemo.Core.Contracts;
using Forms = System.Windows.Forms;

namespace DesktopMemo.Infrastructure.Services;

public class TrayService : ITrayService
{
    private Forms.NotifyIcon? _notifyIcon;
    private bool _isDisposed = false;

    public event EventHandler? TrayIconDoubleClick;
    public event EventHandler? ShowHideWindowClick;
    public event EventHandler? NewMemoClick;
    public event EventHandler? SettingsClick;
    public event EventHandler? ExitClick;

    public void Initialize()
    {
        if (_notifyIcon != null) return;

        _notifyIcon = new Forms.NotifyIcon
        {
            Text = "DesktopMemo - 桌面便签工具",
            Visible = false,
            Icon = ExtractIconFromExecutable()
        };

        CreateContextMenu();
        _notifyIcon.DoubleClick += (s, e) => TrayIconDoubleClick?.Invoke(s, e);
    }

    private void CreateContextMenu()
    {
        if (_notifyIcon == null) return;

        var menu = new Forms.ContextMenuStrip();

        // 应用暗色主题
        menu.Renderer = new DarkTrayMenuRenderer();
        menu.BackColor = Color.FromArgb(45, 45, 48);
        menu.ForeColor = Color.FromArgb(241, 241, 241);
        menu.Padding = new Forms.Padding(8, 4, 8, 4);

        var regularFont = new Font("Microsoft YaHei", 9F, FontStyle.Regular);
        var boldFont = new Font("Microsoft YaHei", 9F, FontStyle.Bold);

        // 主要功能组
        var showHideItem = new Forms.ToolStripMenuItem("🏠 显示/隐藏窗口")
        {
            Font = boldFont
        };
        showHideItem.Click += (s, e) => ShowHideWindowClick?.Invoke(s, e);

        var newNoteItem = new Forms.ToolStripMenuItem("📝 新建便签")
        {
            Font = regularFont
        };
        newNoteItem.Click += (s, e) => NewMemoClick?.Invoke(s, e);

        var settingsItem = new Forms.ToolStripMenuItem("⚙️ 设置")
        {
            Font = regularFont
        };
        settingsItem.Click += (s, e) => SettingsClick?.Invoke(s, e);

        var separator = new Forms.ToolStripSeparator();

        var exitItem = new Forms.ToolStripMenuItem("❌ 退出")
        {
            Font = boldFont
        };
        exitItem.Click += (s, e) => ExitClick?.Invoke(s, e);

        menu.Items.AddRange(new Forms.ToolStripItem[]
        {
            showHideItem, newNoteItem, settingsItem, separator, exitItem
        });

        _notifyIcon.ContextMenuStrip = menu;
    }

    private Icon? ExtractIconFromExecutable()
    {
        try
        {
            var currentProcess = Process.GetCurrentProcess();
            var mainModule = currentProcess.MainModule;
            if (mainModule?.FileName != null)
            {
                return Icon.ExtractAssociatedIcon(mainModule.FileName);
            }
        }
        catch
        {
            // 忽略错误，使用默认图标
        }
        return SystemIcons.Application;
    }

    public void Show()
    {
        if (_notifyIcon != null)
            _notifyIcon.Visible = true;
    }

    public void Hide()
    {
        if (_notifyIcon != null)
            _notifyIcon.Visible = false;
    }

    public void ShowBalloonTip(string title, string text, int timeout = 2000)
    {
        _notifyIcon?.ShowBalloonTip(timeout, title, text, Forms.ToolTipIcon.Info);
    }

    public void UpdateText(string text)
    {
        if (_notifyIcon != null)
            _notifyIcon.Text = text;
    }

    public void Dispose()
    {
        if (_isDisposed) return;

        _notifyIcon?.Dispose();
        _notifyIcon = null;
        _isDisposed = true;
        GC.SuppressFinalize(this);
    }

    ~TrayService()
    {
        Dispose();
    }

    /// <summary>
    /// 暗色主题托盘菜单渲染器
    /// </summary>
    private class DarkTrayMenuRenderer : Forms.ToolStripProfessionalRenderer
    {
        public DarkTrayMenuRenderer() : base(new DarkColorTable()) { }

        protected override void OnRenderMenuItemBackground(Forms.ToolStripItemRenderEventArgs e)
        {
            if (!e.Item.Selected && !e.Item.Pressed)
                return;

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

    /// <summary>
    /// 暗色主题颜色表
    /// </summary>
    private class DarkColorTable : Forms.ProfessionalColorTable
    {
        public override Color MenuItemSelected => Color.FromArgb(62, 62, 66);
        public override Color MenuItemBorder => Color.FromArgb(63, 63, 70);
        public override Color MenuBorder => Color.FromArgb(63, 63, 70);
        public override Color MenuItemSelectedGradientBegin => Color.FromArgb(62, 62, 66);
        public override Color MenuItemSelectedGradientEnd => Color.FromArgb(62, 62, 66);
        public override Color MenuItemPressedGradientBegin => Color.FromArgb(0, 122, 204);
        public override Color MenuItemPressedGradientEnd => Color.FromArgb(0, 122, 204);
        public override Color ToolStripDropDownBackground => Color.FromArgb(45, 45, 48);
        public override Color ImageMarginGradientBegin => Color.FromArgb(45, 45, 48);
        public override Color ImageMarginGradientMiddle => Color.FromArgb(45, 45, 48);
        public override Color ImageMarginGradientEnd => Color.FromArgb(45, 45, 48);
        public override Color SeparatorDark => Color.FromArgb(63, 63, 70);
        public override Color SeparatorLight => Color.FromArgb(63, 63, 70);
    }
}