using System;
using System.Globalization;
using System.Windows.Data;

namespace DesktopMemo.App.Converters;

public sealed class EnumToBooleanConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is null || parameter is null)
        {
            return false;
        }

        var enumValue = value.ToString();
        var targetValue = parameter.ToString();
        return string.Equals(enumValue, targetValue, StringComparison.OrdinalIgnoreCase);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isChecked && isChecked && parameter is string enumString)
        {
            return Enum.Parse(targetType, enumString);
        }

        return System.Windows.Data.Binding.DoNothing;
    }
}
