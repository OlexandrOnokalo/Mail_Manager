
﻿using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MailKit.Security;
using MimeKit;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

﻿using Mail_Manager.Windows;

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
        const string mailServer = "imap.gmail.com";
        const int port = 993;
        ImapClient client;


        ObservableCollection<MimeMessage> msgTemp = new ObservableCollection<MimeMessage>();
        public MainWindow()
        {
            InitializeComponent();
            msgTemp = new ObservableCollection<MimeMessage>();
            this.DataContext = msgTemp;
            LoginWindow loginWindow = new LoginWindow();
            loginWindow.ShowDialog();
            email = loginWindow.TextEmail;
            password = loginWindow.TextPassword;
            client = new ImapClient();
            if (email == null || password == null) { Close(); }
            client.Connect(mailServer, port, SecureSocketOptions.SslOnConnect);
            client.Authenticate(email, password);
            //this.DataContext = messages;


            foreach (var item in client.GetFolders(client.PersonalNamespaces[0]))
            {
                FolderBox.Items.Add(item);
            }
        }

        private void FolderBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            GetFolders();

        }
        private async void GetFolders()
        {
            msgTemp.Clear();
            var folder = FolderBox.SelectedItem as IMailFolder;
            
            var temp = client.GetFolder(folder?.FullName);
            temp.Open(FolderAccess.ReadOnly);
             await getMessagesAsync(temp);
        }
        private  Task getMessagesAsync(IMailFolder folder)
        {
            return Task.Run(() => {
                var idcollection = folder?.Search(SearchQuery.All);
                foreach (var item in idcollection)
                {
                    var ms = folder?.GetMessage(item);
                    //messages.Add(new MyMessage(ms.MessageId, ms.Subject, ms.Date));
                    Application.Current.Dispatcher.Invoke(() => {
                        msgTemp.Add(ms);
                        
                    });
                }
            });
        }
        private void BtnNew_Click(object sender, RoutedEventArgs e)
        {
            var compose = new ComposeWindow();
            compose.Owner = this;
            compose.ShowDialog();
        }
    }
   





}