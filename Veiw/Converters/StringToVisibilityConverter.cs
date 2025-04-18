using System;
using System.Windows;
using System.Windows.Data;
using System.Globalization;

namespace DataGridNamespace.Veiw.Converters
{
    public class StringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string text)
            {
                // If parameter is provided and equals "Inverse", use inverse logic
                bool inverse = parameter != null && parameter.ToString() == "Inverse";
                
                if (inverse)
                {
                    return string.IsNullOrEmpty(text) ? Visibility.Visible : Visibility.Collapsed;
                }
                else
                {
                    return string.IsNullOrEmpty(text) ? Visibility.Collapsed : Visibility.Visible;
                }
            }
            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
