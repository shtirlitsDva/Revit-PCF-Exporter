using System;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace ModelessForms.IssuesManager.Views
{
    /// <summary>
    /// Visible when the value is "present": true for a bool, non-empty for a
    /// string, non-null for anything else. <see cref="Invert"/> flips it.
    /// </summary>
    public sealed class VisibleWhenConverter : IValueConverter
    {
        public bool Invert { get; set; }

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            bool present = value switch
            {
                null => false,
                bool b => b,
                string s => s.Length > 0,
                _ => true,
            };
            return present != Invert ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }

    /// <summary>
    /// Loads an image file as a small thumbnail. The file is read fully into memory
    /// (OnLoad), so it is not locked and can be deleted while it is shown.
    /// </summary>
    public sealed class ThumbnailConverter : IValueConverter
    {
        private const int DecodeWidth = 280;

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (!(value is string path) || !File.Exists(path)) return null;

            try
            {
                var image = new BitmapImage();
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                image.DecodePixelWidth = DecodeWidth;
                image.UriSource = new Uri(path);
                image.EndInit();
                image.Freeze();
                return image;
            }
            catch (Exception ex) when (ex is IOException || ex is FormatException // incl. System.IO.FileFormatException
                                       || ex is NotSupportedException || ex is UnauthorizedAccessException
                                       || ex is System.Runtime.InteropServices.COMException // WIC decoder errors
                                       || ex is ArgumentException || ex is InvalidOperationException)
            {
                // A converter exception would escape the binding engine into Revit.
                // Null makes the view show "Cannot show image" for this thumbnail.
                return null;
            }
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }
}
