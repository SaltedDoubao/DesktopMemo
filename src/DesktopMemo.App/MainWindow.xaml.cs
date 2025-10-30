using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using DesktopMemo.App.ViewModels;
using DesktopMemo.Core.Contracts;
using DesktopMemo.Core.Helpers;
using DesktopMemo.Core.Models;
using WpfApp = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace DesktopMemo.App;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly IWindowService _windowService;
    private readonly ITrayService _trayService;
    private DispatcherTimer? _autoSaveTimer;

    public MainWindow(MainViewModel viewModel, IWindowService windowService, ITrayService trayService)
    {
        try
        {
            InitializeComponent();

            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            _windowService = windowService ?? throw new ArgumentNullException(nameof(windowService));
            _trayService = trayService ?? throw new ArgumentNullException(nameof(trayService));

            DataContext = _viewModel;
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
            _viewModel.ThemeChanged += OnThemeChanged;

            ConfigureWindow();
            ConfigureTrayService();
            InitializeAutoSaveTimer();

            Loaded += OnLoaded;
            Closing += OnClosing;
            LocationChanged += OnLocationChanged; // 监听位置变化
        }
        catch (Exception ex)
        {
            MessageBox.Show($"窗口初始化错误: {ex.Message}", "DesktopMemo启动失败",
                MessageBoxButton.OK, MessageBoxImage.Error);
            throw;
        }
    }

    private void ConfigureWindow()
    {
        Loaded += (s, e) =>
        {
            // 按当前用户选择的置顶模式应用，而非强制 Desktop
            _windowService.SetTopmostMode(_viewModel.SelectedTopmostMode);
            _windowService.PlayFadeInAnimation();
        };
    }

    private void ConfigureTrayService()
    {
        _trayService.TrayIconDoubleClick += (s, e) => _windowService.ToggleWindowVisibility();
        _trayService.ShowHideWindowClick += (s, e) => _windowService.ToggleWindowVisibility();
        _trayService.NewMemoClick += async (s, e) =>
        {
            _windowService.RestoreFromTray();
            await _viewModel.CreateMemoCommand.ExecuteAsync(null);
        };
        _trayService.SettingsClick += (s, e) =>
        {
            _windowService.RestoreFromTray();
            if (!_viewModel.IsSettingsPanelVisible)
            {
                _viewModel.IsSettingsPanelVisible = true;
            }
        };
        _trayService.MoveToPresetClick += (s, preset) => _viewModel.MoveToPresetCommand.Execute(preset);
        _trayService.RememberPositionClick += (s, e) => _viewModel.RememberPositionCommand.Execute(null);
        _trayService.RestorePositionClick += (s, e) => _viewModel.RestorePositionCommand.Execute(null);
        _trayService.ExportNotesClick += (s, e) => _viewModel.ExportMarkdownCommand.Execute(null);
        _trayService.ImportNotesClick += (s, e) => _viewModel.ImportLegacyCommand.Execute(null);
        _trayService.ClearContentClick += (s, e) => _viewModel.ClearEditorCommand.Execute(null);
        _trayService.AboutClick += (s, e) => _viewModel.ShowAboutCommand.Execute(null);
        _trayService.RestartTrayClick += (s, e) => _viewModel.TrayRestartCommand.Execute(null);
        _trayService.ClickThroughToggleClick += (s, enabled) => _viewModel.IsClickThroughEnabled = enabled;
        _trayService.ReenableExitPromptClick += async (s, e) =>
        {
            _viewModel.WindowSettings = _viewModel.WindowSettings with { ShowExitConfirmation = true };
            await _viewModel.GetSettingsService().SaveAsync(_viewModel.WindowSettings);
            _trayService.ShowBalloonTip("设置已更新", "已重新启用退出提示");
        };
        _trayService.ReenableDeletePromptClick += async (s, e) =>
        {
            _viewModel.WindowSettings = _viewModel.WindowSettings with { ShowDeleteConfirmation = true };
            await _viewModel.GetSettingsService().SaveAsync(_viewModel.WindowSettings);
            _trayService.ShowBalloonTip("设置已更新", "已重新启用删除确认提示");
        };
        _trayService.TopmostModeChangeClick += (s, mode) => _viewModel.SelectedTopmostMode = mode;
        _trayService.ExitClick += (s, e) => WpfApp.Current.Shutdown();
    }

    private void InitializeAutoSaveTimer()
    {
        _autoSaveTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(2)
        };
        _autoSaveTimer.Tick += AutoSave_Tick;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.IsSettingsPanelVisible))
        {
            Dispatcher.Invoke(() => ApplySettingsPanelVisibility(_viewModel.IsSettingsPanelVisible));
        }
        else if (e.PropertyName == nameof(MainViewModel.IsLogPanelVisible))
        {
            Dispatcher.Invoke(() => ApplyLogPanelVisibility(_viewModel.IsLogPanelVisible));
        }
    }

    private void ApplySettingsPanelVisibility(bool show)
    {
        if (show)
        {
            SettingsPanel.Visibility = Visibility.Visible;
            AnimateSettingsPanel(true);
        }
        else
        {
            AnimateSettingsPanel(false);
        }
    }

    private void AnimateSettingsPanel(bool show)
    {
        if (SettingsPanel.RenderTransform is not System.Windows.Media.TranslateTransform transform)
        {
            transform = new System.Windows.Media.TranslateTransform();
            SettingsPanel.RenderTransform = transform;
        }

        var animation = new DoubleAnimation
        {
            From = show ? 340 : 0,
            To = show ? 0 : 340,
            Duration = TimeSpan.FromMilliseconds(300),
            EasingFunction = new ExponentialEase
            {
                EasingMode = show ? EasingMode.EaseOut : EasingMode.EaseIn
            }
        };

        if (!show)
        {
            animation.Completed += (s, e) => SettingsPanel.Visibility = Visibility.Collapsed;
        }

        transform.BeginAnimation(System.Windows.Media.TranslateTransform.XProperty, animation);
    }

    private void ApplyLogPanelVisibility(bool show)
    {
        if (show)
        {
            LogPanel.Visibility = Visibility.Visible;
            AnimateLogPanel(true);
        }
        else
        {
            AnimateLogPanel(false);
        }
    }

    private void AnimateLogPanel(bool show)
    {
        if (LogPanel.RenderTransform is not System.Windows.Media.TranslateTransform transform)
        {
            transform = new System.Windows.Media.TranslateTransform();
            LogPanel.RenderTransform = transform;
        }

        var animation = new DoubleAnimation
        {
            From = show ? 340 : 0,
            To = show ? 0 : 340,
            Duration = TimeSpan.FromMilliseconds(300),
            EasingFunction = new ExponentialEase
            {
                EasingMode = show ? EasingMode.EaseOut : EasingMode.EaseIn
            }
        };

        if (!show)
        {
            animation.Completed += (s, e) => LogPanel.Visibility = Visibility.Collapsed;
        }

        transform.BeginAnimation(System.Windows.Media.TranslateTransform.XProperty, animation);
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        try
        {
            // 配置已在 App.OnStartup 中加载，这里只需要应用UI状态
            ApplySettingsPanelVisibility(_viewModel.IsSettingsPanelVisible);
            ApplyLogPanelVisibility(_viewModel.IsLogPanelVisible);
            
            // 应用初始主题
            ApplyTheme(_viewModel.SelectedTheme);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"应用程序初始化失败: {ex.Message}\n\n详细信息: {ex}", 
                "DesktopMemo 初始化错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
            
            // 尝试使用默认设置继续运行
            try
            {
                ApplySettingsPanelVisibility(false);
                _viewModel.SetStatus("初始化失败，使用默认设置运行");
            }
            catch
            {
                // 如果连默认设置都无法应用，则关闭应用程序
                WpfApp.Current.Shutdown();
            }
        }
    }

    private void OnThemeChanged(object? sender, AppTheme theme)
    {
        Dispatcher.Invoke(() => ApplyTheme(theme));
    }

    private void ApplyTheme(AppTheme theme)
    {
        var actualTheme = theme;
        
        // 如果选择了跟随系统，检测系统主题
        if (theme == AppTheme.System)
        {
            actualTheme = IsSystemDarkMode() ? AppTheme.Dark : AppTheme.Light;
        }

        // 找到旧的主题字典并移除
        var oldThemeDict = System.Windows.Application.Current.Resources.MergedDictionaries
            .FirstOrDefault(d => d.Source != null && d.Source.OriginalString.Contains("Themes/"));
        
        if (oldThemeDict != null)
        {
            System.Windows.Application.Current.Resources.MergedDictionaries.Remove(oldThemeDict);
        }

        // 根据主题添加新的资源字典
        var themeUri = new Uri(
            actualTheme == AppTheme.Dark 
                ? "Resources/Themes/Dark.xaml" 
                : "Resources/Themes/Light.xaml", 
            UriKind.Relative);
        
        System.Windows.Application.Current.Resources.MergedDictionaries.Insert(0, 
            new ResourceDictionary { Source = themeUri });

        // 更新滑块样式
        if (BackgroundOpacitySlider != null)
        {
            BackgroundOpacitySlider.Style = actualTheme == AppTheme.Dark 
                ? (Style)FindResource("AppleSliderStyleDark") 
                : (Style)FindResource("AppleSliderStyle");
        }
    }


    private bool IsSystemDarkMode()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            var value = key?.GetValue("AppsUseLightTheme");
            return value is int intValue && intValue == 0;
        }
        catch
        {
            return false; // 默认为亮色模式
        }
    }

    private void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        _autoSaveTimer?.Stop();
        _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        _viewModel.ThemeChanged -= OnThemeChanged;
    }

    private void OnLocationChanged(object? sender, EventArgs e)
    {
        // 实时更新位置显示
        _viewModel.UpdateCurrentPosition();
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed && !(_viewModel?.IsWindowPinned ?? false))
        {
            DragMove();
        }
    }

    private void SettingsPanel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.Source == sender)
        {
            if (_viewModel.IsSettingsPanelVisible)
            {
                _viewModel.IsSettingsPanelVisible = false;
                e.Handled = true;
            }
            else if (_viewModel.IsLogPanelVisible)
            {
                _viewModel.IsLogPanelVisible = false;
                e.Handled = true;
            }
        }
    }

    private void MainContentArea_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_viewModel.IsSettingsPanelVisible)
        {
            _viewModel.IsSettingsPanelVisible = false;
            e.Handled = true;
        }
        else if (_viewModel.IsLogPanelVisible)
        {
            _viewModel.IsLogPanelVisible = false;
            e.Handled = true;
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        // 避免async void死锁，使用fire-and-forget方式
        _ = Task.Run(async () =>
        {
            try
            {
                await HandleCloseButtonClickAsync();
            }
            catch (Exception ex)
            {
                // 在后台线程中捕获异常，记录到调试输出
                System.Diagnostics.Debug.WriteLine($"处理关闭按钮异常: {ex}");

                // 如果异步处理失败，回退到UI线程执行默认关闭行为
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    try
                    {
                        if (_viewModel.WindowSettings.DefaultExitToTray)
                        {
                            _viewModel.TrayHideWindowCommand.Execute(null);
                        }
                        else
                        {
                            WpfApp.Current.Shutdown();
                        }
                    }
                    catch (Exception fallbackEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"回退关闭失败: {fallbackEx}");
                        // 最后的安全措施
                        Environment.Exit(0);
                    }
                });
            }
        });
    }

    private async Task HandleCloseButtonClickAsync()
    {
        // 检查是否需要显示退出确认
        if (_viewModel.WindowSettings.ShowExitConfirmation)
        {
            Views.ExitConfirmationDialog? dialog = null;
            bool? result = null;

            // 在UI线程上创建和显示对话框
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                try
                {
                    dialog = new Views.ExitConfirmationDialog(_viewModel.LocalizationService)
                    {
                        Owner = this
                    };
                    result = dialog.ShowDialog();
                }
                catch (InvalidOperationException ex)
                {
                    _viewModel.SetStatus($"无法显示退出确认对话框: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"退出确认对话框InvalidOperationException: {ex}");
                }
                catch (Exception ex)
                {
                    _viewModel.SetStatus($"退出对话框错误: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"退出确认对话框异常: {ex}");
                }
            });

            if (result != true || dialog == null)
            {
                // 用户取消或对话框创建失败
                if (result == null)
                {
                    // 对话框创建失败，使用默认行为
                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        if (_viewModel.WindowSettings.DefaultExitToTray)
                        {
                            _viewModel.TrayHideWindowCommand.Execute(null);
                        }
                        else
                        {
                            WpfApp.Current.Shutdown();
                        }
                    });
                }
                return;
            }

            // 如果用户选择了"不再显示"，立即更新内存中的设置，然后异步保存
            if (dialog.DontShowAgain)
            {
                // 立即更新内存中的设置，避免被后续的设置保存覆盖
                bool exitToTray = dialog.Action == Views.ExitAction.MinimizeToTray;
                var newSettings = _viewModel.WindowSettings with
                {
                    ShowExitConfirmation = false,
                    DefaultExitToTray = exitToTray
                };
                _viewModel.WindowSettings = newSettings;

                // 异步保存到磁盘（不等待，避免阻塞退出操作）
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _viewModel.GetSettingsService().SaveAsync(newSettings);
                        System.Diagnostics.Debug.WriteLine("退出确认设置已保存");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"保存退出设置失败: {ex}");
                        // 设置保存失败不影响退出操作
                    }
                });
            }

            // 在UI线程执行退出操作
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                switch (dialog.Action)
                {
                    case Views.ExitAction.MinimizeToTray:
                        _viewModel.TrayHideWindowCommand.Execute(null);
                        break;
                    case Views.ExitAction.Exit:
                        _viewModel.Dispose();
                        _trayService.Dispose();
                        if (_windowService is IDisposable disposableWindowService)
                        {
                            disposableWindowService.Dispose();
                        }
                        _autoSaveTimer?.Stop();
                        _autoSaveTimer = null;
                        WpfApp.Current.Shutdown();
                        break;
                }
            });
        }
        else
        {
            // 不显示确认，根据保存的设置执行默认行为
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                if (_viewModel.WindowSettings.DefaultExitToTray)
                {
                    _viewModel.TrayHideWindowCommand.Execute(null);
                }
                else
                {
                    _viewModel.Dispose();
                    _trayService.Dispose();
                    if (_windowService is IDisposable disposableWindowService)
                    {
                        disposableWindowService.Dispose();
                    }
                    _autoSaveTimer?.Stop();
                    _autoSaveTimer = null;
                    WpfApp.Current.Shutdown();
                }
            });
        }
    }

    private void NoteTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        _autoSaveTimer?.Stop();
        _autoSaveTimer?.Start();
        _viewModel.MarkEditing();
    }

    private void NoteTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyboardDevice.Modifiers == ModifierKeys.Control)
        {
            switch (e.Key)
            {
                case Key.S:
                    e.Handled = true;
                    _ = _viewModel.SaveMemoCommand.ExecuteAsync(null);
                    break;
                case Key.N:
                    e.Handled = true;
                    _ = _viewModel.CreateMemoCommand.ExecuteAsync(null);
                    break;
                case Key.F:
                    e.Handled = true;
                    ShowFindDialog();
                    break;
                case Key.H:
                    e.Handled = true;
                    ShowReplaceDialog();
                    break;
                case Key.Tab:
                    e.Handled = true;
                    SwitchToNextMemo();
                    break;
                case Key.D:
                    e.Handled = true;
                    DuplicateCurrentLine();
                    break;
                case Key.OemOpenBrackets: // Ctrl + [
                    e.Handled = true;
                    DecreaseIndent();
                    break;
                case Key.OemCloseBrackets: // Ctrl + ]
                    e.Handled = true;
                    IncreaseIndent();
                    break;
            }
        }
        else if (e.KeyboardDevice.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift))
        {
            switch (e.Key)
            {
                case Key.Tab:
                    e.Handled = true;
                    SwitchToPreviousMemo();
                    break;
            }
        }
        else if (e.KeyboardDevice.Modifiers == ModifierKeys.Shift)
        {
            switch (e.Key)
            {
                case Key.Tab:
                    e.Handled = true;
                    DecreaseIndent();
                    break;
                case Key.F3:
                    e.Handled = true;
                    // 查找上一个功能
                    _viewModel.SetStatus("查找上一个");
                    break;
            }
        }
        else if (e.KeyboardDevice.Modifiers == ModifierKeys.None)
        {
            switch (e.Key)
            {
                case Key.Tab:
                    e.Handled = true;
                    InsertIndent();
                    break;
                case Key.F3:
                    e.Handled = true;
                    _viewModel.FindNextCommand?.Execute(null);
                    break;
            }
        }
    }

    private void ShowFindDialog()
    {
        // 实现查找对话框或内联查找功能
        _viewModel.SetStatus("查找功能 - 使用 F3 查找下一个");
    }

    private void ShowReplaceDialog()
    {
        // 实现替换对话框或内联替换功能
        _viewModel.SetStatus("替换功能 - 使用 Ctrl+H 替换");
    }

    private void SwitchToNextMemo()
    {
        var memos = _viewModel.Memos;
        if (_viewModel.SelectedMemo == null) return;

        var currentIndex = memos.IndexOf(_viewModel.SelectedMemo);
        if (currentIndex < memos.Count - 1)
        {
            _viewModel.SelectedMemo = memos[currentIndex + 1];
        }
        else if (memos.Count > 0)
        {
            _viewModel.SelectedMemo = memos[0];
        }
    }

    private void SwitchToPreviousMemo()
    {
        var memos = _viewModel.Memos;
        if (_viewModel.SelectedMemo == null) return;

        var currentIndex = memos.IndexOf(_viewModel.SelectedMemo);
        if (currentIndex > 0)
        {
            _viewModel.SelectedMemo = memos[currentIndex - 1];
        }
        else if (memos.Count > 0)
        {
            _viewModel.SelectedMemo = memos[memos.Count - 1];
        }
    }

    private void DuplicateCurrentLine()
    {
        var textBox = NoteTextBox;
        if (textBox == null) return;

        var caretIndex = textBox.CaretIndex;
        var text = textBox.Text;

        // 找到当前行的开始和结束
        var lineStart = text.LastIndexOf('\n', Math.Max(0, caretIndex - 1)) + 1;
        var lineEnd = text.IndexOf('\n', caretIndex);
        if (lineEnd == -1) lineEnd = text.Length;

        var currentLine = text.Substring(lineStart, lineEnd - lineStart);
        var newText = text.Insert(lineEnd, "\n" + currentLine);

        textBox.Text = newText;
        textBox.CaretIndex = lineEnd + 1 + currentLine.Length;
    }

    private void InsertIndent()
    {
        var textBox = NoteTextBox;
        if (textBox == null) return;

        var caretIndex = textBox.CaretIndex;
        textBox.Text = textBox.Text.Insert(caretIndex, "    ");
        textBox.CaretIndex = caretIndex + 4;
    }

    private void IncreaseIndent()
    {
        var textBox = NoteTextBox;
        if (textBox == null) return;

        var caretIndex = textBox.CaretIndex;
        var text = textBox.Text;

        // 找到当前行开始
        var lineStart = text.LastIndexOf('\n', Math.Max(0, caretIndex - 1)) + 1;
        textBox.Text = text.Insert(lineStart, "    ");
        textBox.CaretIndex = caretIndex + 4;
    }

    private void DecreaseIndent()
    {
        var textBox = NoteTextBox;
        if (textBox == null) return;

        var caretIndex = textBox.CaretIndex;
        var text = textBox.Text;

        // 找到当前行开始
        var lineStart = text.LastIndexOf('\n', Math.Max(0, caretIndex - 1)) + 1;

        // 检查是否有缩进可以删除
        if (lineStart < text.Length && text.Substring(lineStart, Math.Min(4, text.Length - lineStart)) == "    ")
        {
            textBox.Text = text.Remove(lineStart, 4);
            textBox.CaretIndex = Math.Max(lineStart, caretIndex - 4);
        }
        else if (lineStart < text.Length && text[lineStart] == ' ')
        {
            // 删除单个空格
            textBox.Text = text.Remove(lineStart, 1);
            textBox.CaretIndex = Math.Max(lineStart, caretIndex - 1);
        }
    }

    private void MainWindow_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyboardDevice.Modifiers == ModifierKeys.Control && e.Key == Key.N)
        {
            e.Handled = true;
            _ = _viewModel.CreateMemoCommand.ExecuteAsync(null);
        }
    }

    private async void AutoSave_Tick(object? sender, EventArgs e)
    {
        _autoSaveTimer?.Stop();
        if (_viewModel.SelectedMemo != null)
        {
            try
            {
                await _viewModel.SaveMemoCommand.ExecuteAsync(null);
                _viewModel.SetStatus("自动保存");
            }
            catch
            {
                // ignore
            }
        }
    }

    private void BackgroundOpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (sender is Slider slider && _viewModel != null)
        {
            // 使用统一的透明度转换逻辑
            var actualOpacity = TransparencyHelper.FromPercent(slider.Value);

            // 更新背景透明度（影响主容器背景）
            UpdateBackgroundOpacity(actualOpacity);

            // 更新ViewModel中的值
            _viewModel.BackgroundOpacity = actualOpacity;
            _viewModel.BackgroundOpacityPercent = slider.Value;

            // 状态提示
            _viewModel.SetStatus($"透明度已调整为 {(int)slider.Value}%");
        }
    }

    private void UpdateBackgroundOpacity(double opacity)
    {
        if (MainContainer != null)
        {
            // 创建新的背景画刷
            var backgroundColor = System.Windows.Media.Color.FromArgb(
                (byte)(255 * opacity), // Alpha通道
                255, 255, 255); // RGB白色

            MainContainer.Background = new SolidColorBrush(backgroundColor);
        }
    }
}