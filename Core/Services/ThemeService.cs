using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using DesktopMemo.Core.Interfaces;
using Color = System.Windows.Media.Color;

namespace DesktopMemo.Core.Services
{
    public class ThemeService : IThemeService
    {
        private readonly ISettingsService _settingsService;
        private readonly Dictionary<string, ThemeColors> _themes;
        private string _currentTheme;

        public event EventHandler<ThemeChangedEventArgs>? ThemeChanged;

        public ThemeService(ISettingsService settingsService)
        {
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            _themes = InitializeThemes();
            _currentTheme = _settingsService.GetSetting("Theme", "Dark");
        }

        private Dictionary<string, ThemeColors> InitializeThemes()
        {
            return new Dictionary<string, ThemeColors>
            {
                ["Dark"] = new ThemeColors
                {
                    Primary = Color.FromRgb(33, 150, 243),      // #2196F3
                    Secondary = Color.FromRgb(100, 181, 246),    // #64B5F6
                    Background = Color.FromRgb(30, 30, 30),      // #1E1E1E
                    Surface = Color.FromRgb(45, 45, 45),         // #2D2D2D
                    OnBackground = Color.FromRgb(224, 224, 224), // #E0E0E0
                    OnSurface = Color.FromRgb(255, 255, 255),   // #FFFFFF
                    Accent = Color.FromRgb(100, 181, 246),       // #64B5F6
                    Error = Color.FromRgb(244, 67, 54),          // #F44336
                    Warning = Color.FromRgb(255, 152, 0),        // #FF9800
                    Success = Color.FromRgb(76, 175, 80),        // #4CAF50
                    TextPrimary = Color.FromRgb(255, 255, 255),  // #FFFFFF
                    TextSecondary = Color.FromRgb(189, 189, 189),// #BDBDBD
                    Border = Color.FromArgb(64, 255, 255, 255),  // #40FFFFFF
                    BackgroundOpacity = 0.95
                },
                ["Light"] = new ThemeColors
                {
                    Primary = Color.FromRgb(25, 118, 210),       // #1976D2
                    Secondary = Color.FromRgb(33, 150, 243),     // #2196F3
                    Background = Color.FromRgb(250, 250, 250),   // #FAFAFA
                    Surface = Color.FromRgb(255, 255, 255),      // #FFFFFF
                    OnBackground = Color.FromRgb(33, 33, 33),    // #212121
                    OnSurface = Color.FromRgb(0, 0, 0),          // #000000
                    Accent = Color.FromRgb(33, 150, 243),        // #2196F3
                    Error = Color.FromRgb(211, 47, 47),          // #D32F2F
                    Warning = Color.FromRgb(245, 124, 0),        // #F57C00
                    Success = Color.FromRgb(56, 142, 60),        // #388E3C
                    TextPrimary = Color.FromRgb(33, 33, 33),     // #212121
                    TextSecondary = Color.FromRgb(117, 117, 117),// #757575
                    Border = Color.FromArgb(32, 0, 0, 0),        // #20000000
                    BackgroundOpacity = 0.95
                },
                ["HighContrast"] = new ThemeColors
                {
                    Primary = Color.FromRgb(255, 255, 0),        // #FFFF00
                    Secondary = Color.FromRgb(0, 255, 255),      // #00FFFF
                    Background = Color.FromRgb(0, 0, 0),         // #000000
                    Surface = Color.FromRgb(30, 30, 30),         // #1E1E1E
                    OnBackground = Color.FromRgb(255, 255, 255), // #FFFFFF
                    OnSurface = Color.FromRgb(255, 255, 255),   // #FFFFFF
                    Accent = Color.FromRgb(255, 255, 0),         // #FFFF00
                    Error = Color.FromRgb(255, 0, 0),            // #FF0000
                    Warning = Color.FromRgb(255, 165, 0),        // #FFA500
                    Success = Color.FromRgb(0, 255, 0),          // #00FF00
                    TextPrimary = Color.FromRgb(255, 255, 255),  // #FFFFFF
                    TextSecondary = Color.FromRgb(200, 200, 200),// #C8C8C8
                    Border = Color.FromRgb(255, 255, 255),       // #FFFFFF
                    BackgroundOpacity = 1.0
                }
            };
        }

        public void SetTheme(string themeName)
        {
            if (!_themes.ContainsKey(themeName))
            {
                throw new ArgumentException($"Theme '{themeName}' not found");
            }

            _currentTheme = themeName;
            _settingsService.SetSetting("Theme", themeName);

            ApplyTheme(_themes[themeName]);

            ThemeChanged?.Invoke(this, new ThemeChangedEventArgs(themeName, _themes[themeName]));
        }

        private void ApplyTheme(ThemeColors colors)
        {
            var app = System.Windows.Application.Current;
            if (app?.Resources == null) return;

            // Update application resources
            app.Resources["PrimaryColor"] = new SolidColorBrush(colors.Primary);
            app.Resources["SecondaryColor"] = new SolidColorBrush(colors.Secondary);
            app.Resources["BackgroundColor"] = new SolidColorBrush(colors.Background);
            app.Resources["SurfaceColor"] = new SolidColorBrush(colors.Surface);
            app.Resources["OnBackgroundColor"] = new SolidColorBrush(colors.OnBackground);
            app.Resources["OnSurfaceColor"] = new SolidColorBrush(colors.OnSurface);
            app.Resources["AccentColor"] = new SolidColorBrush(colors.Accent);
            app.Resources["ErrorColor"] = new SolidColorBrush(colors.Error);
            app.Resources["WarningColor"] = new SolidColorBrush(colors.Warning);
            app.Resources["SuccessColor"] = new SolidColorBrush(colors.Success);
            app.Resources["TextPrimaryColor"] = new SolidColorBrush(colors.TextPrimary);
            app.Resources["TextSecondaryColor"] = new SolidColorBrush(colors.TextSecondary);
            app.Resources["BorderColor"] = new SolidColorBrush(colors.Border);
            app.Resources["BackgroundOpacity"] = colors.BackgroundOpacity;
        }

        public string GetCurrentTheme()
        {
            return _currentTheme;
        }

        public IEnumerable<string> GetAvailableThemes()
        {
            return _themes.Keys;
        }

        public ThemeColors GetThemeColors()
        {
            return _themes.ContainsKey(_currentTheme)
                ? _themes[_currentTheme]
                : _themes["Dark"];
        }
    }
}