using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Diagnostics;
using MySql.Data.MySqlClient;
using ThesesModels;
using FavorisModels;
using DataGrid.Models;

namespace DataGridNamespace.Etudiant
{
    public partial class EtudiantFavoritesView : UserControl
    {
        private ObservableCollection<Favoris> favorites;

        public EtudiantFavoritesView()
        {
            InitializeComponent();
            LoadFavorites();
        }

        private void LoadFavorites()
        {
            try
            {
                favorites = new ObservableCollection<Favoris>();
                string connectionString = DatabaseConnection.GetConnectionString();

                // Get current user ID from session
                int currentUserId = DataGridNamespace.Session.CurrentUserId;

                string query = @"SELECT f.id, f.user_id, f.these_id, 
                                t.id as thesis_id, t.titre, t.auteur, t.speciality, t.Type, 
                                t.mots_cles, t.annee, t.Resume, t.fichier, t.user_id as thesis_user_id
                                FROM favoris f
                                INNER JOIN theses t ON f.these_id = t.id
                                WHERE f.user_id = @userId
                                ORDER BY f.id DESC";

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
                                    int thesisId = reader.GetInt32("thesis_id");
                                    string titre = reader.IsDBNull(reader.GetOrdinal("titre")) ? string.Empty : reader.GetString("titre");
                                    string auteur = reader.IsDBNull(reader.GetOrdinal("auteur")) ? string.Empty : reader.GetString("auteur");
                                    string specialite = reader.IsDBNull(reader.GetOrdinal("speciality")) ? string.Empty : reader.GetString("speciality");
                                    string typeStr = reader.IsDBNull(reader.GetOrdinal("Type")) ? "Doctorat" : reader.GetString("Type");
                                    string motsCles = reader.IsDBNull(reader.GetOrdinal("mots_cles")) ? string.Empty : reader.GetString("mots_cles");
                                    DateTime annee = reader.IsDBNull(reader.GetOrdinal("annee")) ? DateTime.Now : reader.GetDateTime("annee");
                                    string resume = reader.IsDBNull(reader.GetOrdinal("Resume")) ? string.Empty : reader.GetString("Resume");
                                    string fichier = reader.IsDBNull(reader.GetOrdinal("fichier")) ? string.Empty : reader.GetString("fichier");
                                    int thesisUserId = reader.IsDBNull(reader.GetOrdinal("thesis_user_id")) ? 0 : reader.GetInt32("thesis_user_id");

                                    int favoriteId = reader.GetInt32("id");
                                    int userId = reader.GetInt32("user_id");
                                    int theseId = reader.GetInt32("these_id");

                                    // Parse enum safely
                                    TypeThese type;
                                    if (!Enum.TryParse(typeStr, out type))
                                    {
                                        type = TypeThese.Doctorat; // Default
                                        Debug.WriteLine($"Failed to parse Type enum value: {typeStr}, defaulting to Doctorat");
                                    }

                                    var thesis = new Theses
                                    {
                                        Id = thesisId,
                                        Titre = titre,
                                        Auteur = auteur,
                                        Specialite = specialite,
                                        Type = type,
                                        MotsCles = motsCles,
                                        Annee = annee,
                                        Resume = resume,
                                        Fichier = fichier,
                                        UserId = thesisUserId
                                    };

                                    var favorite = new Favoris
                                    {
                                        Id = favoriteId,
                                        UserId = userId,
                                        TheseId = theseId,
                                        These = thesis
                                    };

                                    favorites.Add(favorite);
                                }
                                catch (Exception ex)
                                {
                                    Debug.WriteLine($"Error reading favorite record: {ex.Message}");
                                    // Continue with next record
                                }
                            }
                        }
                    }
                }

                FavoritesListView.ItemsSource = favorites;
                Debug.WriteLine($"Loaded {favorites.Count} favorites successfully");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading favorites: {ex.Message}");
                MessageBox.Show($"Error loading favorites: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ViewItemButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is Favoris favorite && favorite.These != null)
            {
                MessageBox.Show($"Viewing thesis: {favorite.These.Titre}\nAuthor: {favorite.These.Auteur}\n" +
                              $"Abstract: {favorite.These.Resume}", 
                              "Thesis Details", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Cannot display thesis details. The favorite or thesis may be null.", 
                                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RemoveFavoriteButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is Favoris favorite && favorite.These != null)
            {
                var result = MessageBox.Show($"Are you sure you want to remove {favorite.These.Titre} from favorites?", 
                                          "Remove Favorite", 
                                          MessageBoxButton.YesNo, 
                                          MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try 
                    {
                        string connectionString = DatabaseConnection.GetConnectionString();
                        using (MySqlConnection conn = new MySqlConnection(connectionString))
                        {
                            conn.Open();
                            string deleteQuery = "DELETE FROM favoris WHERE id = @id";
                            using (MySqlCommand cmd = new MySqlCommand(deleteQuery, conn))
                            {
                                cmd.Parameters.AddWithValue("@id", favorite.Id);
                                int rowsAffected = cmd.ExecuteNonQuery();
                                
                                if (rowsAffected > 0)
                                {
                                    favorites.Remove(favorite);
                                    MessageBox.Show("Favorite removed successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
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
                MessageBox.Show("Cannot remove favorite. The item may be null or invalid.", 
                                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
} 