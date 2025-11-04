using Mail_Manager.Windows;
using System.Windows;

namespace Mail_Manager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string? email;
        string? password;
        public MainWindow()
        {
            InitializeComponent();
            LoginWindow loginWindow = new LoginWindow();
            loginWindow.ShowDialog();
            email = loginWindow.TextEmail;
            password = loginWindow.TextPassword;
            if (email == null || password == null) { Close(); }
        }
        private void BtnNew_Click(object sender, RoutedEventArgs e)
        {
            var compose = new ComposeWindow();
            compose.Owner = this;
            compose.ShowDialog();
        }





    }





}