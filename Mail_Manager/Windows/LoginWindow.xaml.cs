using System.Windows;

namespace Mail_Manager
{
    /// <summary>
    /// Interaction logic for LoginWindow.xaml
    /// </summary>
    public partial class LoginWindow : Window
    {
        public string? TextEmail { get; set; }
        public string? TextPassword { get; set; }
        public LoginWindow()
        {
            InitializeComponent();

            Email.Text = "lenailyshun@gmail.com";
            Password.Text = "hadv dehd bsdf ocye";
        }
        private void ToConfirm(object sender, RoutedEventArgs e)
        {
            if (Email.Text == "lenailyshun@gmail.com" && Password.Text == "hadv dehd bsdf ocye")
            {
                TextEmail = Email.Text;
                TextPassword = Password.Text;
                Close();
            }
            else { MessageBox.Show("Incorrect email address or password"); }
        }
    }
}
