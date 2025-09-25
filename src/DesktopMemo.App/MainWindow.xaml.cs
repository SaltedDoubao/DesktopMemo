using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
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
                _windowService.MinimizeToTray();
                break;
            case MessageBoxResult.No:
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
            }
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
}