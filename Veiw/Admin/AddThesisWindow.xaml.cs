using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Diagnostics;
using MySql.Data.MySqlClient;
using System.IO;
using ThesesModels;
using DataGridNamespace.Services;
using System.Threading.Tasks;

namespace DataGridNamespace.Admin
{
    public partial class AddThesisWindow : Window
    {
        private readonly CloudStorageService _cloudStorageService;
        
        public AddThesisWindow()
        {
            InitializeComponent();
            
            // Set default date to current date
            YearDatePicker.SelectedDate = DateTime.Now;
            
            // Set default type to first item
            TypeComboBox.SelectedIndex = 0;
            
            // Initialize cloud storage service
            _cloudStorageService = new CloudStorageService();
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

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
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

                // Show "Please wait" message
                SaveButton.IsEnabled = false;
                CancelButton.IsEnabled = false;
                
                this.Cursor = Cursors.Wait;
                
                // Upload file to Cloud Storage
                string fileName = Path.GetFileName(FilePathTextBox.Text);
                string objectName = $"theses/{Guid.NewGuid()}/{fileName}";
                
                string uploadedObjectName;
                try
                {
                    uploadedObjectName = await _cloudStorageService.UploadFileViaSignedUrl(FilePathTextBox.Text, objectName);
                    
                    if (uploadedObjectName == null)
                    {
                        MessageBox.Show("Failed to upload file to cloud storage.", "Upload Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        
                        SaveButton.IsEnabled = true;
                        CancelButton.IsEnabled = true;
                        this.Cursor = Cursors.Arrow;
                        
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error uploading file: {ex.Message}");
                    MessageBox.Show($"Error uploading file: {ex.Message}", "Upload Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    
                    SaveButton.IsEnabled = true;
                    CancelButton.IsEnabled = true;
                    this.Cursor = Cursors.Arrow;
                    
                    return;
                }

                // Get type from ComboBox
                string typeStr = ((ComboBoxItem)TypeComboBox.SelectedItem).Content.ToString();
                TypeThese type = (TypeThese)Enum.Parse(typeof(TypeThese), typeStr);

                // Save to database
                string query = @"INSERT INTO theses (titre, auteur, speciality, Type, mots_cles, annee, Resume, fichier, user_id) 
                               VALUES (@titre, @auteur, @speciality, @type, @motsCles, @annee, @resume, @fichier, @userId)";

                using (MySqlConnection conn = new MySqlConnection(AppConfig.CloudSqlConnectionString))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@titre", TitleTextBox.Text);
                        cmd.Parameters.AddWithValue("@auteur", AuthorTextBox.Text);
                        cmd.Parameters.AddWithValue("@speciality", SpecialtyTextBox.Text);
                        cmd.Parameters.AddWithValue("@type", type.ToString());
                        cmd.Parameters.AddWithValue("@motsCles", KeywordsTextBox.Text);
                        cmd.Parameters.AddWithValue("@annee", YearDatePicker.SelectedDate.Value);
                        cmd.Parameters.AddWithValue("@resume", AbstractTextBox.Text);
                        cmd.Parameters.AddWithValue("@fichier", uploadedObjectName);
                        cmd.Parameters.AddWithValue("@userId", currentUserId);

                        cmd.ExecuteNonQuery();
                    }
                }

                MessageBox.Show("Thesis added successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                
                // Close the window with success result
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error adding thesis: {ex.Message}");
                MessageBox.Show($"Error adding thesis: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                
                SaveButton.IsEnabled = true;
                CancelButton.IsEnabled = true;
                this.Cursor = Cursors.Arrow;
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
} 