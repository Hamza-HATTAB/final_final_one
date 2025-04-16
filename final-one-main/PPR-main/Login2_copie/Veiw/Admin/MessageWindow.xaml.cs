using System;
using System.Windows;
using System.Diagnostics;
using MySql.Data.MySqlClient;
using ThesesModels;

namespace DataGridNamespace.Admin
{
    public partial class MessageWindow : Window
    {
        private int thesisId;
        private string thesisTitle;

        public MessageWindow()
        {
            InitializeComponent();
        }
        
        public void SetThesis(int id, string title)
        {
            thesisId = id;
            thesisTitle = title;
            ThesisTitleTextBox.Text = title;
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validate message
                if (string.IsNullOrWhiteSpace(MessageTextBox.Text))
                {
                    MessageBox.Show("Please enter a message.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    MessageTextBox.Focus();
                    return;
                }

                // Get current user ID from session
                int currentUserId = DataGridNamespace.Session.CurrentUserId;

                // Insert the message into the contacts table
                string connectionString = DataGrid.Models.DatabaseConnection.GetConnectionString();
                string query = @"INSERT INTO contacts (user_id, these_id, message, date_envoi) 
                               VALUES (@userId, @theseId, @message, NOW())";

                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@userId", currentUserId);
                        cmd.Parameters.AddWithValue("@theseId", thesisId);
                        cmd.Parameters.AddWithValue("@message", MessageTextBox.Text);

                        int result = cmd.ExecuteNonQuery();
                        if (result > 0)
                        {
                            DialogResult = true;
                            MessageBox.Show("Message sent successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                            Close();
                        }
                        else
                        {
                            MessageBox.Show("Failed to send message. Please try again.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error sending message: {ex.Message}");
                MessageBox.Show($"Error sending message: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
} 