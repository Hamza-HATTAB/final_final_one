using System;
using System.Windows;
using System.Windows.Controls;

namespace DataGridNamespace.Admin
{
    public partial class AdminDashboard : UserControl
    {
        public AdminDashboard()
        {
            InitializeComponent();
            LoadDashboardData();
        }

        private void LoadDashboardData()
        {
            try
            {
                // TODO: Load dashboard data from database
                // This will include:
                // - Total number of theses
                // - Total number of users
                // - Recent activity
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading dashboard data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
} 