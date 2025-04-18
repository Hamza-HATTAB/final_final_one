using System;
using System.Windows.Data;
using System.Windows.Media;
using System.Globalization;
using UserModels;

namespace DataGridNamespace.Veiw.Converters
{
    public class RoleToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is RoleUtilisateur role)
            {
                return role switch
                {
                    RoleUtilisateur.Admin => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4F46E5")),
                    RoleUtilisateur.Etudiant => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0EA5E9")),
                    RoleUtilisateur.SimpleUser => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981")),
                    _ => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6B7280"))
                };
            }
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6B7280"));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 