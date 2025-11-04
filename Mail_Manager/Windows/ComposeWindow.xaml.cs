using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using MimeKit;
using System.Windows.Input;
using Mail_Manager.Services;


namespace Mail_Manager.Windows
{
    /// <summary>
    /// Interaction logic for ComposeWindow.xaml
    /// </summary>
    public partial class ComposeWindow : Window
    {
        private readonly List<string> _attachments = new();

        public ComposeWindow(string to = "", string subject = "")
        {
            InitializeComponent();
            txtTo.Text = to;
            txtSubject.Text = subject;
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void BtnAttach_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Multiselect = true,
                Title = "Select files to attach"
            };
            if (dlg.ShowDialog(this) == true)
            {
                foreach (var file in dlg.FileNames)
                {
                    if (File.Exists(file))
                        _attachments.Add(file);
                }
                MessageBox.Show($"{_attachments.Count} attachment(s) selected.", "Attachments", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private async void BtnSend_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var from = "lenailyshun@gmail.com";
                var password = "dqmq yyqu uxfb ikfc";



                var to = txtTo.Text.Trim();
                if (string.IsNullOrWhiteSpace(to))
                {
                    MessageBox.Show("Recipient (To) is required.", "Validation", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var message = new MimeMessage();
                message.From.Add(MailboxAddress.Parse(from));
                message.To.Add(MailboxAddress.Parse(to));
                message.Subject = txtSubject.Text;

                var builder = new BodyBuilder
                {
                    TextBody = txtBody.Text
                };

                foreach (var path in _attachments)
                {
                    try
                    {
                        builder.Attachments.Add(path);
                    }
                    catch { }
                }

                message.Body = builder.ToMessageBody();

                this.IsEnabled = false;
                

                await SmtpService.SendAsync(from, password, message);

                MessageBox.Show("Message sent.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to send message.\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                this.IsEnabled = true;
                
            }
        }
    }
}
