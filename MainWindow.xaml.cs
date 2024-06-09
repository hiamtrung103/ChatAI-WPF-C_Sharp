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
            // Khi cửa sổ được tải, hiển thị Dashboard mặc định
            ShowDashboard();
        }

        private void HomeButton_Click(object sender, RoutedEventArgs e)
        {
            // Hiển thị Dashboard khi nút Home được nhấn
            ShowDashboard();
        }

        private void CreditCardButton_Click(object sender, RoutedEventArgs e)
        {
            // Xử lý hiển thị khi nút Credit Card được nhấn
        }

        private void CalendarButton_Click(object sender, RoutedEventArgs e)
        {
            // Xử lý hiển thị khi nút Calendar được nhấn
        }

        private void EqualiserButton_Click(object sender, RoutedEventArgs e)
        {
            // Xử lý hiển thị khi nút Equaliser được nhấn
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            // Xử lý hiển thị khi nút Settings được nhấn
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            // Đóng ứng dụng khi nút Exit được nhấn
            Application.Current.Shutdown();
        }

        private void ShowDashboard()
        {
            // Tạo một thể hiện của Dashboard và hiển thị trong MainContent
            var dashboard = new WPF_UI.Dashboard();
            MainContent.Content = dashboard;
        }
    }
}
