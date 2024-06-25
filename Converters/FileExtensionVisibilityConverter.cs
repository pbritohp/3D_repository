using System;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Data;

namespace _3d_repo.Converters
{
    public class FileExtensionVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string fileName = value as string;
            if (fileName != null)
            {
                string extension = Path.GetExtension(fileName).ToLower();
                return (extension == ".obj" || extension == ".stl") ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
