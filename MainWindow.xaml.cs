using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using DesktopMemo.Core.Interfaces;
using DesktopMemo.Infrastructure;
using DesktopMemo.UI.ViewModels;
using DesktopMemo.UI.Converters;

namespace DesktopMemo
{
    /// <summary>
    /// Refactored MainWindow using MVVM architecture with proper service separation
    /// </summary>
    public partial class MainWindow : Window
    {
        private MainViewModel _viewModel = null!;
        private ITrayService _trayService = null!;
        private IThemeService _themeService = null!;
        private ILocalizationService _localizationService = null!;
        private ISettingsService _settingsService = null!;

        public MainWindow()
        {
            InitializeComponent();
            InitializeServices();
            InitializeWindow();
            InitializeDataBinding();
            InitializeTrayService();
            LoadWindowSettings();
        }

        private void InitializeServices()
        {
            // Get services from DI container
            _settingsService = ServiceLocator.GetService<ISettingsService>();
            _themeService = ServiceLocator.GetService<IThemeService>();
            _localizationService = ServiceLocator.GetService<ILocalizationService>();
            _trayService = ServiceLocator.GetService<ITrayService>();

            // Create ViewModel with all required services
            _viewModel = new MainViewModel(
                ServiceLocator.GetService<IMemoService>(),
                ServiceLocator.GetService<IWindowManagementService>(),
                _settingsService,
                _localizationService);

            // Set DataContext
            DataContext = _viewModel;
        }

        private void InitializeWindow()
        {
            // Register value converters in resources
            Resources.Add("InverseBooleanConverter", new InverseBooleanConverter());
            Resources.Add("PinIconConverter", new PinIconConverter());
            Resources.Add("BooleanToVisibilityConverter", new BooleanToVisibilityConverter());

            // Window event handlers
            this.MouseLeftButtonDown += Window_MouseLeftButtonDown;
            this.Closing += Window_Closing;
            this.Loaded += Window_Loaded;
            this.LocationChanged += Window_LocationChanged;
            this.SizeChanged += Window_SizeChanged;

            // Subscribe to theme changes
            _themeService.ThemeChanged += OnThemeChanged;
            _localizationService.LanguageChanged += OnLanguageChanged;
        }

        private void InitializeDataBinding()
        {
            // Additional data binding setup if needed
            // The main binding is handled through DataContext = _viewModel
        }

        private void InitializeTrayService()
        {
            _trayService.Initialize();

            // Subscribe to tray events
            _trayService.ShowHideRequested += OnShowHideRequested;
            _trayService.ExitRequested += OnExitRequested;
            _trayService.NewMemoRequested += OnNewMemoRequested;
            _trayService.TopmostModeChanged += OnTopmostModeChanged;
            _trayService.ClickThroughToggled += OnClickThroughToggled;
        }

        private void LoadWindowSettings()
        {
            // Load window position and size from settings
            var left = _settingsService.GetSetting("WindowLeft", Left);
            var top = _settingsService.GetSetting("WindowTop", Top);
            var width = _settingsService.GetSetting("WindowWidth", Width);
            var height = _settingsService.GetSetting("WindowHeight", Height);

            // Ensure window is on screen
            if (left >= 0 && top >= 0 &&
                left < SystemParameters.VirtualScreenWidth &&
                top < SystemParameters.VirtualScreenHeight)
            {
                Left = left;
                Top = top;
            }

            if (width > 0 && height > 0)
            {
                Width = Math.Max(200, width);  // Minimum width
                Height = Math.Max(150, height); // Minimum height
            }
        }

        #region Window Events

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void Window_Closing(object? sender, CancelEventArgs e)
        {
            // Hide to tray instead of closing
            if (_settingsService.GetSetting("ShowExitPrompt", true))
            {
                var result = System.Windows.MessageBox.Show(
                    _localizationService.GetString("dialog.exit_confirm"),
                    _localizationService.GetString("window.title"),
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Question);

                if (result != System.Windows.MessageBoxResult.Yes)
                {
                    e.Cancel = true;
                    return;
                }
            }

            // Save current state
            SaveWindowSettings();
            _settingsService.SaveSettingsAsync().Wait();

            // Clean up services
            _trayService?.Dispose();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Configure window for Win32 operations
            var hwnd = new WindowInteropHelper(this).Handle;
            if (hwnd != IntPtr.Zero)
            {
                Utils.Win32Helper.HideFromTaskbar(hwnd);
            }

            // Apply initial theme
            ApplyCurrentTheme();
        }

