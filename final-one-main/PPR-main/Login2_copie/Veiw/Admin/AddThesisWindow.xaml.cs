using System;
using System.Windows;
using System.Windows.Controls;
using System.Diagnostics;
using MySql.Data.MySqlClient;
using System.IO;
using ThesesModels;

namespace DataGridNamespace.Admin
{
    public partial class AddThesisWindow : Window
    {
        public AddThesisWindow()
        {
            InitializeComponent();
            // Set default date to current date
            YearDatePicker.SelectedDate = DateTime.Now;
            
            // Set default type to first item
            TypeComboBox.SelectedIndex = 0;
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "PDF Files (*.pdf)|*.pdf",
                Title = "Select Thesis PDF File"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                FilePathTextBox.Text = openFileDialog.FileName;
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validate all fields
                if (string.IsNullOrWhiteSpace(TitleTextBox.Text))
                {
                    MessageBox.Show("Please enter a title.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    TitleTextBox.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(AuthorTextBox.Text))
                {
                    MessageBox.Show("Please enter an author.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    AuthorTextBox.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(SpecialtyTextBox.Text))
                {
                    MessageBox.Show("Please enter a specialty.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    SpecialtyTextBox.Focus();
                    return;
                }

                if (TypeComboBox.SelectedItem == null)
                {
                    MessageBox.Show("Please select a type.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    TypeComboBox.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(KeywordsTextBox.Text))
                {
                    MessageBox.Show("Please enter keywords.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    KeywordsTextBox.Focus();
                    return;
                }

                if (YearDatePicker.SelectedDate == null)
                {
                    MessageBox.Show("Please select a date.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    YearDatePicker.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(AbstractTextBox.Text))
                {
                    MessageBox.Show("Please enter an abstract.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    AbstractTextBox.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(FilePathTextBox.Text))
                {
                    MessageBox.Show("Please select a PDF file.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    BrowseButton.Focus();
                    return;
                }

                // Get current user ID from session
                int currentUserId = DataGridNamespace.Session.CurrentUserId;

                // Process file path - store only relative path in DB
                string fileName = Path.GetFileName(FilePathTextBox.Text);
                string relativePath = "pdfs/" + fileName;

                // Create target directory if it doesn't exist
                string targetDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "pdfs");
                if (!Directory.Exists(targetDirectory))
                {
                    Directory.CreateDirectory(targetDirectory);
                }

                // Copy file to target directory if it doesn't already exist there
                string targetPath = Path.Combine(targetDirectory, fileName);
                if (FilePathTextBox.Text != targetPath && !File.Exists(targetPath))
                {
                    File.Copy(FilePathTextBox.Text, targetPath);
                }

                // Get type from ComboBox
                string typeStr = ((ComboBoxItem)TypeComboBox.SelectedItem).Content.ToString();
                TypeThese type = (TypeThese)Enum.Parse(typeof(TypeThese), typeStr);

                // Save to database
                string connectionString = DataGrid.Models.DatabaseConnection.GetConnectionString();
                string query = @"INSERT INTO theses (titre, auteur, speciality, Type, mots_cles, annee, Resume, fichier, user_id) 
                               VALUES (@titre, @auteur, @specialite, @type, @motsCles, @annee, @resume, @fichier, @userId)";

                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@titre", TitleTextBox.Text);
                        cmd.Parameters.AddWithValue("@auteur", AuthorTextBox.Text);
                        cmd.Parameters.AddWithValue("@specialite", SpecialtyTextBox.Text);
                        cmd.Parameters.AddWithValue("@type", type.ToString());
                        cmd.Parameters.AddWithValue("@motsCles", KeywordsTextBox.Text);
                        cmd.Parameters.AddWithValue("@annee", YearDatePicker.SelectedDate.Value);
                        cmd.Parameters.AddWithValue("@resume", AbstractTextBox.Text);
                        cmd.Parameters.AddWithValue("@fichier", relativePath);
                        cmd.Parameters.AddWithValue("@userId", currentUserId);

                        cmd.ExecuteNonQuery();
                    }
                }

                // Close the window with success result
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error adding thesis: {ex.Message}");
                MessageBox.Show($"Error adding thesis: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
} 