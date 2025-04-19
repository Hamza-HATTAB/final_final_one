using System;
using System.Collections.ObjectModel;
using System.Windows.Controls;
using MySql.Data.MySqlClient;

namespace DataGridNamespace.Admin
{
    public partial class DashboardView : UserControl
    {
        private ObservableCollection<RecentThesis> recentTheses;
        private ObservableCollection<Activity> recentActivities;

        public DashboardView()
        {
            InitializeComponent();
            LoadDashboardData();
        }

        private void LoadDashboardData()
        {
            try
            {
                LoadStatistics();
                LoadRecentTheses();
                LoadRecentActivities();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error loading dashboard data: {ex.Message}", 
                    "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void LoadStatistics()
        {
            string connectionString = "Server=localhost;Database=gestion_theses;User ID=root;Password=";
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();

                // Get total users
                using (MySqlCommand cmd = new MySqlCommand("SELECT COUNT(*) FROM users", conn))
                {
                    int totalUsers = Convert.ToInt32(cmd.ExecuteScalar());
                    TotalUsersText.Text = totalUsers.ToString();
                }

                // Get total theses
                using (MySqlCommand cmd = new MySqlCommand("SELECT COUNT(*) FROM theses", conn))
                {
                    int totalTheses = Convert.ToInt32(cmd.ExecuteScalar());
                    TotalThesesText.Text = totalTheses.ToString();
                }

                // Get active projects (showing total theses for now)
                using (MySqlCommand cmd = new MySqlCommand("SELECT COUNT(*) FROM theses", conn))
                {
                    int activeProjects = Convert.ToInt32(cmd.ExecuteScalar());
                    ActiveProjectsText.Text = activeProjects.ToString();
                }
            }
        }

        private void LoadRecentTheses()
        {
            recentTheses = new ObservableCollection<RecentThesis>();
            string connectionString = "Server=localhost;Database=gestion_theses;User ID=root;Password=";
            
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string query = @"SELECT titre, author, submission_date 
                               FROM theses 
                               ORDER BY submission_date DESC 
                               LIMIT 5";

                using (MySqlCommand cmd = new MySqlCommand(query, conn))
                {
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            recentTheses.Add(new RecentThesis
                            {
                                Titre = reader.GetString("titre"),
                                Author = reader.GetString("author"),
                                Date = reader.GetDateTime("submission_date").ToString("MMM dd, yyyy")
                            });
                        }
                    }
                }
            }

            RecentThesesList.ItemsSource = recentTheses;
        }

        private void LoadRecentActivities()
        {
            recentActivities = new ObservableCollection<Activity>();
            string connectionString = "Server=localhost;Database=gestion_theses;User ID=root;Password=";
            
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string query = @"SELECT action_type, description, created_at 
                               FROM activity_log 
                               ORDER BY created_at DESC 
                               LIMIT 5";

                using (MySqlCommand cmd = new MySqlCommand(query, conn))
                {
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            recentActivities.Add(new Activity
                            {
                                Action = reader.GetString("action_type"),
                                Description = reader.GetString("description"),
                                Time = reader.GetDateTime("created_at").ToString("HH:mm"),
                                Icon = GetActivityIcon(reader.GetString("action_type"))
                            });
                        }
                    }
                }
            }

            RecentActivitiesList.ItemsSource = recentActivities;
        }

        private string GetActivityIcon(string actionType)
        {
            return actionType.ToLower() switch
            {
                "login" => "Login",
                "logout" => "Logout",
                "add_thesis" => "BookPlus",
                "update_thesis" => "BookEdit",
                "delete_thesis" => "BookRemove",
                "add_user" => "AccountPlus",
                "update_user" => "AccountEdit",
                "delete_user" => "AccountRemove",
                _ => "Information"
            };
        }
    }

    public class RecentThesis
    {
        public string Titre { get; set; }
        public string Author { get; set; }
        public string Date { get; set; }
    }

    public class Activity
    {
        public string Action { get; set; }
        public string Description { get; set; }
        public string Time { get; set; }
        public string Icon { get; set; }
    }
}
