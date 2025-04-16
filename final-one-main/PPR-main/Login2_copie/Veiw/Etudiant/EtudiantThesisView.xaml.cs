using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace DataGridNamespace.Etudiant
{
    public partial class EtudiantThesisView : UserControl
    {
        private List<Thesis> theses;

        public EtudiantThesisView()
        {
            InitializeComponent();
            LoadTheses();
        }

        private void LoadTheses()
        {
            try
            {
                // TODO: Load theses from database
                theses = new List<Thesis>();
                ThesisListView.ItemsSource = theses;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading theses: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UploadButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenFileDialog openFileDialog = new OpenFileDialog
                {
                    Filter = "PDF files (*.pdf)|*.pdf|Word files (*.doc;*.docx)|*.doc;*.docx",
                    Title = "Select Thesis File"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    string filePath = openFileDialog.FileName;
                    // TODO: Implement file upload logic here
                    MessageBox.Show($"File selected: {filePath}", "File Selected", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error uploading file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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

            var filteredTheses = theses.FindAll(t =>
                t.Title.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                t.Author.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                t.Year.Contains(searchText, StringComparison.OrdinalIgnoreCase));

            ThesisListView.ItemsSource = filteredTheses;
        }

        private void ViewThesisButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is Thesis thesis)
            {
                // TODO: Open thesis details view
                MessageBox.Show($"Viewing thesis: {thesis.Title}", "Thesis Details", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }

    public class Thesis
    {
        public string Title { get; set; }
        public string Author { get; set; }
        public string Year { get; set; }
    }
} 