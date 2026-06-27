using Mail_Data_Access;
using Mail_Manager.Models;
using Mail_Manager.Services;
using System.Windows;
using System.Windows.Input;


namespace Mail_Manager
{
    public partial class LoginWindow : Window
    {
        MailDbContext db;
        public LoginWindow()
        {
            InitializeComponent();

            db = new MailDbContext();
            // хардкод credentials для зручності тестування — не треба вводити щоразу вручну
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
            // спочатку перевіряю в БД — тільки зареєстровані юзери можуть підключитись
            // якщо null — далі навіть не намагаюсь стукати в IMAP
            var user = db.Users.Where((user) => user.Mail == email && user.Password == password).FirstOrDefault();

            if (user != null)
            {
                var imap = new ImapService();
                try
                {
                    this.IsEnabled = false;
                    Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;

                    await imap.ConnectAsync(email, password);

                    // зберігаю в SessionState після успішного connect —
                    // тільки тут знаю, що credentials валідні і з'єднання живе
                    SessionState.Email = email;
                    SessionState.Password = password;

                    var main = new MainWindow(imap);
                    main.Show();
                    this.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error connecting to the server.\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    this.IsEnabled = true;
                    Mouse.OverrideCursor = null;
                }
            }
            else { MessageBox.Show($"Login failed.\nThere is no user with this email address.", "Error", MessageBoxButton.OK, MessageBoxImage.Error); }
        }
    }
}
