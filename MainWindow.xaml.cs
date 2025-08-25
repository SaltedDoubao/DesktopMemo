using System;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Forms = System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace DesktopMemo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly string _appDataDir;
        private readonly string _noteFilePath;
        private readonly string _memosFilePath;
        private readonly string _settingsFilePath;
        private Forms.NotifyIcon _notifyIcon = null!;
        private bool _isLoadedFromDisk;
        // 备忘录管理
        private List<MemoModel> _memos = new List<MemoModel>();
        private MemoModel? _currentMemo = null;
        private bool _isEditMode = false;
        
        // 窗口位置管理
        private System.Windows.Point _savedPosition;
        private bool _positionRemembered = false;
        private bool _autoRestorePositionEnabled = true; // 默认启用自动恢复位置
        private System.Windows.Threading.DispatcherTimer _positionUpdateTimer;
        private System.Windows.Threading.DispatcherTimer _autoSavePositionTimer;
        
        // 窗口置顶模式枚举
        public enum TopmostMode
        {
            Normal,     // 普通模式，不置顶
            Desktop,    // 桌面层面置顶
            Always      // 总是置顶
        }
        
        private TopmostMode _currentTopmostMode = TopmostMode.Desktop;
        private System.Windows.Threading.DispatcherTimer _desktopModeTimer;
        
        // 退出提示设置
        private bool _showExitPrompt = true; // 默认显示退出提示
        private bool _showDeletePrompt = true; // 默认显示删除提示
        private bool _isHandlingActivation = false;
        private bool _isClickThroughEnabled = false;
        private bool _isSettingsPanelVisible = false;
        private Forms.ToolStripMenuItem _trayClickThroughItem = null!;
        private Forms.ToolStripMenuItem _normalModeMenuItem = null!;
        private Forms.ToolStripMenuItem _desktopModeMenuItem = null!;
        private Forms.ToolStripMenuItem _alwaysModeMenuItem = null!;
        
        // 窗口固定状态
        private bool _isWindowPinned = false;

        // 公共属性
        public TopmostMode CurrentTopmostMode => _currentTopmostMode;
        public bool IsClickThroughEnabled 
        { 
            get => _isClickThroughEnabled; 
            set => _isClickThroughEnabled = value; 
        }

        // 公共方法
        public void SetTopmostMode(TopmostMode mode)
        {
            var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            if (hwnd == IntPtr.Zero) return;

            _currentTopmostMode = mode;
            
            switch (mode)
            {
                case TopmostMode.Normal:
                    // 普通模式：取消置顶
                    Topmost = false;
                    SetWindowPos(hwnd, HWND_NOTOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
                    break;
                    
                case TopmostMode.Desktop:
                    // 桌面层面置顶：将窗口放在桌面层之上，但会被应用程序窗口遮挡
                    Topmost = false;
                    // 先取消TopMost状态
                    SetWindowPos(hwnd, HWND_NOTOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
                    // 将窗口设置到底层
                    SetWindowPos(hwnd, HWND_BOTTOM, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
                    // 稍微提升，使其在桌面之上但在应用程序窗口之下
                    IntPtr progmanHwnd = FindWindow("Progman", "Program Manager");
                    if (progmanHwnd != IntPtr.Zero)
                    {
                        SetWindowPos(hwnd, progmanHwnd, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
                    }
                    break;
                    
                case TopmostMode.Always:
                    // 总是置顶：传统的TopMost行为
                    Topmost = true;
                    SetWindowPos(hwnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
                    break;
            }
            
            // 同步UI状态
            SyncTopmostToggle();
        }

        public void SetNoteTextBoxHitTest(bool enabled)
        {
            NoteTextBox.IsHitTestVisible = enabled;
        }

        private void ToggleSettingsPanel()
        {
            _isSettingsPanelVisible = !_isSettingsPanelVisible;
            
            if (_isSettingsPanelVisible)
            {
                // 显示设置面板并播放动画
                SettingsPanel.Visibility = Visibility.Visible;
                
                // 播放滑入动画
                if (FindResource("SlideInAnimation") is System.Windows.Media.Animation.Storyboard slideInStoryboard)
                {
                    System.Windows.Media.Animation.Storyboard.SetTarget(slideInStoryboard, SettingsPanel);
                    slideInStoryboard.Begin();
                }
                
                // 初始化设置控件状态
                InitializeSettingsControls();
            }
            else
            {
                // 播放滑出动画后隐藏面板
                if (FindResource("SlideOutAnimation") is System.Windows.Media.Animation.Storyboard slideOutStoryboard)
                {
                    System.Windows.Media.Animation.Storyboard.SetTarget(slideOutStoryboard, SettingsPanel);
                    slideOutStoryboard.Completed += (s, e) =>
                    {
                        SettingsPanel.Visibility = Visibility.Collapsed;
                    };
                    slideOutStoryboard.Begin();
                }
                else
                {
                    // 如果未找到动画，直接隐藏
                    SettingsPanel.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void InitializeSettingsControls()
        {
            // 设置当前置顶模式（不触发事件）
            NormalModeRadio.Checked -= TopmostModeRadio_Checked;
            DesktopModeRadio.Checked -= TopmostModeRadio_Checked;
            AlwaysModeRadio.Checked -= TopmostModeRadio_Checked;
            
            switch (_currentTopmostMode)
            {
                case TopmostMode.Normal:
                    NormalModeRadio.IsChecked = true;
                    break;
                case TopmostMode.Desktop:
                    DesktopModeRadio.IsChecked = true;
                    break;
                case TopmostMode.Always:
                    AlwaysModeRadio.IsChecked = true;
                    break;
            }
            
            // 重新添加事件处理
            NormalModeRadio.Checked += TopmostModeRadio_Checked;
            DesktopModeRadio.Checked += TopmostModeRadio_Checked;
            AlwaysModeRadio.Checked += TopmostModeRadio_Checked;

            // 设置透明度
            if (OpacitySlider != null)
            {
                OpacitySlider.Value = this.Opacity;
                OpacityValueText.Text = $"{(int)(this.Opacity * 100)}%";
            }
            
            // 设置穿透模式
            if (ClickThroughCheckBox != null)
            {
                ClickThroughCheckBox.IsChecked = _isClickThroughEnabled;
            }
            
            // 设置开机自启动状态
            if (AutoStartCheckBox != null)
            {
                // 暂时移除事件处理，避免触发
                AutoStartCheckBox.Checked -= AutoStartCheckBox_Checked;
                AutoStartCheckBox.Unchecked -= AutoStartCheckBox_Unchecked;
                
                AutoStartCheckBox.IsChecked = IsAutoStartEnabled();
                
                // 重新添加事件处理
                AutoStartCheckBox.Checked += AutoStartCheckBox_Checked;
                AutoStartCheckBox.Unchecked += AutoStartCheckBox_Unchecked;
            }
            
            // 同步托盘菜单状态
            if (_trayClickThroughItem != null)
            {
                _trayClickThroughItem.Checked = _isClickThroughEnabled;
            }
            
            // 更新应用信息
            if (AppInfoText != null)
            {
                var appDataDir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
                AppInfoText.Text = $"版本：1.0 | 数据目录：{appDataDir}";
            }
            
            // 更新状态信息
            if (StatusText != null)
            {
                StatusText.Text = "就绪";
            }
            
            // 初始化位置相关控件
            InitializePositionControls();
        }

        // Windows消息常量
        private const int WM_ACTIVATE = 0x0006;
        private const int WA_INACTIVE = 0;
        private const int WA_ACTIVE = 1;
        private const int WA_CLICKACTIVE = 2;

        public MainWindow()
        {
            try
            {
                InitializeComponent();
                _appDataDir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
                _noteFilePath = System.IO.Path.Combine(_appDataDir, "notes.json");
                _memosFilePath = System.IO.Path.Combine(_appDataDir, "memos.json");
                _settingsFilePath = System.IO.Path.Combine(_appDataDir, "settings.json");

                Directory.CreateDirectory(_appDataDir);
            
                // 监听窗口激活事件，防止在桌面模式下被提升到前台
                this.Activated += MainWindow_Activated;
                this.Deactivated += MainWindow_Deactivated;
            
                // 初始化桌面模式定时器
                _desktopModeTimer = new System.Windows.Threading.DispatcherTimer();
                _desktopModeTimer.Interval = TimeSpan.FromMilliseconds(1); // 立即重新设置层级
                _desktopModeTimer.Tick += (s, e) =>
                {
                    _desktopModeTimer.Stop();
                    if (_currentTopmostMode == TopmostMode.Desktop)
                    {
                        ReapplyDesktopMode();
                    }
                };
                
                // 初始化位置更新定时器
                _positionUpdateTimer = new System.Windows.Threading.DispatcherTimer();
                _positionUpdateTimer.Interval = TimeSpan.FromMilliseconds(500); // 每500ms更新一次位置显示
                _positionUpdateTimer.Tick += PositionUpdateTimer_Tick;
                
                // 初始化自动保存位置定时器（防抖）
                _autoSavePositionTimer = new System.Windows.Threading.DispatcherTimer();
                _autoSavePositionTimer.Interval = TimeSpan.FromMilliseconds(1000); // 停止移动1秒后自动保存
                _autoSavePositionTimer.Tick += AutoSavePositionTimer_Tick;
            
                ConfigureWindow();
                ConfigureTrayIcon();
                LoadMemosFromDisk();
                LoadSettingsFromDisk();
                
                // 监听窗口位置变化
                this.LocationChanged += MainWindow_LocationChanged;
                
                // 在所有控件初始化完成后设置默认状态
                this.Loaded += (s, e) => 
                {
                    // 设置默认的桌面置顶RadioButton选中状态
                    if (DesktopModeRadio != null)
                    {
                        DesktopModeRadio.IsChecked = true;
                    }
                    
                    // 启动位置更新定时器
                    _positionUpdateTimer.Start();
                    UpdateCurrentPositionDisplay();
                    
                    // 初始化备忘录界面
                    RefreshMemoList();
                    ShowMemoList();
                    
                    // 播放窗口淡入动画
                    if (FindResource("FadeInAnimation") is System.Windows.Media.Animation.Storyboard fadeInStoryboard)
                    {
                        System.Windows.Media.Animation.Storyboard.SetTarget(fadeInStoryboard, this);
                        fadeInStoryboard.Begin();
                    }
                };
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"初始化错误: {ex.Message}\n\n详细信息:\n{ex.StackTrace}", 
                    "DesktopMemo启动失败", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Error);
                throw;
            }
        }

        private void ConfigureWindow()
        {
            // 窗口加载完成后设置默认的桌面层面置顶模式和消息钩子
            this.Loaded += (s, e) => 
            {
                SetTopmostMode(_currentTopmostMode);
                AddWindowMessageHook();
            };
        }

        /// <summary>
        /// 添加窗口消息钩子来处理激活消息
        /// </summary>
        private void AddWindowMessageHook()
        {
            var hwndSource = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
            hwndSource?.AddHook(WndProc);
        }

        /// <summary>
        /// 窗口消息处理器
        /// </summary>
        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_ACTIVATE && _currentTopmostMode == TopmostMode.Desktop && !_isHandlingActivation)
            {
                int activationType = wParam.ToInt32() & 0xFFFF;
                
                // 如果窗口被激活（通过点击任务栏或Alt+Tab），延迟重新设置层级
                if (activationType == WA_ACTIVE || activationType == WA_CLICKACTIVE)
                {
                    _isHandlingActivation = true;
                    
                    // 立即重新设置层级，无视觉延迟
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        ReapplyDesktopMode();
                        _isHandlingActivation = false;
                    }), System.Windows.Threading.DispatcherPriority.Send);
                }
            }
            
            return IntPtr.Zero;
        }

        private void ConfigureTrayIcon()
        {
            _notifyIcon = new Forms.NotifyIcon();
            _notifyIcon.Text = "DesktopMemo 便签 - 桌面便签工具";
            _notifyIcon.Visible = true;
            
            // 设置更好的图标（如果有自定义图标文件可以替换）
            _notifyIcon.Icon = System.Drawing.SystemIcons.Application;

            var menu = new Forms.ContextMenuStrip();
            
            // 主要功能组
            var showHideItem = new Forms.ToolStripMenuItem("🏠 显示/隐藏窗口");
            showHideItem.Font = new System.Drawing.Font("Microsoft YaHei", 9F, System.Drawing.FontStyle.Bold);
            showHideItem.Click += (s, e) => ToggleWindowVisibility();
            
            var newNoteItem = new Forms.ToolStripMenuItem("📝 新建便签");
            newNoteItem.Click += (s, e) => CreateNewNote();
            
            var settingsItem = new Forms.ToolStripMenuItem("⚙️ 设置");
            settingsItem.Click += (s, e) => {
                if (Visibility == Visibility.Hidden) Show();
                ToggleSettingsPanel();
            };
            
            // 分隔符
            var separator1 = new Forms.ToolStripSeparator();
            
            // 窗口控制组
            var windowControlGroup = new Forms.ToolStripMenuItem("🖼️ 窗口控制");
            
            // 置顶模式子菜单
            var topmostGroup = new Forms.ToolStripMenuItem("📌 置顶模式");
            var normalModeItem = new Forms.ToolStripMenuItem("普通模式");
            var desktopModeItem = new Forms.ToolStripMenuItem("桌面置顶");
            var alwaysModeItem = new Forms.ToolStripMenuItem("总是置顶");
            
            normalModeItem.Click += (s, e) => {
                SetTopmostMode(TopmostMode.Normal);
                UpdateTopmostMenuItems();
            };
            desktopModeItem.Click += (s, e) => {
                SetTopmostMode(TopmostMode.Desktop);
                UpdateTopmostMenuItems();
            };
            alwaysModeItem.Click += (s, e) => {
                SetTopmostMode(TopmostMode.Always);
                UpdateTopmostMenuItems();
            };
            
            topmostGroup.DropDownItems.AddRange(new Forms.ToolStripItem[] {
                normalModeItem, desktopModeItem, alwaysModeItem
            });
            
            // 透明度控制
            var opacityGroup = new Forms.ToolStripMenuItem("🔍 透明度");
            var opacity100Item = new Forms.ToolStripMenuItem("100% (不透明)");
            var opacity90Item = new Forms.ToolStripMenuItem("90%");
            var opacity80Item = new Forms.ToolStripMenuItem("80%");
            var opacity70Item = new Forms.ToolStripMenuItem("70%");
            var opacity60Item = new Forms.ToolStripMenuItem("60%");
            var opacity50Item = new Forms.ToolStripMenuItem("50%");
            var opacity40Item = new Forms.ToolStripMenuItem("40%");
            var opacity30Item = new Forms.ToolStripMenuItem("30%");
            var opacity20Item = new Forms.ToolStripMenuItem("20%");
            var opacity10Item = new Forms.ToolStripMenuItem("10% (几乎透明)");
            
            opacity100Item.Click += (s, e) => SetWindowOpacity(1.0);
            opacity90Item.Click += (s, e) => SetWindowOpacity(0.9);
            opacity80Item.Click += (s, e) => SetWindowOpacity(0.8);
            opacity70Item.Click += (s, e) => SetWindowOpacity(0.7);
            opacity60Item.Click += (s, e) => SetWindowOpacity(0.6);
            opacity50Item.Click += (s, e) => SetWindowOpacity(0.5);
            opacity40Item.Click += (s, e) => SetWindowOpacity(0.4);
            opacity30Item.Click += (s, e) => SetWindowOpacity(0.3);
            opacity20Item.Click += (s, e) => SetWindowOpacity(0.2);
            opacity10Item.Click += (s, e) => SetWindowOpacity(0.1);
            
            opacityGroup.DropDownItems.AddRange(new Forms.ToolStripItem[] {
                opacity100Item, opacity90Item, opacity80Item, opacity70Item, opacity60Item,
                new Forms.ToolStripSeparator(),
                opacity50Item, opacity40Item, opacity30Item, opacity20Item, opacity10Item
            });
            
            // 窗口位置控制
            var positionGroup = new Forms.ToolStripMenuItem("📍 窗口位置");
            var quickPosGroup = new Forms.ToolStripMenuItem("快速定位");
            
            var topLeftItem = new Forms.ToolStripMenuItem("左上角");
            var topCenterItem = new Forms.ToolStripMenuItem("顶部中央");
            var topRightItem = new Forms.ToolStripMenuItem("右上角");
            var centerItem = new Forms.ToolStripMenuItem("屏幕中央");
            var bottomLeftItem = new Forms.ToolStripMenuItem("左下角");
            var bottomRightItem = new Forms.ToolStripMenuItem("右下角");
            
            topLeftItem.Click += (s, e) => MoveToTrayPresetPosition("TopLeft");
            topCenterItem.Click += (s, e) => MoveToTrayPresetPosition("TopCenter");
            topRightItem.Click += (s, e) => MoveToTrayPresetPosition("TopRight");
            centerItem.Click += (s, e) => MoveToTrayPresetPosition("Center");
            bottomLeftItem.Click += (s, e) => MoveToTrayPresetPosition("BottomLeft");
            bottomRightItem.Click += (s, e) => MoveToTrayPresetPosition("BottomRight");
            
            quickPosGroup.DropDownItems.AddRange(new Forms.ToolStripItem[] {
                topLeftItem, topCenterItem, topRightItem, new Forms.ToolStripSeparator(),
                centerItem, new Forms.ToolStripSeparator(),
                bottomLeftItem, bottomRightItem
            });
            
            var rememberPosItem = new Forms.ToolStripMenuItem("记住当前位置");
            var restorePosItem = new Forms.ToolStripMenuItem("恢复保存位置");
            
            rememberPosItem.Click += (s, e) => {
                _savedPosition = new System.Windows.Point(Left, Top);
                _positionRemembered = true;
                SaveSettingsToDisk();
                _notifyIcon.ShowBalloonTip(2000, "位置已保存", $"已记住当前位置 (X: {(int)Left}, Y: {(int)Top})", Forms.ToolTipIcon.Info);
            };
            
            restorePosItem.Click += (s, e) => {
                if (_positionRemembered)
                {
                    SetWindowPosition(_savedPosition.X, _savedPosition.Y);
                    _notifyIcon.ShowBalloonTip(2000, "位置已恢复", $"已恢复到保存位置 (X: {(int)_savedPosition.X}, Y: {(int)_savedPosition.Y})", Forms.ToolTipIcon.Info);
                }
                else
                {
                    _notifyIcon.ShowBalloonTip(3000, "位置恢复失败", "没有保存的位置信息", Forms.ToolTipIcon.Warning);
                }
            };
            
            positionGroup.DropDownItems.AddRange(new Forms.ToolStripItem[] {
                quickPosGroup, new Forms.ToolStripSeparator(),
                rememberPosItem, restorePosItem
            });
            
            // 添加穿透模式菜单项
            _trayClickThroughItem = new Forms.ToolStripMenuItem("👻 穿透模式");
            _trayClickThroughItem.Checked = _isClickThroughEnabled;
            _trayClickThroughItem.CheckOnClick = true;
            _trayClickThroughItem.CheckedChanged += TrayClickThrough_CheckedChanged;
            
            windowControlGroup.DropDownItems.AddRange(new Forms.ToolStripItem[] {
                topmostGroup, opacityGroup, positionGroup, _trayClickThroughItem
            });
            
            // 分隔符
            var separator2 = new Forms.ToolStripSeparator();
            
            // 工具组
            var toolsGroup = new Forms.ToolStripMenuItem("🛠️ 工具");
            
            var exportItem = new Forms.ToolStripMenuItem("📤 导出便签");
            exportItem.Click += (s, e) => ExportNotes();
            
            var importItem = new Forms.ToolStripMenuItem("📥 导入便签");
            importItem.Click += (s, e) => ImportNotes();
            
            var clearItem = new Forms.ToolStripMenuItem("🗑️ 清空内容");
            clearItem.Click += (s, e) => ClearNoteContent();
            
            toolsGroup.DropDownItems.AddRange(new Forms.ToolStripItem[] {
                exportItem, importItem, clearItem
            });
            
            // 分隔符
            var separator3 = new Forms.ToolStripSeparator();
            
            // 帮助和关于
            var aboutItem = new Forms.ToolStripMenuItem("ℹ️ 关于");
            aboutItem.Click += (s, e) => ShowAboutDialog();
            
            var exitPromptItem = new Forms.ToolStripMenuItem("🔄 重新启用退出提示");
            exitPromptItem.Click += (s, e) => 
            {
                _showExitPrompt = true;
                SaveSettingsToDisk();
                _notifyIcon.ShowBalloonTip(2000, "设置已更新", "已重新启用退出提示", Forms.ToolTipIcon.Info);
            };
            
            var deletePromptItem = new Forms.ToolStripMenuItem("🗑️ 重新启用删除提示");
            deletePromptItem.Click += (s, e) => 
            {
                _showDeletePrompt = true;
                SaveSettingsToDisk();
                _notifyIcon.ShowBalloonTip(2000, "设置已更新", "已重新启用删除确认提示", Forms.ToolTipIcon.Info);
            };

            var exitItem = new Forms.ToolStripMenuItem("❌ 退出");
            exitItem.Font = new System.Drawing.Font("Microsoft YaHei", 9F, System.Drawing.FontStyle.Bold);
            exitItem.Click += (s, e) => HandleApplicationExit();

            menu.Items.AddRange(new Forms.ToolStripItem[] {
                showHideItem, newNoteItem, settingsItem,
                separator1,
                windowControlGroup,
                separator2,
                toolsGroup,
                separator3,
                aboutItem, exitPromptItem, deletePromptItem, exitItem
            });

            _notifyIcon.ContextMenuStrip = menu;
            _notifyIcon.DoubleClick += (s, e) => ToggleWindowVisibility();
            
            // 存储菜单项引用以便后续更新
            _normalModeMenuItem = normalModeItem;
            _desktopModeMenuItem = desktopModeItem;
            _alwaysModeMenuItem = alwaysModeItem;
            
            // 初始化菜单状态
            UpdateTopmostMenuItems();
        }

        /// <summary>
        /// 托盘菜单穿透模式状态变化处理
        /// </summary>
        private void TrayClickThrough_CheckedChanged(object? sender, EventArgs e)
        {
            _isClickThroughEnabled = _trayClickThroughItem.Checked;
            ApplyClickThrough(_isClickThroughEnabled);
            
            // 同步设置面板中的复选框状态
            if (ClickThroughCheckBox != null)
            {
                // 暂时移除事件处理，避免循环调用
                ClickThroughCheckBox.Checked -= ClickThroughCheckBox_Checked;
                ClickThroughCheckBox.Unchecked -= ClickThroughCheckBox_Unchecked;
                
                ClickThroughCheckBox.IsChecked = _isClickThroughEnabled;
                
                // 重新添加事件处理
                ClickThroughCheckBox.Checked += ClickThroughCheckBox_Checked;
                ClickThroughCheckBox.Unchecked += ClickThroughCheckBox_Unchecked;
            }
            
            // 更新状态信息
            if (StatusText != null)
            {
                StatusText.Text = _isClickThroughEnabled ? "穿透模式已启用" : "穿透模式已关闭";
            }
        }

        private void SyncTopmostToggle()
        {
            // 更新置顶模式相关的UI状态（最小化按钮已移除）
            // 可以在这里添加其他与置顶模式相关的UI更新
        }

        private void ToggleWindowVisibility()
        {
            if (Visibility == Visibility.Visible)
            {
                // 只有在普通模式下才允许最小化隐藏
                if (_currentTopmostMode == TopmostMode.Normal)
                {
                    WindowState = WindowState.Minimized;
                    Hide();
                }
                else
                {
                    // 在置顶模式下只隐藏窗口，不最小化
                    Hide();
                }
            }
            else
            {
                Show();
                if (WindowState == WindowState.Minimized)
                {
                    WindowState = WindowState.Normal;
                }
                
                // 在桌面模式下，不激活窗口以避免提升到前台
                if (_currentTopmostMode == TopmostMode.Desktop)
                {
                    // 立即应用桌面层级设置
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        ReapplyDesktopMode();
                    }), System.Windows.Threading.DispatcherPriority.Send);
                }
                else
                {
                    Activate();
                    Focus();
                }
            }
        }

        /// <summary>
        /// 从磁盘加载备忘录数据
        /// </summary>
        private void LoadMemosFromDisk()
        {
            try
            {
                // 先尝试加载新格式的备忘录数据
                if (System.IO.File.Exists(_memosFilePath))
                {
                    var json = System.IO.File.ReadAllText(_memosFilePath, Encoding.UTF8);
                    var memosData = JsonSerializer.Deserialize<MemosData>(json);
                    _memos = memosData?.Memos ?? new List<MemoModel>();
                }
                // 如果没有新格式数据，尝试从旧格式迁移
                else if (System.IO.File.Exists(_noteFilePath))
                {
                    MigrateFromOldNoteFormat();
                }
                
                // 如果没有任何备忘录，创建一个默认的
                if (!_memos.Any())
                {
                    CreateDefaultMemo();
                }
            }
            catch (Exception)
            {
                // 加载失败时创建默认备忘录
                CreateDefaultMemo();
            }
        }
        
        /// <summary>
        /// 从旧版本笔记格式迁移数据
        /// </summary>
        private void MigrateFromOldNoteFormat()
        {
            try
                {
                    var json = System.IO.File.ReadAllText(_noteFilePath, Encoding.UTF8);
                var oldNote = JsonSerializer.Deserialize<JsonElement>(json);
                
                string content = string.Empty;
                if (oldNote.TryGetProperty("Content", out var contentProperty))
                {
                    content = contentProperty.GetString() ?? string.Empty;
                }
                
                if (!string.IsNullOrWhiteSpace(content))
                {
                    var memo = new MemoModel
                    {
                        Title = "导入的笔记",
                        Content = content,
                        CreatedTime = DateTime.Now,
                        ModifiedTime = DateTime.Now
                    };
                    _memos.Add(memo);
                    SaveMemosToDisk();
                }
            }
            catch (Exception)
            {
                // 迁移失败，不影响程序运行
            }
        }
        
        /// <summary>
        /// 创建默认备忘录
        /// </summary>
        private void CreateDefaultMemo()
        {
            var defaultMemo = new MemoModel
            {
                Title = "欢迎使用DesktopMemo",
                Content = "这是您的第一条备忘录！\n\n点击此处开始编辑...",
                CreatedTime = DateTime.Now,
                ModifiedTime = DateTime.Now
            };
            _memos.Add(defaultMemo);
            SaveMemosToDisk();
        }

        /// <summary>
        /// 保存备忘录数据到磁盘
        /// </summary>
        private void SaveMemosToDisk()
        {
            try
            {
                var memosData = new MemosData
                {
                    Memos = _memos,
                    CurrentMemoId = _currentMemo?.Id ?? string.Empty
                };
                var json = JsonSerializer.Serialize(memosData, new JsonSerializerOptions { WriteIndented = true });
                System.IO.File.WriteAllText(_memosFilePath, json, Encoding.UTF8);
            }
            catch (Exception)
            {
                // 保存失败时不显示错误信息
            }
        }
        
        /// <summary>
        /// 保存当前编辑的备忘录
        /// </summary>
        private void SaveCurrentMemo()
        {
            if (_currentMemo != null && NoteTextBox != null)
            {
                // 更新当前备忘录
                var updatedMemo = _currentMemo with
                {
                    Content = NoteTextBox.Text,
                    ModifiedTime = DateTime.Now
                };
                
                // 智能更新标题：使用第一行非空内容作为标题
                var lines = NoteTextBox.Text.Split('\n');
                var firstNonEmptyLine = lines.FirstOrDefault(line => !string.IsNullOrWhiteSpace(line))?.Trim();
                
                // 如果有非空内容且与当前标题不同，或者当前标题是默认的"新建备忘录"，则更新标题
                if (!string.IsNullOrWhiteSpace(firstNonEmptyLine) && 
                    (firstNonEmptyLine != _currentMemo.Title || _currentMemo.Title == "新建备忘录"))
                {
                    updatedMemo = updatedMemo with { Title = firstNonEmptyLine };
                }
                // 如果内容完全为空，保持默认标题
                else if (string.IsNullOrWhiteSpace(NoteTextBox.Text) && _currentMemo.Title == "新建备忘录")
                {
                    updatedMemo = updatedMemo with { Title = "空白备忘录" };
                }
                
                // 更新列表中的备忘录
                var index = _memos.FindIndex(m => m.Id == _currentMemo.Id);
                if (index >= 0)
                {
                    _memos[index] = updatedMemo;
                    _currentMemo = updatedMemo;
                }
                
                SaveMemosToDisk();
                
                // 确保刷新列表显示
                RefreshMemoList();
            }
        }



        /// <summary>
        /// 窗口激活事件处理 - 在桌面模式下延迟重新设置层级
        /// </summary>
        private void MainWindow_Activated(object? sender, EventArgs e)
        {
            if (_currentTopmostMode == TopmostMode.Desktop)
            {
                // 启动定时器，延迟重新设置桌面层级
                _desktopModeTimer.Stop();
                _desktopModeTimer.Start();
            }
        }

        /// <summary>
        /// 窗口取消激活事件处理
        /// </summary>
        private void MainWindow_Deactivated(object? sender, EventArgs e)
        {
            // 停止定时器
            _desktopModeTimer.Stop();
        }

        /// <summary>
        /// 重新应用桌面模式层级设置
        /// </summary>
        private void ReapplyDesktopMode()
        {
            if (_currentTopmostMode != TopmostMode.Desktop) return;
            
            var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            if (hwnd == IntPtr.Zero) return;

            // 重新设置桌面层级
            SetWindowPos(hwnd, HWND_NOTOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
            SetWindowPos(hwnd, HWND_BOTTOM, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
            
            IntPtr progmanHwnd = FindWindow("Progman", "Program Manager");
            if (progmanHwnd != IntPtr.Zero)
            {
                SetWindowPos(hwnd, progmanHwnd, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            
            // 程序关闭时自动保存所有应用状态
            AutoSaveAllSettings();
            
            _notifyIcon?.Dispose();
        }
        
        /// <summary>
        /// 自动保存所有应用设置状态
        /// </summary>
        private void AutoSaveAllSettings()
        {
            try
            {
                // 自动保存当前位置
                _savedPosition = new System.Windows.Point(Left, Top);
                _positionRemembered = true;
                
                // 保存所有设置
                SaveSettingsToDisk();
                
                // 如果正在编辑备忘录，保存当前内容
                if (_isEditMode)
                {
                    SaveCurrentMemo();
                }
            }
            catch (Exception ex)
            {
                // 保存失败时不影响程序退出
                System.Diagnostics.Debug.WriteLine($"自动保存设置失败: {ex.Message}");
            }
        }

        // 事件处理
        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed && !_isWindowPinned)
            {
                DragMove();
            }
        }

        private void SettingsToggle_Click(object sender, RoutedEventArgs e)
        {
            ToggleSettingsPanel();
        }

        private void SettingsBackButton_Click(object sender, RoutedEventArgs e)
        {
            // 关闭设置面板
            if (_isSettingsPanelVisible)
            {
                ToggleSettingsPanel();
            }
        }

        private void SettingsPanel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // 检查点击的是否是设置面板的背景区域（空白处）
            if (e.Source == sender && _isSettingsPanelVisible)
            {
                // 关闭设置面板，返回主页面
                ToggleSettingsPanel();
            }
        }

        private void MainContentArea_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // 如果设置面板可见，点击主内容区域关闭设置面板
            if (_isSettingsPanelVisible)
            {
                // 检查点击的不是设置按钮本身，避免点击设置按钮时立即关闭
                if (e.Source != SettingsToggle)
                {
                    ToggleSettingsPanel();
                    e.Handled = true; // 阻止事件继续传播
                }
            }
        }

        private void TopmostModeRadio_Checked(object sender, RoutedEventArgs e)
        {
            // 在初始化期间避免处理事件
            if (StatusText == null) return;
            
            if (sender == NormalModeRadio && NormalModeRadio.IsChecked == true)
            {
                SetTopmostMode(TopmostMode.Normal);
                StatusText.Text = "已切换到普通模式";
            }
            else if (sender == DesktopModeRadio && DesktopModeRadio.IsChecked == true)
            {
                SetTopmostMode(TopmostMode.Desktop);
                StatusText.Text = "已切换到桌面置顶模式";
            }
            else if (sender == AlwaysModeRadio && AlwaysModeRadio.IsChecked == true)
            {
                SetTopmostMode(TopmostMode.Always);
                StatusText.Text = "已切换到总是置顶模式";
            }
        }

        private void OpacitySlider_ValueChanged(object sender, System.Windows.RoutedPropertyChangedEventArgs<double> e)
        {
            if (this != null && MainContainer != null)
            {
                // 对于玻璃拟态设计，我们需要同时调整窗口和主容器的透明度
                this.Opacity = e.NewValue;
                
                // 更新主容器的背景透明度
                var radialGradient = new RadialGradientBrush();
                radialGradient.GradientStops.Add(new GradientStop(System.Windows.Media.Color.FromArgb((byte)(32 * e.NewValue), 255, 255, 255), 0));
                radialGradient.GradientStops.Add(new GradientStop(System.Windows.Media.Color.FromArgb((byte)(8 * e.NewValue), 255, 255, 255), 1));
                MainContainer.Background = radialGradient;
                
                if (OpacityValueText != null)
                {
                    OpacityValueText.Text = $"{(int)(e.NewValue * 100)}%";
                }
                if (StatusText != null)
                {
                    StatusText.Text = $"透明度已设置为 {(int)(e.NewValue * 100)}%";
                }
            }
        }

        private void ClickThroughCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            _isClickThroughEnabled = true;
            ApplyClickThrough(true);
            
            // 同步托盘菜单状态
            if (_trayClickThroughItem != null)
            {
                _trayClickThroughItem.CheckedChanged -= TrayClickThrough_CheckedChanged;
                _trayClickThroughItem.Checked = true;
                _trayClickThroughItem.CheckedChanged += TrayClickThrough_CheckedChanged;
            }
            
            if (StatusText != null)
            {
                StatusText.Text = "穿透模式已启用";
            }
            
            // 启动穿透模式后自动关闭设置页面
            if (_isSettingsPanelVisible)
            {
                // 延迟一点关闭，让用户看到状态变化
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    ToggleSettingsPanel();
                }), System.Windows.Threading.DispatcherPriority.Background);
            }
        }

        private void ClickThroughCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            _isClickThroughEnabled = false;
            ApplyClickThrough(false);
            
            // 同步托盘菜单状态
            if (_trayClickThroughItem != null)
            {
                _trayClickThroughItem.CheckedChanged -= TrayClickThrough_CheckedChanged;
                _trayClickThroughItem.Checked = false;
                _trayClickThroughItem.CheckedChanged += TrayClickThrough_CheckedChanged;
            }
            
            if (StatusText != null)
            {
                StatusText.Text = "穿透模式已关闭";
            }
        }

        private void ApplyClickThrough(bool enabled)
        {
            var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            if (hwnd == IntPtr.Zero) return;
            
            int exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            
            if (enabled)
            {
                exStyle |= WS_EX_TRANSPARENT | WS_EX_LAYERED;
                SetWindowLong(hwnd, GWL_EXSTYLE, exStyle);
                SetNoteTextBoxHitTest(false);
            }
            else
            {
                exStyle &= ~WS_EX_TRANSPARENT;
                SetWindowLong(hwnd, GWL_EXSTYLE, exStyle);
                SetNoteTextBoxHitTest(true);
            }
            
            // 确保内部状态正确
            _isClickThroughEnabled = enabled;
        }



        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            HandleApplicationExit();
        }

        // 新的标题栏按钮事件处理
        private void MainAddMemoButton_Click(object sender, RoutedEventArgs e)
        {
            AddNewMemo();
        }

        private void EditSaveAndBackButton_Click(object sender, RoutedEventArgs e)
        {
            // 保存当前备忘录并返回列表
            SaveCurrentMemo();
            ShowMemoList();
        }

        private void MainBackButton_Click(object sender, RoutedEventArgs e)
        {
            ShowMemoList();
        }

        private void MainDeleteButton_Click(object sender, RoutedEventArgs e)
        {
            DeleteCurrentMemo();
        }

        private void PinButton_Click(object sender, RoutedEventArgs e)
        {
            _isWindowPinned = !_isWindowPinned;
            UpdatePinButtonState();
        }

        private void NoteTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isLoadedFromDisk || !_isEditMode) return;
            // 简单的防抖: 短时间内的多次写入避免频繁IO，这里先直接写，后续可优化
            SaveCurrentMemo();
            
            // 更新窗口标题
            UpdateWindowTitle();
        }

        /// <summary>
        /// 备忘录数据模型
        /// </summary>
        private record MemoModel
        {
            public string Id { get; init; } = Guid.NewGuid().ToString();
            public string Title { get; init; } = string.Empty;
            public string Content { get; init; } = string.Empty;
            public DateTime CreatedTime { get; init; } = DateTime.Now;
            public DateTime ModifiedTime { get; init; } = DateTime.Now;
            
            /// <summary>
            /// 获取备忘录预览内容（前100个字符）
            /// </summary>
            public string Preview => Content.Length > 100 ? Content.Substring(0, 100) + "..." : Content;
            
            /// <summary>
            /// 获取显示标题（如果标题为空则使用内容开头作为标题）
            /// </summary>
            public string DisplayTitle => !string.IsNullOrWhiteSpace(Title) ? Title : 
                (Content.Length > 30 ? Content.Substring(0, 30) + "..." : 
                (!string.IsNullOrWhiteSpace(Content) ? Content : "未命名备忘录"));
        }
        
        /// <summary>
        /// 备忘录集合数据模型
        /// </summary>
        private record MemosData
        {
            public List<MemoModel> Memos { get; init; } = new List<MemoModel>();
            public string CurrentMemoId { get; init; } = string.Empty;
        }

        #region Win32 APIs
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TRANSPARENT = 0x00000020;
        private const int WS_EX_LAYERED = 0x00080000;

        // SetWindowPos constants
        private static readonly IntPtr HWND_BOTTOM = new IntPtr(1);
        private static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);
        private static readonly IntPtr HWND_TOP = new IntPtr(0);
        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOZORDER = 0x0004;
        private const uint SWP_NOACTIVATE = 0x0010;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr GetActiveWindow();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
        #endregion

        #region 扩展功能方法
        
        /// <summary>
        /// 更新置顶模式菜单项的状态
        /// </summary>
        private void UpdateTopmostMenuItems()
        {
            if (_normalModeMenuItem != null && _desktopModeMenuItem != null && _alwaysModeMenuItem != null)
            {
                _normalModeMenuItem.Checked = _currentTopmostMode == TopmostMode.Normal;
                _desktopModeMenuItem.Checked = _currentTopmostMode == TopmostMode.Desktop;
                _alwaysModeMenuItem.Checked = _currentTopmostMode == TopmostMode.Always;
            }
        }
        
        /// <summary>
        /// 设置窗口透明度
        /// </summary>
        private void SetWindowOpacity(double opacity)
        {
            this.Opacity = opacity;
            
            // 更新主容器的背景透明度
            if (MainContainer != null)
            {
                var radialGradient = new RadialGradientBrush();
                radialGradient.GradientStops.Add(new GradientStop(System.Windows.Media.Color.FromArgb((byte)(32 * opacity), 255, 255, 255), 0));
                radialGradient.GradientStops.Add(new GradientStop(System.Windows.Media.Color.FromArgb((byte)(8 * opacity), 255, 255, 255), 1));
                MainContainer.Background = radialGradient;
            }
            
            if (OpacitySlider != null)
            {
                OpacitySlider.Value = opacity;
            }
            if (OpacityValueText != null)
            {
                OpacityValueText.Text = $"{(int)(opacity * 100)}%";
            }
            if (StatusText != null)
            {
                StatusText.Text = $"透明度已设置为 {(int)(opacity * 100)}%";
            }
        }
        
        /// <summary>
        /// 创建新便签（清空内容）
        /// </summary>
        private void CreateNewNote()
        {
            var result = System.Windows.MessageBox.Show(
                "是否清空当前便签内容并创建新便签？",
                "创建新便签",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
                
            if (result == MessageBoxResult.Yes)
            {
                NoteTextBox.Text = "";
                if (StatusText != null)
                {
                    StatusText.Text = "已创建新便签";
                }
            }
        }
        
        /// <summary>
        /// 导出便签内容
        /// </summary>
        private void ExportNotes()
        {
            try
            {
                var saveDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "文本文件 (*.txt)|*.txt|所有文件 (*.*)|*.*",
                    DefaultExt = "txt",
                    FileName = $"DesktopMemo_便签_{DateTime.Now:yyyyMMdd_HHmmss}.txt"
                };
                
                if (saveDialog.ShowDialog() == true)
                {
                    System.IO.File.WriteAllText(saveDialog.FileName, NoteTextBox.Text, Encoding.UTF8);
                    if (StatusText != null)
                    {
                        StatusText.Text = "便签已导出";
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"导出失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        /// <summary>
        /// 导入便签内容
        /// </summary>
        private void ImportNotes()
        {
            try
            {
                var openDialog = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = "文本文件 (*.txt)|*.txt|所有文件 (*.*)|*.*",
                    DefaultExt = "txt"
                };
                
                if (openDialog.ShowDialog() == true)
                {
                    var content = System.IO.File.ReadAllText(openDialog.FileName, Encoding.UTF8);
                    if (!string.IsNullOrEmpty(NoteTextBox.Text))
                    {
                        var result = System.Windows.MessageBox.Show(
                            "当前便签有内容，是否覆盖？",
                            "导入便签",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Question);
                            
                        if (result == MessageBoxResult.No)
                        {
                            NoteTextBox.Text += "\n\n" + content;
                        }
                        else
                        {
                            NoteTextBox.Text = content;
                        }
                    }
                    else
                    {
                        NoteTextBox.Text = content;
                    }
                    
                    if (StatusText != null)
                    {
                        StatusText.Text = "便签已导入";
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"导入失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        /// <summary>
        /// 清空便签内容
        /// </summary>
        private void ClearNoteContent()
        {
            var result = System.Windows.MessageBox.Show(
                "是否清空当前便签内容？",
                "清空内容",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
                
            if (result == MessageBoxResult.Yes)
            {
                NoteTextBox.Text = "";
                if (StatusText != null)
                {
                    StatusText.Text = "内容已清空";
                }
            }
        }
        
        /// <summary>
        /// 显示关于对话框
        /// </summary>
        private void ShowAboutDialog()
        {
            var aboutInfo = "📝 DesktopMemo 便签\n\n"
                + "版本：1.0.0\n"
                + "开发者：DesktopMemo Team\n"
                + "技术框架：.NET 8.0 + WPF\n\n"
                + "功能特点：\n"
                + "• 多种置顶模式\n"
                + "• 穿透模式\n"
                + "• 透明度调节\n"
                + "• 自动保存\n"
                + "• 暗色主题\n\n"
                + "感谢使用 DesktopMemo！";
                
            System.Windows.MessageBox.Show(aboutInfo, "关于 DesktopMemo", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        
        /// <summary>
        /// 设置开机自启动
        /// </summary>
        private void SetAutoStart(bool enabled)
        {
            try
            {
                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
                    
                if (key != null)
                {
                    if (enabled)
                    {
                        string exePath = GetExecutablePath();
                        if (string.IsNullOrEmpty(exePath))
                        {
                            System.Windows.MessageBox.Show("无法获取程序路径，开机自启动设置失败。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                        
                        key.SetValue("DesktopMemo", exePath);
                        if (StatusText != null)
                        {
                            StatusText.Text = "已启用开机自启动";
                        }
                    }
                    else
                    {
                        key.DeleteValue("DesktopMemo", false);
                        if (StatusText != null)
                        {
                            StatusText.Text = "已禁用开机自启动";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"设置开机自启动失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        /// <summary>
        /// 获取当前可执行文件的完整路径
        /// </summary>
        private string GetExecutablePath()
        {
            try
            {
                // .NET 6+ 推荐的方法，适用于单文件发布
                string? processPath = Environment.ProcessPath;
                if (!string.IsNullOrEmpty(processPath) && System.IO.File.Exists(processPath))
                {
                    return processPath;
                }

                // 备用方法1：使用Process.MainModule
                var currentProcess = System.Diagnostics.Process.GetCurrentProcess();
                string? mainModulePath = currentProcess.MainModule?.FileName;
                if (!string.IsNullOrEmpty(mainModulePath) && System.IO.File.Exists(mainModulePath))
                {
                    return mainModulePath;
                }

                // 备用方法2：使用Assembly.Location（适用于非单文件发布）
                // 注意：在单文件发布中，Assembly.Location会返回空字符串
                string assemblyLocation = System.Reflection.Assembly.GetExecutingAssembly().Location;
                if (!string.IsNullOrEmpty(assemblyLocation) && System.IO.File.Exists(assemblyLocation))
                {
                    return assemblyLocation;
                }
                
                // 备用方法3：使用AppContext.BaseDirectory（推荐用于单文件发布）
                string baseDirectory = AppContext.BaseDirectory;
                if (!string.IsNullOrEmpty(baseDirectory))
                {
                    // 在单文件发布中，可执行文件位于临时目录
                    // 我们需要查找实际的可执行文件
                    var executableName = System.IO.Path.GetFileNameWithoutExtension(Environment.ProcessPath ?? "DesktopMemo") + ".exe";
                    var executablePath = System.IO.Path.Combine(baseDirectory, executableName);
                    if (System.IO.File.Exists(executablePath))
                    {
                        return executablePath;
                    }
                }

                // 备用方法4：使用命令行参数
                string[] args = Environment.GetCommandLineArgs();
                if (args.Length > 0)
                {
                    string firstArg = args[0];
                    if (System.IO.File.Exists(firstArg))
                    {
                        return System.IO.Path.GetFullPath(firstArg);
                    }
                }

                return string.Empty;
            }
            catch (Exception ex)
            {
                // 记录错误但不抛出异常
                System.Diagnostics.Debug.WriteLine($"获取可执行文件路径时出错: {ex.Message}");
                return string.Empty;
            }
        }
        
        /// <summary>
        /// 检查是否已设置开机自启动
        /// </summary>
        private bool IsAutoStartEnabled()
        {
            try
            {
                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", false);
                    
                return key?.GetValue("DesktopMemo") != null;
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// 查找菜单项点击事件
        /// </summary>
        private void FindMenuItem_Click(object sender, RoutedEventArgs e)
        {
            string searchText = Microsoft.VisualBasic.Interaction.InputBox(
                "请输入要查找的文本：",
                "查找",
                "");
                
            if (!string.IsNullOrEmpty(searchText))
            {
                int index = NoteTextBox.Text.IndexOf(searchText, StringComparison.OrdinalIgnoreCase);
                if (index >= 0)
                {
                    NoteTextBox.Select(index, searchText.Length);
                    NoteTextBox.Focus();
                    if (StatusText != null)
                    {
                        StatusText.Text = $"找到匹配项：{searchText}";
                    }
                }
                else
                {
                    if (StatusText != null)
                    {
                        StatusText.Text = $"未找到：{searchText}";
                    }
                }
            }
        }
        
        /// <summary>
        /// 替换菜单项点击事件
        /// </summary>
        private void ReplaceMenuItem_Click(object sender, RoutedEventArgs e)
        {
            string findText = Microsoft.VisualBasic.Interaction.InputBox(
                "请输入要查找的文本：",
                "查找和替换",
                "");
                
            if (!string.IsNullOrEmpty(findText))
            {
                string replaceText = Microsoft.VisualBasic.Interaction.InputBox(
                    "请输入替换文本：",
                    "查找和替换",
                    "");
                    
                int count = 0;
                string newText = NoteTextBox.Text;
                while (newText.Contains(findText))
                {
                    newText = newText.Replace(findText, replaceText);
                    count++;
                }
                
                if (count > 0)
                {
                    NoteTextBox.Text = newText;
                    if (StatusText != null)
                    {
                        StatusText.Text = $"已替换 {count} 处";
                    }
                }
                else
                {
                    if (StatusText != null)
                    {
                        StatusText.Text = $"未找到：{findText}";
                    }
                }
            }
        }
        
        /// <summary>
        /// 开机自启动复选框事件处理
        /// </summary>
        private void AutoStartCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            SetAutoStart(true);
        }
        
        private void AutoStartCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            SetAutoStart(false);
        }
        
        #endregion

        #region 窗口位置管理功能
        
        /// <summary>
        /// 窗口位置设置类
        /// </summary>
        private record WindowSettings
        {
            public double SavedX { get; init; } = 100;
            public double SavedY { get; init; } = 100;
            public bool PositionRemembered { get; init; } = false;
            public bool AutoRestorePositionEnabled { get; init; } = true;
            public double WindowOpacity { get; init; } = 1.0;
            public TopmostMode TopmostMode { get; init; } = TopmostMode.Desktop;
            public bool ClickThroughEnabled { get; init; } = false;
            public bool AutoStartEnabled { get; init; } = false;
            public string NoteContent { get; init; } = string.Empty;
            public bool ShowExitPrompt { get; init; } = true;
            public bool WindowPinned { get; init; } = false;
            public bool ShowDeletePrompt { get; init; } = true;
        }
        
        /// <summary>
        /// 初始化位置相关控件
        /// </summary>
        private void InitializePositionControls()
        {
            if (CustomXTextBox != null && CustomYTextBox != null)
            {
                CustomXTextBox.Text = ((int)Left).ToString();
                CustomYTextBox.Text = ((int)Top).ToString();
            }
            
            UpdateCurrentPositionDisplay();
        }
        
        /// <summary>
        /// 更新当前位置显示
        /// </summary>
        private void UpdateCurrentPositionDisplay()
        {
            if (CurrentPositionText != null)
            {
                CurrentPositionText.Text = $"X: {(int)Left}, Y: {(int)Top}";
            }
        }
        
        /// <summary>
        /// 窗口位置变化事件处理
        /// </summary>
        private void MainWindow_LocationChanged(object? sender, EventArgs e)
        {
            UpdateCurrentPositionDisplay();
            if (CustomXTextBox != null && CustomYTextBox != null)
            {
                CustomXTextBox.Text = ((int)Left).ToString();
                CustomYTextBox.Text = ((int)Top).ToString();
            }
            
            // 启动自动保存位置定时器（防抖）
            _autoSavePositionTimer.Stop();
            _autoSavePositionTimer.Start();
        }
        
        /// <summary>
        /// 自动保存位置定时器事件（防抖）
        /// </summary>
        private void AutoSavePositionTimer_Tick(object? sender, EventArgs e)
        {
            _autoSavePositionTimer.Stop();
            
            // 自动记住当前位置
            _savedPosition = new System.Windows.Point(Left, Top);
            _positionRemembered = true;
            SaveSettingsToDisk();
            
            // 更新状态信息（如果设置面板可见）
            if (StatusText != null && _isSettingsPanelVisible)
            {
                StatusText.Text = $"位置已自动保存 (X: {(int)Left}, Y: {(int)Top})";
            }
        }
        
        /// <summary>
        /// 位置更新定时器事件
        /// </summary>
        private void PositionUpdateTimer_Tick(object? sender, EventArgs e)
        {
            UpdateCurrentPositionDisplay();
        }
        
        /// <summary>
        /// 预设位置按钮点击事件
        /// </summary>
        private void PresetPosition_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not System.Windows.Controls.Button button || button.Tag is not string position)
                return;
                
            var workingArea = SystemParameters.WorkArea;
            double newX = 0, newY = 0;
            double windowWidth = Width;
            double windowHeight = Height;
            
            // 计算预设位置
            switch (position)
            {
                case "TopLeft":
                    newX = workingArea.Left + 10;
                    newY = workingArea.Top + 10;
                    break;
                case "TopCenter":
                    newX = workingArea.Left + (workingArea.Width - windowWidth) / 2;
                    newY = workingArea.Top + 10;
                    break;
                case "TopRight":
                    newX = workingArea.Right - windowWidth - 10;
                    newY = workingArea.Top + 10;
                    break;
                case "MiddleLeft":
                    newX = workingArea.Left + 10;
                    newY = workingArea.Top + (workingArea.Height - windowHeight) / 2;
                    break;
                case "Center":
                    newX = workingArea.Left + (workingArea.Width - windowWidth) / 2;
                    newY = workingArea.Top + (workingArea.Height - windowHeight) / 2;
                    break;
                case "MiddleRight":
                    newX = workingArea.Right - windowWidth - 10;
                    newY = workingArea.Top + (workingArea.Height - windowHeight) / 2;
                    break;
                case "BottomLeft":
                    newX = workingArea.Left + 10;
                    newY = workingArea.Bottom - windowHeight - 10;
                    break;
                case "BottomCenter":
                    newX = workingArea.Left + (workingArea.Width - windowWidth) / 2;
                    newY = workingArea.Bottom - windowHeight - 10;
                    break;
                case "BottomRight":
                    newX = workingArea.Right - windowWidth - 10;
                    newY = workingArea.Bottom - windowHeight - 10;
                    break;
                default:
                    return;
            }
            
            // 设置窗口位置
            SetWindowPosition(newX, newY);
            
            if (StatusText != null)
            {
                StatusText.Text = $"已移动到{GetPositionDisplayName(position)}";
            }
        }
        
        /// <summary>
        /// 获取位置显示名称
        /// </summary>
        private string GetPositionDisplayName(string position)
        {
            return position switch
            {
                "TopLeft" => "左上角",
                "TopCenter" => "顶部中央",
                "TopRight" => "右上角",
                "MiddleLeft" => "左侧中央",
                "Center" => "屏幕中央",
                "MiddleRight" => "右侧中央",
                "BottomLeft" => "左下角",
                "BottomCenter" => "底部中央",
                "BottomRight" => "右下角",
                _ => "未知位置"
            };
        }
        
        /// <summary>
        /// 数字输入验证
        /// </summary>
        private void NumberOnly_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            // 只允许数字和负号
            if (!char.IsDigit(e.Text[0]) && e.Text[0] != '-')
            {
                e.Handled = true;
            }
            
            // 负号只能在开头
            if (e.Text[0] == '-' && sender is System.Windows.Controls.TextBox textBox && textBox.SelectionStart != 0)
            {
                e.Handled = true;
            }
        }
        
        /// <summary>
        /// 应用自定义位置
        /// </summary>
        private void ApplyCustomPosition_Click(object sender, RoutedEventArgs e)
        {
            if (CustomXTextBox == null || CustomYTextBox == null) return;
            
            if (double.TryParse(CustomXTextBox.Text, out double x) && 
                double.TryParse(CustomYTextBox.Text, out double y))
            {
                SetWindowPosition(x, y);
                
                if (StatusText != null)
                {
                    StatusText.Text = $"已移动到自定义位置 (X: {(int)x}, Y: {(int)y})";
                }
            }
            else
            {
                if (StatusText != null)
                {
                    StatusText.Text = "位置坐标格式错误";
                }
                System.Windows.MessageBox.Show("请输入有效的数字坐标", "位置设置", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        
        /// <summary>
        /// 记住当前位置
        /// </summary>
        private void RememberPosition_Click(object sender, RoutedEventArgs e)
        {
            _savedPosition = new System.Windows.Point(Left, Top);
            _positionRemembered = true;
            SaveSettingsToDisk();
            
            if (StatusText != null)
            {
                StatusText.Text = $"已记住当前位置 (X: {(int)Left}, Y: {(int)Top})";
            }
        }
        
        /// <summary>
        /// 恢复保存的位置
        /// </summary>
        private void RestorePosition_Click(object sender, RoutedEventArgs e)
        {
            if (_positionRemembered)
            {
                SetWindowPosition(_savedPosition.X, _savedPosition.Y);
                
                if (StatusText != null)
                {
                    StatusText.Text = $"已恢复到保存位置 (X: {(int)_savedPosition.X}, Y: {(int)_savedPosition.Y})";
                }
            }
            else
            {
                if (StatusText != null)
                {
                    StatusText.Text = "没有保存的位置信息";
                }
                System.Windows.MessageBox.Show("您还没有保存过位置，请先使用'记住当前位置'功能", "恢复位置", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        
        /// <summary>
        /// 设置窗口位置（带边界检查）
        /// </summary>
        private void SetWindowPosition(double x, double y)
        {
            var workingArea = SystemParameters.WorkArea;
            
            // 边界检查，确保窗口不会完全超出屏幕
            double minX = workingArea.Left - Width + 50; // 至少留50像素可见
            double maxX = workingArea.Right - 50;
            double minY = workingArea.Top;
            double maxY = workingArea.Bottom - Height;
            
            Left = Math.Max(minX, Math.Min(maxX, x));
            Top = Math.Max(minY, Math.Min(maxY, y));
        }
        
        /// <summary>
        /// 从磁盘加载设置
        /// </summary>
        private void LoadSettingsFromDisk()
        {
            try
            {
                if (System.IO.File.Exists(_settingsFilePath))
                {
                    var json = System.IO.File.ReadAllText(_settingsFilePath, Encoding.UTF8);
                    var settings = JsonSerializer.Deserialize<WindowSettings>(json);
                    
                    if (settings != null)
                    {
                        // 恢复保存的位置信息
                        _savedPosition = new System.Windows.Point(settings.SavedX, settings.SavedY);
                        _positionRemembered = settings.PositionRemembered;
                        _autoRestorePositionEnabled = settings.AutoRestorePositionEnabled;
                        
                        // 恢复其他设置
                        Opacity = settings.WindowOpacity;
                        _currentTopmostMode = settings.TopmostMode;
                        _isClickThroughEnabled = settings.ClickThroughEnabled;
                        _showExitPrompt = settings.ShowExitPrompt;
                        _isWindowPinned = settings.WindowPinned;
                        _showDeletePrompt = settings.ShowDeletePrompt;
                        
                        // 如果有保存的位置且启用了自动恢复，自动恢复位置
                        if (_positionRemembered && _autoRestorePositionEnabled)
                        {
                            SetWindowPosition(_savedPosition.X, _savedPosition.Y);
                        }
                    }
                }
            }
            catch (Exception)
            {
                // 加载失败时不显示错误，使用默认设置
            }
        }
        
        /// <summary>
        /// 保存设置到磁盘
        /// </summary>
        private void SaveSettingsToDisk()
        {
            try
            {
                var settings = new WindowSettings
                {
                    SavedX = _savedPosition.X,
                    SavedY = _savedPosition.Y,
                    PositionRemembered = _positionRemembered,
                    AutoRestorePositionEnabled = _autoRestorePositionEnabled,
                    WindowOpacity = Opacity,
                    TopmostMode = _currentTopmostMode,
                    ClickThroughEnabled = _isClickThroughEnabled,
                    AutoStartEnabled = IsAutoStartEnabled(),
                    NoteContent = NoteTextBox?.Text ?? string.Empty,
                    ShowExitPrompt = _showExitPrompt,
                    WindowPinned = _isWindowPinned,
                    ShowDeletePrompt = _showDeletePrompt
                };
                
                var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                System.IO.File.WriteAllText(_settingsFilePath, json, Encoding.UTF8);
            }
            catch (Exception)
            {
                // 保存失败时不显示错误
            }
        }
        
        /// <summary>
        /// 托盘菜单快速位置移动
        /// </summary>
        private void MoveToTrayPresetPosition(string position)
        {
            var workingArea = SystemParameters.WorkArea;
            double newX = 0, newY = 0;
            double windowWidth = Width;
            double windowHeight = Height;
            
            // 计算预设位置
            switch (position)
            {
                case "TopLeft":
                    newX = workingArea.Left + 10;
                    newY = workingArea.Top + 10;
                    break;
                case "TopCenter":
                    newX = workingArea.Left + (workingArea.Width - windowWidth) / 2;
                    newY = workingArea.Top + 10;
                    break;
                case "TopRight":
                    newX = workingArea.Right - windowWidth - 10;
                    newY = workingArea.Top + 10;
                    break;
                case "Center":
                    newX = workingArea.Left + (workingArea.Width - windowWidth) / 2;
                    newY = workingArea.Top + (workingArea.Height - windowHeight) / 2;
                    break;
                case "BottomLeft":
                    newX = workingArea.Left + 10;
                    newY = workingArea.Bottom - windowHeight - 10;
                    break;
                case "BottomRight":
                    newX = workingArea.Right - windowWidth - 10;
                    newY = workingArea.Bottom - windowHeight - 10;
                    break;
                default:
                    return;
            }
            
            // 设置窗口位置
            SetWindowPosition(newX, newY);
            
            // 显示气泡提示
            _notifyIcon.ShowBalloonTip(2000, "位置已更改", 
                $"已移动到{GetPositionDisplayName(position)} (X: {(int)newX}, Y: {(int)newY})", 
                Forms.ToolTipIcon.Info);
        }
        

        
        #endregion
        
        #region 备忘录界面管理
        
        /// <summary>
        /// 刷新备忘录列表显示
        /// </summary>
        private void RefreshMemoList()
        {
            if (MemoItemsControl == null) return;
            
            MemoItemsControl.Items.Clear();
            
            // 按修改时间倒序排列
            var sortedMemos = _memos.OrderByDescending(m => m.ModifiedTime).ToList();
            
            foreach (var memo in sortedMemos)
            {
                var memoCard = CreateMemoCard(memo);
                MemoItemsControl.Items.Add(memoCard);
            }
            
            // 更新计数
            if (MemoCountText != null)
            {
                MemoCountText.Text = $"({_memos.Count})";
            }
        }
        
        /// <summary>
        /// 创建备忘录长横条
        /// </summary>
        private Border CreateMemoCard(MemoModel memo)
        {
            var card = new Border
            {
                Height = 80,
                Margin = new Thickness(0, 5, 0, 5),
                CornerRadius = new CornerRadius(12),
                Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(40, 255, 255, 255)),
                BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromArgb(60, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            
            // 添加阴影效果
            card.Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                Color = System.Windows.Media.Colors.Black,
                BlurRadius = 8,
                ShadowDepth = 4,
                Opacity = 0.1
            };
            
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new System.Windows.GridLength(150, System.Windows.GridUnitType.Pixel) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = System.Windows.GridLength.Auto });
            
            // 标题区域
            var titleStack = new StackPanel
            {
                Margin = new Thickness(15, 15, 10, 15),
                VerticalAlignment = System.Windows.VerticalAlignment.Center
            };
            
            var titleText = new TextBlock
            {
                Text = memo.DisplayTitle,
                FontSize = 14,
                FontWeight = System.Windows.FontWeights.SemiBold,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromArgb(240, 255, 255, 255)),
                TextTrimming = System.Windows.TextTrimming.CharacterEllipsis,
                Margin = new Thickness(0, 0, 0, 4)
            };
            
            var subtitleText = new TextBlock
            {
                Text = memo.ModifiedTime.ToString("MM/dd HH:mm"),
                FontSize = 11,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromArgb(150, 255, 255, 255))
            };
            
            titleStack.Children.Add(titleText);
            titleStack.Children.Add(subtitleText);
            Grid.SetColumn(titleStack, 0);
            
            // 内容预览
            var contentText = new TextBlock
            {
                Text = memo.Preview,
                FontSize = 12,
                Foreground = new SolidColorBrush(System.Windows.Media.Color.FromArgb(180, 255, 255, 255)),
                Margin = new Thickness(0, 15, 15, 15),
                TextWrapping = System.Windows.TextWrapping.Wrap,
                TextTrimming = System.Windows.TextTrimming.CharacterEllipsis,
                VerticalAlignment = System.Windows.VerticalAlignment.Center,
                MaxHeight = 50
            };
            Grid.SetColumn(contentText, 1);
            
            // 操作区域（可以添加更多按钮）
            var actionPanel = new StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                Margin = new Thickness(0, 15, 15, 15),
                VerticalAlignment = System.Windows.VerticalAlignment.Center
            };
            Grid.SetColumn(actionPanel, 2);
            
            grid.Children.Add(titleStack);
            grid.Children.Add(contentText);
            grid.Children.Add(actionPanel);
            
            card.Child = grid;
            
            // 添加点击事件
            card.MouseLeftButtonUp += (s, e) =>
            {
                EditMemo(memo);
            };
            
            // 添加悬停效果
            card.MouseEnter += (s, e) =>
            {
                card.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(60, 255, 255, 255));
            };
            
            card.MouseLeave += (s, e) =>
            {
                card.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(40, 255, 255, 255));
            };
            
            return card;
        }
        
        /// <summary>
        /// 显示备忘录列表视图
        /// </summary>
        private void ShowMemoList()
        {
            _isEditMode = false;
            _currentMemo = null;
            
            if (MemoListView != null) MemoListView.Visibility = Visibility.Visible;
            if (MemoEditView != null) MemoEditView.Visibility = Visibility.Collapsed;
            
            // 切换到主页模式的标题栏
            SwitchToHomeMode();
            
            // 恢复默认窗口标题
            Title = "备忘录";
        }
        
        /// <summary>
        /// 显示备忘录编辑视图
        /// </summary>
        private void ShowMemoEdit(MemoModel memo)
        {
            _isEditMode = true;
            _currentMemo = memo;
            
            if (MemoListView != null) MemoListView.Visibility = Visibility.Collapsed;
            if (MemoEditView != null) MemoEditView.Visibility = Visibility.Visible;
            
            // 切换到编辑模式的标题栏
            SwitchToEditMode();
            
            // 设置编辑内容
            if (NoteTextBox != null)
            {
                _isLoadedFromDisk = true;
                NoteTextBox.Text = memo.Content;
                _isLoadedFromDisk = false;
                NoteTextBox.Focus();
            }
            
            // 更新标题
            UpdateWindowTitle();
        }
        
        /// <summary>
        /// 编辑备忘录
        /// </summary>
        private void EditMemo(MemoModel memo)
        {
            ShowMemoEdit(memo);
        }
        
        /// <summary>
        /// 更新窗口标题
        /// </summary>
        private void UpdateWindowTitle()
        {
            if (_isEditMode && _currentMemo != null)
            {
                Title = _currentMemo.DisplayTitle;
                if (EditingMemoTitle != null)
                {
                    EditingMemoTitle.Text = _currentMemo.DisplayTitle;
                }
            }
            else
            {
                Title = "备忘录";
            }
        }
        
        /// <summary>
        /// 新建备忘录按钮点击事件
        /// </summary>
        private void AddMemoButton_Click(object sender, RoutedEventArgs e)
        {
            var newMemo = new MemoModel
            {
                Title = "新建备忘录",
                Content = "",
                CreatedTime = DateTime.Now,
                ModifiedTime = DateTime.Now
            };
            
            _memos.Add(newMemo);
            SaveMemosToDisk();
            RefreshMemoList();
            EditMemo(newMemo);
        }
        
        /// <summary>
        /// 返回列表按钮点击事件
        /// </summary>
        private void BackToListButton_Click(object sender, RoutedEventArgs e)
        {
            ShowMemoList();
        }
        
        /// <summary>
        /// 删除备忘录按钮点击事件
        /// </summary>
        private void DeleteMemoButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentMemo == null) return;
            
            var result = System.Windows.MessageBox.Show(
                $"确定要删除备忘录 \"{_currentMemo.DisplayTitle}\" 吗？",
                "删除确认",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                _memos.RemoveAll(m => m.Id == _currentMemo.Id);
                SaveMemosToDisk();
                
                // 如果删除后没有备忘录了，创建一个默认的
                if (!_memos.Any())
                {
                    CreateDefaultMemo();
                }
                
                RefreshMemoList();
                ShowMemoList();
                
                if (StatusText != null)
                {
                    StatusText.Text = "备忘录已删除";
                }
            }
        }
        
        /// <summary>
        /// 切换到主页模式标题栏
        /// </summary>
        private void SwitchToHomeMode()
        {
            if (HomeTitlePanel != null) HomeTitlePanel.Visibility = Visibility.Visible;
            if (EditTitlePanel != null) EditTitlePanel.Visibility = Visibility.Collapsed;
            if (HomeModeBtns != null) HomeModeBtns.Visibility = Visibility.Visible;
            if (EditModeBtns != null) EditModeBtns.Visibility = Visibility.Collapsed;
        }
        
        /// <summary>
        /// 切换到编辑模式标题栏
        /// </summary>
        private void SwitchToEditMode()
        {
            if (HomeTitlePanel != null) HomeTitlePanel.Visibility = Visibility.Collapsed;
            if (EditTitlePanel != null) EditTitlePanel.Visibility = Visibility.Visible;
            if (HomeModeBtns != null) HomeModeBtns.Visibility = Visibility.Collapsed;
            if (EditModeBtns != null) EditModeBtns.Visibility = Visibility.Visible;
        }
        
        /// <summary>
        /// 添加新备忘录的统一方法
        /// </summary>
        private void AddNewMemo()
        {
            var newMemo = new MemoModel
            {
                Title = "新建备忘录",
                Content = "",
                CreatedTime = DateTime.Now,
                ModifiedTime = DateTime.Now
            };
            
            _memos.Add(newMemo);
            SaveMemosToDisk();
            RefreshMemoList();
            EditMemo(newMemo);
        }
        
        /// <summary>
        /// 删除当前备忘录的统一方法
        /// </summary>
        private void DeleteCurrentMemo()
        {
            if (_currentMemo == null) return;
            
            if (_showDeletePrompt)
            {
                // 显示自定义删除确认对话框
                bool shouldDelete = ShowDeleteConfirmDialog(_currentMemo.DisplayTitle);
                if (!shouldDelete) return;
            }
            
            // 执行删除操作
            _memos.RemoveAll(m => m.Id == _currentMemo.Id);
            SaveMemosToDisk();
            
            // 如果删除后没有备忘录了，创建一个默认的
            if (!_memos.Any())
            {
                CreateDefaultMemo();
            }
            
            RefreshMemoList();
            ShowMemoList();
            
            if (StatusText != null)
            {
                StatusText.Text = "备忘录已删除";
            }
        }

        /// <summary>
        /// 显示删除确认对话框
        /// </summary>
        private bool ShowDeleteConfirmDialog(string memoTitle)
        {
            var dialog = new Window
            {
                Title = "删除确认",
                Width = 400,
                Height = 180,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                ResizeMode = ResizeMode.NoResize,
                WindowStyle = WindowStyle.ToolWindow
            };

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // 消息文本
            var messageText = new TextBlock
            {
                Text = $"确定要删除备忘录 \"{memoTitle}\" 吗？",
                TextAlignment = TextAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 14,
                Margin = new Thickness(20),
                TextWrapping = TextWrapping.Wrap
            };
            Grid.SetRow(messageText, 0);
            grid.Children.Add(messageText);

            // "不再提示"复选框
            var dontAskCheckBox = new System.Windows.Controls.CheckBox
            {
                Content = "不再提示，直接删除",
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                Margin = new Thickness(20, 0, 20, 10)
            };
            Grid.SetRow(dontAskCheckBox, 1);
            grid.Children.Add(dontAskCheckBox);

            // 按钮面板
            var buttonPanel = new StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                Margin = new Thickness(20, 0, 20, 20)
            };

            var deleteButton = new System.Windows.Controls.Button
            {
                Content = "删除",
                Width = 80,
                Height = 30,
                Margin = new Thickness(5)
            };

            var cancelButton = new System.Windows.Controls.Button
            {
                Content = "取消",
                Width = 80,
                Height = 30,
                Margin = new Thickness(5)
            };

            bool result = false;

            deleteButton.Click += (s, e) =>
            {
                if (dontAskCheckBox.IsChecked == true)
                {
                    _showDeletePrompt = false;
                    SaveSettingsToDisk();
                }
                result = true;
                dialog.Close();
            };

            cancelButton.Click += (s, e) => dialog.Close();

            buttonPanel.Children.Add(deleteButton);
            buttonPanel.Children.Add(cancelButton);

            Grid.SetRow(buttonPanel, 2);
            grid.Children.Add(buttonPanel);

            dialog.Content = grid;
            dialog.ShowDialog();

            return result;
        }
        
        /// <summary>
        /// 更新图钉按钮状态
        /// </summary>
        private void UpdatePinButtonState()
        {
            if (PinButton != null && PinIcon != null)
            {
                if (_isWindowPinned)
                {
                    // 固定状态：改变颜色和提示文本
                    PinIcon.Fill = new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 165, 0)); // 橙色
                    PinButton.ToolTip = "取消固定窗口";
                }
                else
                {
                    // 未固定状态：恢复默认颜色
                    PinIcon.Fill = new SolidColorBrush(System.Windows.Media.Color.FromArgb(224, 255, 255, 255)); // 默认白色
                    PinButton.ToolTip = "固定窗口";
                }
            }
            
            // 更新状态信息
            if (StatusText != null)
            {
                StatusText.Text = _isWindowPinned ? "窗口已固定，无法拖动" : "窗口已解除固定";
            }
        }

        #region 退出处理

        /// <summary>
        /// 处理应用程序退出
        /// </summary>
        private void HandleApplicationExit()
        {
            if (_showExitPrompt)
            {
                ShowExitConfirmDialog();
            }
            else
            {
                // 直接完全退出程序
                System.Windows.Application.Current.Shutdown();
            }
        }

        /// <summary>
        /// 显示退出确认对话框
        /// </summary>
        private void ShowExitConfirmDialog()
        {
            var dialog = new Window
            {
                Title = "退出确认",
                Width = 400,
                Height = 200,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                ResizeMode = ResizeMode.NoResize,
                WindowStyle = WindowStyle.ToolWindow
            };

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // 消息文本
            var messageText = new TextBlock
            {
                Text = "您要如何退出程序？",
                TextAlignment = TextAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 14,
                Margin = new Thickness(20)
            };
            Grid.SetRow(messageText, 0);
            grid.Children.Add(messageText);

            // "不再提示"复选框
            var dontAskCheckBox = new System.Windows.Controls.CheckBox
            {
                Content = "不再提示（可在托盘菜单中重新设置）",
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                Margin = new Thickness(20, 0, 20, 10)
            };
            Grid.SetRow(dontAskCheckBox, 1);
            grid.Children.Add(dontAskCheckBox);

            // 按钮面板
            var buttonPanel = new StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                Margin = new Thickness(20, 0, 20, 20)
            };

            var toTrayButton = new System.Windows.Controls.Button
            {
                Content = "最小化到托盘",
                Width = 100,
                Height = 30,
                Margin = new Thickness(5)
            };

            var exitButton = new System.Windows.Controls.Button
            {
                Content = "完全退出",
                Width = 100,
                Height = 30,
                Margin = new Thickness(5)
            };

            var cancelButton = new System.Windows.Controls.Button
            {
                Content = "取消",
                Width = 100,
                Height = 30,
                Margin = new Thickness(5)
            };

            toTrayButton.Click += (s, e) =>
            {
                if (dontAskCheckBox.IsChecked == true)
                {
                    _showExitPrompt = false;
                    SaveSettingsToDisk();
                }
                Hide(); // 隐藏到托盘
                dialog.Close();
            };

            exitButton.Click += (s, e) =>
            {
                if (dontAskCheckBox.IsChecked == true)
                {
                    _showExitPrompt = false;
                    SaveSettingsToDisk();
                }
                dialog.Close();
                System.Windows.Application.Current.Shutdown();
            };

            cancelButton.Click += (s, e) => dialog.Close();

            buttonPanel.Children.Add(toTrayButton);
            buttonPanel.Children.Add(exitButton);
            buttonPanel.Children.Add(cancelButton);

            Grid.SetRow(buttonPanel, 2);
            grid.Children.Add(buttonPanel);

            dialog.Content = grid;
            dialog.ShowDialog();
        }

        #endregion
        
        #endregion






    }
}