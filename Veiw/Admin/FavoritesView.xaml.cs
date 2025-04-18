using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using MySql.Data.MySqlClient;
using ThesesModels;
using FavorisModels;
using System.Windows.Media;
using System.Windows.Input;

namespace DataGridNamespace.Admin
{
    public partial class FavoritesView : UserControl
    {
        private ObservableCollection<Favoris> allFavorites;
        private CollectionViewSource favoritesViewSource;
        private int currentPage = 1;
        private int itemsPerPage = 10;
        private int totalPages = 1;
        private int totalItems = 0;
        private string currentSearchText = "";
        private string currentTypeFilter = "All Types";
        private ObservableCollection<Favoris> filteredFavorites;

        public FavoritesView()
        {
            InitializeComponent();
            // Important: delay loading data until the control is fully loaded
            this.Loaded += FavoritesView_Loaded;
        }

        private void FavoritesView_Loaded(object sender, RoutedEventArgs e)
        {
            // Now that the control is loaded, we can safely load data
            LoadFavorites();
            SetupPagination();
        }

        private void LoadFavorites()
        {
            try
            {
                // First test database connection
                bool isConnected = DataGrid.Models.DatabaseConnection.TestConnection();
                if (!isConnected)
                {
                    MessageBox.Show("Cannot connect to database. Please check your database settings.", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                allFavorites = new ObservableCollection<Favoris>();
                string connectionString = DataGrid.Models.DatabaseConnection.GetConnectionString();

                // Get current user ID from session
                int currentUserId = DataGridNamespace.Session.CurrentUserId;

                Debug.WriteLine($"Loading favorites for user ID: {currentUserId}");

                try
                {
                    // Fixed SQL query with correct column names and ascending order
                    string query = @"SELECT f.id, f.user_id, f.these_id, 
                                   t.id as ThesisId, t.titre, t.auteur, t.speciality, t.Type, 
                                   t.mots_cles as MotsCles, t.annee, t.Resume, t.fichier, t.user_id as ThesisUserId
                               FROM favoris f
                                   INNER JOIN theses t ON f.these_id = t.id
                                   WHERE f.user_id = @userId
                                   ORDER BY f.these_id ASC";

                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@userId", currentUserId);

                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                                {
                                    try
                            {
                                var thesis = new Theses
                                {
                                    Id = reader.GetInt32("ThesisId"),
                                            Titre = reader.IsDBNull(reader.GetOrdinal("titre")) ? "" : reader.GetString("titre"),
                                            Auteur = reader.IsDBNull(reader.GetOrdinal("auteur")) ? "" : reader.GetString("auteur"),
                                            Speciality = reader.IsDBNull(reader.GetOrdinal("speciality")) ? "" : reader.GetString("speciality"),
                                    Type = (TypeThese)Enum.Parse(typeof(TypeThese), reader.GetString("Type")),
                                            MotsCles = reader.IsDBNull(reader.GetOrdinal("MotsCles")) ? "" : reader.GetString("MotsCles"),
                                            Annee = reader.GetDateTime("annee"),
                                            Resume = reader.IsDBNull(reader.GetOrdinal("Resume")) ? "" : reader.GetString("Resume"),
                                            Fichier = reader.IsDBNull(reader.GetOrdinal("fichier")) ? "" : reader.GetString("fichier"),
                                    UserId = reader.GetInt32("ThesisUserId")
                                };

                                var favorite = new Favoris
                                {
                                            Id = reader.GetInt32("id"),
                                            UserId = reader.GetInt32("user_id"),
                                            TheseId = reader.GetInt32("these_id"),
                                    These = thesis
                                };

                                allFavorites.Add(favorite);
                                    }
                                    catch (Exception ex)
                                    {
                                        Debug.WriteLine($"Error reading favorite record: {ex.Message}");
                                        // Continue processing other records
                                    }
                                }
                            }
                        }
                    }
                }
                catch (MySqlException sqlEx)
                {
                    Debug.WriteLine($"SQL Error in LoadFavorites: {sqlEx.Message}, Error code: {sqlEx.Number}");
                    MessageBox.Show($"Database error: {sqlEx.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    
                    // Ensure we have an empty collection to avoid null reference exceptions
                    allFavorites = new ObservableCollection<Favoris>();
                }

                // Initialize with empty collection if nothing was loaded
                if (allFavorites == null)
                {
                    allFavorites = new ObservableCollection<Favoris>();
                }

                totalItems = allFavorites.Count;
                totalPages = Math.Max(1, (int)Math.Ceiling((double)totalItems / itemsPerPage));

                favoritesViewSource = new CollectionViewSource { Source = allFavorites };
                favoritesViewSource.Filter += FavoritesViewSource_Filter;

                ApplyPagination();
                UpdatePaginationControls();
                
                // Update the UI with loaded data count
                Debug.WriteLine($"Loaded {allFavorites.Count} favorites successfully");
                UpdateFavoritesCounter();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Critical error in LoadFavorites: {ex.Message}\n{ex.StackTrace}");
                MessageBox.Show($"Error loading favorites: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                
                // Ensure we have an empty collection to avoid null reference exceptions
                if (allFavorites == null)
                {
                    allFavorites = new ObservableCollection<Favoris>();
                }
                
                // Update UI with empty data
                totalItems = 0;
                totalPages = 1;
                currentPage = 1;
                
                FavoritesDataGrid.ItemsSource = allFavorites;
                UpdatePaginationControls();
                UpdateFavoritesCounter();
            }
        }

        private void ApplyPagination()
        {
            try
            {
                if (filteredFavorites == null)
                {
                    // If filtered collection doesn't exist, create it from all favorites with current filters
                    ApplyFilters();
                    return;
                }

                if (filteredFavorites.Count == 0)
                {
                    FavoritesDataGrid.ItemsSource = new List<Favoris>();
                    return;
                }

                int skipCount = (currentPage - 1) * itemsPerPage;
                
                // Ensure skip count is valid
                if (skipCount >= filteredFavorites.Count)
                {
                    currentPage = 1;
                    skipCount = 0;
                }
                
                var currentPageItems = filteredFavorites
                    .Skip(skipCount)
                    .Take(itemsPerPage)
                    .ToList();

                FavoritesDataGrid.ItemsSource = currentPageItems;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error applying pagination: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                FavoritesDataGrid.ItemsSource = new List<Favoris>();
            }
        }

        private void SetupPagination()
        {
            UpdatePaginationControls();
        }

        private void UpdatePaginationControls()
        {
            PaginationItemsControl.Items.Clear();

            // Disable/enable navigation buttons
            FirstPageButton.IsEnabled = currentPage > 1;
            PreviousPageButton.IsEnabled = currentPage > 1;
            NextPageButton.IsEnabled = currentPage < totalPages;
            LastPageButton.IsEnabled = currentPage < totalPages;

            // Add page number buttons
            int startPage = Math.Max(1, currentPage - 2);
            int endPage = Math.Min(totalPages, startPage + 4);

            for (int i = startPage; i <= endPage; i++)
            {
                Button pageButton = new Button
                {
                    Content = i.ToString(),
                    Width = 35,
                    Height = 35,
                    Margin = new Thickness(2),
                    Tag = i
                };

                if (i == currentPage)
                {
                    pageButton.Style = FindResource("ActivePageButtonStyle") as Style;
                }
                else
                {
                    pageButton.Style = FindResource("PaginationButtonStyle") as Style;
                    pageButton.Click += PageButton_Click;
                }

                PaginationItemsControl.Items.Add(pageButton);
            }
        }

        private void PageButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int pageNumber)
            {
                currentPage = pageNumber;
                ApplyPagination();
                UpdatePaginationControls();
            }
        }

