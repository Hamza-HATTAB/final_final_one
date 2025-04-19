using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using MySql.Data.MySqlClient;
using ThesesModels;
using FavorisModels;
using System.Windows.Input;

namespace DataGridNamespace.Admin
{
    public partial class ThesisView : UserControl
    {
        private ObservableCollection<Theses> allTheses;
        private CollectionViewSource thesesViewSource;
        private int currentPage = 1;
        private int itemsPerPage = 10;
        private int totalPages = 1;
        private int totalItems = 0;
        private string currentSearchText = "";
        private string currentTypeFilter = "All Types";
        private string currentYearFilter = "All Years";
        private ObservableCollection<Theses> filteredTheses;
        private bool isDataLoaded = false;

        public ThesisView()
        {
            InitializeComponent();
            // Important: delay loading data until the control is fully loaded
            this.Loaded += ThesisView_Loaded;
        }

        private void ThesisView_Loaded(object sender, RoutedEventArgs e)
        {
            // Only load data if it hasn't been loaded yet
            if (!isDataLoaded)
            {
                // Now that the control is loaded, we can safely load data
                LoadTheses();
                SetupPagination();
                isDataLoaded = true;
            }
        }

        private void LoadTheses()
        {
            try
            {
                // First test database connection
                bool isConnected = TestDatabaseConnection();
                if (!isConnected)
                {
                    MessageBox.Show("Cannot connect to database. Please check your database settings.", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                allTheses = new ObservableCollection<Theses>();
                string connectionString = AppConfig.CloudSqlConnectionString;
                
                try
                {
                    // Fixed SQL query with correct column names and ascending order
                    string query = @"SELECT t.id, t.titre, t.auteur, t.speciality, t.Type, 
                                   t.mots_cles as MotsCles, t.annee, t.Resume, t.fichier, t.user_id as UserId 
                                   FROM theses t
                                   ORDER BY t.id ASC";

                    using (MySqlConnection conn = new MySqlConnection(connectionString))
                    {
                        conn.Open();
                        using (MySqlCommand cmd = new MySqlCommand(query, conn))
                        {
                            using (MySqlDataReader reader = cmd.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    try 
                                    {
                                        var thesis = new Theses
                                        {
                                            Id = reader.GetInt32("id"),
                                            Titre = reader.IsDBNull(reader.GetOrdinal("titre")) ? "" : reader.GetString("titre"),
                                            Auteur = reader.IsDBNull(reader.GetOrdinal("auteur")) ? "" : reader.GetString("auteur"),
                                            Speciality = reader.IsDBNull(reader.GetOrdinal("speciality")) ? "" : reader.GetString("speciality"),
                                            Type = (TypeThese)Enum.Parse(typeof(TypeThese), reader.GetString("Type")),
                                            MotsCles = reader.IsDBNull(reader.GetOrdinal("MotsCles")) ? "" : reader.GetString("MotsCles"),
                                            Annee = reader.GetDateTime("annee"),
                                            Resume = reader.IsDBNull(reader.GetOrdinal("Resume")) ? "" : reader.GetString("Resume"),
                                            Fichier = reader.IsDBNull(reader.GetOrdinal("fichier")) ? "" : reader.GetString("fichier"),
                                            UserId = reader.GetInt32("UserId")
                                        };
                                        allTheses.Add(thesis);
                                    }
                                    catch (Exception ex)
                                    {
                                        Debug.WriteLine($"Error reading thesis record: {ex.Message}");
                                        // Continue processing other records
                                    }
                                }
                            }
                        }
                    }
                }
                catch (MySqlException sqlEx)
                {
                    Debug.WriteLine($"SQL Error: {sqlEx.Message} (Error code: {sqlEx.Number})");
                    MessageBox.Show($"Database error: {sqlEx.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Initialize with empty collection if nothing was loaded
                if (allTheses == null)
                {
                    allTheses = new ObservableCollection<Theses>();
                }

                totalItems = allTheses.Count;
                totalPages = Math.Max(1, (int)Math.Ceiling((double)totalItems / itemsPerPage));

                thesesViewSource = new CollectionViewSource { Source = allTheses };
                thesesViewSource.Filter += ThesesViewSource_Filter;

                // Populate Year Filter ComboBox with available years
                if (YearFilterComboBox != null)
                {
                    YearFilterComboBox.Items.Clear();
                    YearFilterComboBox.Items.Add(new ComboBoxItem { Content = "All Years", IsSelected = true });
                    
                    var years = allTheses
                        .Select(t => t.Annee.Year)
                        .Distinct()
                        .OrderByDescending(y => y);
                        
                    foreach (var year in years)
                    {
                        YearFilterComboBox.Items.Add(new ComboBoxItem { Content = year.ToString() });
                    }
                }

                ApplyPagination();
                UpdatePaginationControls();

                // Update the UI with loaded data count
                Debug.WriteLine($"Loaded {allTheses.Count} theses successfully");
                UpdateThesisCounter();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Critical error in LoadTheses: {ex.Message}\n{ex.StackTrace}");
                MessageBox.Show($"Error loading theses: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                
                // Ensure we have an empty collection to avoid null reference exceptions
                if (allTheses == null)
                {
                    allTheses = new ObservableCollection<Theses>();
                }
                
                // Update UI with empty data
                totalItems = 0;
                totalPages = 1;
                currentPage = 1;
                
                if (ThesisDataGrid != null)
                {
                    ThesisDataGrid.ItemsSource = allTheses;
                }
                
                UpdatePaginationControls();
            }
        }

        private bool TestDatabaseConnection()
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(AppConfig.CloudSqlConnectionString))
                {
                    conn.Open();
                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Database connection test failed: {ex.Message}");
                return false;
            }
        }

        private void ApplyPagination()
        {
            try
            {
                if (filteredTheses == null)
                {
                    // If filtered collection doesn't exist, create it from all theses with current filters
                    ApplyFilters();
                    return;
                }

                if (filteredTheses.Count == 0)
                {
                    ThesisDataGrid.ItemsSource = new List<Theses>();
                    return;
                }

                int skipCount = (currentPage - 1) * itemsPerPage;
                
                // Ensure skip count is valid
                if (skipCount >= filteredTheses.Count)
                {
                    currentPage = 1;
                    skipCount = 0;
                }
                
                var currentPageItems = filteredTheses
                    .Skip(skipCount)
                    .Take(itemsPerPage)
                    .ToList();

                ThesisDataGrid.ItemsSource = currentPageItems;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error applying pagination: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                ThesisDataGrid.ItemsSource = new List<Theses>();
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

        private void YearFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Only apply filters if the control is fully loaded
            if (IsLoaded && YearFilterComboBox != null)
        {
            ApplyFilters();
            }
        }

        private void ApplyFilters()
        {
            try 
            {
                // Check if any UI elements are null (could happen if called during initialization)
                if (SearchTextBox == null || TypeFilterComboBox == null || YearFilterComboBox == null || ThesisDataGrid == null)
                {
                    Debug.WriteLine("ApplyFilters called before UI elements were initialized.");
                    return;
                }

                // Store current filter values
                currentSearchText = SearchTextBox.Text?.ToLower() ?? "";
                
                // Safely get Type filter
                if (TypeFilterComboBox.SelectedItem != null && TypeFilterComboBox.SelectedItem is ComboBoxItem typeItem)
                {
                    currentTypeFilter = typeItem.Content?.ToString() ?? "All Types";
                }
                
                // Safely get Year filter
                if (YearFilterComboBox.SelectedItem != null && YearFilterComboBox.SelectedItem is ComboBoxItem yearItem)
                {
                    currentYearFilter = yearItem.Content?.ToString() ?? "All Years";
                }

                if (allTheses == null)
                {
                    Debug.WriteLine("ApplyFilters called when allTheses was null");
                    ThesisDataGrid.ItemsSource = new List<Theses>();
                    return;
                }

                if (allTheses.Count == 0)
                {
                    ThesisDataGrid.ItemsSource = new List<Theses>();
                    return;
                }

                // Apply all filters to get filtered collection
                filteredTheses = new ObservableCollection<Theses>(allTheses.Where(t =>
                    // Search text filter
                    (string.IsNullOrEmpty(currentSearchText) ||
                     (t.Titre != null && t.Titre.ToLower().Contains(currentSearchText)) ||
                     (t.Auteur != null && t.Auteur.ToLower().Contains(currentSearchText)) ||
                     (t.MotsCles != null && t.MotsCles.ToLower().Contains(currentSearchText))) &&
                    // Type filter
                    (currentTypeFilter == "All Types" || t.Type.ToString() == currentTypeFilter) &&
                    // Year filter
                    (currentYearFilter == "All Years" || t.Annee.Year.ToString() == currentYearFilter)
                ));

                totalItems = filteredTheses.Count;
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
                if (allTheses != null && ThesisDataGrid != null)
                {
                    ThesisDataGrid.ItemsSource = allTheses.Take(itemsPerPage).ToList();
                }
            }
        }

        private void ThesesViewSource_Filter(object sender, FilterEventArgs e)
        {
            try 
            {
                if (e.Item == null)
                {
                    e.Accepted = false;
                    return;
                }
                
                if (SearchTextBox == null || TypeFilterComboBox == null || YearFilterComboBox == null)
                {
                    // UI elements not initialized yet
                    e.Accepted = true;  // Show everything by default
                    return;
                }

                if (e.Item is Theses thesis)
                {
                    string searchText = SearchTextBox.Text?.ToLower() ?? "";
                    
                    // Safely get Type filter
                    string typeFilter = "All Types";
                    if (TypeFilterComboBox.SelectedItem != null && TypeFilterComboBox.SelectedItem is ComboBoxItem typeItem)
                    {
                        typeFilter = typeItem.Content?.ToString() ?? "All Types";
                    }
                    
                    // Safely get Year filter
                    string yearFilter = "All Years";
                    if (YearFilterComboBox.SelectedItem != null && YearFilterComboBox.SelectedItem is ComboBoxItem yearItem)
                    {
                        yearFilter = yearItem.Content?.ToString() ?? "All Years";
                    }

                    bool matchesSearch = string.IsNullOrEmpty(searchText) ||
                                        (thesis.Titre != null && thesis.Titre.ToLower().Contains(searchText)) ||
                                        (thesis.Auteur != null && thesis.Auteur.ToLower().Contains(searchText)) ||
                                        (thesis.MotsCles != null && thesis.MotsCles.ToLower().Contains(searchText));

                    bool matchesType = typeFilter == "All Types" || thesis.Type.ToString() == typeFilter;
                    
                    bool matchesYear = yearFilter == "All Years" || thesis.Annee.Year.ToString() == yearFilter;

                    e.Accepted = matchesSearch && matchesType && matchesYear;
                }
                else
                {
                    // Not a Theses object
                    e.Accepted = false;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in filter: {ex.Message}");
                // Show the item by default if there's an error
                e.Accepted = true;
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            // Clear filters before refreshing to get fresh data
            if (SearchTextBox != null) SearchTextBox.Clear();
            if (TypeFilterComboBox != null) TypeFilterComboBox.SelectedIndex = 0;
            if (YearFilterComboBox != null) YearFilterComboBox.SelectedIndex = 0;
            
            // Reset filter tracking variables
            currentSearchText = "";
            currentTypeFilter = "All Types";
            currentYearFilter = "All Years";
            
            // Reload data
            LoadTheses();
        }

        private void FilterRefreshButton_Click(object sender, RoutedEventArgs e)
        {
            // Both refresh buttons should do the same thing for consistency
            RefreshButton_Click(sender, e);
        }

        private void AddThesisButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var addThesisWindow = new AddThesisWindow();
                addThesisWindow.Owner = Window.GetWindow(this);
                addThesisWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                
                // When the window is closed, check if a thesis was added successfully
                if (addThesisWindow.ShowDialog() == true)
                {
                    // Refresh the thesis list
                    LoadTheses();
                        MessageBox.Show("Thesis added successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error opening Add Thesis window: {ex.Message}");
                MessageBox.Show($"Error opening Add Thesis window: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ViewDetailsButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Theses thesis)
            {
                try
                {
                    // Create a window to display thesis details
                    var detailsWindow = new Window
                    {
                        Title = "Thesis Details",
                        Width = 750,
                        Height = 600,
                        WindowStartupLocation = WindowStartupLocation.CenterScreen,
                        ResizeMode = ResizeMode.NoResize,
                        Background = new SolidColorBrush(Colors.White)
                    };

                    // Create the content
                    var scrollViewer = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
                    var mainGrid = new Grid { Margin = new Thickness(0) };

                    mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Header
                    mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Content
                    mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Buttons

                    // Header with thesis title
                    var headerBorder = new Border
                    {
                        Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#008080")),
                        Padding = new Thickness(25, 20, 25, 20),
                    };

                    var headerText = new TextBlock
                    {
                        Text = thesis.Titre ?? "Thesis Details",
                        Foreground = Brushes.White,
                        FontSize = 22,
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
                        BorderThickness = new Thickness(1, 0, 1, 1),
                        Padding = new Thickness(25)
                    };

                    var contentGrid = new Grid();
                    contentGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Author label
                    contentGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Author value
                    contentGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Specialty label
                    contentGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Specialty value
                    contentGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Type label
                    contentGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Type value
                    contentGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Keywords label
                    contentGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Keywords value
                    contentGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Year label
                    contentGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Year value
                    contentGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Abstract label
                    contentGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Abstract value

                    // Author
                    AddDetailField(contentGrid, 0, "Author", thesis.Auteur ?? "N/A");

                    // Specialty
                    AddDetailField(contentGrid, 2, "Specialty", thesis.Speciality ?? "N/A");

                    // Type
                    AddDetailField(contentGrid, 4, "Type", thesis.Type.ToString());

                    // Keywords
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

                    // Abstract
                    AddDetailField(contentGrid, 10, "Abstract", thesis.Resume ?? "N/A", true);

                    contentBorder.Child = contentGrid;
                    Grid.SetRow(contentBorder, 1);
                    mainGrid.Children.Add(contentBorder);

                    // Buttons panel
                    var buttonsPanel = new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        HorizontalAlignment = HorizontalAlignment.Right,
                        Margin = new Thickness(0, 20, 25, 20)
                    };

                    // View PDF button (only if there's a file path)
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
                            BorderThickness = new Thickness(0),
                            Style = (Style)FindResource("ActionButtonStyle"),
                            Tag = thesis
                        };
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

                    Grid.SetRow(buttonsPanel, 2);
                    mainGrid.Children.Add(buttonsPanel);

                    scrollViewer.Content = mainGrid;
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

        private async void ViewPdfButton_Click(object sender, RoutedEventArgs e)
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

                    Debug.WriteLine($"Attempting to open PDF document with object name: {thesis.Fichier}");
                    this.Cursor = Cursors.Wait;

                    // Use CloudStorageService to get a signed URL for the file
                    var cloudStorageService = new DataGridNamespace.Services.CloudStorageService();
                    string signedUrl = await cloudStorageService.GetSignedReadUrl(thesis.Fichier);
                    
                    if (string.IsNullOrEmpty(signedUrl))
                    {
                        Debug.WriteLine("Failed to generate signed URL for PDF document");
                        MessageBox.Show("Could not generate a download URL for this file.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        this.Cursor = Cursors.Arrow;
                        return;
                    }

                    Debug.WriteLine($"Successfully generated signed URL: {signedUrl}");

                    // Open the signed URL in the default browser
                    try
                    {
                        // Use ProcessStartInfo with UseShellExecute set to true to open URL in default browser
                        var processStartInfo = new ProcessStartInfo
                        {
                            FileName = signedUrl,
                            UseShellExecute = true
                        };
                        Process.Start(processStartInfo);
                        Debug.WriteLine("Successfully launched browser with signed URL");
                    }
                    catch (System.ComponentModel.Win32Exception win32Ex)
                    {
                        Debug.WriteLine($"Win32 error opening PDF URL: {win32Ex.Message} (Error code: {win32Ex.NativeErrorCode})");
                        
                        // Try an alternative approach if the first method fails
                        try
                        {
                            var baseUri = new Uri(signedUrl);
                            Debug.WriteLine($"Attempting to open URL using alternative approach: {baseUri}");
                            
                            // On Windows 10, this alternative method may work better in some cases
                            Process.Start(new ProcessStartInfo
                            {
                                FileName = "cmd",
                                Arguments = $"/c start \"\" \"{signedUrl}\"",
                                CreateNoWindow = true
                            });
                            Debug.WriteLine("Successfully launched using cmd start command");
                        }
                        catch (Exception altEx)
                        {
                            Debug.WriteLine($"Alternative method also failed: {altEx.Message}");
                            MessageBox.Show("Unable to open the PDF file. Please copy the URL and open it manually in your browser.", 
                                          "Browser Launch Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            
                            // Offer to copy the URL to clipboard
                            var clipboardResult = MessageBox.Show("Would you like to copy the URL to your clipboard?", 
                                                               "Copy URL", MessageBoxButton.YesNo, MessageBoxImage.Question);
                            if (clipboardResult == MessageBoxResult.Yes)
                            {
                                System.Windows.Clipboard.SetText(signedUrl);
                                MessageBox.Show("URL copied to clipboard. You can paste it into your browser.", 
                                              "URL Copied", MessageBoxButton.OK, MessageBoxImage.Information);
                            }
                        }
                    }
                    this.Cursor = Cursors.Arrow;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error opening PDF file: {ex.Message}\nStack trace: {ex.StackTrace}");
                    MessageBox.Show($"Error opening PDF file: {ex.Message}", 
                                  "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    this.Cursor = Cursors.Arrow;
                }
            }
            else
            {
                MessageBox.Show("Cannot open PDF. The thesis information may be incomplete.", 
                              "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void FavoriteButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Theses thesis)
            {
                try
                {
                    // Get current user ID from session
                    int currentUserId = DataGridNamespace.Session.CurrentUserId;

                    // Check if this thesis is already in favorites for this user
                    bool isAlreadyInFavorites = false;
                    string connectionString = AppConfig.CloudSqlConnectionString;

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
                    Debug.WriteLine($"Error managing favorites: {ex.Message}");
                    MessageBox.Show($"Error managing favorites: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Cannot manage favorites. The thesis information may be incomplete.", 
                              "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Theses thesis)
            {
                try
                {
                    var result = MessageBox.Show($"Are you sure you want to delete '{thesis.Titre}'? This action cannot be undone.", 
                                               "Confirm Deletion", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    
                    if (result == MessageBoxResult.Yes)
                    {
                        string connectionString = AppConfig.CloudSqlConnectionString;
                        using (MySqlConnection conn = new MySqlConnection(connectionString))
                        {
                            conn.Open();
                            
                            // First, delete from favorites if exists
                            string deleteFavoritesQuery = "DELETE FROM favoris WHERE these_id = @thesisId";
                            using (MySqlCommand cmd = new MySqlCommand(deleteFavoritesQuery, conn))
                            {
                                cmd.Parameters.AddWithValue("@thesisId", thesis.Id);
                                cmd.ExecuteNonQuery();
                            }
                            
                            // Then delete the thesis
                            string deleteThesisQuery = "DELETE FROM theses WHERE id = @id";
                            using (MySqlCommand cmd = new MySqlCommand(deleteThesisQuery, conn))
                            {
                                cmd.Parameters.AddWithValue("@id", thesis.Id);
                                int rowsAffected = cmd.ExecuteNonQuery();
                                
                                if (rowsAffected > 0)
                                {
                                    // Remove from the collection and refresh the UI
                                    allTheses.Remove(thesis);
                                    
                                    // Recalculate pagination
                                    totalItems = allTheses.Count;
                                    totalPages = Math.Max(1, (int)Math.Ceiling((double)totalItems / itemsPerPage));
                                    
                                    // Adjust current page if needed
                                    if (currentPage > totalPages)
                                    {
                                        currentPage = totalPages;
                                    }
                                    
                                    // Apply pagination and update controls
                                    ApplyPagination();
                                    UpdatePaginationControls();
                                    
                                    MessageBox.Show("Thesis deleted successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                                }
                                else
                                {
                                    MessageBox.Show("Failed to delete thesis. Please try again.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error deleting thesis: {ex.Message}");
                    MessageBox.Show($"Error deleting thesis: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Cannot delete thesis. The thesis information may be incomplete.", 
                              "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ThesisDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // This method can be used to update UI elements based on selection
        }

        private void UpdateThesisCounter()
        {
            if (ThesisCounterText != null)
            {
                ThesisCounterText.Text = $"({totalItems} theses)";
            }
        }
        
        private void SendMessageButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Get the thesis object from the button tag
                if (sender is Button button && button.Tag is Theses thesis)
                {
                    // Create and show the message window
                    var messageWindow = new MessageWindow();
                    messageWindow.Owner = Window.GetWindow(this);
                    messageWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    messageWindow.SetThesis(thesis.Id, thesis.Titre);
                    
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