using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;
using DataGridNamespace.Admin;
using MyProject;
using DataGrid;
using System.Collections.ObjectModel;
using System.Linq;
using UserModels;
using ThesesModels;
using FavorisModels;

namespace DataGridNamespace
{
    public partial class MainWindow : Window
    {
        private User currentUser;
        private bool IsMaximize = false;

        public MainWindow()
        {
            InitializeComponent();
            
            // Get current user from Session
            int userId = Session.CurrentUserId;
            string userName = Session.CurrentUserName;
            RoleUtilisateur userRole = Session.CurrentUserRole;
            
            currentUser = new User
            {
                Id = userId,
                Nom = userName,
                Role = userRole
            };
            
            // Set window to maximize on startup
            this.WindowState = WindowState.Maximized;
            IsMaximize = true;
            
            LoadRoleSpecificContent();
        }

        public MainWindow(User user)
        {
            InitializeComponent();
            currentUser = user;
            
            // Set window to maximize on startup
            this.WindowState = WindowState.Maximized;
            IsMaximize = true;
            
            LoadRoleSpecificContent();
        }

        private void LoadRoleSpecificContent()
        {
            switch (currentUser.Role)
            {
                case RoleUtilisateur.Admin:
                    LoadAdminContent();
                    break;
                case RoleUtilisateur.SimpleUser:
                    LoadSimpleUserContent();
                    break;
                case RoleUtilisateur.Etudiant:
                    LoadEtudiantContent();
                    break;
                default:
                    LoadSimpleUserContent();
                    break;
            }
        }

        private void LoadAdminContent()
        {
            try
            {
                // Show admin-specific buttons
                MembersButton.Visibility = Visibility.Visible;
                DashboardButton.Visibility = Visibility.Visible;
                ThesisButton.Visibility = Visibility.Visible;
                ProfileButton.Visibility = Visibility.Visible;
                FavoritesButton.Visibility = Visibility.Visible;

                // Set initial view to dashboard
                DashboardButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7B5CD6"));
                ThesisButton.Background = Brushes.Transparent;
                MembersButton.Background = Brushes.Transparent;
                ProfileButton.Background = Brushes.Transparent;
                FavoritesButton.Background = Brushes.Transparent;
                
                // Load dashboard view by default
                var dashboardView = new DashboardView();
                MainFrame.Navigate(dashboardView);

                // Set up admin-specific event handlers
                DashboardButton.Click += DashboardButton_Click;
                ThesisButton.Click += ThesisButton_Click;
                MembersButton.Click += MembersButton_Click;
                ProfileButton.Click += ProfileButton_Click;
                FavoritesButton.Click += FavoritesButton_Click;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading admin content: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadSimpleUserContent()
        {
            try
            {
                // Hide all buttons first
                DashboardButton.Visibility = Visibility.Collapsed;
                MembersButton.Visibility = Visibility.Collapsed;
                
                // Show only simple user specific buttons
                ThesisButton.Visibility = Visibility.Visible;
                ProfileButton.Visibility = Visibility.Visible;
                FavoritesButton.Visibility = Visibility.Visible;

                // Set initial view to profile
                ProfileButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7B5CD6"));
                
                // Set up user-specific event handlers
                ThesisButton.Click += ThesisButton_Click;
                ProfileButton.Click += ProfileButton_Click;
                FavoritesButton.Click += FavoritesButton_Click;

                // Load profile view by default
                var profileView = new ProfileView();
                MainFrame.Navigate(profileView);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading simple user content: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadEtudiantContent()
        {
            try
            {
                // Hide all buttons first
                DashboardButton.Visibility = Visibility.Collapsed;
                MembersButton.Visibility = Visibility.Collapsed;
                
                // Show only etudiant specific buttons
                ThesisButton.Visibility = Visibility.Visible;
                ProfileButton.Visibility = Visibility.Visible;
                FavoritesButton.Visibility = Visibility.Visible;

                // Set initial view to profile
                ProfileButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7B5CD6"));
                
                // Set up etudiant-specific event handlers
                ThesisButton.Click += ThesisButton_Click;
                ProfileButton.Click += ProfileButton_Click;
                FavoritesButton.Click += FavoritesButton_Click;

                // Load profile view by default
                var profileView = new ProfileView();
                MainFrame.Navigate(profileView);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading etudiant content: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DashboardButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DashboardButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7B5CD6"));
                ThesisButton.Background = Brushes.Transparent;
                MembersButton.Background = Brushes.Transparent;
                ProfileButton.Background = Brushes.Transparent;
                FavoritesButton.Background = Brushes.Transparent;
                
                var dashboardView = new DashboardView();
                MainFrame.Navigate(dashboardView);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading Dashboard view: {ex.Message}", "Navigation Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ThesisButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ThesisButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7B5CD6"));
                DashboardButton.Background = Brushes.Transparent;
                MembersButton.Background = Brushes.Transparent;
                ProfileButton.Background = Brushes.Transparent;
                FavoritesButton.Background = Brushes.Transparent;
                
                // Load thesis view based on user role
                if (currentUser.Role == RoleUtilisateur.Admin)
                {
                    var thesisView = new Admin.ThesisView();
                    MainFrame.Navigate(thesisView);
                }
                else
                {
                    // For all other user types, use the admin thesis view with restricted rights
                    var thesisView = new Admin.ThesisView();
                    MainFrame.Navigate(thesisView);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading Thesis view: {ex.Message}", "Navigation Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MembersButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                MembersButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7B5CD6"));
                DashboardButton.Background = Brushes.Transparent;
                ThesisButton.Background = Brushes.Transparent;
                ProfileButton.Background = Brushes.Transparent;
                FavoritesButton.Background = Brushes.Transparent;
                
                var membersView = new MembersListView();
                MainFrame.Navigate(membersView);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading Members view: {ex.Message}", "Navigation Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ProfileButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ProfileButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7B5CD6"));
                ThesisButton.Background = Brushes.Transparent;
                FavoritesButton.Background = Brushes.Transparent;
                
                var profileView = new ProfileView();
                MainFrame.Navigate(profileView);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading Profile view: {ex.Message}", "Navigation Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void FavoritesButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                FavoritesButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7B5CD6"));
                DashboardButton.Background = Brushes.Transparent;
                ThesisButton.Background = Brushes.Transparent;
                MembersButton.Background = Brushes.Transparent;
                ProfileButton.Background = Brushes.Transparent;
                
                // Load favorites view based on user role
                var favoritesView = new Admin.FavoritesView();
                MainFrame.Navigate(favoritesView);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading Favorites view: {ex.Message}", "Navigation Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            LogoutConfirmationWindow confirmWindow = new LogoutConfirmationWindow();
            bool? result = confirmWindow.ShowDialog();
            if (result == true && confirmWindow.IsConfirmed)
            {
                Login loginWindow = new Login();
                loginWindow.Show();
                this.Close();
            }
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
            if (IsMaximize)
            {
                this.WindowState = WindowState.Normal;
                this.Width = 1280;
                this.Height = 720;
                IsMaximize = false;
            }
            else
            {
                this.WindowState = WindowState.Maximized;
                IsMaximize = true;
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
    }
}