        private void FirstPageButton_Click(object sender, RoutedEventArgs e)
        {
            currentPage = 1;
            ApplyPagination();
            UpdatePaginationControls();
        }

        private void PreviousPageButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentPage > 1)
            {
                currentPage--;
                ApplyPagination();
                UpdatePaginationControls();
            }
        }

        private void NextPageButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentPage < totalPages)
            {
                currentPage++;
                ApplyPagination();
                UpdatePaginationControls();
            }
        }

        private void LastPageButton_Click(object sender, RoutedEventArgs e)
        {
            currentPage = totalPages;
            ApplyPagination();
            UpdatePaginationControls();
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Only apply filters if the control is fully loaded
            if (IsLoaded && SearchTextBox != null)
            {
                ApplyFilters();
            }
        }

        private void TypeFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Only apply filters if the control is fully loaded
            if (IsLoaded && TypeFilterComboBox != null)
            {
                ApplyFilters();
            }
        }

        private void ApplyFilters()
        {
            try 
            {
                // Check if any UI elements are null (could happen if called during initialization)
                if (SearchTextBox == null || TypeFilterComboBox == null || FavoritesDataGrid == null)
                {
                    Debug.WriteLine("ApplyFilters called before UI elements were initialized.");
                    return;
                }

                // Store current filter values
                currentSearchText = SearchTextBox.Text?.ToLower() ?? "";
                
                // Safely get ComboBox selection
                if (TypeFilterComboBox.SelectedItem != null && TypeFilterComboBox.SelectedItem is ComboBoxItem selectedItem)
                {
                    currentTypeFilter = selectedItem.Content?.ToString() ?? "All Types";
                }

                if (allFavorites == null)
                {
                    Debug.WriteLine("ApplyFilters called when allFavorites was null");
                    FavoritesDataGrid.ItemsSource = new List<Favoris>();
                    return;
                }

                if (allFavorites.Count == 0)
                {
                    FavoritesDataGrid.ItemsSource = new List<Favoris>();
                    return;
                }

                // Apply all filters to get filtered collection
                filteredFavorites = new ObservableCollection<Favoris>(allFavorites.Where(f =>
                    (string.IsNullOrEmpty(currentSearchText) ||
                        (f.These?.Titre != null && f.These.Titre.ToLower().Contains(currentSearchText)) ||
                        (f.These?.Auteur != null && f.These.Auteur.ToLower().Contains(currentSearchText)) ||
                        (f.These?.MotsCles != null && f.These.MotsCles.ToLower().Contains(currentSearchText))) &&
                    (currentTypeFilter == "All Types" ||
                        (f.These != null && f.These.Type.ToString() == currentTypeFilter))
                ));

                totalItems = filteredFavorites.Count;
                totalPages = Math.Max(1, (int)Math.Ceiling((double)totalItems / itemsPerPage));
                
                // Reset to page 1 when filters change
                currentPage = 1;

                // Apply pagination to the filtered results
                ApplyPagination();
                UpdatePaginationControls();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error applying filters: {ex.Message}");
                MessageBox.Show($"Error applying filters: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                
                // Fallback to showing all if filtering fails
                if (allFavorites != null && FavoritesDataGrid != null)
                {
                    FavoritesDataGrid.ItemsSource = allFavorites.Take(itemsPerPage).ToList();
                }
            }
        }

        private void FavoritesViewSource_Filter(object sender, FilterEventArgs e)
        {
            try 
            {
                if (e.Item == null)
                {
                    e.Accepted = false;
                    return;
                }

                if (SearchTextBox == null || TypeFilterComboBox == null)
                {
                    // UI elements not initialized yet
                    e.Accepted = true;  // Show everything by default
                    return;
                }

                if (e.Item is Favoris favorite)
                {
                    if (favorite.These == null)
                    {
                        e.Accepted = false;
                        return;
                    }

                    string searchText = SearchTextBox.Text?.ToLower() ?? "";
                    
                    // Safely get ComboBox selection
                    string typeFilter = "All Types";
                    if (TypeFilterComboBox.SelectedItem != null && TypeFilterComboBox.SelectedItem is ComboBoxItem selectedItem)
                    {
                        typeFilter = selectedItem.Content?.ToString() ?? "All Types";
                    }

                    // Safely check properties
                    bool matchesSearch = string.IsNullOrEmpty(searchText) ||
                                        (favorite.These.Titre != null && favorite.These.Titre.ToLower().Contains(searchText)) ||
                                        (favorite.These.Auteur != null && favorite.These.Auteur.ToLower().Contains(searchText)) ||
                                        (favorite.These.MotsCles != null && favorite.These.MotsCles.ToLower().Contains(searchText));

                    bool matchesType = typeFilter == "All Types" || favorite.These.Type.ToString() == typeFilter;

                    e.Accepted = matchesSearch && matchesType;
                }
                else
                {
                    // Not a Favoris object
                    e.Accepted = false;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in filter: {ex.Message}");
                // Show the item by default if there's an error to prevent data from disappearing
                e.Accepted = true;
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            // Clear filters before refreshing to get fresh data
            if (SearchTextBox != null) SearchTextBox.Clear();
            if (TypeFilterComboBox != null) TypeFilterComboBox.SelectedIndex = 0;
            
            // Reset filter tracking variables
            currentSearchText = "";
            currentTypeFilter = "All Types";
            
            // Reload data with fresh DB query
            LoadFavorites();
        }

        private void ViewDetailsButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Favoris favorite && favorite.These != null)
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
                    var grid = new Grid { Margin = new Thickness(30) };

                    // Define rows for the grid
                    for (int i = 0; i < 15; i++)
                    {
                    grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                    }

                    // Add a header
                    var headerBorder = new Border
                    {
                        Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#008080")),
                        Padding = new Thickness(20, 15, 20, 15),
                        CornerRadius = new CornerRadius(8, 8, 0, 0)
                    };
                    var headerTitle = new TextBlock
                    {
                        Text = favorite.These.Titre ?? "Thesis Details",
                        Foreground = Brushes.White,
                        FontSize = 20,
                        FontWeight = FontWeights.Bold,
                        TextWrapping = TextWrapping.Wrap
                    };
                    headerBorder.Child = headerTitle;
                    Grid.SetRow(headerBorder, 0);
                    Grid.SetColumnSpan(headerBorder, 2);
                    grid.Children.Add(headerBorder);

                    // Add a content border
                    var contentBorder = new Border
                    {
                        Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F0FFFF")),
                        Padding = new Thickness(25),
                        CornerRadius = new CornerRadius(0, 0, 8, 8),
                        BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E5E7EB")),
                        BorderThickness = new Thickness(1)
                    };
                    var contentGrid = new Grid();
                    for (int i = 0; i < 14; i++)
                    {
                        contentGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                    }
                    contentBorder.Child = contentGrid;
                    Grid.SetRow(contentBorder, 1);
                    Grid.SetColumnSpan(contentBorder, 2);
                    grid.Children.Add(contentBorder);

                    // Title
                    AddDetailRow(contentGrid, 0, "Title:", favorite.These.Titre ?? "N/A");

                    // Author
                    AddDetailRow(contentGrid, 2, "Author:", favorite.These.Auteur ?? "N/A");

                    // Specialty
                    AddDetailRow(contentGrid, 4, "Specialty:", favorite.These.Speciality ?? "N/A");

                    // Type
                    AddDetailRow(contentGrid, 6, "Type:", favorite.These.Type.ToString());

                    // Keywords
                    AddDetailRow(contentGrid, 8, "Keywords:", favorite.These.MotsCles ?? "N/A");

                    // Year
                    string yearText = "N/A";
                    if (favorite.These.Annee != default)
                    {
                        try
                        {
                            yearText = favorite.These.Annee.Year.ToString();
                        }
                        catch (Exception)
                        {
                            yearText = "N/A";
                        }
                    }
                    AddDetailRow(contentGrid, 10, "Year:", yearText);

                    // Abstract
                    AddDetailRow(contentGrid, 12, "Abstract:", favorite.These.Resume ?? "N/A", true);

                    // Add buttons at the bottom
                    var buttonsPanel = new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        HorizontalAlignment = HorizontalAlignment.Right,
                        Margin = new Thickness(0, 20, 0, 0)
                    };

                    // View PDF button
                    if (!string.IsNullOrEmpty(favorite.These.Fichier))
                    {
                        var viewPdfButton = new Button
                        {
                            Content = "View PDF",
                            Width = 120,
                            Height = 40,
                            Margin = new Thickness(0, 0, 10, 0),
                            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50")),
                            Foreground = Brushes.White,
                            BorderThickness = new Thickness(0),
                            Style = (Style)FindResource("ActionButtonStyle")
                        };
                        viewPdfButton.Tag = favorite;
                        viewPdfButton.Click += ViewPdfButton_Click;
                        buttonsPanel.Children.Add(viewPdfButton);
                    }

                    // Close button
                    var closeButton = new Button
                    {
                        Content = "Close",
                        Width = 120,
                        Height = 40,
                        Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#008080")),
                        Foreground = Brushes.White,
                        BorderThickness = new Thickness(0),
                        Style = (Style)FindResource("ActionButtonStyle")
                    };
                    closeButton.Click += (s, args) => detailsWindow.Close();
                    buttonsPanel.Children.Add(closeButton);

                    // Add buttons to the grid
                    Grid.SetRow(buttonsPanel, 3);
                    grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                    grid.Children.Add(buttonsPanel);

                    scrollViewer.Content = grid;
                    detailsWindow.Content = scrollViewer;
                    detailsWindow.ShowDialog();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error displaying thesis details: {ex.Message}");
                    MessageBox.Show($"Error displaying thesis details: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Cannot display thesis details. The thesis information may be incomplete.", 
                              "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddDetailRow(Grid grid, int rowIndex, string label, string value, bool isMultiline = false)
        {
            // Label
            var labelText = new TextBlock 
            { 
                Text = label + ":", 
                FontWeight = FontWeights.Bold, 
                Margin = new Thickness(0, 10, 0, 5),
                FontSize = 14,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#333333"))
            };
            Grid.SetRow(labelText, rowIndex);
            grid.Children.Add(labelText);

            // Value
            var valueText = new TextBlock 
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
                
                scrollViewer.Content = valueText;
                abstractBorder.Child = scrollViewer;
                
                Grid.SetRow(abstractBorder, rowIndex + 1);
                grid.Children.Add(abstractBorder);
            }
            else
            {
                Grid.SetRow(valueText, rowIndex + 1);
                grid.Children.Add(valueText);
            }
        }

        private async void ViewPdfButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Favoris favorite && favorite.These != null)
            {
                try
                {
                    if (string.IsNullOrEmpty(favorite.These.Fichier))
                    {
                        MessageBox.Show("No PDF file is associated with this thesis.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }

                    this.Cursor = Cursors.Wait;

                    // Use CloudStorageService to get a signed URL for the file
                    var cloudStorageService = new DataGridNamespace.Services.CloudStorageService();
                    string signedUrl = await cloudStorageService.GetSignedReadUrl(favorite.These.Fichier);
                    
                    if (string.IsNullOrEmpty(signedUrl))
                    {
                        MessageBox.Show("Could not generate a download URL for this file.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        this.Cursor = Cursors.Arrow;
                        return;
                    }

                    // Open the signed URL in the default browser
                    var startInfo = new ProcessStartInfo
                    {
                        FileName = signedUrl,
                        UseShellExecute = true
                    };
                    
                    Process.Start(startInfo);
                    Debug.WriteLine($"Successfully opened PDF with signed URL: {signedUrl}");
                    this.Cursor = Cursors.Arrow;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error opening PDF file: {ex.Message}");
                    MessageBox.Show($"Error opening PDF file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    this.Cursor = Cursors.Arrow;
                }
            }
            else
            {
                MessageBox.Show("Cannot open PDF. The thesis information may be incomplete.", 
                              "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RemoveFavoriteButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Favoris favorite && favorite.These != null)
            {
                var result = MessageBox.Show($"Are you sure you want to remove '{favorite.These.Titre}' from favorites?",
                                           "Confirm Remove",
                                           MessageBoxButton.YesNo,
                                           MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        string connectionString = DataGrid.Models.DatabaseConnection.GetConnectionString();
                        using (MySqlConnection conn = new MySqlConnection(connectionString))
                        {
                            conn.Open();
                            string query = "DELETE FROM favoris WHERE id = @id";
                            using (MySqlCommand cmd = new MySqlCommand(query, conn))
                            {
                                cmd.Parameters.AddWithValue("@id", favorite.Id);
                                int rowsAffected = cmd.ExecuteNonQuery();
                                
                                if (rowsAffected > 0)
                                {
                                    // Remove from the collection and refresh the UI
                                    allFavorites.Remove(favorite);
                                    
                                    // Recalculate pagination
                                    totalItems = allFavorites.Count;
                                    totalPages = Math.Max(1, (int)Math.Ceiling((double)totalItems / itemsPerPage));
                                    
                                    // Adjust current page if needed
                                    if (currentPage > totalPages)
                                    {
                                        currentPage = totalPages;
                                    }
                                    
                                    // Apply pagination and update controls
                                    ApplyPagination();
                                    UpdatePaginationControls();
                                    UpdateFavoritesCounter();
                                    
                                    MessageBox.Show("Favorite removed successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                                }
                                else
                                {
                                    MessageBox.Show("Failed to remove favorite. Please try again.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error removing favorite: {ex.Message}");
                        MessageBox.Show($"Error removing favorite: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("Cannot remove favorite. The favorite information may be incomplete.", 
                             "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void FavoritesDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // This method can be used to update UI elements based on selection
        }

        private void UpdateFavoritesCounter()
        {
            if (FavoritesCounterText != null)
            {
                FavoritesCounterText.Text = $"({totalItems} favorites)";
            }
        }

        private void SendMessageButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Get the favorite object from the button tag
                if (sender is Button button && button.Tag is Favoris favorite && favorite.These != null)
                {
                    // Create and show the message window
                    var messageWindow = new MessageWindow();
                    messageWindow.Owner = Window.GetWindow(this);
                    messageWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    messageWindow.SetThesis(favorite.These.Id, favorite.These.Titre);
                    
                    messageWindow.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error sending message: {ex.Message}");
                MessageBox.Show($"Error sending message: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
