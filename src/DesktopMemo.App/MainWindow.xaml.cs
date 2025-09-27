using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using DesktopMemo.App.ViewModels;
using DesktopMemo.Core.Contracts;
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
            _windowService.SetTopmostMode(TopmostMode.Desktop);
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
            From = show ? 320 : 0,
            To = show ? 0 : 320,
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

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        await _viewModel.InitializeAsync();
        ApplySettingsPanelVisibility(_viewModel.IsSettingsPanelVisible);
    }

    private void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        _autoSaveTimer?.Stop();
        _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
    }

    private void OnLocationChanged(object? sender, EventArgs e)
    {
        // 实时更新位置显示
        _viewModel.UpdateCurrentPosition();
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void SettingsPanel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.Source == sender && _viewModel.IsSettingsPanelVisible)
        {
            _viewModel.IsSettingsPanelVisible = false;
            e.Handled = true;
        }
    }

    private void MainContentArea_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_viewModel.IsSettingsPanelVisible)
        {
            _viewModel.IsSettingsPanelVisible = false;
            e.Handled = true;
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            "是否最小化到托盘？\n点击'是'最小化到托盘，点击'否'完全退出程序。",
            "退出确认",
            MessageBoxButton.YesNoCancel,
            MessageBoxImage.Question);

        switch (result)
        {
            case MessageBoxResult.Yes:
                _viewModel.TrayHideWindowCommand.Execute(null);
                break;
            case MessageBoxResult.No:
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
            // 将滑块的0-100%映射到实际的0-60%效果
            // 公式：实际透明度 = 滑块值 * 0.6 / 100
            var actualOpacity = (slider.Value * 0.6) / 100.0;

            // 更新背景透明度（影响主容器背景）
            UpdateBackgroundOpacity(actualOpacity);

            // 更新ViewModel中的值
            _viewModel.BackgroundOpacityPercent = slider.Value;

            // 状态提示
            _viewModel.SetStatus($"背景透明度已调整为 {(int)slider.Value}%");
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