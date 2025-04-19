using System.Windows;

namespace DataGridNamespace
{
    public partial class LogoutConfirmationWindow : Window
    {
        public bool IsConfirmed { get; private set; } = false;

        public LogoutConfirmationWindow()
        {
            InitializeComponent();
        }

        private void YesButton_Click(object sender, RoutedEventArgs e)
        {
            IsConfirmed = true;
            this.DialogResult = true;  // Close the window with a positive result
            this.Close();
        }

        private void NoButton_Click(object sender, RoutedEventArgs e)
        {
            IsConfirmed = false;
            this.DialogResult = false; // Close the window with a negative result
            this.Close();
        }
    }
}
