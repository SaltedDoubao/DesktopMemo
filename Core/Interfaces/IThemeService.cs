using System;
using System.Collections.Generic;
using System.Windows.Media;

namespace DesktopMemo.Core.Interfaces
{
    public interface IThemeService
    {
        void SetTheme(string themeName);
        string GetCurrentTheme();
        IEnumerable<string> GetAvailableThemes();
        ThemeColors GetThemeColors();
        event EventHandler<ThemeChangedEventArgs>? ThemeChanged;
    }

    public class ThemeChangedEventArgs : EventArgs
    {
        public string ThemeName { get; }
        public ThemeColors Colors { get; }

        public ThemeChangedEventArgs(string themeName, ThemeColors colors)
        {
            ThemeName = themeName;
            Colors = colors;
        }
    }

    public class ThemeColors
    {
        public System.Windows.Media.Color Primary { get; set; }
        public System.Windows.Media.Color Secondary { get; set; }
        public System.Windows.Media.Color Background { get; set; }
        public System.Windows.Media.Color Surface { get; set; }
        public System.Windows.Media.Color OnBackground { get; set; }
        public System.Windows.Media.Color OnSurface { get; set; }
        public System.Windows.Media.Color Accent { get; set; }
        public System.Windows.Media.Color Error { get; set; }
        public System.Windows.Media.Color Warning { get; set; }
        public System.Windows.Media.Color Success { get; set; }
        public System.Windows.Media.Color TextPrimary { get; set; }
        public System.Windows.Media.Color TextSecondary { get; set; }
        public System.Windows.Media.Color Border { get; set; }
        public double BackgroundOpacity { get; set; }
    }
}