        private void Window_LocationChanged(object? sender, EventArgs e)
        {
            // Auto-save position (debounced)
            _settingsService.SetSetting("WindowLeft", Left);
            _settingsService.SetSetting("WindowTop", Top);
        }

        private void Window_SizeChanged(object? sender, SizeChangedEventArgs e)
        {
            // Auto-save size
            _settingsService.SetSetting("WindowWidth", Width);
            _settingsService.SetSetting("WindowHeight", Height);
        }

        #endregion

        #region Tray Service Events

        private void OnShowHideRequested(object? sender, EventArgs e)
        {
            if (IsVisible)
            {
                Hide();
            }
            else
            {
                Show();
                Activate();
                Focus();
            }
        }

        private void OnExitRequested(object? sender, EventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
        }

        private void OnNewMemoRequested(object? sender, EventArgs e)
        {
            _viewModel.AddMemoCommand.Execute(null);
            Show();
            Activate();
        }

        private void OnTopmostModeChanged(object? sender, TopmostMode mode)
        {
            _viewModel.SetTopmostModeCommand.Execute(mode.ToString());
        }

        private void OnClickThroughToggled(object? sender, EventArgs e)
        {
            _viewModel.TogglePenetrationModeCommand.Execute(null);
        }

        #endregion

        #region Theme and Localization Events

        private void OnThemeChanged(object? sender, ThemeChangedEventArgs e)
        {
            ApplyCurrentTheme();
        }

        private void OnLanguageChanged(object? sender, LanguageChangedEventArgs e)
        {
            // UI language bindings will be updated automatically through property change notifications
            // when the localization service language changes
        }

        private void ApplyCurrentTheme()
        {
            var colors = _themeService.GetThemeColors();

            // Apply theme colors to window resources
            Resources["BackgroundOpacity"] = colors.BackgroundOpacity;
            Resources["FontFamily"] = _settingsService.GetSetting("FontFamily", "Segoe UI, Microsoft YaHei");
            Resources["FontSize"] = _settingsService.GetSetting("FontSize", 14.0);
        }

        #endregion

        #region UI Event Handlers

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Hide();
        }

        #endregion

        #region Keyboard Shortcuts

        protected override void OnKeyDown(System.Windows.Input.KeyEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                switch (e.Key)
                {
                    case Key.N:
                        _viewModel.AddMemoCommand.Execute(null);
                        e.Handled = true;
                        break;
                    case Key.S:
                        _viewModel.SaveMemoCommand.Execute(null);
                        e.Handled = true;
                        break;
                    case Key.D:
                        _viewModel.DeleteMemoCommand.Execute(null);
                        e.Handled = true;
                        break;
                    case Key.Tab:
                        if (Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift))
                            _viewModel.SwitchToPreviousMemoCommand.Execute(null);
                        else
                            _viewModel.SwitchToNextMemoCommand.Execute(null);
                        e.Handled = true;
                        break;
                    case Key.E:
                        _viewModel.ToggleEditModeCommand.Execute(null);
                        e.Handled = true;
                        break;
                    case Key.P:
                        _viewModel.TogglePinCommand.Execute(null);
                        e.Handled = true;
                        break;
                    case Key.Escape:
                        Hide();
                        e.Handled = true;
                        break;
                }
            }

            base.OnKeyDown(e);
        }

        #endregion

        #region Helper Methods

        private void SaveWindowSettings()
        {
            _settingsService.SetSetting("WindowLeft", Left);
            _settingsService.SetSetting("WindowTop", Top);
            _settingsService.SetSetting("WindowWidth", Width);
            _settingsService.SetSetting("WindowHeight", Height);
        }

        #endregion
    }
}