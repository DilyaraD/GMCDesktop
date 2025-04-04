using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace GeotekMetallCompleteDesktop.Converters
{
    public class EditButtonVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string fileType)
            {
                return fileType.Equals(".docx", StringComparison.OrdinalIgnoreCase) ||
                       fileType.Equals(".txt", StringComparison.OrdinalIgnoreCase)
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}