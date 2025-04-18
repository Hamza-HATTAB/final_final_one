using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using MySql.Data.MySqlClient;
using UserModels;
using DataGridNamespace;

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
                string connectionString = AppConfig.CloudSqlConnectionString;
                string query = "SELECT id, nom, email, role FROM users ORDER BY id";

                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                int userId = reader.GetInt32("id");
                                string userName = reader.GetString("nom");
                                string email = reader.GetString("email");
                                string roleStr = reader.GetString("role");

                                // Convert string role to enum
                                RoleUtilisateur role = ConvertStringToRole(roleStr);

                                var user = new User
                                {
                                    Id = userId,
                                    Nom = userName,
                                    Email = email,
                                    Role = role
                                };
                                allMembers.Add(user);
                            }
                        }
                    }
                }

                membersViewSource = new CollectionViewSource { Source = allMembers };
                membersViewSource.Filter += MembersViewSource_Filter;
                MembersDataGrid.ItemsSource = membersViewSource.View;

                //Counter.Text = $"Total: {allMembers.Count} members";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading members: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (membersViewSource != null)
            {
                membersViewSource.View.Refresh();
            }
        }

        private void MembersViewSource_Filter(object sender, FilterEventArgs e)
        {
            if (e.Item is User user && !string.IsNullOrEmpty(SearchTextBox.Text))
            {
                string searchText = SearchTextBox.Text.ToLower();
                bool nameMatches = !string.IsNullOrEmpty(user.Nom) && user.Nom.ToLower().Contains(searchText);
                bool emailMatches = !string.IsNullOrEmpty(user.Email) && user.Email.ToLower().Contains(searchText);
                bool roleMatches = user.Role.ToString().ToLower().Contains(searchText);

                e.Accepted = nameMatches || emailMatches || roleMatches;
            }
            else
            {
                e.Accepted = true;
            }
        }

        private RoleUtilisateur ConvertStringToRole(string roleString)
        {
            if (string.Equals(roleString, "admin", StringComparison.OrdinalIgnoreCase))
            {
                return RoleUtilisateur.Admin;
            }
            else if (string.Equals(roleString, "etudiant", StringComparison.OrdinalIgnoreCase))
            {
                return RoleUtilisateur.Etudiant;
            }
            else
            {
                return RoleUtilisateur.SimpleUser;
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is User user)
            {
                EditMember editWindow = new EditMember(user);
                if (editWindow.ShowDialog() == true)
                {
                    // Refresh the list after editing
                    LoadMembers();
                }
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is User user)
            {
                if (user.Role == RoleUtilisateur.Admin)
                {
                    MessageBox.Show("Cannot delete an administrator account.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Ask for confirmation
                var result = MessageBox.Show($"Are you sure you want to delete the user: {user.Nom}?\nThis action cannot be undone.", 
                                          "Delete Confirmation", 
                                          MessageBoxButton.YesNo, 
                                          MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        // Delete from database
                        string connectionString = AppConfig.CloudSqlConnectionString;
                        string query = "DELETE FROM users WHERE id = @userId";

                        using (MySqlConnection conn = new MySqlConnection(connectionString))
                        {
                            conn.Open();
                            using (MySqlCommand cmd = new MySqlCommand(query, conn))
                            {
                                cmd.Parameters.AddWithValue("@userId", user.Id);
                                int rowsAffected = cmd.ExecuteNonQuery();

                                if (rowsAffected > 0)
                                {
                                    // Remove from the list
                                    allMembers.Remove(user);
                                    membersViewSource.View.Refresh();
                                    MessageBox.Show("User deleted successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                                }
                                else
                                {
                                    MessageBox.Show("Failed to delete user. User not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error deleting user: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }
    }
}