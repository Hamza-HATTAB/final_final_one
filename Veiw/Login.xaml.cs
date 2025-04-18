using DataGrid;
using DataGridNamespace;
using MySql.Data.MySqlClient;
using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using UserModels;
using ThesesModels;
using FavorisModels;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MyProject
{
    public partial class Login : Window
    {
        private readonly HttpClient httpClient = new HttpClient();

        public Login()
        {
            InitializeComponent();
        }

        private void CloseImage_MouseUp(object sender, MouseButtonEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }

        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                MaximizeRestoreButton_Click(sender, e);
            }
        }

        private void MaximizeRestoreButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
            {
                this.WindowState = WindowState.Normal;
                this.Width = 1280;
                this.Height = 720;
            }
            else
            {
                this.WindowState = WindowState.Maximized;
            }
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Are you sure you want to exit?", 
                                       "Exit Confirmation", 
                                       MessageBoxButton.YesNo, 
                                       MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                Application.Current.Shutdown();
            }
        }

        private void GoToLayout2_Click(object sender, RoutedEventArgs e)
        {
            Layout1.Visibility = Visibility.Collapsed;
            Layout2.Visibility = Visibility.Visible;
        }

        private void GoToLayout1_Click(object sender, RoutedEventArgs e)
        {
            Layout2.Visibility = Visibility.Collapsed;
            Layout1.Visibility = Visibility.Visible;
        }

        // Layout1 Events
        private void textUser_L1_MouseDown(object sender, MouseButtonEventArgs e)
        {
            txtUser_L1.Focus();
        }

        private void txtUser_L1_TextChanged(object sender, TextChangedEventArgs e)
        {
            textUser_L1.Visibility = string.IsNullOrEmpty(txtUser_L1.Text)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private void textPassword_L1_MouseDown(object sender, MouseButtonEventArgs e)
        {
            txtPassword_L1.Focus();
        }

        private void txtPassword_L1_PasswordChanged(object sender, RoutedEventArgs e)
        {
            textPassword_L1.Visibility = string.IsNullOrEmpty(txtPassword_L1.Password)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        // Sign In button in Layout1
        private async void SignIn_L1_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(txtUser_L1.Text) || string.IsNullOrEmpty(txtPassword_L1.Password))
            {
                MessageBox.Show("Please fill Username & Password.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // First test database connection
            bool isConnected = DataGrid.Models.DatabaseConnection.TestConnection();
            if (!isConnected)
            {
                MessageBox.Show("Cannot connect to database. Please check your database settings.", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                Cursor = Cursors.Wait;
                
                // Authenticate with Firebase first
                string email = txtUser_L1.Text;
                string password = txtPassword_L1.Password;
                
                // Call Firebase Auth API
                var firebaseResponse = await SignInWithFirebase(email, password);
                
                if (firebaseResponse == null)
                {
                    // Firebase authentication failed, already handled in SignInWithFirebase
                    Cursor = Cursors.Arrow;
                    return;
                }
                
                string firebaseUid = firebaseResponse.LocalId;
                string idToken = firebaseResponse.IdToken;
                
                Debug.WriteLine($"Successfully authenticated with Firebase. UID: {firebaseUid}");
                
                // Now find this user in our database by firebase_uid
                string query = "SELECT id, nom, role, email FROM users WHERE firebase_uid = @FirebaseUid";
                
                User foundUser = null;
                
                using (var conn = new MySqlConnection(AppConfig.CloudSqlConnectionString))
                {
                    conn.Open();
                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@FirebaseUid", firebaseUid);
                        
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                int userId = reader.GetInt32("id");
                                string username = reader.GetString("nom");
                                string role = reader.GetString("role");
                                string userEmail = reader.GetString("email");
                                
                                // Map database role to enum
                                RoleUtilisateur userRole = RoleUtilisateur.SimpleUser; // Default
                                
                                if (string.Equals(role, "admin", StringComparison.OrdinalIgnoreCase))
                                {
                                    userRole = RoleUtilisateur.Admin;
                                }
                                else if (string.Equals(role, "etudiant", StringComparison.OrdinalIgnoreCase))
                                {
                                    userRole = RoleUtilisateur.Etudiant;
                                }
                                
                                // Create user object
                                foundUser = new User
                                {
                                    Id = userId,
                                    Nom = username,
                                    Email = userEmail,
                                    Role = userRole,
                                    FirebaseUid = firebaseUid
                                };
                            }
                        }
                    }
                }
                
                if (foundUser == null)
                {
                    MessageBox.Show("User authenticated but not found in the database.", "Login Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    Cursor = Cursors.Arrow;
                    return;
                }
                
                // Initialize the session with Firebase UID and token
                Session.Initialize(
                    foundUser.Id,
                    foundUser.Nom,
                    foundUser.Role,
                    foundUser.FirebaseUid,
                    idToken
                );
                
                Debug.WriteLine($"User successfully logged in: {foundUser.Nom} (ID: {foundUser.Id}, Role: {foundUser.Role})");
                
                // Open main window
                MainWindow mainWindow = new MainWindow();
                mainWindow.Show();
                
                // Close the login window
                this.Close();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception during login: {ex.Message}");
                MessageBox.Show($"Error during login: {ex.Message}", "Login Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Cursor = Cursors.Arrow;
            }
        }

        // Layout2 Events (for registration)
        private void textUser_L2_MouseDown(object sender, MouseButtonEventArgs e)
        {
            txtUser_L2.Focus();
        }

        private void txtUser_L2_TextChanged(object sender, TextChangedEventArgs e)
        {
            textUser_L2.Visibility = string.IsNullOrEmpty(txtUser_L2.Text)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private void textEmail_L2_MouseDown(object sender, MouseButtonEventArgs e)
        {
            txtEmail_L2.Focus();
        }

        private void txtEmail_L2_TextChanged(object sender, TextChangedEventArgs e)
        {
            textEmail_L2.Visibility = string.IsNullOrEmpty(txtEmail_L2.Text)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private void textPassword_L2_MouseDown(object sender, MouseButtonEventArgs e)
        {
            txtPassword_L2.Focus();
        }

        private void txtPassword_L2_PasswordChanged(object sender, RoutedEventArgs e)
        {
            textPassword_L2.Visibility = string.IsNullOrEmpty(txtPassword_L2.Password)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        // Helper method for Firebase sign in
        private async Task<FirebaseAuthResponse> SignInWithFirebase(string email, string password)
        {
            try
            {
                string signInEndpoint = $"{AppConfig.FirebaseAuthBaseUrl}:signInWithPassword?key={AppConfig.FirebaseApiKey}";
                
                var requestData = new
                {
                    email,
                    password,
                    returnSecureToken = true
                };
                
                var jsonContent = JsonConvert.SerializeObject(requestData);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                
                var response = await httpClient.PostAsync(signInEndpoint, content);
                var responseContent = await response.Content.ReadAsStringAsync();
                
                Debug.WriteLine($"Firebase Sign-In Response: {responseContent}");
                
                if (response.IsSuccessStatusCode)
                {
                    return JsonConvert.DeserializeObject<FirebaseAuthResponse>(responseContent);
                }
                else
                {
                    var errorObj = JObject.Parse(responseContent);
                    string errorMessage = errorObj["error"]["message"].ToString();
                    string userFriendlyMessage = GetUserFriendlyFirebaseError(errorMessage);
                    
                    MessageBox.Show(userFriendlyMessage, "Login Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return null;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception during Firebase sign-in: {ex.Message}");
                MessageBox.Show($"Error during authentication: {ex.Message}", "Login Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }

        // Helper method for Firebase sign up
        private async Task<FirebaseAuthResponse> SignUpWithFirebase(string email, string password)
        {
            try
            {
                string signUpEndpoint = $"{AppConfig.FirebaseAuthBaseUrl}:signUp?key={AppConfig.FirebaseApiKey}";
                
                var requestData = new
                {
                    email,
                    password,
                    returnSecureToken = true
                };
                
                var jsonContent = JsonConvert.SerializeObject(requestData);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                
                var response = await httpClient.PostAsync(signUpEndpoint, content);
                var responseContent = await response.Content.ReadAsStringAsync();
                
                Debug.WriteLine($"Firebase Sign-Up Response: {responseContent}");
                
                if (response.IsSuccessStatusCode)
                {
                    return JsonConvert.DeserializeObject<FirebaseAuthResponse>(responseContent);
                }
                else
                {
                    var errorObj = JObject.Parse(responseContent);
                    string errorMessage = errorObj["error"]["message"].ToString();
                    string userFriendlyMessage = GetUserFriendlyFirebaseError(errorMessage);
                    
                    MessageBox.Show(userFriendlyMessage, "Registration Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return null;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception during Firebase sign-up: {ex.Message}");
                MessageBox.Show($"Error during registration: {ex.Message}", "Registration Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }

        // Sign Up button in Layout2
        private async void SignUp_L2_Click(object sender, RoutedEventArgs e)
        {
            // First validate form fields
            if (string.IsNullOrEmpty(txtUser_L2.Text))
            {
                MessageBox.Show("Please enter a username.", "Registration Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            if (string.IsNullOrEmpty(txtEmail_L2.Text) || !IsValidEmail(txtEmail_L2.Text))
            {
                MessageBox.Show("Please enter a valid email address.", "Registration Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            if (string.IsNullOrEmpty(txtPassword_L2.Password) || txtPassword_L2.Password.Length < 6)
            {
                MessageBox.Show("Password must be at least 6 characters long.", "Registration Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            // First test database connection
            bool isConnected = DataGrid.Models.DatabaseConnection.TestConnection();
            if (!isConnected)
            {
                MessageBox.Show("Cannot connect to database. Please check your database settings.", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            
            try
            {
                Cursor = Cursors.Wait;
                
                string username = txtUser_L2.Text;
                string email = txtEmail_L2.Text;
                string password = txtPassword_L2.Password;
                
                // Check if email already exists in our database
                string checkQuery = "SELECT COUNT(*) FROM users WHERE email = @Email";
                
                using (var conn = new MySqlConnection(AppConfig.CloudSqlConnectionString))
                {
                    conn.Open();
                    using (var cmd = new MySqlCommand(checkQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@Email", email);
                        
                        long count = (long)cmd.ExecuteScalar();
                        if (count > 0)
                        {
                            MessageBox.Show("This email address is already registered.", "Registration Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                            Cursor = Cursors.Arrow;
                            return;
                        }
                    }
                }
                
                // Register with Firebase
                var firebaseResponse = await SignUpWithFirebase(email, password);
                
                if (firebaseResponse == null)
                {
                    // Firebase registration failed, already handled in SignUpWithFirebase
                    Cursor = Cursors.Arrow;
                    return;
                }
                
                string firebaseUid = firebaseResponse.LocalId;
                string idToken = firebaseResponse.IdToken;
                
                // Now create the user in our database
                string insertQuery = @"
                    INSERT INTO users (nom, email, password, role, firebase_uid)
                    VALUES (@Username, @Email, 'FIREBASE_AUTH', @Role, @FirebaseUid)";
                
                using (var conn = new MySqlConnection(AppConfig.CloudSqlConnectionString))
                {
                    conn.Open();
                    using (var cmd = new MySqlCommand(insertQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@Username", username);
                        cmd.Parameters.AddWithValue("@Email", email);
                        cmd.Parameters.AddWithValue("@Role", "SimpleUser"); // Default role for new users
                        cmd.Parameters.AddWithValue("@FirebaseUid", firebaseUid);
                        
                        int rowsAffected = cmd.ExecuteNonQuery();
                        
                        if (rowsAffected <= 0)
                        {
                            MessageBox.Show("Failed to create user in database.", "Registration Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            Cursor = Cursors.Arrow;
                            return;
                        }
                        
                        // Get the new user ID
                        cmd.CommandText = "SELECT LAST_INSERT_ID()";
                        int userId = Convert.ToInt32(cmd.ExecuteScalar());
                        
                        // Initialize session
                        Session.Initialize(
                            userId,
                            username,
                            RoleUtilisateur.SimpleUser,
                            firebaseUid,
                            idToken
                        );
                        
                        MessageBox.Show("Registration successful!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                        
                        // Open main window
                        MainWindow mainWindow = new MainWindow();
                        mainWindow.Show();
                        
                        // Close the login window
                        this.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception during registration: {ex.Message}");
                MessageBox.Show($"Error during registration: {ex.Message}", "Registration Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Cursor = Cursors.Arrow;
            }
        }

        private bool IsValidEmail(string email)
        {
            string pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
            return Regex.IsMatch(email, pattern);
        }

        // Helper class for Firebase responses
        private class FirebaseAuthResponse
        {
            [JsonProperty("idToken")]
            public string IdToken { get; set; }
            
            [JsonProperty("email")]
            public string Email { get; set; }
            
            [JsonProperty("refreshToken")]
            public string RefreshToken { get; set; }
            
            [JsonProperty("expiresIn")]
            public string ExpiresIn { get; set; }
            
            [JsonProperty("localId")]
            public string LocalId { get; set; }
        }

        // Helper method to get user-friendly error messages
        private string GetUserFriendlyFirebaseError(string errorCode)
        {
            switch (errorCode)
            {
                case "EMAIL_EXISTS":
                    return "This email address is already in use by another account.";
                case "OPERATION_NOT_ALLOWED":
                    return "Password sign-in is disabled for this project.";
                case "TOO_MANY_ATTEMPTS_TRY_LATER":
                    return "Too many unsuccessful login attempts. Please try again later.";
                case "EMAIL_NOT_FOUND":
                    return "There is no user account with this email address.";
                case "INVALID_PASSWORD":
                    return "The password is invalid.";
                case "USER_DISABLED":
                    return "This user account has been disabled by an administrator.";
                default:
                    return $"Authentication error: {errorCode}";
            }
        }
    }
}
