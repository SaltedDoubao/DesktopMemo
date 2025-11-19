using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace DesktopMemo.App.Converters;

/// <summary>
/// 比较两个值是否相等并转换为可见性（反向）。
/// 相等时返回 Visible，不相等时返回 Collapsed。
/// </summary>
public class InverseEqualityToVisibilityConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values == null || values.Length != 2)
        {
            return Visibility.Collapsed;
        }

        // 处理 null 值
        if (values[0] == null && values[1] == null)
        {
            return Visibility.Visible;
        }

        if (values[0] == null || values[1] == null)
        {
            return Visibility.Collapsed;
        }

        // 比较两个值
        return Equals(values[0], values[1]) ? Visibility.Visible : Visibility.Collapsed;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
