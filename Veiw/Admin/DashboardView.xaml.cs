using System;
using System.Collections.ObjectModel;
using System.Windows.Controls;
using MySql.Data.MySqlClient;
using System.Diagnostics;
using DataGridNamespace; // For AppConfig

namespace DataGridNamespace.Admin
{
    public partial class DashboardView : UserControl
    {
        private ObservableCollection<RecentThesis> recentTheses;
        private ObservableCollection<Activity> recentActivities;
        private bool isDataLoaded = false;

        public DashboardView()
        {
            InitializeComponent();
            this.Loaded += (s, e) => 
            {
                if (!isDataLoaded)
                {
                    LoadDashboardData();
                    isDataLoaded = true;
                }
            };
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
                Debug.WriteLine($"Error loading dashboard data: {ex.Message}");
                System.Windows.MessageBox.Show($"Error loading dashboard data: {ex.Message}", 
                    "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void LoadStatistics()
        {
            try
            {
                string connectionString = AppConfig.CloudSqlConnectionString;
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
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading statistics: {ex.Message}");
                // Continue with other sections even if statistics fail
            }
        }

        private void LoadRecentTheses()
        {
            recentTheses = new ObservableCollection<RecentThesis>();
            try
            {
                string connectionString = AppConfig.CloudSqlConnectionString;
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    // Use auteur column instead of author, and check if submission_date exists
                    // If not, use the annee column which we know exists
                    string query = @"SELECT titre, auteur, annee 
                                  FROM theses 
                                  ORDER BY annee DESC 
                                  LIMIT 5";

                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                try
                                {
                                    string title = reader.IsDBNull(reader.GetOrdinal("titre")) 
                                        ? "Untitled" 
                                        : reader.GetString("titre");
                                    
                                    string author = reader.IsDBNull(reader.GetOrdinal("auteur")) 
                                        ? "Unknown" 
                                        : reader.GetString("auteur");
                                    
                                    string date = "Unknown";
                                    try
                                    {
                                        DateTime dateValue = reader.GetDateTime("annee");
                                        date = dateValue.ToString("MMM dd, yyyy");
                                    }
                                    catch
                                    {
                                        // If direct conversion fails, try to get as string and parse
                                        if (!reader.IsDBNull(reader.GetOrdinal("annee")))
                                        {
                                            string dateStr = reader.GetString("annee");
                                            if (DateTime.TryParse(dateStr, out DateTime parsedDate))
                                            {
                                                date = parsedDate.ToString("MMM dd, yyyy");
                                            }
                                        }
                                    }

                                    recentTheses.Add(new RecentThesis
                                    {
                                        Titre = title,
                                        Author = author,
                                        Date = date
                                    });
                                }
                                catch (Exception ex)
                                {
                                    Debug.WriteLine($"Error processing thesis record: {ex.Message}");
                                    // Continue with next record
                                }
                            }
                        }
                    }
                }

                RecentThesesList.ItemsSource = recentTheses;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading recent theses: {ex.Message}");
                // Continue with other sections even if loading theses fails
            }
        }

        private void LoadRecentActivities()
        {
            recentActivities = new ObservableCollection<Activity>();
            try
            {
                string connectionString = AppConfig.CloudSqlConnectionString;
                
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    // Check if activity_log table exists first
                    bool tableExists = false;
                    string checkTableQuery = "SHOW TABLES LIKE 'activity_log'";
                    using (MySqlCommand cmd = new MySqlCommand(checkTableQuery, conn))
                    {
                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            tableExists = reader.HasRows;
                        }
                    }

                    if (tableExists)
                    {
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
                                    try
                                    {
                                        string action = reader.IsDBNull(reader.GetOrdinal("action_type")) 
                                            ? "Unknown" 
                                            : reader.GetString("action_type");
                                        
                                        string description = reader.IsDBNull(reader.GetOrdinal("description")) 
                                            ? "No description" 
                                            : reader.GetString("description");
                                        
                                        string time = "Unknown";
                                        if (!reader.IsDBNull(reader.GetOrdinal("created_at")))
                                        {
                                            DateTime timestamp = reader.GetDateTime("created_at");
                                            time = timestamp.ToString("HH:mm");
                                        }

                                        recentActivities.Add(new Activity
                                        {
                                            Action = action,
                                            Description = description,
                                            Time = time,
                                            Icon = GetActivityIcon(action)
                                        });
                                    }
                                    catch (Exception ex)
                                    {
                                        Debug.WriteLine($"Error processing activity record: {ex.Message}");
                                        // Continue with next record
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        // If the table doesn't exist, add placeholder activities
                        Debug.WriteLine("Activity log table doesn't exist, adding placeholder activities");
                        
                        // Add some placeholder activities
                        recentActivities.Add(new Activity
                        {
                            Action = "System",
                            Description = "Activity logging is not yet configured",
                            Time = DateTime.Now.ToString("HH:mm"),
                            Icon = "Information"
                        });
                    }
                }

                RecentActivitiesList.ItemsSource = recentActivities;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading recent activities: {ex.Message}");
                // Continue even if loading activities fails
            }
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
