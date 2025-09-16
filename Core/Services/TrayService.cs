using System;
using System.Drawing;
using System.Windows;
using System.Windows.Forms;
using DesktopMemo.Core.Interfaces;
using Application = System.Windows.Application;

namespace DesktopMemo.Core.Services
{
    public class TrayService : ITrayService
    {
        private NotifyIcon? _notifyIcon;
        private ContextMenuStrip? _contextMenu;
        private readonly ISettingsService _settingsService;
        private readonly IWindowManagementService _windowService;

        public event EventHandler? DoubleClick;
        public event EventHandler? ShowHideRequested;
        public event EventHandler? ExitRequested;
        public event EventHandler<TopmostMode>? TopmostModeChanged;
        public event EventHandler? NewMemoRequested;
        public event EventHandler? ClickThroughToggled;

        public ContextMenuStrip? ContextMenu => _contextMenu;

        public TrayService(ISettingsService settingsService, IWindowManagementService windowService)
        {
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            _windowService = windowService ?? throw new ArgumentNullException(nameof(windowService));
        }

        public void Initialize()
        {
            _notifyIcon = new NotifyIcon
            {
                Text = "DesktopMemo 便签 - 桌面便签工具",
                Visible = true
            };

            // Load icon
            LoadTrayIcon();

            // Create context menu
            CreateContextMenu();

            // Wire up events
            _notifyIcon.DoubleClick += OnDoubleClick;
        }

        private void LoadTrayIcon()
        {
            if (_notifyIcon == null) return;

            try
            {
                var iconStream = Application.GetResourceStream(new Uri("pack://application:,,,/Resources/AppIcon.ico"));
                if (iconStream != null)
                {
                    _notifyIcon.Icon = new Icon(iconStream.Stream);
                }
            }
            catch
            {
                // Use default icon if resource not found
                _notifyIcon.Icon = SystemIcons.Application;
            }
        }

        private void CreateContextMenu()
        {
            _contextMenu = new ContextMenuStrip();

            // Apply dark theme if enabled
            if (_settingsService.GetSetting("Theme", "Dark") == "Dark")
            {
                ApplyDarkTheme(_contextMenu);
            }

            // Show/Hide
            var showHideItem = new ToolStripMenuItem("显示/隐藏");
            showHideItem.Click += (s, e) => ShowHideRequested?.Invoke(this, EventArgs.Empty);
            _contextMenu.Items.Add(showHideItem);

            _contextMenu.Items.Add(new ToolStripSeparator());

            // Topmost Mode submenu
            var topmostMenu = new ToolStripMenuItem("置顶模式");

            var normalModeItem = new ToolStripMenuItem("普通模式");
            normalModeItem.Click += (s, e) => TopmostModeChanged?.Invoke(this, TopmostMode.Normal);
            topmostMenu.DropDownItems.Add(normalModeItem);

            var desktopModeItem = new ToolStripMenuItem("桌面置顶");
            desktopModeItem.Click += (s, e) => TopmostModeChanged?.Invoke(this, TopmostMode.Desktop);
            topmostMenu.DropDownItems.Add(desktopModeItem);

            var alwaysModeItem = new ToolStripMenuItem("总是置顶");
            alwaysModeItem.Click += (s, e) => TopmostModeChanged?.Invoke(this, TopmostMode.Always);
            topmostMenu.DropDownItems.Add(alwaysModeItem);

            _contextMenu.Items.Add(topmostMenu);

            // Click Through Mode
            var clickThroughItem = new ToolStripMenuItem("穿透模式");
            clickThroughItem.Click += (s, e) => ClickThroughToggled?.Invoke(this, EventArgs.Empty);
            _contextMenu.Items.Add(clickThroughItem);

            _contextMenu.Items.Add(new ToolStripSeparator());

            // New Memo
            var newMemoItem = new ToolStripMenuItem("新建便签");
            newMemoItem.Click += (s, e) => NewMemoRequested?.Invoke(this, EventArgs.Empty);
            _contextMenu.Items.Add(newMemoItem);

            _contextMenu.Items.Add(new ToolStripSeparator());

            // Exit
            var exitItem = new ToolStripMenuItem("退出");
            exitItem.Click += (s, e) => ExitRequested?.Invoke(this, EventArgs.Empty);
            _contextMenu.Items.Add(exitItem);

            if (_notifyIcon != null)
            {
                _notifyIcon.ContextMenuStrip = _contextMenu;
            }
        }

        private void ApplyDarkTheme(ContextMenuStrip menu)
        {
            menu.Renderer = new DarkMenuRenderer();
            menu.BackColor = Color.FromArgb(30, 30, 30);
            menu.ForeColor = Color.White;

            foreach (ToolStripItem item in menu.Items)
            {
                item.ForeColor = Color.White;
                if (item is ToolStripMenuItem menuItem)
                {
                    ApplyDarkThemeToMenuItem(menuItem);
                }
            }
        }

        private void ApplyDarkThemeToMenuItem(ToolStripMenuItem menuItem)
        {
            menuItem.BackColor = Color.FromArgb(30, 30, 30);
            menuItem.ForeColor = Color.White;

            if (menuItem.HasDropDownItems)
            {
                menuItem.DropDown.BackColor = Color.FromArgb(30, 30, 30);
                menuItem.DropDown.ForeColor = Color.White;
                menuItem.DropDown.Renderer = new DarkMenuRenderer();

                foreach (ToolStripItem item in menuItem.DropDownItems)
                {
                    item.ForeColor = Color.White;
                    if (item is ToolStripMenuItem subMenuItem)
                    {
                        ApplyDarkThemeToMenuItem(subMenuItem);
                    }
                }
            }
        }

        public void ShowTrayIcon()
        {
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = true;
            }
        }

        public void HideTrayIcon()
        {
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
            }
        }

        public void UpdateTooltip(string text)
        {
            if (_notifyIcon != null)
            {
                _notifyIcon.Text = text.Length > 128 ? text.Substring(0, 128) : text;
            }
        }

        public void ShowBalloonTip(string title, string text, ToolTipIcon icon = ToolTipIcon.Info)
        {
            _notifyIcon?.ShowBalloonTip(3000, title, text, icon);
        }

        private void OnDoubleClick(object? sender, EventArgs e)
        {
            DoubleClick?.Invoke(this, e);
            ShowHideRequested?.Invoke(this, e);
        }

        public void Dispose()
        {
            _contextMenu?.Dispose();
            _notifyIcon?.Dispose();
        }

        // Dark theme renderer for context menu
        private class DarkMenuRenderer : ToolStripProfessionalRenderer
        {
            protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
            {
                if (e.Item.Selected)
                {
                    var rect = new Rectangle(0, 0, e.Item.Width, e.Item.Height);
                    using (var brush = new SolidBrush(Color.FromArgb(50, 50, 50)))
                    {
                        e.Graphics.FillRectangle(brush, rect);
                    }
                }
                else
                {
                    base.OnRenderMenuItemBackground(e);
                }
            }

            protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
            {
                var rect = new Rectangle(30, 3, e.Item.Width - 60, 1);
                using (var pen = new Pen(Color.FromArgb(60, 60, 60)))
                {
                    e.Graphics.DrawLine(pen, rect.Left, rect.Top, rect.Right, rect.Top);
                }
            }
        }
    }
}