using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using DesktopMemo.App.ViewModels;
using DesktopMemo.Core.Contracts;

namespace DesktopMemo.App;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly IWindowService _windowService;
    private readonly ITrayService _trayService;
    private bool _isSettingsPanelVisible = false;
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

            ConfigureWindow();
            ConfigureTrayService();
            InitializeAutoSaveTimer();

            Loaded += OnLoaded;
            Closing += OnClosing;
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
        // 设置默认桌面模式
        Loaded += (s, e) =>
        {
            _windowService.SetTopmostMode(TopmostMode.Desktop);
            _windowService.PlayFadeInAnimation();
        };
    }

    private void ConfigureTrayService()
    {
        // 托盘事件处理
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
            if (!_isSettingsPanelVisible)
            {
                ToggleSettingsPanel();
            }
        };
        _trayService.ExitClick += (s, e) => Application.Current.Shutdown();
    }

    private void InitializeAutoSaveTimer()
    {
        _autoSaveTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(2)
        };
        _autoSaveTimer.Tick += AutoSave_Tick;
    }

    private void ToggleSettingsPanel()
    {
        _isSettingsPanelVisible = !_isSettingsPanelVisible;

        if (_isSettingsPanelVisible)
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
        var transform = SettingsPanel.RenderTransform as TranslateTransform;
        if (transform == null)
        {
            transform = new TranslateTransform();
            SettingsPanel.RenderTransform = transform;
        }

        var animation = new DoubleAnimation
        {
            From = show ? 320 : 0,
            To = show ? 0 : 320,
            Duration = TimeSpan.FromMilliseconds(300),
            EasingFunction = new ExponentialEase { EasingMode = show ? EasingMode.EaseOut : EasingMode.EaseIn }
        };

        if (!show)
        {
            animation.Completed += (s, e) => SettingsPanel.Visibility = Visibility.Collapsed;
        }

        transform.BeginAnimation(TranslateTransform.XProperty, animation);
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        await _viewModel.InitializeAsync();
    }

    private void OnClosing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        _autoSaveTimer?.Stop();
    }

    // 事件处理程序
    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
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
        if (_isSettingsPanelVisible)
        {
            ToggleSettingsPanel();
        }
    }

    private void SettingsPanel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.Source == sender && _isSettingsPanelVisible)
        {
            ToggleSettingsPanel();
            e.Handled = true;
        }
    }

    private void MainContentArea_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_isSettingsPanelVisible && e.Source != SettingsToggle)
        {
            ToggleSettingsPanel();
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
                _windowService.MinimizeToTray();
                _trayService.ShowBalloonTip("DesktopMemo", "应用已最小化到托盘，双击图标可恢复窗口");
                break;
            case MessageBoxResult.No:
                Application.Current.Shutdown();
                break;
            default:
                break;
        }
    }

    private void NoteTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        // 重新启动自动保存计时器
        _autoSaveTimer?.Stop();
        _autoSaveTimer?.Start();
    }

    private void NoteTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        // 处理快捷键
        if (e.KeyboardDevice.Modifiers == ModifierKeys.Control)
        {
            switch (e.Key)
            {
                case Key.S:
                    e.Handled = true;
                    _ = _viewModel.SaveMemoCommand.ExecuteAsync(null);
                    StatusText.Text = "已保存";
                    break;
            }
        }
    }

    private void MainWindow_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyboardDevice.Modifiers == ModifierKeys.Control)
        {
            switch (e.Key)
            {
                case Key.N:
                    e.Handled = true;
                    _ = _viewModel.CreateMemoCommand.ExecuteAsync(null);
                    break;
            }
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
            }
            catch
            {
                // 忽略自动保存错误
            }
        }
    }
}