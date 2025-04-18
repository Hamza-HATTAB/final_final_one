using System;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows.Shapes;
using MySql.Data.MySqlClient;
using DataGridNamespace;
using UserModels;

namespace DataGrid
{
    public partial class ProfileView : Page
    {
        private bool isEditing = false;
        private User currentUser;

        public bool IsMaximize { get; private set; }
        public object EditButton { get; private set; }

        public ProfileView()
        {
            InitializeComponent();
            LoadUserData();
        }

        private void LoadUserData()
        {
            try
            {
                string connectionString = "server=localhost;database=gestion_theses;user=root;password=;";
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT Id, Nom, Email, Role FROM users WHERE Id = @UserId";
                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@UserId", Session.CurrentUserId);
                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                currentUser = new User
                                {
                                    Id = reader.GetInt32("Id"),
                                    Nom = reader.GetString("Nom"),
                                    Email = reader.GetString("Email"),
                                    Role = ConvertStringToRole(reader.GetString("Role"))
                                };

                                // Update UI with user data
                                UsernameTextBox.Text = currentUser.Nom;
                                EmailTextBox.Text = currentUser.Email;
                                RoleTextBox.Text = currentUser.Role.ToString();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading user data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private RoleUtilisateur ConvertStringToRole(string roleString)
        {
            // Handle case-insensitive conversion
            if (string.IsNullOrEmpty(roleString))
                return RoleUtilisateur.SimpleUser; // Default role

            switch (roleString.ToLower())
            {
                case "admin":
                    return RoleUtilisateur.Admin;
                case "etudiant":
                    return RoleUtilisateur.Etudiant;
                case "simpleuser":
                case "simple user":
                    return RoleUtilisateur.SimpleUser;
                default:
                    return RoleUtilisateur.SimpleUser;
            }
        }

        private void EditProfile_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            if (button == null) return;

            if (!isEditing)
            {
                // Enable editing mode
                UsernameTextBox.IsReadOnly = false;
                EmailTextBox.IsReadOnly = false;
                UsernameTextBox.Background = Brushes.White;
                EmailTextBox.Background = Brushes.White;
                PasswordContainer.Visibility = Visibility.Visible;
                button.Content = "Save Changes";
                isEditing = true;
            }
            else
            {
                // Save changes and disable editing mode
                SaveChanges();
                UsernameTextBox.IsReadOnly = true;
                EmailTextBox.IsReadOnly = true;
                UsernameTextBox.Background = Brushes.Transparent;
                EmailTextBox.Background = Brushes.Transparent;
                PasswordContainer.Visibility = Visibility.Collapsed;
                button.Content = "Edit Profile";
                isEditing = false;
            }
        }

        private void SaveChanges()
        {
            string newUsername = UsernameTextBox.Text;
            string newEmail = EmailTextBox.Text;
            string newPassword = PasswordBox.Password;

            if (string.IsNullOrWhiteSpace(newUsername) || string.IsNullOrWhiteSpace(newEmail))
            {
                MessageBox.Show("Username and email cannot be empty!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Replace hardcoded connection string with AppConfig.CloudSqlConnectionString
            string connectionString = AppConfig.CloudSqlConnectionString;
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                try
                {
                    conn.Open();
                    string query = "UPDATE users SET Nom = @Nom, Email = @Email";
                    if (!string.IsNullOrWhiteSpace(newPassword))
                    {
                        query += ", Password = @Password";
                    }
                    query += " WHERE Id = @Id";

                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Nom", newUsername);
                        cmd.Parameters.AddWithValue("@Email", newEmail);
                        cmd.Parameters.AddWithValue("@Id", Session.CurrentUserId);

                        if (!string.IsNullOrWhiteSpace(newPassword))
                        {
                            cmd.Parameters.AddWithValue("@Password", newPassword);
                        }

                        int rowsAffected = cmd.ExecuteNonQuery();
                        if (rowsAffected > 0)
                        {
                            MessageBox.Show("Profile updated successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                            // Update current user data
                            currentUser.Nom = newUsername;
                            currentUser.Email = newEmail;
                        }
                        else
                        {
                            MessageBox.Show("Update failed! No changes were made.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Database error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                if (IsMaximize)
                {
                    this.NavigationService.Content = null; // أو إعادة تعيين الحجم الافتراضي
                    IsMaximize = false;
                }
                else
                {
                    // للتكبير، يمكنك تعديل حجم الصفحة حسب الحاجة
                    IsMaximize = true;
                }
            }
        }

        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                // استخدام DragMove ليس متاحاً للصفحات، فهذه الخاصية للنوافذ
            }
        }

        private void ChangeProfilePicture_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = "Choose a Profile Picture",
                Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp"
            };
            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    string imagePath = openFileDialog.FileName;
                    ImageBrush brush = new ImageBrush();
                    brush.ImageSource = new BitmapImage(new Uri(imagePath, UriKind.Absolute));
                    ProfileAvatar.Fill = brush;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading image: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BackToDashboard_Click(object sender, RoutedEventArgs e)
        {
            if (NavigationService != null && NavigationService.CanGoBack)
            {
                NavigationService.GoBack();
            }
        }
    }
}
