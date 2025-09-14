﻿using System;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
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
        private readonly string _memosFilePath; // 旧版本兼容
        private readonly string _memosMetadataFilePath; // 新版本元数据
        private readonly string _settingsFilePath;
        private readonly string _contentDir; // Markdown 内容目录
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
        private System.Windows.Threading.DispatcherTimer _autoSaveMemoTimer;
        
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

        // 搜索状态管理
        private string _currentSearchText = string.Empty;
        private int _currentSearchIndex = -1;
        private List<int> _searchMatches = new List<int>();

        // 背景透明度设置 (0.0-1.0，对应0%-100%)
        private double _backgroundOpacity = 0.1;

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
                AppInfoText.Text = $"版本：1.2.1 | 数据目录：{appDataDir}";
            }
            
            // 更新状态信息
            if (StatusText != null)
            {
                StatusText.Text = "就绪";
            }
            
            // 初始化位置相关控件
            InitializePositionControls();
            
            // 更新固定窗口按钮状态
            UpdatePinButtonState();
            
            // 初始化透明度滑块
            if (BackgroundOpacitySlider != null)
            {
                // 将实际透明度转换回滑块显示值（0-100%）
                // 公式：滑块值 = 实际透明度 * 100 / 0.6
                BackgroundOpacitySlider.Value = (_backgroundOpacity * 100) / 0.6;
                UpdateOpacityValueText();
                UpdateBackgroundOpacity(); // 应用背景透明度
                
                // 延迟更新进度条，确保控件已完全加载
                Dispatcher.BeginInvoke(new Action(() => UpdateProgressBar(BackgroundOpacitySlider)), System.Windows.Threading.DispatcherPriority.Loaded);
            }
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

                // 设置窗口图标
                SetWindowIcon();

                _appDataDir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
                _noteFilePath = System.IO.Path.Combine(_appDataDir, "notes.json");
                _memosFilePath = System.IO.Path.Combine(_appDataDir, "memos.json");
                _memosMetadataFilePath = System.IO.Path.Combine(_appDataDir, "memos_metadata.json");
                _settingsFilePath = System.IO.Path.Combine(_appDataDir, "settings.json");
                _contentDir = System.IO.Path.Combine(_appDataDir, "content");

                Directory.CreateDirectory(_appDataDir);
                Directory.CreateDirectory(_contentDir);
            
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
                
                // 初始化自动保存备忘录定时器（防抖）
                _autoSaveMemoTimer = new System.Windows.Threading.DispatcherTimer();
                _autoSaveMemoTimer.Interval = TimeSpan.FromMilliseconds(500); // 停止输入500ms后保存
                _autoSaveMemoTimer.Tick += AutoSaveMemoTimer_Tick;
            
                ConfigureWindow();
                ConfigureTrayIcon();
                LoadMemosFromDisk();
                LoadSettingsFromDisk();
                
                // 监听窗口位置变化
                this.LocationChanged += MainWindow_LocationChanged;
                
                // 在所有控件初始化完成后设置状态
                this.Loaded += (s, e) =>
                {
                    // 初始化设置控件状态（应用加载的设置）
                    InitializeSettingsControls();

                    // 启动位置更新定时器
                    _positionUpdateTimer.Start();
                    UpdateCurrentPositionDisplay();

                    // 确保备忘录数据已加载后再刷新界面
                    if (_memos == null || !_memos.Any())
                    {
                        // 如果没有备忘录，创建默认的
                        CreateDefaultMemo();
                    }

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

        /// <summary>
        /// 设置窗口图标
        /// </summary>
        private void SetWindowIcon()
        {
            try
            {
                string iconPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logo", "logo.ico");
                if (System.IO.File.Exists(iconPath))
                {
                    var uri = new Uri(iconPath, UriKind.Absolute);
                    var bitmapFrame = BitmapFrame.Create(uri);
                    this.Icon = bitmapFrame;
                }
            }
            catch
            {
                // 如果加载失败，使用默认图标（无图标）
            }
        }

        private void ConfigureTrayIcon()
        {
            _notifyIcon = new Forms.NotifyIcon();
            _notifyIcon.Text = "DesktopMemo 便签 - 桌面便签工具";
            _notifyIcon.Visible = true;
            
            // 设置自定义托盘图标
            try
            {
                string iconPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logo", "logo.ico");
                if (System.IO.File.Exists(iconPath))
                {
                    _notifyIcon.Icon = new System.Drawing.Icon(iconPath);
                }
                else
                {
                    // 如果文件不存在，使用系统默认图标
                    _notifyIcon.Icon = System.Drawing.SystemIcons.Application;
                }
            }
            catch
            {
                // 如果加载失败，使用系统默认图标
                _notifyIcon.Icon = System.Drawing.SystemIcons.Application;
            }

            var menu = new Forms.ContextMenuStrip();
            
            // 应用暗色主题和圆角设计
            menu.Renderer = new DarkTrayMenuRenderer();
            menu.ShowImageMargin = false;
            menu.ShowCheckMargin = true;
            menu.AutoSize = true;
            menu.Padding = new Forms.Padding(8, 4, 8, 4);
            
            // 设置菜单背景色以隐藏底层白色窗口
            menu.BackColor = System.Drawing.Color.FromArgb(45, 45, 48);
            menu.ForeColor = System.Drawing.Color.FromArgb(241, 241, 241);
            
            // 启用双缓冲以减少闪烁
            typeof(Forms.ToolStripDropDownMenu).InvokeMember("DoubleBuffered",
                System.Reflection.BindingFlags.SetProperty | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
                null, menu, new object[] { true });
            
            // 创建统一的字体样式
            var regularFont = new System.Drawing.Font("Microsoft YaHei", 9F, System.Drawing.FontStyle.Regular);
            var boldFont = new System.Drawing.Font("Microsoft YaHei", 9F, System.Drawing.FontStyle.Bold);
            
            // 主要功能组
            var showHideItem = new Forms.ToolStripMenuItem("🏠 显示/隐藏窗口");
            showHideItem.Font = boldFont;
            showHideItem.Click += (s, e) => ToggleWindowVisibility();
            
            var newNoteItem = new Forms.ToolStripMenuItem("📝 新建便签");
            newNoteItem.Font = regularFont;
            newNoteItem.Click += (s, e) => CreateNewNote();
            
            var settingsItem = new Forms.ToolStripMenuItem("⚙️ 设置");
            settingsItem.Font = regularFont;
            settingsItem.Click += (s, e) => {
                if (Visibility == Visibility.Hidden) Show();
                ToggleSettingsPanel();
            };
            
            // 分隔符
            var separator1 = new Forms.ToolStripSeparator();
            
            // 窗口控制组
            var windowControlGroup = new Forms.ToolStripMenuItem("🖼️ 窗口控制");
            windowControlGroup.Font = regularFont;
            
            // 置顶模式子菜单
            var topmostGroup = new Forms.ToolStripMenuItem("📌 置顶模式");
            topmostGroup.Font = regularFont;
            var normalModeItem = new Forms.ToolStripMenuItem("普通模式");
            normalModeItem.Font = regularFont;
            var desktopModeItem = new Forms.ToolStripMenuItem("桌面置顶");
            desktopModeItem.Font = regularFont;
            var alwaysModeItem = new Forms.ToolStripMenuItem("总是置顶");
            alwaysModeItem.Font = regularFont;
            
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
            

            
            // 窗口位置控制
            var positionGroup = new Forms.ToolStripMenuItem("📍 窗口位置");
            positionGroup.Font = regularFont;
            var quickPosGroup = new Forms.ToolStripMenuItem("快速定位");
            quickPosGroup.Font = regularFont;
            
            var topLeftItem = new Forms.ToolStripMenuItem("左上角");
            topLeftItem.Font = regularFont;
            topLeftItem.Click += (s, e) => MoveToTrayPresetPosition("TopLeft");
            var topCenterItem = new Forms.ToolStripMenuItem("顶部中央");
            topCenterItem.Font = regularFont;
            topCenterItem.Click += (s, e) => MoveToTrayPresetPosition("TopCenter");
            var topRightItem = new Forms.ToolStripMenuItem("右上角");
            topRightItem.Font = regularFont;
            topRightItem.Click += (s, e) => MoveToTrayPresetPosition("TopRight");
            var centerItem = new Forms.ToolStripMenuItem("屏幕中央");
            centerItem.Font = regularFont;
            centerItem.Click += (s, e) => MoveToTrayPresetPosition("Center");
            var bottomLeftItem = new Forms.ToolStripMenuItem("左下角");
            bottomLeftItem.Font = regularFont;
            bottomLeftItem.Click += (s, e) => MoveToTrayPresetPosition("BottomLeft");
            var bottomRightItem = new Forms.ToolStripMenuItem("右下角");
            bottomRightItem.Font = regularFont;
            bottomRightItem.Click += (s, e) => MoveToTrayPresetPosition("BottomRight");
            
            quickPosGroup.DropDownItems.AddRange(new Forms.ToolStripItem[] {
                topLeftItem, topCenterItem, topRightItem, new Forms.ToolStripSeparator(),
                centerItem, new Forms.ToolStripSeparator(),
                bottomLeftItem, bottomRightItem
            });
            
            var rememberPosItem = new Forms.ToolStripMenuItem("记住当前位置");
            rememberPosItem.Font = regularFont;
            var restorePosItem = new Forms.ToolStripMenuItem("恢复保存位置");
            restorePosItem.Font = regularFont;
            
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
            _trayClickThroughItem.Font = regularFont;
            _trayClickThroughItem.Checked = _isClickThroughEnabled;
            _trayClickThroughItem.CheckOnClick = true;
            _trayClickThroughItem.CheckedChanged += TrayClickThrough_CheckedChanged;
            
            windowControlGroup.DropDownItems.AddRange(new Forms.ToolStripItem[] {
                topmostGroup, positionGroup, _trayClickThroughItem
            });
            
            // 分隔符
            var separator2 = new Forms.ToolStripSeparator();
            
            // 工具组
            var toolsGroup = new Forms.ToolStripMenuItem("🛠️ 工具");
            toolsGroup.Font = regularFont;
            
            var exportItem = new Forms.ToolStripMenuItem("📤 导出便签");
            exportItem.Font = regularFont;
            exportItem.Click += (s, e) => ExportNotes();
            
            var importItem = new Forms.ToolStripMenuItem("📥 导入便签");
            importItem.Font = regularFont;
            importItem.Click += (s, e) => ImportNotes();
            
            var clearItem = new Forms.ToolStripMenuItem("🗑️ 清空内容");
            clearItem.Font = regularFont;
            clearItem.Click += (s, e) => ClearNoteContent();
            
            toolsGroup.DropDownItems.AddRange(new Forms.ToolStripItem[] {
                exportItem, importItem, clearItem
            });
            
            // 分隔符
            var separator3 = new Forms.ToolStripSeparator();
            
            // 帮助和关于
            var aboutItem = new Forms.ToolStripMenuItem("ℹ️ 关于");
            aboutItem.Font = regularFont;
            aboutItem.Click += (s, e) => ShowAboutDialog();
            
            var exitPromptItem = new Forms.ToolStripMenuItem("🔄 重新启用退出提示");
            exitPromptItem.Font = regularFont;
            exitPromptItem.Click += (s, e) => 
            {
                _showExitPrompt = true;
                SaveSettingsToDisk();
                _notifyIcon.ShowBalloonTip(2000, "设置已更新", "已重新启用退出提示", Forms.ToolTipIcon.Info);
            };
            
            var deletePromptItem = new Forms.ToolStripMenuItem("🗑️ 重新启用删除提示");
            deletePromptItem.Font = regularFont;
            deletePromptItem.Click += (s, e) => 
            {
                _showDeletePrompt = true;
                SaveSettingsToDisk();
                _notifyIcon.ShowBalloonTip(2000, "设置已更新", "已重新启用删除确认提示", Forms.ToolTipIcon.Info);
            };

            var exitItem = new Forms.ToolStripMenuItem("❌ 退出");
            exitItem.Font = boldFont;
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
            
            // 为子菜单也应用暗色主题
            ApplyDarkThemeToSubMenus(menu);
            
            // 主菜单不再设置圆角，以提高性能
            
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
        /// 从磁盘加载备忘录数据（优先加载Markdown格式）
        /// </summary>
        private async void LoadMemosFromDisk()
        {
            try
            {
                // 优先尝试加载Markdown文件
                if (Directory.Exists(_contentDir))
                {
                    await LoadMemosFromMarkdownAsync();

                    // 如果有数据，返回
                    if (_memos.Any())
                        return;
                }

                // 尝试从旧格式迁移
                if (System.IO.File.Exists(_memosMetadataFilePath))
                {
                    await LoadMemosFromHybridStorageAsync();

                    if (_memos.Any())
                    {
                        // 迁移到新的Markdown格式
                        await MigrateToMarkdownStorageAsync();
                        return;
                    }
                }

                // 尝试加载旧的 JSON 格式
                if (System.IO.File.Exists(_memosFilePath))
                {
                    var json = System.IO.File.ReadAllText(_memosFilePath, Encoding.UTF8);
                    var memosData = JsonSerializer.Deserialize<MemosData>(json);
                    _memos = memosData?.Memos ?? new List<MemoModel>();

                    // 迁移到新格式
                    if (_memos.Any())
                    {
                        await MigrateToMarkdownStorageAsync();
                        return;
                    }
                }
                // 如果没有新格式数据，尝试从最旧的格式迁移
                else if (System.IO.File.Exists(_noteFilePath))
                {
                    MigrateFromOldNoteFormat();

                    // 迁移到新格式
                    if (_memos.Any())
                    {
                        await MigrateToMarkdownStorageAsync();
                        return;
                    }
                }

                // 如果没有任何备忘录，创建一个默认的
                if (!_memos.Any())
                {
                    CreateDefaultMemo();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载备忘录失败: {ex.Message}");
                // 加载失败时创建默认备忘录
                CreateDefaultMemo();
            }
        }

        /// <summary>
        /// 迁移到简化的Markdown存储格式
        /// </summary>
        private async Task MigrateToMarkdownStorageAsync()
        {
            try
            {
                await SaveMemosToMarkdownAsync();
                System.Diagnostics.Debug.WriteLine("已成功迁移到Markdown存储格式");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"迁移到Markdown存储格式失败: {ex.Message}");
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
                    SaveMemosAsync();
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
            SaveMemosAsync();
        }

        #region 简化的Markdown存储模式

        /// <summary>
        /// 生成基于时间的文件名
        /// </summary>
        private string GenerateTimeBasedFileName(DateTime createdTime)
        {
            return $"{createdTime:yyyyMMdd_HHmmss}.md";
        }

        /// <summary>
        /// 从Markdown文件加载所有备忘录
        /// </summary>
        private async Task LoadMemosFromMarkdownAsync()
        {
            try
            {
                if (!Directory.Exists(_contentDir))
                    return;

                var memosList = new List<MemoModel>();
                var markdownFiles = Directory.GetFiles(_contentDir, "*.md");

                foreach (var filePath in markdownFiles)
                {
                    try
                    {
                        var fileName = System.IO.Path.GetFileNameWithoutExtension(filePath);
                        var content = await System.IO.File.ReadAllTextAsync(filePath, Encoding.UTF8);

                        // 解析Markdown文件
                        var memo = ParseMarkdownFile(fileName, content, System.IO.File.GetCreationTime(filePath), System.IO.File.GetLastWriteTime(filePath));
                        if (memo != null)
                        {
                            memosList.Add(memo);
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"加载备忘录文件 {filePath} 失败: {ex.Message}");
                    }
                }

                _memos = memosList;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载Markdown备忘录失败: {ex.Message}");
                _memos.Clear();
            }
        }

        /// <summary>
        /// 解析Markdown文件内容为备忘录对象
        /// </summary>
        private MemoModel? ParseMarkdownFile(string fileName, string content, DateTime createdTime, DateTime modifiedTime)
        {
            try
            {
                var lines = content.Split('\n');
                string title = "";
                string bodyContent = "";
                bool foundFirstHeading = false;

                foreach (var line in lines)
                {
                    var trimmedLine = line.Trim();

                    // 找到第一个标题作为备忘录标题
                    if (!foundFirstHeading && trimmedLine.StartsWith("# "))
                    {
                        title = trimmedLine.Substring(2).Trim();
                        foundFirstHeading = true;
                        continue;
                    }

                    // 剩余内容作为正文
                    if (foundFirstHeading || !trimmedLine.StartsWith("#"))
                    {
                        bodyContent += line + "\n";
                    }
                }

                // 如果没有找到标题，使用文件名或内容开头
                if (string.IsNullOrWhiteSpace(title))
                {
                    // 尝试从文件名解析时间作为标题的一部分
                    if (DateTime.TryParseExact(fileName, "yyyyMMdd_HHmmss", null, System.Globalization.DateTimeStyles.None, out var parsedTime))
                    {
                        title = $"备忘录 {parsedTime:yyyy年MM月dd日 HH:mm}";
                    }
                    else
                    {
                        var firstLine = bodyContent.Split('\n').FirstOrDefault()?.Trim();
                        title = !string.IsNullOrWhiteSpace(firstLine) && firstLine.Length > 30
                            ? firstLine.Substring(0, 30) + "..."
                            : firstLine ?? "未命名备忘录";
                    }
                }

                return new MemoModel
                {
                    Id = Guid.NewGuid().ToString(),
                    Title = title,
                    Content = bodyContent.Trim(),
                    CreatedTime = createdTime,
                    ModifiedTime = modifiedTime
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"解析Markdown文件失败: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 保存所有备忘录为Markdown文件
        /// </summary>
        private async Task SaveMemosToMarkdownAsync()
        {
            try
            {
                // 确保目录存在
                Directory.CreateDirectory(_contentDir);

                foreach (var memo in _memos)
                {
                    // 生成基于创建时间的文件名
                    var fileName = GenerateTimeBasedFileName(memo.CreatedTime);
                    var filePath = System.IO.Path.Combine(_contentDir, fileName);

                    // 如果文件名冲突，添加后缀
                    int suffix = 1;
                    while (System.IO.File.Exists(filePath))
                    {
                        var nameWithoutExt = System.IO.Path.GetFileNameWithoutExtension(fileName);
                        fileName = $"{nameWithoutExt}_{suffix:D2}.md";
                        filePath = System.IO.Path.Combine(_contentDir, fileName);
                        suffix++;
                    }

                    // 创建简化的Markdown内容
                    var markdownContent = CreateSimpleMarkdownContent(memo.Title, memo.Content);
                    await System.IO.File.WriteAllTextAsync(filePath, markdownContent, Encoding.UTF8);

                    // 设置文件时间
                    System.IO.File.SetCreationTime(filePath, memo.CreatedTime);
                    System.IO.File.SetLastWriteTime(filePath, memo.ModifiedTime);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"保存Markdown备忘录失败: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 创建简化的Markdown格式内容
        /// </summary>
        private string CreateSimpleMarkdownContent(string title, string content)
        {
            var sb = new StringBuilder();

            // 添加标题
            if (!string.IsNullOrWhiteSpace(title))
            {
                sb.AppendLine($"# {title}");
                sb.AppendLine();
            }

            // 添加内容
            sb.Append(content);

            return sb.ToString();
        }

        #endregion

        /// <summary>
        /// 生成内容文件名
        /// </summary>
        private string GenerateContentFileName(string memoId, string title)
        {
            // 清理标题中的无效字符
            var cleanTitle = string.IsNullOrWhiteSpace(title) ? "untitled" : title;
            foreach (char c in System.IO.Path.GetInvalidFileNameChars())
            {
                cleanTitle = cleanTitle.Replace(c, '_');
            }

            // 限制文件名长度
            if (cleanTitle.Length > 50)
                cleanTitle = cleanTitle.Substring(0, 50);

            return $"{memoId}_{cleanTitle}.md";
        }

        /// <summary>
        /// 保存备忘录内容到 Markdown 文件
        /// </summary>
        private async Task SaveMemoContentAsync(string memoId, string title, string content)
        {
            try
            {
                string fileName = GenerateContentFileName(memoId, title);
                string filePath = System.IO.Path.Combine(_contentDir, fileName);

                // 创建 Markdown 格式的内容
                var markdownContent = CreateMarkdownContent(title, content);

                await System.IO.File.WriteAllTextAsync(filePath, markdownContent, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                // 记录错误但不中断程序
                System.Diagnostics.Debug.WriteLine($"保存备忘录内容失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 从 Markdown 文件加载备忘录内容
        /// </summary>
        private async Task<string> LoadMemoContentAsync(string contentFileName)
        {
            try
            {
                if (string.IsNullOrEmpty(contentFileName))
                    return string.Empty;

                string filePath = System.IO.Path.Combine(_contentDir, contentFileName);
                if (!System.IO.File.Exists(filePath))
                    return string.Empty;

                string markdownContent = await System.IO.File.ReadAllTextAsync(filePath, Encoding.UTF8);
                return ExtractContentFromMarkdown(markdownContent);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载备忘录内容失败: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// 创建 Markdown 格式的内容
        /// </summary>
        private string CreateMarkdownContent(string title, string content)
        {
            var sb = new StringBuilder();

            // 添加元数据头部
            sb.AppendLine("---");
            sb.AppendLine($"title: \"{title?.Replace("\"", "\\\"")}\"");
            sb.AppendLine($"created: \"{DateTime.Now:yyyy-MM-ddTHH:mm:ss}\"");
            sb.AppendLine("---");
            sb.AppendLine();

            // 添加标题
            if (!string.IsNullOrWhiteSpace(title))
            {
                sb.AppendLine($"# {title}");
                sb.AppendLine();
            }

            // 添加内容
            sb.Append(content);

            return sb.ToString();
        }

        /// <summary>
        /// 从 Markdown 内容中提取纯文本
        /// </summary>
        private string ExtractContentFromMarkdown(string markdownContent)
        {
            if (string.IsNullOrEmpty(markdownContent))
                return string.Empty;

            var lines = markdownContent.Split('\n');
            var contentLines = new List<string>();
            bool inFrontMatter = false;
            bool foundContent = false;

            foreach (var line in lines)
            {
                // 跳过 YAML front matter
                if (line.Trim() == "---")
                {
                    if (!foundContent)
                    {
                        inFrontMatter = !inFrontMatter;
                        continue;
                    }
                }

                if (inFrontMatter)
                    continue;

                foundContent = true;

                // 跳过第一级标题（通常是标题）
                if (line.StartsWith("# "))
                    continue;

                contentLines.Add(line);
            }

            // 移除开头的空行
            while (contentLines.Count > 0 && string.IsNullOrWhiteSpace(contentLines[0]))
            {
                contentLines.RemoveAt(0);
            }

            return string.Join("\n", contentLines).TrimEnd();
        }

        /// <summary>
        /// 删除备忘录内容文件
        /// </summary>
        private void DeleteMemoContentFile(string contentFileName)
        {
            try
            {
                if (string.IsNullOrEmpty(contentFileName))
                    return;

                string filePath = System.IO.Path.Combine(_contentDir, contentFileName);
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"删除备忘录内容文件失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 保存备忘录数据到混合存储格式
        /// </summary>
        private async Task SaveMemosToHybridStorageAsync()
        {
            try
            {
                // 保存所有备忘录的内容到独立文件
                var metadataList = new List<MemoMetadata>();

                foreach (var memo in _memos)
                {
                    string fileName = GenerateContentFileName(memo.Id, memo.Title);
                    await SaveMemoContentAsync(memo.Id, memo.Title, memo.Content);
                    metadataList.Add(memo.ToMetadata(fileName));
                }

                // 保存元数据
                var memosMetadata = new MemosMetadata
                {
                    Memos = metadataList,
                    CurrentMemoId = _currentMemo?.Id ?? string.Empty,
                    Version = 2
                };

                var json = JsonSerializer.Serialize(memosMetadata, new JsonSerializerOptions { WriteIndented = true });
                await System.IO.File.WriteAllTextAsync(_memosMetadataFilePath, json, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"保存混合存储格式失败: {ex.Message}");
                // 如果新格式保存失败，回退到旧格式
                SaveMemosAsync();
            }
        }

        /// <summary>
        /// 从混合存储格式加载备忘录
        /// </summary>
        private async Task LoadMemosFromHybridStorageAsync()
        {
            try
            {
                if (!System.IO.File.Exists(_memosMetadataFilePath))
                    return;

                var json = await System.IO.File.ReadAllTextAsync(_memosMetadataFilePath, Encoding.UTF8);
                var memosMetadata = JsonSerializer.Deserialize<MemosMetadata>(json);

                if (memosMetadata?.Memos == null) return;

                var memosList = new List<MemoModel>();

                foreach (var metadata in memosMetadata.Memos)
                {
                    string content = await LoadMemoContentAsync(metadata.ContentFileName);
                    var memo = MemoModel.FromMetadata(metadata, content);
                    memosList.Add(memo);
                }

                _memos = memosList;

                // 设置当前备忘录
                if (!string.IsNullOrEmpty(memosMetadata.CurrentMemoId))
                {
                    _currentMemo = _memos.FirstOrDefault(m => m.Id == memosMetadata.CurrentMemoId);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载混合存储格式失败: {ex.Message}");
                _memos.Clear();
            }
        }

        #region Markdown 语法支持

        /// <summary>
        /// 插入 Markdown 格式
        /// </summary>
        private void InsertMarkdownFormat(System.Windows.Controls.TextBox textBox, string prefix, string suffix = "")
        {
            if (textBox == null) return;

            int selectionStart = textBox.SelectionStart;
            int selectionLength = textBox.SelectionLength;
            string selectedText = textBox.SelectedText;

            if (string.IsNullOrEmpty(suffix))
                suffix = prefix;

            string formattedText;
            int newCaretPosition;

            if (selectionLength > 0)
            {
                // 有选中文本，包围选中内容
                formattedText = prefix + selectedText + suffix;
                newCaretPosition = selectionStart + formattedText.Length;
            }
            else
            {
                // 没有选中文本，插入格式并将光标置于中间
                formattedText = prefix + suffix;
                newCaretPosition = selectionStart + prefix.Length;
            }

            textBox.SelectedText = formattedText;
            textBox.CaretIndex = newCaretPosition;
            textBox.Focus();
        }

        /// <summary>
        /// 插入 Markdown 标题
        /// </summary>
        private void InsertMarkdownHeading(System.Windows.Controls.TextBox textBox, int level)
        {
            if (textBox == null) return;

            string prefix = new string('#', level) + " ";
            int caretIndex = textBox.CaretIndex;

            // 找到当前行的开始
            string text = textBox.Text;
            int lineStart = caretIndex;
            while (lineStart > 0 && text[lineStart - 1] != '\n')
                lineStart--;

            // 检查当前行是否已经是标题
            string currentLine = "";
            int lineEnd = caretIndex;
            while (lineEnd < text.Length && text[lineEnd] != '\n')
                lineEnd++;

            if (lineEnd > lineStart)
                currentLine = text.Substring(lineStart, lineEnd - lineStart);

            // 如果当前行已经是标题，则替换
            if (currentLine.StartsWith("#"))
            {
                var match = System.Text.RegularExpressions.Regex.Match(currentLine, @"^#+\s*(.*)");
                if (match.Success)
                {
                    string titleContent = match.Groups[1].Value;
                    string newLine = prefix + titleContent;
                    textBox.Select(lineStart, lineEnd - lineStart);
                    textBox.SelectedText = newLine;
                    textBox.CaretIndex = lineStart + newLine.Length;
                }
            }
            else
            {
                // 在行首插入标题格式
                textBox.CaretIndex = lineStart;
                textBox.SelectedText = prefix;
                textBox.CaretIndex = lineStart + prefix.Length;
            }

            textBox.Focus();
        }

        /// <summary>
        /// 插入 Markdown 列表
        /// </summary>
        private void InsertMarkdownList(System.Windows.Controls.TextBox textBox, bool isNumbered = false)
        {
            if (textBox == null) return;

            int caretIndex = textBox.CaretIndex;
            string text = textBox.Text;

            // 找到当前行的开始
            int lineStart = caretIndex;
            while (lineStart > 0 && text[lineStart - 1] != '\n')
                lineStart--;

            string listPrefix = isNumbered ? "1. " : "- ";

            // 在行首插入列表标记
            textBox.CaretIndex = lineStart;
            textBox.SelectedText = listPrefix;
            textBox.CaretIndex = lineStart + listPrefix.Length;
            textBox.Focus();
        }

        /// <summary>
        /// 处理 Markdown 快捷键
        /// </summary>
        private void HandleMarkdownShortcuts(System.Windows.Controls.TextBox textBox, System.Windows.Input.KeyEventArgs e)
        {
            if (e.KeyboardDevice.Modifiers == ModifierKeys.Control)
            {
                switch (e.Key)
                {
                    case Key.B:  // Ctrl+B 加粗
                        e.Handled = true;
                        InsertMarkdownFormat(textBox, "**");
                        break;

                    case Key.I:  // Ctrl+I 斜体
                        e.Handled = true;
                        InsertMarkdownFormat(textBox, "*");
                        break;

                    case Key.U:  // Ctrl+U 下划线（使用HTML标签）
                        e.Handled = true;
                        InsertMarkdownFormat(textBox, "<u>", "</u>");
                        break;

                    case Key.K:  // Ctrl+K 链接
                        e.Handled = true;
                        InsertMarkdownFormat(textBox, "[", "](url)");
                        break;

                    case Key.E:  // Ctrl+E 行内代码
                        e.Handled = true;
                        InsertMarkdownFormat(textBox, "`");
                        break;

                    case Key.D1:  // Ctrl+1 一级标题
                        e.Handled = true;
                        InsertMarkdownHeading(textBox, 1);
                        break;

                    case Key.D2:  // Ctrl+2 二级标题
                        e.Handled = true;
                        InsertMarkdownHeading(textBox, 2);
                        break;

                    case Key.D3:  // Ctrl+3 三级标题
                        e.Handled = true;
                        InsertMarkdownHeading(textBox, 3);
                        break;
                }
            }
            else if (e.KeyboardDevice.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift))
            {
                switch (e.Key)
                {
                    case Key.C:  // Ctrl+Shift+C 代码块
                        e.Handled = true;
                        InsertMarkdownFormat(textBox, "```\n", "\n```");
                        break;

                    case Key.L:  // Ctrl+Shift+L 无序列表
                        e.Handled = true;
                        InsertMarkdownList(textBox, false);
                        break;

                    case Key.O:  // Ctrl+Shift+O 有序列表
                        e.Handled = true;
                        InsertMarkdownList(textBox, true);
                        break;
                }
            }
        }

        #endregion

        /// <summary>
        /// 统一的备忘录保存方法
        /// </summary>
        private async void SaveMemosAsync()
        {
            try
            {
                await SaveMemosToMarkdownAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"异步保存失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 保存备忘录数据到磁盘（兼容旧格式）
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
                
                SaveMemosAsync();
                
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

            // 启动防抖定时器，而不是直接保存
            _autoSaveMemoTimer.Stop();
            _autoSaveMemoTimer.Start();

            // 更新窗口标题可以立即执行
            UpdateWindowTitle();

            // 清除搜索状态（文本内容已更改）
            ClearSearchState();
        }

        /// <summary>
        /// 处理全局窗口快捷键（当焦点不在文本框时）
        /// </summary>
        private void MainWindow_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            // 如果焦点在文本框上，让文本框的PreviewKeyDown处理
            if (NoteTextBox != null && NoteTextBox.IsFocused)
                return;

            // 处理 Ctrl 组合键
            if (e.KeyboardDevice.Modifiers == ModifierKeys.Control)
            {
                switch (e.Key)
                {
                    case Key.N:  // Ctrl+N 新建备忘录
                        e.Handled = true;
                        AddNewMemo();
                        ShowTemporaryMessage("新建备忘录");
                        break;

                    case Key.S:  // Ctrl+S 保存
                        e.Handled = true;
                        if (_currentMemo != null)
                        {
                            SaveCurrentMemo();
                            ShowTemporaryMessage("已保存");
                        }
                        break;

                    case Key.F:  // Ctrl+F 查找
                        e.Handled = true;
                        ShowSearchDialog();
                        break;

                    case Key.H:  // Ctrl+H 替换
                        e.Handled = true;
                        ShowReplaceDialog();
                        break;

                    case Key.Tab:  // Ctrl+Tab 切换到下一个备忘录
                        e.Handled = true;
                        SwitchToNextMemo();
                        break;
                }
            }
            // 处理 Ctrl+Shift 组合键
            else if (e.KeyboardDevice.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift))
            {
                switch (e.Key)
                {
                    case Key.Tab:  // Ctrl+Shift+Tab 切换到上一个备忘录
                        e.Handled = true;
                        SwitchToPreviousMemo();
                        break;
                }
            }
            // 处理 Shift 组合键
            else if (e.KeyboardDevice.Modifiers == ModifierKeys.Shift)
            {
                switch (e.Key)
                {
                    case Key.F3:  // Shift+F3 查找上一个
                        e.Handled = true;
                        if (!string.IsNullOrEmpty(_currentSearchText))
                            FindPrevious();
                        break;
                }
            }
            // 处理单独按键
            else
            {
                switch (e.Key)
                {
                    case Key.F3:  // F3 查找下一个
                        e.Handled = true;
                        if (!string.IsNullOrEmpty(_currentSearchText))
                            FindNext();
                        else
                            ShowSearchDialog();
                        break;
                }
            }
        }

        /// <summary>
        /// 处理文本框快捷键
        /// </summary>
        private void NoteTextBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            var textBox = sender as System.Windows.Controls.TextBox;
            if (textBox == null) return;

            // 优先处理 Markdown 快捷键
            HandleMarkdownShortcuts(textBox, e);
            if (e.Handled) return;

            // 处理 Ctrl 组合键
            if (e.KeyboardDevice.Modifiers == ModifierKeys.Control)
            {
                switch (e.Key)
                {
                    case Key.S:  // Ctrl+S 保存
                        e.Handled = true;
                        SaveCurrentMemo();
                        ShowTemporaryMessage("已保存");
                        break;

                    case Key.N:  // Ctrl+N 新建备忘录
                        e.Handled = true;
                        AddNewMemo();
                        break;

                    case Key.Tab:  // Ctrl+Tab 切换到下一个备忘录
                        e.Handled = true;
                        SwitchToNextMemo();
                        break;

                    case Key.F:  // Ctrl+F 查找
                        e.Handled = true;
                        ShowSearchDialog();
                        break;

                    case Key.H:  // Ctrl+H 替换
                        e.Handled = true;
                        ShowReplaceDialog();
                        break;

                    case Key.OemCloseBrackets:  // Ctrl+] 增加缩进
                        e.Handled = true;
                        IncreaseIndentation(textBox);
                        break;

                    case Key.OemOpenBrackets:  // Ctrl+[ 减少缩进
                        e.Handled = true;
                        DecreaseIndentation(textBox);
                        break;

                    case Key.D:  // Ctrl+D 复制当前行
                        e.Handled = true;
                        DuplicateCurrentLine(textBox);
                        break;
                }
            }
            // 处理 Ctrl+Shift 组合键
            else if (e.KeyboardDevice.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift))
            {
                switch (e.Key)
                {
                    case Key.Tab:  // Ctrl+Shift+Tab 切换到上一个备忘录
                        e.Handled = true;
                        SwitchToPreviousMemo();
                        break;
                }
            }
            // 处理 Shift 组合键
            else if (e.KeyboardDevice.Modifiers == ModifierKeys.Shift)
            {
                switch (e.Key)
                {
                    case Key.Tab:  // Shift+Tab 减少缩进
                        e.Handled = true;
                        DecreaseIndentation(textBox);
                        break;

                    case Key.F3:  // Shift+F3 查找上一个
                        e.Handled = true;
                        if (!string.IsNullOrEmpty(_currentSearchText))
                            FindPrevious();
                        break;
                }
            }
            // 处理单独按键
            else
            {
                switch (e.Key)
                {
                    case Key.Tab:  // Tab 插入缩进
                        e.Handled = true;
                        InsertIndentation(textBox);
                        break;

                    case Key.F3:  // F3 查找下一个
                        e.Handled = true;
                        if (!string.IsNullOrEmpty(_currentSearchText))
                            FindNext();
                        else
                            ShowSearchDialog();
                        break;
                }
            }
        }

        /// <summary>
        /// 插入缩进（4个空格）
        /// </summary>
        private void InsertIndentation(System.Windows.Controls.TextBox textBox)
        {
            int caretIndex = textBox.CaretIndex;
            string indentation = "    "; // 4个空格
            textBox.Text = textBox.Text.Insert(caretIndex, indentation);
            textBox.CaretIndex = caretIndex + indentation.Length;
        }

        /// <summary>
        /// 增加选中行的缩进
        /// </summary>
        private void IncreaseIndentation(System.Windows.Controls.TextBox textBox)
        {
            if (textBox.SelectedText.Length > 0)
            {
                // 处理多行缩进
                var selectedText = textBox.SelectedText;
                var lines = selectedText.Split('\n');
                var indentedLines = lines.Select(line => "    " + line);
                var newText = string.Join("\n", indentedLines);

                int selectionStart = textBox.SelectionStart;
                textBox.SelectedText = newText;
                textBox.SelectionStart = selectionStart;
                textBox.SelectionLength = newText.Length;
            }
            else
            {
                // 单行缩进
                InsertIndentation(textBox);
            }
        }

        /// <summary>
        /// 减少选中行的缩进
        /// </summary>
        private void DecreaseIndentation(System.Windows.Controls.TextBox textBox)
        {
            if (textBox.SelectedText.Length > 0)
            {
                // 处理多行取消缩进
                var selectedText = textBox.SelectedText;
                var lines = selectedText.Split('\n');
                var unindentedLines = lines.Select(line =>
                {
                    if (line.StartsWith("    "))
                        return line.Substring(4);
                    else if (line.StartsWith("\t"))
                        return line.Substring(1);
                    return line;
                });
                var newText = string.Join("\n", unindentedLines);

                int selectionStart = textBox.SelectionStart;
                textBox.SelectedText = newText;
                textBox.SelectionStart = selectionStart;
                textBox.SelectionLength = newText.Length;
            }
            else
            {
                // 单行取消缩进 - 找到当前行并移除缩进
                int caretIndex = textBox.CaretIndex;
                string text = textBox.Text;

                // 找到当前行的开始
                int lineStart = caretIndex;
                while (lineStart > 0 && text[lineStart - 1] != '\n')
                    lineStart--;

                // 检查行开头是否有缩进
                if (lineStart < text.Length)
                {
                    if (lineStart + 4 <= text.Length && text.Substring(lineStart, 4) == "    ")
                    {
                        textBox.Text = text.Remove(lineStart, 4);
                        textBox.CaretIndex = Math.Max(lineStart, caretIndex - 4);
                    }
                    else if (lineStart < text.Length && text[lineStart] == '\t')
                    {
                        textBox.Text = text.Remove(lineStart, 1);
                        textBox.CaretIndex = Math.Max(lineStart, caretIndex - 1);
                    }
                }
            }
        }

        /// <summary>
        /// 复制当前行
        /// </summary>
        private void DuplicateCurrentLine(System.Windows.Controls.TextBox textBox)
        {
            int caretIndex = textBox.CaretIndex;
            string text = textBox.Text;

            // 找到当前行的开始和结束
            int lineStart = caretIndex;
            while (lineStart > 0 && text[lineStart - 1] != '\n')
                lineStart--;

            int lineEnd = caretIndex;
            while (lineEnd < text.Length && text[lineEnd] != '\n')
                lineEnd++;

            // 获取当前行内容
            string currentLine = text.Substring(lineStart, lineEnd - lineStart);

            // 在当前行后插入重复的行
            string newLine = "\n" + currentLine;
            textBox.Text = text.Insert(lineEnd, newLine);
            textBox.CaretIndex = lineEnd + newLine.Length;
        }

        /// <summary>
        /// 切换到下一个备忘录
        /// </summary>
        private void SwitchToNextMemo()
        {
            if (_memos.Count <= 1) return;

            int currentIndex = _memos.FindIndex(m => m.Id == _currentMemo?.Id);
            int nextIndex = (currentIndex + 1) % _memos.Count;
            EditMemo(_memos[nextIndex]);
        }

        /// <summary>
        /// 切换到上一个备忘录
        /// </summary>
        private void SwitchToPreviousMemo()
        {
            if (_memos.Count <= 1) return;

            int currentIndex = _memos.FindIndex(m => m.Id == _currentMemo?.Id);
            int prevIndex = currentIndex <= 0 ? _memos.Count - 1 : currentIndex - 1;
            EditMemo(_memos[prevIndex]);
        }

        /// <summary>
        /// 显示临时消息
        /// </summary>
        private void ShowTemporaryMessage(string message)
        {
            // 这里可以实现一个临时的提示消息，比如在状态栏或者弹出提示
            // 暂时使用窗口标题显示
            string originalTitle = this.Title;
            this.Title = $"{message} - {originalTitle}";

            var timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2)
            };
            timer.Tick += (s, e) =>
            {
                this.Title = originalTitle;
                timer.Stop();
            };
            timer.Start();
        }

        /// <summary>
        /// 显示查找对话框
        /// </summary>
        private void ShowSearchDialog()
        {
            // 这里调用现有的查找功能
            FindMenuItem_Click(null!, null!);
        }

        /// <summary>
        /// 显示替换对话框
        /// </summary>
        private void ShowReplaceDialog()
        {
            // 这里调用现有的替换功能
            ReplaceMenuItem_Click(null!, null!);
        }

        /// <summary>
        /// 备忘录数据模型
        /// </summary>
        /// <summary>
        /// 备忘录元数据模型（不包含内容）
        /// </summary>
        private record MemoMetadata
        {
            public string Id { get; init; } = Guid.NewGuid().ToString();
            public string Title { get; init; } = string.Empty;
            public string ContentFileName { get; init; } = string.Empty; // 内容文件名
            public DateTime CreatedTime { get; init; } = DateTime.Now;
            public DateTime ModifiedTime { get; init; } = DateTime.Now;
            public List<string> Tags { get; init; } = new List<string>(); // 标签支持
            public string Description { get; init; } = string.Empty; // 描述摘要
        }

        /// <summary>
        /// 完整备忘录模型（包含内容，用于内存操作）
        /// </summary>
        private record MemoModel
        {
            public string Id { get; init; } = Guid.NewGuid().ToString();
            public string Title { get; init; } = string.Empty;
            public string Content { get; init; } = string.Empty;
            public DateTime CreatedTime { get; init; } = DateTime.Now;
            public DateTime ModifiedTime { get; init; } = DateTime.Now;
            public List<string> Tags { get; init; } = new List<string>();
            public string Description { get; init; } = string.Empty;

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

            /// <summary>
            /// 从元数据模型创建完整模型
            /// </summary>
            public static MemoModel FromMetadata(MemoMetadata metadata, string content)
            {
                return new MemoModel
                {
                    Id = metadata.Id,
                    Title = metadata.Title,
                    Content = content,
                    CreatedTime = metadata.CreatedTime,
                    ModifiedTime = metadata.ModifiedTime,
                    Tags = metadata.Tags,
                    Description = metadata.Description
                };
            }

            /// <summary>
            /// 转换为元数据模型
            /// </summary>
            public MemoMetadata ToMetadata(string contentFileName)
            {
                return new MemoMetadata
                {
                    Id = Id,
                    Title = Title,
                    ContentFileName = contentFileName,
                    CreatedTime = CreatedTime,
                    ModifiedTime = ModifiedTime,
                    Tags = Tags,
                    Description = !string.IsNullOrWhiteSpace(Description) ? Description :
                        (Content.Length > 150 ? Content.Substring(0, 150).Trim() + "..." : Content.Trim())
                };
            }
        }
        
        /// <summary>
        /// 备忘录集合元数据模型
        /// </summary>
        private record MemosMetadata
        {
            public List<MemoMetadata> Memos { get; init; } = new List<MemoMetadata>();
            public string CurrentMemoId { get; init; } = string.Empty;
            public int Version { get; init; } = 2; // 存储格式版本号
        }

        /// <summary>
        /// 备忘录集合数据模型（向后兼容）
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

        #region 自定义暗色托盘菜单渲染器
        
        /// <summary>
        /// 自定义暗色主题托盘菜单渲染器，支持圆角设计
        /// </summary>
        private class DarkTrayMenuRenderer : Forms.ToolStripProfessionalRenderer
        {
            // 暗色主题颜色配置
            private readonly System.Drawing.Color _backgroundColor = System.Drawing.Color.FromArgb(45, 45, 48);
            private readonly System.Drawing.Color _borderColor = System.Drawing.Color.FromArgb(63, 63, 70);
            private readonly System.Drawing.Color _textColor = System.Drawing.Color.FromArgb(241, 241, 241);
            private readonly System.Drawing.Color _hoverColor = System.Drawing.Color.FromArgb(62, 62, 66);
            private readonly System.Drawing.Color _pressedColor = System.Drawing.Color.FromArgb(0, 122, 204);
            private readonly System.Drawing.Color _separatorColor = System.Drawing.Color.FromArgb(63, 63, 70);
            private readonly System.Drawing.Color _checkedColor = System.Drawing.Color.FromArgb(51, 153, 255);

            public DarkTrayMenuRenderer() : base(new DarkColorTable())
            {
                RoundedEdges = true;
            }

            protected override void OnRenderMenuItemBackground(Forms.ToolStripItemRenderEventArgs e)
            {
                if (!e.Item.Selected && !e.Item.Pressed)
                    return;

                var rect = new System.Drawing.Rectangle(2, 0, e.Item.Width - 4, e.Item.Height);
                var color = e.Item.Pressed ? _pressedColor : _hoverColor;
                
                using (var brush = new System.Drawing.SolidBrush(color))
                {
                    // 绘制圆角矩形背景
                    DrawRoundedRectangle(e.Graphics, brush, rect, 6);
                }
            }

            protected override void OnRenderToolStripBackground(Forms.ToolStripRenderEventArgs e)
            {
                // 首先填充整个区域以确保没有白色背景露出
                using (var brush = new System.Drawing.SolidBrush(_backgroundColor))
                {
                    e.Graphics.FillRectangle(brush, 0, 0, e.ToolStrip.Width, e.ToolStrip.Height);
                }
                
                // 然后绘制圆角背景
                using (var brush = new System.Drawing.SolidBrush(_backgroundColor))
                {
                    var rect = new System.Drawing.Rectangle(0, 0, e.ToolStrip.Width, e.ToolStrip.Height);
                    DrawRoundedRectangle(e.Graphics, brush, rect, 8);
                }
            }

            protected override void OnRenderToolStripBorder(Forms.ToolStripRenderEventArgs e)
            {
                using (var pen = new System.Drawing.Pen(_borderColor, 1))
                {
                    var rect = new System.Drawing.Rectangle(0, 0, e.ToolStrip.Width - 1, e.ToolStrip.Height - 1);
                    DrawRoundedRectangleBorder(e.Graphics, pen, rect, 8);
                }
            }

            protected override void OnRenderItemText(Forms.ToolStripItemTextRenderEventArgs e)
            {
                e.TextColor = _textColor;
                e.TextFont = new System.Drawing.Font("Microsoft YaHei", 9F, System.Drawing.FontStyle.Regular);
                base.OnRenderItemText(e);
            }

            protected override void OnRenderSeparator(Forms.ToolStripSeparatorRenderEventArgs e)
            {
                var rect = new System.Drawing.Rectangle(10, e.Item.Height / 2, e.Item.Width - 20, 1);
                using (var brush = new System.Drawing.SolidBrush(_separatorColor))
                {
                    e.Graphics.FillRectangle(brush, rect);
                }
            }

            protected override void OnRenderImageMargin(Forms.ToolStripRenderEventArgs e)
            {
                // 渲染图像边距为暗色背景
                using (var brush = new System.Drawing.SolidBrush(_backgroundColor))
                {
                    e.Graphics.FillRectangle(brush, e.AffectedBounds);
                }
            }

            protected override void OnRenderItemCheck(Forms.ToolStripItemImageRenderEventArgs e)
            {
                var rect = new System.Drawing.Rectangle(e.ImageRectangle.X - 2, e.ImageRectangle.Y - 2, 
                    e.ImageRectangle.Width + 4, e.ImageRectangle.Height + 4);
                
                using (var brush = new System.Drawing.SolidBrush(_checkedColor))
                {
                    DrawRoundedRectangle(e.Graphics, brush, rect, 3);
                }
                
                // 绘制对勾
                using (var pen = new System.Drawing.Pen(System.Drawing.Color.White, 2))
                {
                    var checkRect = e.ImageRectangle;
                    var points = new System.Drawing.Point[]
                    {
                        new System.Drawing.Point(checkRect.X + 3, checkRect.Y + checkRect.Height / 2),
                        new System.Drawing.Point(checkRect.X + checkRect.Width / 2, checkRect.Y + checkRect.Height - 4),
                        new System.Drawing.Point(checkRect.X + checkRect.Width - 3, checkRect.Y + 3)
                    };
                    e.Graphics.DrawLines(pen, points);
                }
            }

            /// <summary>
            /// 绘制圆角矩形
            /// </summary>
            private void DrawRoundedRectangle(System.Drawing.Graphics graphics, System.Drawing.Brush brush, 
                System.Drawing.Rectangle rect, int radius)
            {
                using (var path = CreateRoundedRectanglePath(rect, radius))
                {
                    graphics.FillPath(brush, path);
                }
            }

            /// <summary>
            /// 绘制圆角矩形边框
            /// </summary>
            private void DrawRoundedRectangleBorder(System.Drawing.Graphics graphics, System.Drawing.Pen pen, 
                System.Drawing.Rectangle rect, int radius)
            {
                using (var path = CreateRoundedRectanglePath(rect, radius))
                {
                    graphics.DrawPath(pen, path);
                }
            }

            /// <summary>
            /// 创建圆角矩形路径
            /// </summary>
            private System.Drawing.Drawing2D.GraphicsPath CreateRoundedRectanglePath(System.Drawing.Rectangle rect, int radius)
            {
                var path = new System.Drawing.Drawing2D.GraphicsPath();
                var diameter = radius * 2;

                var arc = new System.Drawing.Rectangle(rect.X, rect.Y, diameter, diameter);
                path.AddArc(arc, 180, 90);

                arc.X = rect.Right - diameter;
                path.AddArc(arc, 270, 90);

                arc.Y = rect.Bottom - diameter;
                path.AddArc(arc, 0, 90);

                arc.X = rect.Left;
                path.AddArc(arc, 90, 90);

                path.CloseFigure();
                return path;
            }
        }

        /// <summary>
        /// 暗色主题颜色表
        /// </summary>
        private class DarkColorTable : Forms.ProfessionalColorTable
        {
            public override System.Drawing.Color MenuItemSelected => System.Drawing.Color.FromArgb(62, 62, 66);
            public override System.Drawing.Color MenuItemBorder => System.Drawing.Color.FromArgb(63, 63, 70);
            public override System.Drawing.Color MenuBorder => System.Drawing.Color.FromArgb(63, 63, 70);
            public override System.Drawing.Color MenuItemSelectedGradientBegin => System.Drawing.Color.FromArgb(62, 62, 66);
            public override System.Drawing.Color MenuItemSelectedGradientEnd => System.Drawing.Color.FromArgb(62, 62, 66);
            public override System.Drawing.Color MenuItemPressedGradientBegin => System.Drawing.Color.FromArgb(0, 122, 204);
            public override System.Drawing.Color MenuItemPressedGradientEnd => System.Drawing.Color.FromArgb(0, 122, 204);
            public override System.Drawing.Color ToolStripDropDownBackground => System.Drawing.Color.FromArgb(45, 45, 48);
            public override System.Drawing.Color ImageMarginGradientBegin => System.Drawing.Color.FromArgb(45, 45, 48);
            public override System.Drawing.Color ImageMarginGradientMiddle => System.Drawing.Color.FromArgb(45, 45, 48);
            public override System.Drawing.Color ImageMarginGradientEnd => System.Drawing.Color.FromArgb(45, 45, 48);
            public override System.Drawing.Color SeparatorDark => System.Drawing.Color.FromArgb(63, 63, 70);
            public override System.Drawing.Color SeparatorLight => System.Drawing.Color.FromArgb(63, 63, 70);
        }

        #endregion
        
        #region 托盘菜单辅助方法
        
        /// <summary>
        /// 为子菜单递归应用暗色主题
        /// </summary>
        private void ApplyDarkThemeToSubMenus(Forms.ToolStrip toolStrip)
        {
            foreach (Forms.ToolStripItem item in toolStrip.Items)
            {
                if (item is Forms.ToolStripMenuItem menuItem && menuItem.HasDropDownItems)
                {
                    // 设置子菜单的背景色和渲染器
                    menuItem.DropDown.Renderer = new DarkTrayMenuRenderer();
                    menuItem.DropDown.BackColor = System.Drawing.Color.FromArgb(45, 45, 48);
                    menuItem.DropDown.ForeColor = System.Drawing.Color.FromArgb(241, 241, 241);
                    
                    // 子菜单不再设置圆角，以提高性能
                    
                    // 递归处理更深层的子菜单
                    ApplyDarkThemeToSubMenus(menuItem.DropDown);
                }
            }
        }
        
        /// <summary>
        /// 为菜单设置圆角区域
        /// </summary>
        private void SetRoundedRegion(Forms.ToolStrip toolStrip, int radius)
        {
            try
            {
                // 确保控件已创建且有有效尺寸
                if (toolStrip == null || !toolStrip.IsHandleCreated || toolStrip.IsDisposed ||
                    toolStrip.Width <= 0 || toolStrip.Height <= 0 || radius <= 0)
                {
                    return;
                }

                using (var path = new System.Drawing.Drawing2D.GraphicsPath())
                {
                    var rect = new System.Drawing.Rectangle(0, 0, toolStrip.Width, toolStrip.Height);
                    var diameter = Math.Min(radius * 2, Math.Min(rect.Width, rect.Height));

                    // 确保直径不会超过矩形的尺寸
                    if (diameter >= Math.Min(rect.Width, rect.Height))
                    {
                        diameter = Math.Min(rect.Width, rect.Height) / 2;
                    }

                    // 创建圆角矩形路径
                    var arc = new System.Drawing.Rectangle(rect.X, rect.Y, diameter, diameter);
                    path.AddArc(arc, 180, 90);

                    arc.X = rect.Right - diameter;
                    path.AddArc(arc, 270, 90);

                    arc.Y = rect.Bottom - diameter;
                    path.AddArc(arc, 0, 90);

                    arc.X = rect.Left;
                    path.AddArc(arc, 90, 90);

                    path.CloseFigure();

                    // 释放之前的Region（如果存在）
                    toolStrip.Region?.Dispose();
                    
                    // 设置新的圆角区域
                    toolStrip.Region = new System.Drawing.Region(path);
                }
            }
            catch (Exception ex)
            {
                // 记录错误但不抛出异常，确保菜单基本功能不受影响
                System.Diagnostics.Debug.WriteLine($"设置菜单圆角区域失败: {ex.Message}");
            }
        }
        
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
                "请输入要查找的文本：\n\n提示：\n- 输入相同文本继续查找下一个\n- 输入新文本开始新的搜索",
                "查找",
                _currentSearchText);

            if (!string.IsNullOrEmpty(searchText))
            {
                // 如果是新的搜索文本，重新搜索
                if (searchText != _currentSearchText)
                {
                    _currentSearchText = searchText;
                    _currentSearchIndex = -1;
                    FindAllMatches();
                }

                // 查找下一个匹配项
                FindNext();
            }
        }

        /// <summary>
        /// 查找所有匹配项（优化性能）
        /// </summary>
        private void FindAllMatches()
        {
            _searchMatches.Clear();

            if (string.IsNullOrEmpty(_currentSearchText) || NoteTextBox == null)
                return;

            try
            {
                string text = NoteTextBox.Text;
                int index = 0;

                // 查找所有匹配项，但限制最大数量以避免性能问题
                const int maxMatches = 1000; // 限制最大匹配数量
                int matchCount = 0;

                while ((index = text.IndexOf(_currentSearchText, index, StringComparison.OrdinalIgnoreCase)) >= 0 && matchCount < maxMatches)
                {
                    _searchMatches.Add(index);
                    index += _currentSearchText.Length;
                    matchCount++;
                }

                // 如果达到最大匹配数，提示用户
                if (matchCount >= maxMatches && StatusText != null)
                {
                    StatusText.Text = $"找到匹配项过多（已显示前{maxMatches}个），请使用更具体的搜索词";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"查找匹配项时出错: {ex.Message}");
                _searchMatches.Clear();
            }
        }

        /// <summary>
        /// 查找下一个匹配项（改进版本）
        /// </summary>
        private void FindNext()
        {
            if (_searchMatches.Count == 0)
            {
                if (StatusText != null)
                {
                    StatusText.Text = $"未找到：{_currentSearchText}";
                }
                return;
            }

            try
            {
                // 移动到下一个匹配项
                _currentSearchIndex = (_currentSearchIndex + 1) % _searchMatches.Count;
                int index = _searchMatches[_currentSearchIndex];

                // 安全地选中文本
                if (NoteTextBox != null && index >= 0 && index + _currentSearchText.Length <= NoteTextBox.Text.Length)
                {
                    NoteTextBox.Select(index, _currentSearchText.Length);
                    NoteTextBox.Focus();
                    NoteTextBox.ScrollToLine(NoteTextBox.GetLineIndexFromCharacterIndex(index));

                    if (StatusText != null)
                    {
                        StatusText.Text = $"找到匹配项 {_currentSearchIndex + 1}/{_searchMatches.Count}：{_currentSearchText}";
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"查找下一个匹配项时出错: {ex.Message}");
                if (StatusText != null)
                {
                    StatusText.Text = "查找时发生错误";
                }
            }
        }

        /// <summary>
        /// 查找上一个匹配项（改进版本）
        /// </summary>
        private void FindPrevious()
        {
            if (_searchMatches.Count == 0)
            {
                if (StatusText != null)
                {
                    StatusText.Text = $"未找到：{_currentSearchText}";
                }
                return;
            }

            try
            {
                // 移动到上一个匹配项
                _currentSearchIndex = _currentSearchIndex <= 0 ? _searchMatches.Count - 1 : _currentSearchIndex - 1;
                int index = _searchMatches[_currentSearchIndex];

                // 安全地选中文本
                if (NoteTextBox != null && index >= 0 && index + _currentSearchText.Length <= NoteTextBox.Text.Length)
                {
                    NoteTextBox.Select(index, _currentSearchText.Length);
                    NoteTextBox.Focus();
                    NoteTextBox.ScrollToLine(NoteTextBox.GetLineIndexFromCharacterIndex(index));

                    if (StatusText != null)
                    {
                        StatusText.Text = $"找到匹配项 {_currentSearchIndex + 1}/{_searchMatches.Count}：{_currentSearchText}";
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"查找上一个匹配项时出错: {ex.Message}");
                if (StatusText != null)
                {
                    StatusText.Text = "查找时发生错误";
                }
            }
        }

        /// <summary>
        /// 清除搜索状态
        /// </summary>
        private void ClearSearchState()
        {
            _currentSearchText = string.Empty;
            _currentSearchIndex = -1;
            _searchMatches.Clear();
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
            public TopmostMode TopmostMode { get; init; } = TopmostMode.Desktop;
            public bool ClickThroughEnabled { get; init; } = false;
            public bool AutoStartEnabled { get; init; } = false;
            public string NoteContent { get; init; } = string.Empty;
            public bool ShowExitPrompt { get; init; } = true;
            public bool WindowPinned { get; init; } = false;
            public bool ShowDeletePrompt { get; init; } = true;
            public double BackgroundOpacity { get; init; } = 0.1; // 默认10%显示值对应的实际透明度（0.1 = 10% * 0.6 / 60）
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
            
            // 启动自动保存位置定时器（防抖） - 功能已禁用
            // _autoSavePositionTimer.Stop();
            // _autoSavePositionTimer.Start();
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
        /// 自动保存备忘录定时器事件（防抖）
        /// </summary>
        private void AutoSaveMemoTimer_Tick(object? sender, EventArgs e)
        {
            _autoSaveMemoTimer.Stop();
            SaveCurrentMemo();
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
            MoveToPresetPosition(position);
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
                        _currentTopmostMode = settings.TopmostMode;
                        _isClickThroughEnabled = settings.ClickThroughEnabled;
                        _showExitPrompt = settings.ShowExitPrompt;
                        _isWindowPinned = settings.WindowPinned;
                        _showDeletePrompt = settings.ShowDeletePrompt;
                        _backgroundOpacity = settings.BackgroundOpacity;
                        
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
                    TopmostMode = _currentTopmostMode,
                    ClickThroughEnabled = _isClickThroughEnabled,
                    AutoStartEnabled = IsAutoStartEnabled(),
                    NoteContent = NoteTextBox?.Text ?? string.Empty,
                    ShowExitPrompt = _showExitPrompt,
                    WindowPinned = _isWindowPinned,
                    ShowDeletePrompt = _showDeletePrompt,
                    BackgroundOpacity = _backgroundOpacity
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
            MoveToPresetPosition(position);
            
            // 显示气泡提示
            _notifyIcon.ShowBalloonTip(2000, "位置已更改", 
                $"已移动到{GetPositionDisplayName(position)} (X: {(int)Left}, Y: {(int)Top})", 
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

            // 确保备忘录列表不为空
            if (_memos == null || !_memos.Any())
            {
                // 更新计数为0
                if (MemoCountText != null)
                {
                    MemoCountText.Text = "(0)";
                }
                return;
            }

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
        /// 移动窗口到预设位置的通用方法
        /// </summary>
        private void MoveToPresetPosition(string position)
        {
            var workingArea = SystemParameters.WorkArea;
            double newX = 0, newY = 0;
            double windowWidth = Width;
            double windowHeight = Height;

            // 计算预设位置
            switch (position)
            {
                case "TopLeft": newX = workingArea.Left + 10; newY = workingArea.Top + 10; break;
                case "TopCenter": newX = workingArea.Left + (workingArea.Width - windowWidth) / 2; newY = workingArea.Top + 10; break;
                case "TopRight": newX = workingArea.Right - windowWidth - 10; newY = workingArea.Top + 10; break;
                case "MiddleLeft": newX = workingArea.Left + 10; newY = workingArea.Top + (workingArea.Height - windowHeight) / 2; break;
                case "Center": newX = workingArea.Left + (workingArea.Width - windowWidth) / 2; newY = workingArea.Top + (workingArea.Height - windowHeight) / 2; break;
                case "MiddleRight": newX = workingArea.Right - windowWidth - 10; newY = workingArea.Top + (workingArea.Height - windowHeight) / 2; break;
                case "BottomLeft": newX = workingArea.Left + 10; newY = workingArea.Bottom - windowHeight - 10; break;
                case "BottomCenter": newX = workingArea.Left + (workingArea.Width - windowWidth) / 2; newY = workingArea.Bottom - windowHeight - 10; break;
                case "BottomRight": newX = workingArea.Right - windowWidth - 10; newY = workingArea.Bottom - windowHeight - 10; break;
                default: return;
            }

            // 设置窗口位置
            SetWindowPosition(newX, newY);

            // 更新状态文本
            if (StatusText != null && _isSettingsPanelVisible)
            {
                StatusText.Text = $"已移动到{GetPositionDisplayName(position)}";
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
            SaveMemosAsync();
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
                SaveMemosAsync();
                
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
            SaveMemosAsync();
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
            SaveMemosAsync();
            
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
        
        #region 背景透明度管理
        
        /// <summary>
        /// 背景透明度滑块值变更事件
        /// </summary>
        private void BackgroundOpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (sender is Slider slider)
            {
                // 将滑块的0-100%映射到实际的0-60%效果
                // 公式：实际透明度 = 滑块值 * 0.6 / 100
                _backgroundOpacity = (slider.Value * 0.6) / 100.0;
                UpdateOpacityValueText();
                UpdateBackgroundOpacity();
                UpdateProgressBar(slider);
                
                // 自动保存设置
                SaveSettingsToDisk();
            }
        }
        
        /// <summary>
        /// 更新进度条宽度
        /// </summary>
        private void UpdateProgressBar(Slider slider)
        {
            if (slider.Template?.FindName("ProgressRect", slider) is System.Windows.Shapes.Rectangle progressRect)
            {
                var percentage = slider.Value / slider.Maximum;
                progressRect.Width = slider.ActualWidth * percentage;
            }
        }
        
        /// <summary>
        /// 透明度轨道点击事件
        /// </summary>
        private void OpacityTrack_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border track && BackgroundOpacitySlider != null)
            {
                // 获取点击位置相对于轨道的位置
                var clickPosition = e.GetPosition(track);
                var trackWidth = track.ActualWidth;
                
                if (trackWidth > 0)
                {
                    // 计算点击位置对应的百分比
                    var percentage = clickPosition.X / trackWidth;
                    
                    // 限制在有效范围内
                    percentage = Math.Max(0, Math.Min(1, percentage));
                    
                    // 设置新的滑块值（0-100%显示范围）
                    BackgroundOpacitySlider.Value = percentage * 100;
                }
            }
        }
        
        /// <summary>
        /// 更新透明度数值显示
        /// </summary>
        private void UpdateOpacityValueText()
        {
            if (OpacityValueText != null && BackgroundOpacitySlider != null)
            {
                // 显示滑块的值（0-100%），而不是实际透明度
                OpacityValueText.Text = $"{(int)BackgroundOpacitySlider.Value}%";
            }
        }
        
        /// <summary>
        /// 更新窗口背景透明度
        /// </summary>
        private void UpdateBackgroundOpacity()
        {
            if (MainContainer != null)
            {
                // 创建新的背景画刷
                var backgroundColor = System.Windows.Media.Color.FromArgb(
                    (byte)(255 * _backgroundOpacity), // Alpha通道
                    255, 255, 255); // RGB白色
                
                MainContainer.Background = new SolidColorBrush(backgroundColor);
                
                // 更新状态文本
                if (StatusText != null && BackgroundOpacitySlider != null)
                {
                    StatusText.Text = $"背景透明度已调整为 {(int)BackgroundOpacitySlider.Value}%";
                }
            }
        }
        
        #endregion
        
        #endregion






    }
}