using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace DesktopMemo.App.Converters;

public sealed class InverseBooleanToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool flag)
        {
            var inverse = parameter?.ToString()?.Equals("Inverse", StringComparison.OrdinalIgnoreCase) == true ? !flag : flag;
            return inverse ? Visibility.Collapsed : Visibility.Visible;
        }

        return Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Visibility visibility)
        {
            return visibility != Visibility.Visible;
        }

        return false;
    }
}
