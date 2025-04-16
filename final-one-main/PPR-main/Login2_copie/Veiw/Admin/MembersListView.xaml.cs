using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using MySql.Data.MySqlClient;
using UserModels;
using System.Diagnostics;
using DataGridNamespace.Veiw.Converters;

namespace DataGridNamespace.Admin
{
    public partial class MembersListView : Page
    {
        private List<User> allMembers;
        private CollectionViewSource membersViewSource;

        public MembersListView()
        {
            InitializeComponent();
            LoadMembers();
        }

        private void LoadMembers()
        {
            try
            {
                allMembers = new List<User>();
                string connectionString = "Server=localhost;Database=gestion_theses;User ID=root;Password=";
                string query = "SELECT Id, Nom, Email, Role FROM users";

                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var user = new User
                                {
                                    Id = reader.GetInt32("Id"),
                                    Nom = reader.GetString("Nom"),
                                    Email = reader.GetString("Email"),
                                    Role = ConvertStringToRole(reader.GetString("Role"))
                                };
                                allMembers.Add(user);
                            }
                        }
                    }
                }

                membersViewSource = new CollectionViewSource { Source = allMembers };
                membersViewSource.Filter += MembersViewSource_Filter;
                MembersDataGrid.ItemsSource = membersViewSource.View;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading members: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (membersViewSource?.View != null)
            {
                membersViewSource.View.Refresh();
            }
        }

        private void MembersViewSource_Filter(object sender, FilterEventArgs e)
        {
            if (SearchTextBox == null || string.IsNullOrEmpty(SearchTextBox.Text))
            {
                e.Accepted = true;
                return;
            }

            if (e.Item is User user)
            {
                string searchText = SearchTextBox.Text.ToLower();
                e.Accepted = user.Nom.ToLower().Contains(searchText) ||
                           user.Email.ToLower().Contains(searchText) ||
                           user.Role.ToString().ToLower().Contains(searchText) ||
                           user.Id.ToString().Contains(searchText);
            }
            else
            {
                e.Accepted = false;
            }
        }

        private RoleUtilisateur ConvertStringToRole(string roleString)
        {
            return roleString.ToLower() switch
            {
                "admin" => RoleUtilisateur.Admin,
                "etudiant" => RoleUtilisateur.Etudiant,
                "simpleuser" => RoleUtilisateur.SimpleUser,
                _ => RoleUtilisateur.SimpleUser
            };
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is User user)
            {
                var editWindow = new EditMember(user, LoadMembers);
                editWindow.ShowDialog();
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is User user)
            {
                var result = MessageBox.Show($"Are you sure you want to delete {user.Nom}?", 
                    "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        string connectionString = "Server=localhost;Database=gestion_theses;User ID=root;Password=";
                        using (MySqlConnection conn = new MySqlConnection(connectionString))
                        {
                            conn.Open();
                            string query = "DELETE FROM users WHERE Id = @Id";
                            using (MySqlCommand cmd = new MySqlCommand(query, conn))
                            {
                                cmd.Parameters.AddWithValue("@Id", user.Id);
                                cmd.ExecuteNonQuery();
                            }
                        }
                        LoadMembers(); // Refresh the list
                        MessageBox.Show("User deleted successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error deleting user: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }
    }

    public class RowNumberConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is DataGridRow row)
            {
                return row.GetIndex() + 1;
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class RoleToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
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

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class NameToInitialsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
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

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}