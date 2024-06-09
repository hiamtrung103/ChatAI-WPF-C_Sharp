using System.Windows;

namespace WPF_UI
{
    public partial class InputDialog : Window
    {
        public string ResponseText { get; private set; }

        public InputDialog(string title, string defaultText = "")
        {
            InitializeComponent();
            Title = title;
            ResponseTextBox.Text = defaultText;
        }

        public void OKButton_Click(object sender, RoutedEventArgs e)
        {
            ResponseText = ResponseTextBox.Text;
            DialogResult = true;
        }
    }
}
