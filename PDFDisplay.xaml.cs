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
                    Debug.WriteLine("PDF path is not specified or empty");
                    MessageBox.Show("PDF path is not specified.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                Debug.WriteLine($"Attempting to open PDF at path: {pdfPath}");

                // Ensure the file exists before trying to open it
                if (!File.Exists(pdfPath))
                {
                    Debug.WriteLine($"PDF file not found at path: {pdfPath}");
                    MessageBox.Show($"PDF file not found at: {pdfPath}", "File Not Found", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Try to open the PDF with the default PDF viewer
                var startInfo = new ProcessStartInfo
                {
                    FileName = pdfPath,
                    UseShellExecute = true
                };
                
                try
                {
                    Process.Start(startInfo);
                    Debug.WriteLine($"Successfully opened PDF: {pdfPath}");
                }
                catch (System.ComponentModel.Win32Exception win32Ex) when (win32Ex.NativeErrorCode == 1155 || win32Ex.NativeErrorCode == 1156)
                {
                    // Error codes:
                    // 1155: No application is associated with the specified file for this operation
                    // 1156: The operating system cannot run the specified program
                    
                    Debug.WriteLine($"Win32 error opening PDF file: {win32Ex.Message} (Error code: {win32Ex.NativeErrorCode})");
                    MessageBox.Show("No PDF viewer is installed on your system. Please install a PDF viewer application to open PDF files.",
                                  "PDF Viewer Required", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch (System.ComponentModel.Win32Exception win32Ex)
                {
                    Debug.WriteLine($"Win32 error opening PDF file: {win32Ex.Message} (Error code: {win32Ex.NativeErrorCode})");
                    MessageBox.Show($"Cannot open the PDF file: {win32Ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error opening PDF: {ex.Message}\nStack trace: {ex.StackTrace}");
                MessageBox.Show($"Cannot open the PDF file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
} 