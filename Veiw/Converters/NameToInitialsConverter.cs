using System;
using System.Windows.Data;
using System.Globalization;

namespace DataGridNamespace.Veiw.Converters
{
    public class NameToInitialsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string name && !string.IsNullOrEmpty(name))
            {
                var parts = name.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 0)
                {
                    if (parts.Length == 1)
                        return parts[0][0].ToString().ToUpper();
                    return (parts[0][0].ToString() + parts[^1][0].ToString()).ToUpper();
                }
            }
            return "?";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 