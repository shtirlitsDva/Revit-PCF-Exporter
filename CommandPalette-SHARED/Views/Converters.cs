#nullable enable
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Norsyn.CommandPalette.Views
{
    // true → Collapsed, false → Visible. Used to hide the Expand/Collapse-all row
    // while searching, and to show the monogram only when there is no icon.
    public sealed class InverseBoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is bool b && b ? Visibility.Collapsed : Visibility.Visible;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => value is Visibility v && v != Visibility.Visible;
    }
}
