using System;
using System.Windows;
using System.Windows.Controls;
using DataGridNamespace;

namespace DataGridNamespace.SimpleUser
{
    public partial class SimpleUserSidebar : UserControl
    {
        public event RoutedEventHandler ThesisButton_Click;
        public event RoutedEventHandler FavoritesButton_Click;
        public event RoutedEventHandler ProfileButton_Click;

        public SimpleUserSidebar()
        {
            try
            {
                InitializeComponent();
                SetupEventHandlers();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing SimpleUserSidebar: {ex.Message}\nStack Trace: {ex.StackTrace}", 
                              "Initialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SetupEventHandlers()
        {
            ThesisButton.Click += (s, e) => ThesisButton_Click?.Invoke(s, e);
            FavoritesButton.Click += (s, e) => FavoritesButton_Click?.Invoke(s, e);
            ProfileButton.Click += (s, e) => ProfileButton_Click?.Invoke(s, e);
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var mainWindow = Window.GetWindow(this) as MainWindow;
                if (mainWindow != null)
                {
                    mainWindow.LogoutButton_Click(sender, e);
                }
                else
                {
                    MessageBox.Show("Could not find the main window for logout.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error in LogoutButton_Click: {ex.Message}\nStack Trace: {ex.StackTrace}", 
                              "Logout Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
} 