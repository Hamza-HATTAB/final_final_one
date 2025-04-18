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

        // Helper method to sign in user with Firebase
        private async Task<string> SignInUserFirebase(string email, string password)
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
                    // Parse the response to get the Firebase UID (localId)
                    var responseObj = JObject.Parse(responseContent);
                    string firebaseUid = responseObj["localId"].ToString();
                    Debug.WriteLine($"Firebase UID (localId): {firebaseUid}");
                    return firebaseUid;
                }
                else
                {
                    var errorObj = JObject.Parse(responseContent);
                    string errorMessage = errorObj["error"]["message"].ToString();
                    MessageBox.Show($"Firebase Authentication Error: {errorMessage}", "Login Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                // Authenticate with Firebase first
                string email = txtUser_L1.Text;
                string password = txtPassword_L1.Password;
                
                // Get Firebase UID from authentication
                string firebaseUid = await SignInUserFirebase(email, password);
                
                if (string.IsNullOrEmpty(firebaseUid))
                {
                    // Firebase authentication failed, already handled in SignInUserFirebase
                    return;
                }
                
                Debug.WriteLine($"Successfully authenticated with Firebase. UID: {firebaseUid}");
                
                // Now query the database using the Firebase UID
                string connectionString = AppConfig.CloudSqlConnectionString;
                Debug.WriteLine($"Attempting to connect using: {AppConfig.CloudSqlConnectionString}");

                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    await conn.OpenAsync();
                    Debug.WriteLine($"Connection successful. Executing query for FirebaseUID: {firebaseUid}");

                    string loginQuery = "SELECT id, nom, email, role FROM users WHERE firebase_uid = @FirebaseUid";
                    using (MySqlCommand cmd = new MySqlCommand(loginQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@FirebaseUid", firebaseUid);
                        Debug.WriteLine($"SQL Query attempted: {cmd.CommandText}");
                        Debug.WriteLine($"FirebaseUID Parameter: {firebaseUid}");

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            Debug.WriteLine("Query executed. Reading data...");
                            if (await reader.ReadAsync())
                            {
                                int userId = reader.GetInt32(0);
                                string userName = reader.GetString(1);
                                string userEmail = reader.GetString(2);
                                string roleStr = reader.IsDBNull(3) ? "" : reader.GetString(3);

                                Debug.WriteLine($"Login successful: User ID={userId}, Name={userName}, Role={roleStr}");

                                RoleUtilisateur role;
                                if (Enum.TryParse(roleStr, true, out role))
                                {
                                    // Initialize session
                                    DataGridNamespace.Session.Initialize(userId, userName, role);
                                    
                                    MessageBox.Show("Welcome! Login successful.");
                                    // Open MainWindow instead of DashboardView
                                    var user = new User
                                    {
                                        Id = userId,
                                        Nom = userName,
                                        Email = userEmail,
                                        Password = "FIREBASE_AUTH", // Don't store actual password
                                        Role = role,
                                        FirebaseUid = firebaseUid
                                    };
                                    MainWindow mainWindow = new MainWindow(user);
                                    mainWindow.Show();
                                    this.Close();
                                }
                                else
                                {
                                    MessageBox.Show($"Invalid role in database: '{roleStr}'", "Login Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                }
                            }
                            else
                            {
                                MessageBox.Show("Login successful via Firebase, but user profile not found in application database. Please contact support.", "Login Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                            }
                        }
                    }
                }
            }
            catch (MySqlException myEx)
            {
                Debug.WriteLine($"!!! MySQL Exception during Login DB Query: Code={myEx.Number}, Message={myEx.Message}");
                Debug.WriteLine($"SQL Query attempted: SELECT id, nom, email, role FROM users WHERE firebase_uid = @FirebaseUid");
                MessageBox.Show($"Database Login Error ({myEx.Number}): {myEx.Message}. Check debug output.", "DB Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"!!! General Exception during Login DB Query: {ex.GetType().Name}, Message={ex.Message}");
                MessageBox.Show($"General Login Error: {ex.Message}. Check debug output.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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

        // Helper method to sign up user with Firebase
        private async Task<string> SignUpUserFirebase(string email, string password)
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
                    // Parse the response to get the Firebase UID (localId)
                    var responseObj = JObject.Parse(responseContent);
                    string firebaseUid = responseObj["localId"].ToString();
                    Debug.WriteLine($"Firebase UID (localId): {firebaseUid}");
                    return firebaseUid;
                }
                else
                {
                    var errorObj = JObject.Parse(responseContent);
                    string errorMessage = errorObj["error"]["message"].ToString();
                    MessageBox.Show($"Firebase Registration Error: {errorMessage}", "Registration Error", MessageBoxButton.OK, MessageBoxImage.Error);
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

        private async void SignUp_L2_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(txtUser_L2.Text) || string.IsNullOrEmpty(txtEmail_L2.Text) || string.IsNullOrEmpty(txtPassword_L2.Password))
            {
                MessageBox.Show("Please fill Username, Email & Password.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!IsValidEmail(txtEmail_L2.Text))
            {
                MessageBox.Show("Invalid Email!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var selectedItem = roleCombo_L2.SelectedItem as ComboBoxItem;
            string roleValue = (selectedItem != null) ? selectedItem.Content.ToString() : "SimpleUser";

            try
            {
                // First register with Firebase
                string email = txtEmail_L2.Text;
                string password = txtPassword_L2.Password;
                
                // Get Firebase UID from registration
                string firebaseUid = await SignUpUserFirebase(email, password);
                
                if (string.IsNullOrEmpty(firebaseUid))
                {
                    // Firebase registration failed, already handled in SignUpUserFirebase
                    return;
                }
                
                Debug.WriteLine($"Successfully registered with Firebase. UID: {firebaseUid}");
                
                // Now insert the user into our database with the Firebase UID
                string connectionString = AppConfig.CloudSqlConnectionString;
                
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    await conn.OpenAsync();
                    
                    // Check if email already exists in our database
                    string checkEmailQuery = "SELECT COUNT(*) FROM users WHERE email = @Email";
                    using (MySqlCommand checkCmd = new MySqlCommand(checkEmailQuery, conn))
                    {
                        checkCmd.Parameters.AddWithValue("@Email", email);
                        int emailCount = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());
                        
                        if (emailCount > 0)
                        {
                            MessageBox.Show("Email already registered in our system.", "Registration Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                    }
                    
                    // Insert new user with Firebase UID
                    string insertQuery = @"INSERT INTO users (nom, email, password, role, firebase_uid) 
                                         VALUES (@Nom, @Email, @Password, @Role, @FirebaseUid)";
                    
                    using (MySqlCommand cmd = new MySqlCommand(insertQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@Nom", txtUser_L2.Text);
                        cmd.Parameters.AddWithValue("@Email", email);
                        cmd.Parameters.AddWithValue("@Password", "FIREBASE_AUTH"); // Don't store actual password
                        cmd.Parameters.AddWithValue("@Role", roleValue);
                        cmd.Parameters.AddWithValue("@FirebaseUid", firebaseUid);
                        
                        int result = await cmd.ExecuteNonQueryAsync();
                        
                        if (result > 0)
                        {
                            MessageBox.Show("Registration successful! You can now login.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                            // Switch to login layout
                            GoToLayout1_Click(sender, e);
                        }
                        else
                        {
                            MessageBox.Show("Registration failed. Please try again.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
            catch (MySqlException myEx)
            {
                Debug.WriteLine($"MySQL Exception during Registration: {myEx.Message}");
                MessageBox.Show($"Database Error: {myEx.Message}", "Registration Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Exception during Registration: {ex.Message}");
                MessageBox.Show($"Error: {ex.Message}", "Registration Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool IsValidEmail(string email)
        {
            string pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
            return Regex.IsMatch(email, pattern);
        }
    }
}
