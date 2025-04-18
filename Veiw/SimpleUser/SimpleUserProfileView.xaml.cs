using System;
using System.Windows;
using System.Windows.Controls;

namespace DataGridNamespace.SimpleUser
{
    public partial class SimpleUserProfileView : UserControl
    {
        public SimpleUserProfileView()
        {
            InitializeComponent();
            LoadProfile();
        }

        private void LoadProfile()
        {
            try
            {
                // TODO: Load profile from database
                NameTextBlock.Text = "John Doe";
                EmailTextBlock.Text = "john.doe@example.com";
                RoleTextBlock.Text = "Simple User";
                JoinDateTextBlock.Text = DateTime.Now.ToString("MM/dd/yyyy");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading profile: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EditProfileButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Edit profile functionality will be implemented later.", "Edit Profile", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ChangePasswordButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Change password functionality will be implemented later.", "Change Password", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
} 