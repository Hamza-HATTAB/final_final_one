using System;
using System.Windows;
using System.Windows.Controls;
using System.Linq;
using MySql.Data.MySqlClient;
using UserModels;

namespace DataGridNamespace.Admin
{
    public partial class EditMember : Window
    {
        private readonly User _user;
        private readonly Action _refreshCallback;

        public EditMember(User user, Action refreshCallback = null)
        {
            InitializeComponent();
            _user = user;
            _refreshCallback = refreshCallback;

            // Populate fields with user data
            UserIdTextBox.Text = user.Id.ToString();
            NameTextBox.Text = user.Nom;
            EmailTextBox.Text = user.Email;
            
            // Set the role in the ComboBox
            var roleItem = RoleComboBox.Items.Cast<ComboBoxItem>()
                .FirstOrDefault(item => item.Content.ToString().ToLower() == user.Role.ToString().ToLower());
            if (roleItem != null)
            {
                RoleComboBox.SelectedItem = roleItem;
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validate input
                if (string.IsNullOrWhiteSpace(NameTextBox.Text))
                {
                    MessageBox.Show("Please enter a name.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(EmailTextBox.Text))
                {
                    MessageBox.Show("Please enter an email.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (RoleComboBox.SelectedItem == null)
                {
                    MessageBox.Show("Please select a role.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string connectionString = DataGrid.Models.DatabaseConnection.GetConnectionString();
                string query = "UPDATE users SET Nom = @name, Role = @role, Email = @email WHERE Id = @id";

                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@name", NameTextBox.Text);
                        cmd.Parameters.AddWithValue("@role", ((ComboBoxItem)RoleComboBox.SelectedItem).Content.ToString());
                        cmd.Parameters.AddWithValue("@email", EmailTextBox.Text);
                        cmd.Parameters.AddWithValue("@id", _user.Id);

                        cmd.ExecuteNonQuery();
                    }
                }

                // Update the user object with new values
                _user.Nom = NameTextBox.Text;
                _user.Email = EmailTextBox.Text;
                _user.Role = Enum.Parse<RoleUtilisateur>(((ComboBoxItem)RoleComboBox.SelectedItem).Content.ToString());

                // Refresh the members list if callback provided
                _refreshCallback?.Invoke();

                MessageBox.Show("Member updated successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error updating member: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}