using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MySql.Data.MySqlClient;
using System.Collections.ObjectModel;
using ThesesModels;
using DataGrid.Models;
using System.Diagnostics;
using System.IO;
using System.Windows.Media;

namespace DataGridNamespace.SimpleUser
{
    public partial class SimpleUserThesisView : UserControl
    {
        private ObservableCollection<Theses> theses;

        public SimpleUserThesisView()
        {
            InitializeComponent();
            LoadTheses();
        }

        private void LoadTheses()
        {
            try
            {
                theses = new ObservableCollection<Theses>();
                string connectionString = DatabaseConnection.GetConnectionString();
                string query = @"SELECT t.id, t.titre, t.auteur, t.speciality, t.Type, t.mots_cles, 
                                t.annee, t.Resume, t.fichier, t.user_id 
                                FROM theses t ORDER BY t.id DESC";

                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var thesis = new Theses
                                {
                                    Id = reader.GetInt32("id"),
                                    Titre = reader.GetString("titre"),
                                    Auteur = reader.GetString("auteur"),
                                    Specialite = reader.GetString("speciality"),
                                    Type = (TypeThese)Enum.Parse(typeof(TypeThese), reader.GetString("Type")),
                                    MotsCles = reader.GetString("mots_cles"),
                                    Annee = reader.GetDateTime("annee"),
                                    Resume = reader.GetString("Resume"),
                                    Fichier = reader.GetString("fichier"),
                                    UserId = reader.GetInt32("user_id")
                                };
                                theses.Add(thesis);
                            }
                        }
                    }
                }

                ThesisListView.ItemsSource = theses;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading theses: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            FilterTheses(SearchTextBox.Text);
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            FilterTheses(SearchTextBox.Text);
        }

        private void FilterTheses(string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                ThesisListView.ItemsSource = theses;
                return;
            }

            var filteredTheses = new ObservableCollection<Theses>();
            foreach (var thesis in theses)
            {
                if (thesis.Titre.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                    thesis.Auteur.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                    thesis.MotsCles.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                {
                    filteredTheses.Add(thesis);
                }
            }

            ThesisListView.ItemsSource = filteredTheses;
        }

        private void ViewThesisButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Theses thesis)
            {
                try
                {
                    // Create a window to display thesis details
                    var detailsWindow = new Window
                    {
                        Title = "Thesis Details",
                        Width = 700,
                        Height = 600,
                        WindowStartupLocation = WindowStartupLocation.CenterScreen,
                        ResizeMode = ResizeMode.NoResize,
                        Background = new SolidColorBrush(Colors.White)
                    };

                    // Create the content
                    var scrollViewer = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
                    var mainGrid = new Grid { Margin = new Thickness(20) };

                    mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Header
                    mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Content
                    mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Buttons

                    // Header with thesis title
                    var headerBorder = new Border
                    {
                        Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#008080")),
                        Padding = new Thickness(15),
                        CornerRadius = new CornerRadius(5, 5, 0, 0)
                    };

                    var headerText = new TextBlock
                    {
                        Text = thesis.Titre ?? "Thesis Details",
                        Foreground = Brushes.White,
                        FontSize = 20,
                        FontWeight = FontWeights.SemiBold,
                        TextWrapping = TextWrapping.Wrap
                    };

                    headerBorder.Child = headerText;
                    Grid.SetRow(headerBorder, 0);
                    mainGrid.Children.Add(headerBorder);

                    // Content grid
                    var contentBorder = new Border
                    {
                        Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F8FFFF")),
                        BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E5E7EB")),
                        BorderThickness = new Thickness(1),
                        Padding = new Thickness(20),
                        CornerRadius = new CornerRadius(0, 0, 5, 5)
                    };

                    var contentGrid = new Grid();
                    
                    // Add rows for different fields
                    for (int i = 0; i < 14; i++)
                    {
                        contentGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                    }

                    // Add details to grid
                    AddDetailField(contentGrid, 0, "Author", thesis.Auteur ?? "N/A");
                    AddDetailField(contentGrid, 2, "Specialty", thesis.Specialite ?? "N/A");
                    AddDetailField(contentGrid, 4, "Type", thesis.Type.ToString());
                    AddDetailField(contentGrid, 6, "Keywords", thesis.MotsCles ?? "N/A");
                    
                    // Year
                    string yearText = "N/A";
                    if (thesis.Annee != default)
                    {
                        try
                        {
                            yearText = thesis.Annee.Year.ToString();
                        }
                        catch (Exception)
                        {
                            yearText = "N/A";
                        }
                    }
                    AddDetailField(contentGrid, 8, "Year", yearText);
                    
                    // Abstract (multiline)
                    AddDetailField(contentGrid, 10, "Abstract", thesis.Resume ?? "N/A", true);

                    contentBorder.Child = contentGrid;
                    Grid.SetRow(contentBorder, 1);
                    mainGrid.Children.Add(contentBorder);

                    // Buttons panel
                    var buttonsPanel = new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        HorizontalAlignment = HorizontalAlignment.Right,
                        Margin = new Thickness(0, 15, 0, 0)
                    };

                    // View PDF button
                    if (!string.IsNullOrEmpty(thesis.Fichier))
                    {
                        var viewPdfButton = new Button
                        {
                            Content = "View PDF",
                            Width = 120,
                            Height = 40,
                            Margin = new Thickness(0, 0, 10, 0),
                            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50")),
                            Foreground = Brushes.White,
                            Tag = thesis
                        };
                        viewPdfButton.Click += ViewPdfButton_Click;
                        buttonsPanel.Children.Add(viewPdfButton);
                    }

                    // Add to favorites button
                    var favoriteButton = new Button
                    {
                        Content = "Add to Favorites",
                        Width = 120,
                        Height = 40,
                        Margin = new Thickness(0, 0, 10, 0),
                        Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFC107")),
                        Foreground = Brushes.White,
                        Tag = thesis
                    };
                    favoriteButton.Click += FavoriteThesisButton_Click;
                    buttonsPanel.Children.Add(favoriteButton);

                    // Close button
                    var closeButton = new Button
                    {
                        Content = "Close",
                        Width = 120,
                        Height = 40,
                        Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#008080")),
                        Foreground = Brushes.White
                    };
                    closeButton.Click += (s, args) => detailsWindow.Close();
                    buttonsPanel.Children.Add(closeButton);

                    Grid.SetRow(buttonsPanel, 2);
                    mainGrid.Children.Add(buttonsPanel);

                    scrollViewer.Content = mainGrid;
                    detailsWindow.Content = scrollViewer;
                    detailsWindow.ShowDialog();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error displaying thesis details: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        
        private void AddDetailField(Grid grid, int rowIndex, string label, string value, bool isMultiline = false)
        {
            // Label
            var labelBlock = new TextBlock
            {
                Text = label + ":",
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 10, 0, 5),
                FontSize = 14,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#333333"))
            };
            Grid.SetRow(labelBlock, rowIndex);
            grid.Children.Add(labelBlock);
            
            // Value
            var valueBlock = new TextBlock
            {
                Text = value,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, isMultiline ? 20 : 10),
                FontSize = 14
            };
            
            if (isMultiline)
            {
                var abstractBorder = new Border
                {
                    BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E5E7EB")),
                    BorderThickness = new Thickness(1),
                    Padding = new Thickness(15),
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF")),
                    MaxHeight = 180
                };
                
                var scrollViewer = new ScrollViewer
                {
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
                };
                
                scrollViewer.Content = valueBlock;
                abstractBorder.Child = scrollViewer;
                
                Grid.SetRow(abstractBorder, rowIndex + 1);
                grid.Children.Add(abstractBorder);
            }
            else
            {
                Grid.SetRow(valueBlock, rowIndex + 1);
                grid.Children.Add(valueBlock);
            }
        }

        private void FavoriteThesisButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Theses thesis)
            {
                try
                {
                    // Get current user ID from session
                    int currentUserId = DataGridNamespace.Session.CurrentUserId;

                    // Check if this thesis is already in favorites for this user
                    bool isAlreadyInFavorites = false;
                    string connectionString = DatabaseConnection.GetConnectionString();

                    using (MySqlConnection conn = new MySqlConnection(connectionString))
                    {
                        conn.Open();
                        string checkQuery = "SELECT COUNT(*) FROM favoris WHERE user_id = @userId AND these_id = @thesisId";
                        using (MySqlCommand cmd = new MySqlCommand(checkQuery, conn))
                        {
                            cmd.Parameters.AddWithValue("@userId", currentUserId);
                            cmd.Parameters.AddWithValue("@thesisId", thesis.Id);
                            isAlreadyInFavorites = Convert.ToInt32(cmd.ExecuteScalar()) > 0;
                        }

                        if (isAlreadyInFavorites)
                        {
                            // Remove from favorites
                            string deleteQuery = "DELETE FROM favoris WHERE user_id = @userId AND these_id = @thesisId";
                            using (MySqlCommand cmd = new MySqlCommand(deleteQuery, conn))
                            {
                                cmd.Parameters.AddWithValue("@userId", currentUserId);
                                cmd.Parameters.AddWithValue("@thesisId", thesis.Id);
                                int rowsAffected = cmd.ExecuteNonQuery();

                                if (rowsAffected > 0)
                                {
                                    MessageBox.Show($"'{thesis.Titre}' has been removed from your favorites.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                                }
                                else
                                {
                                    MessageBox.Show("Failed to remove from favorites. Please try again.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                }
                            }
                        }
                        else
                        {
                            // Add to favorites
                            string insertQuery = "INSERT INTO favoris (user_id, these_id) VALUES (@userId, @thesisId)";
                            using (MySqlCommand cmd = new MySqlCommand(insertQuery, conn))
                            {
                                cmd.Parameters.AddWithValue("@userId", currentUserId);
                                cmd.Parameters.AddWithValue("@thesisId", thesis.Id);
                                int rowsAffected = cmd.ExecuteNonQuery();

                                if (rowsAffected > 0)
                                {
                                    MessageBox.Show($"'{thesis.Titre}' has been added to your favorites.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                                }
                                else
                                {
                                    MessageBox.Show("Failed to add to favorites. Please try again.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error managing favorites: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ViewPdfButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Theses thesis)
            {
                try
                {
                    if (thesis == null || string.IsNullOrEmpty(thesis.Fichier))
                    {
                        MessageBox.Show("No PDF file is associated with this thesis.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }

                    // Ensure the file exists before trying to open it
                    if (!System.IO.File.Exists(thesis.Fichier))
                    {
                        MessageBox.Show($"PDF file not found at: {thesis.Fichier}", "File Not Found", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // Try to open the PDF with the default PDF viewer
                    var startInfo = new ProcessStartInfo
                    {
                        FileName = thesis.Fichier,
                        UseShellExecute = true
                    };
                    
                    Process.Start(startInfo);
                }
                catch (System.ComponentModel.Win32Exception win32Ex)
                {
                    MessageBox.Show($"There was a problem opening the PDF file. Please make sure you have a PDF viewer installed.\n\nError: {win32Ex.Message}", 
                                 "PDF Viewer Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error opening PDF file: {ex.Message}\n\nFile path: {thesis?.Fichier ?? "Unknown"}", 
                                 "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Cannot open PDF. The thesis information may be incomplete.", 
                             "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
} 