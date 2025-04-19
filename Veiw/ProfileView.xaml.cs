using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using MySql.Data.MySqlClient;
using System.Diagnostics;
using DataGridNamespace.Services;
using UserModels;
using DataGridNamespace;
using System.Threading.Tasks;

namespace DataGrid
{
    public partial class ProfileView : Page
    {
        private User currentUser;
        private bool isEditMode = false;
        private readonly CloudStorageService _cloudStorageService = new CloudStorageService();
        private string originalProfilePicRef;
        private string newProfilePicPath;
        private Button editProfileButton; // Reference to the edit button

        public ProfileView()
        {
            InitializeComponent();
            LoadUserData();
        }

        private async void LoadUserData()
        {
            try
            {
                // Get current user from session
                int userId = Session.CurrentUserId;
                
                string connectionString = AppConfig.CloudSqlConnectionString;
                string query = "SELECT id, nom, email, role, firebase_uid, profile_pic_ref FROM users WHERE id = @userId";

                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@userId", userId);
                        
                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                // Parse role string from database to enum
                                string roleStr = reader.GetString("role");
                                RoleUtilisateur userRole = RoleUtilisateur.SimpleUser; // Default
                                
                                if (string.Equals(roleStr, "admin", StringComparison.OrdinalIgnoreCase))
                                {
                                    userRole = RoleUtilisateur.Admin;
                                }
                                else if (string.Equals(roleStr, "etudiant", StringComparison.OrdinalIgnoreCase))
                                {
                                    userRole = RoleUtilisateur.Etudiant;
                                }
                                
                                currentUser = new User
                                {
                                    Id = reader.GetInt32("id"),
                                    Nom = reader.GetString("nom"),
                                    Email = reader.GetString("email"),
                                    Role = userRole
                                };

                                // Get Firebase UID if available
                                if (!reader.IsDBNull(reader.GetOrdinal("firebase_uid")))
                                {
                                    currentUser.FirebaseUid = reader.GetString("firebase_uid");
                                }

                                // Get profile picture reference if exists
                                if (!reader.IsDBNull(reader.GetOrdinal("profile_pic_ref")))
                                {
                                    originalProfilePicRef = reader.GetString("profile_pic_ref");
                                    await LoadProfilePicture(originalProfilePicRef);
                                }
                            }
                        }
                    }
                }

                // Populate UI fields
                if (currentUser != null)
                {
                    UsernameTextBox.Text = currentUser.Nom;
                    EmailTextBox.Text = currentUser.Email;
                    RoleTextBox.Text = currentUser.Role.ToString();
                }
                
                // Find the edit profile button after initialization
                editProfileButton = FindName("EditProfileButton") as Button;
                if (editProfileButton == null)
                {
                    Debug.WriteLine("Warning: EditProfileButton not found in XAML");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading user data: {ex.Message}");
                MessageBox.Show($"Error loading user data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadProfilePicture(string profilePicRef)
        {
            try
            {
                if (string.IsNullOrEmpty(profilePicRef))
                {
                    Debug.WriteLine("Profile picture reference is empty, using default avatar");
                    return;
                }

                Debug.WriteLine($"Attempting to load profile picture with object name: {profilePicRef}");
                
                // Get a signed URL for the profile picture from Cloud Storage
                string signedUrl = await _cloudStorageService.GetSignedReadUrl(profilePicRef);
                
                if (string.IsNullOrEmpty(signedUrl))
                {
                    Debug.WriteLine("Failed to get signed URL for profile picture");
                    return;
                }
                
                Debug.WriteLine($"Successfully generated signed URL for profile picture: {signedUrl}");
                
                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(signedUrl);
                bitmap.CacheOption = BitmapCacheOption.OnLoad; // Load the image right away so the URL doesn't expire
                bitmap.EndInit();

                // Set the image as the profile picture
                var brush = new System.Windows.Media.ImageBrush(bitmap);
                ProfileAvatar.Fill = brush;
                Debug.WriteLine("Successfully displayed profile picture");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading profile picture: {ex.Message}\nStack trace: {ex.StackTrace}");
                // Don't show message box for profile picture loading errors
            }
        }

        private void ToggleEditMode()
        {
            isEditMode = !isEditMode;
            
            // Toggle UI elements
            UsernameTextBox.IsReadOnly = !isEditMode;
            EmailTextBox.IsReadOnly = !isEditMode;
            PasswordContainer.Visibility = isEditMode ? Visibility.Visible : Visibility.Collapsed;
            
            // Update button text
            if (editProfileButton != null)
            {
                editProfileButton.Content = isEditMode ? "Save Changes" : "Edit Profile";
            }
        }

        private async void SaveProfile()
        {
            try
            {
                // Validate inputs
                if (string.IsNullOrWhiteSpace(UsernameTextBox.Text))
                {
                    MessageBox.Show("Username cannot be empty.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(EmailTextBox.Text) || !EmailTextBox.Text.Contains("@"))
                {
                    MessageBox.Show("Please enter a valid email address.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Upload new profile picture if selected
                string profilePicRef = originalProfilePicRef;
                if (!string.IsNullOrEmpty(newProfilePicPath))
                {
                    // Use Firebase UID for the profile picture name if available, otherwise use user ID
                    string userId = !string.IsNullOrEmpty(currentUser.FirebaseUid) ? 
                        currentUser.FirebaseUid : currentUser.Id.ToString();
                        
                    string uploadedObjectName = null;
                    
                    try
                    {
                        // Upload via CloudStorageService
                        uploadedObjectName = await _cloudStorageService.UploadProfilePictureAsync(newProfilePicPath, userId);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error uploading profile picture: {ex.Message}");
                        MessageBox.Show("Failed to upload profile picture. Please try again.", 
                            "Upload Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    
                    if (!string.IsNullOrEmpty(uploadedObjectName))
                    {
                        profilePicRef = uploadedObjectName;
                    }
                    else
                    {
                        MessageBox.Show("Failed to upload profile picture. Please try again.", 
                            "Upload Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }

                // Update user in database
                string connectionString = AppConfig.CloudSqlConnectionString;
                string updateQuery = "UPDATE users SET nom = @nom, email = @email";
                
                // Add profile picture update if changed
                if (profilePicRef != originalProfilePicRef)
                {
                    updateQuery += ", profile_pic_ref = @profilePicRef";
                }
                
                updateQuery += " WHERE id = @id";

                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand(updateQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@nom", UsernameTextBox.Text);
                        cmd.Parameters.AddWithValue("@email", EmailTextBox.Text);
                        cmd.Parameters.AddWithValue("@id", currentUser.Id);
                        
                        if (profilePicRef != originalProfilePicRef)
                        {
                            cmd.Parameters.AddWithValue("@profilePicRef", profilePicRef);
                        }
                        
                        int rowsAffected = cmd.ExecuteNonQuery();
                        
                        if (rowsAffected > 0)
                        {
                            MessageBox.Show("Profile updated successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                            
                            // Update the original profile pic reference
                            originalProfilePicRef = profilePicRef;
                            
                            // Clear the new profile pic path
                            newProfilePicPath = null;
                            
                            // Toggle edit mode back
                            ToggleEditMode();
                        }
                        else
                        {
                            MessageBox.Show("No changes were made to your profile.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving profile: {ex.Message}");
                MessageBox.Show($"Error saving profile: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EditProfileButton_Click(object sender, RoutedEventArgs e)
        {
            if (isEditMode)
            {
                // Save profile
                SaveProfile();
            }
            else
            {
                // Enter edit mode
                ToggleEditMode();
            }
        }

        private void ChangeProfilePicture_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenFileDialog openFileDialog = new OpenFileDialog
                {
                    Title = "Select Profile Picture",
                    Filter = "Image files (*.jpg, *.jpeg, *.png, *.gif)|*.jpg;*.jpeg;*.png;*.gif",
                    Multiselect = false
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    // Set the new profile pic path
                    newProfilePicPath = openFileDialog.FileName;
                    
                    // Preview the selected image
                    BitmapImage bitmap = new BitmapImage(new Uri(newProfilePicPath));
                    var brush = new System.Windows.Media.ImageBrush(bitmap);
                    ProfileAvatar.Fill = brush;
                    
                    // Automatically enter edit mode if not already
                    if (!isEditMode)
                    {
                        ToggleEditMode();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error selecting profile picture: {ex.Message}");
                MessageBox.Show($"Error selecting profile picture: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
