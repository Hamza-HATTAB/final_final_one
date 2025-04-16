using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using MySql.Data.MySqlClient;
using ThesesModels;
using FavorisModels;
using DataGrid.Models;

namespace DataGridNamespace.SimpleUser
{
    public partial class SimpleUserFavoritesView : UserControl
    {
        private ObservableCollection<Favoris> favorites;

        public SimpleUserFavoritesView()
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
                                var thesis = new Theses
                                {
                                    Id = reader.GetInt32("thesis_id"),
                                    Titre = reader.GetString("titre"),
                                    Auteur = reader.GetString("auteur"),
                                    Specialite = reader.GetString("speciality"),
                                    Type = (TypeThese)Enum.Parse(typeof(TypeThese), reader.GetString("Type")),
                                    MotsCles = reader.GetString("mots_cles"),
                                    Annee = reader.GetDateTime("annee"),
                                    Resume = reader.GetString("Resume"),
                                    Fichier = reader.GetString("fichier"),
                                    UserId = reader.GetInt32("thesis_user_id")
                                };

                                var favorite = new Favoris
                                {
                                    Id = reader.GetInt32("id"),
                                    UserId = reader.GetInt32("user_id"),
                                    TheseId = reader.GetInt32("these_id"),
                                    These = thesis
                                };

                                favorites.Add(favorite);
                            }
                        }
                    }
                }

                FavoritesListView.ItemsSource = favorites;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading favorites: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ViewItemButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is Favoris favorite)
            {
                MessageBox.Show($"Viewing thesis: {favorite.These.Titre}\nAuthor: {favorite.These.Auteur}\n" +
                              $"Abstract: {favorite.These.Resume}",
                              "Thesis Details", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void RemoveFavoriteButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is Favoris favorite)
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
                        MessageBox.Show($"Error removing favorite: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }
    }
}