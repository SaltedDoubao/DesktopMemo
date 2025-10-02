using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace DesktopMemo.App.Converters;

/// <summary>
/// 将数字转换为可见性。大于0时显示，等于0时隐藏。
/// </summary>
public class CountToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int count)
        {
            return count > 0 ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

