using MailKit;
using System.Windows;
using System.Windows.Controls;
using MailKit;
using Mail_Manager.Services;
using Mail_Manager.Models;

using Mail_Manager.Windows;

using System.Windows;

namespace Mail_Manager
{
    public partial class MainWindow : Window
    {
        private readonly ImapService _imap;
        private IList<IMailFolder> _folders = new List<IMailFolder>();
        private IMailFolder? _currentFolder;
        private int _pageIndex = 0;
        private const int PageSize = 20;

        public MainWindow(ImapService imap)
        {
            InitializeComponent();
            _imap = imap;
            Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadFoldersAsync();
        }

        private async Task LoadFoldersAsync()
        {
            try
            {
                lstFolders.ItemsSource = null;
                _folders = await _imap.GetAllSelectableFoldersAsync();
                lstFolders.ItemsSource = _folders.OrderBy(f => f.FullName).ToList();


                var inbox = _folders.FirstOrDefault(f => f == _imap.Client.Inbox || f.FullName.Equals("INBOX", StringComparison.OrdinalIgnoreCase));
                if (inbox != null)
                {
                    lstFolders.SelectedItem = inbox;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load folders.\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void LstFolders_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstFolders.SelectedItem is IMailFolder folder)
            {
                _currentFolder = folder;
                _pageIndex = 0;
                txtCurrentFolder.Text = folder.FullName;
                await LoadPageAsync();
            }
        }

        private async Task LoadPageAsync()
        {
            if (_currentFolder == null) return;
            try
            {
                var items = await _imap.GetPageAsync(_currentFolder, _pageIndex, PageSize);
                lstMessages.ItemsSource = items;
                txtPage.Text = (_pageIndex + 1).ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load messages.\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnPrev_Click(object sender, RoutedEventArgs e)
        {
            if (_currentFolder == null) return;
            if (_pageIndex > 0)
            {
                _pageIndex--;
                await LoadPageAsync();
            }
        }

        private async void BtnNext_Click(object sender, RoutedEventArgs e)
        {
            if (_currentFolder == null) return;

            _pageIndex++;
            await LoadPageAsync();
        }

        private async void BtnOpen_Click(object sender, RoutedEventArgs e)
        {
            if (_currentFolder == null) return;
            if (lstMessages.SelectedItem is EmailItem item)
            {
                try
                {
                    var msg = await _imap.GetMessageAsync(_currentFolder, item.UniqueId);
                    var view = new ViewMessageWindow(_imap, _currentFolder, msg, item.UniqueId);
                    view.Owner = this;
                    view.ShowDialog();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to open message.\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Please select a message first.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private async void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (_currentFolder == null) return;
            if (lstMessages.SelectedItem is EmailItem item)
            {
                try
                {
                    await _imap.DeleteAsync(_currentFolder, item.UniqueId);
                    await LoadPageAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to delete message.\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Please select a message to delete.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnNew_Click(object sender, RoutedEventArgs e)
        {
            var compose = new ComposeWindow();
            compose.Owner = this;
            compose.ShowDialog();
        }
    }
}