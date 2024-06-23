using System;
using System.Windows;
using System.Windows.Controls;

namespace WPF_UI
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Loaded += Window_Loaded;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ShowDashboard();
        }

        private void HomeButton_Click(object sender, RoutedEventArgs e)
        {
            ShowDashboard();
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void ShowDashboard()
        {
            var dashboard = new WPF_UI.Dashboard();
            MainContent.Content = dashboard;
        }

    }
}
