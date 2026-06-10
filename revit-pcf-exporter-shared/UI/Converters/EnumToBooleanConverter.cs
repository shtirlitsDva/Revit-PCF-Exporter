using System;
using System.Globalization;
using System.Windows.Data;

namespace PcfExporter.UI.Converters
{
    /// <summary>
    /// Binds a RadioButton's IsChecked to an enum property:
    /// IsChecked="{Binding Scope, Converter={StaticResource EnumToBool}, ConverterParameter=AllInOneFile}".
    /// </summary>
    public sealed class EnumToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null) return false;
            return string.Equals(value.ToString(), parameter.ToString(), StringComparison.OrdinalIgnoreCase);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is bool isChecked) || !isChecked || parameter == null)
                return System.Windows.Data.Binding.DoNothing;
            return Enum.Parse(targetType, parameter.ToString(), ignoreCase: true);
        }
    }

    /// <summary>Inverse of a boolean — for disabling panels while busy.</summary>
    public sealed class InverseBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
            value is bool b && !b;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            value is bool b && !b;
    }

    /// <summary>Collapses an element when the bound value is false.</summary>
    public sealed class BooleanToVisibilityCollapsedConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
            value is bool b && b ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }
}
