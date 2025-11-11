using Mail_Manager.Models;
using Mail_Manager.Services;
using Org.BouncyCastle.Crypto;
using System.Windows;
using System.Windows.Input;


namespace Mail_Manager
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();


            Email.Text = "lenailyshun@gmail.com";
            Password.Text = "dqmq yyqu uxfb ikfc";
        }

        private async void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            var email = Email.Text.Trim();
            var password = Password.Text;

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Please enter both Email and Password.", "Validation", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var imap = new ImapService();
            try
            {
                this.IsEnabled = false;
                Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;

                await imap.ConnectAsync(email, password);


                SessionState.Email = email;
                SessionState.Password = password;

                var main = new MainWindow(imap);
                main.Show();
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Login failed.\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                this.IsEnabled = true;
                Mouse.OverrideCursor = null;
            }
        }
    }
}
