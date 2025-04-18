using System;
using System.Windows;
using System.Windows.Data;
using System.Globalization;

namespace DataGridNamespace.Veiw.Converters
{
    public class BoolToFavoriteTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isFavorite)
            {
                return isFavorite ? "Remove from Favorites" : "Add to Favorites";
            }
            return "Add to Favorites";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
