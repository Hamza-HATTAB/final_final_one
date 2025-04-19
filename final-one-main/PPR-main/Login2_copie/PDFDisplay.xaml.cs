using System;
using System.Windows;
using System.Windows.Controls;
using System.Diagnostics;
using System.IO;

namespace DataGridNamespace
{
    /// <summary>
    /// Interaction logic for PDFDisplay.xaml
    /// </summary>
    public partial class PDFDisplay : UserControl
    {
        public PDFDisplay()
        {
            InitializeComponent();
        }

        private void ViewPdfButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Button button = (Button)sender;
                string pdfPath = button.Tag?.ToString();
                
                if (string.IsNullOrEmpty(pdfPath))
                {
                    MessageBox.Show("PDF path is not specified.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Ensure the file exists before trying to open it
                if (!File.Exists(pdfPath))
                {
                    MessageBox.Show($"PDF file not found at: {pdfPath}", "File Not Found", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Try to open the PDF with the default PDF viewer
                var startInfo = new ProcessStartInfo
                {
                    FileName = pdfPath,
                    UseShellExecute = true
                };
                
                Process.Start(startInfo);
                Debug.WriteLine($"Successfully opened PDF: {pdfPath}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error opening PDF: {ex.Message}");
                MessageBox.Show($"Cannot open the PDF file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
} 