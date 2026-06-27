using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace Mail_Manager.Services
{
    // static, бо SMTP — транзакційний протокол: підключився → надіслав → відключився
    // постійне з'єднання тут не потрібне, тому не варто тримати instance як у ImapService
    public static class SmtpService
    {
        public static async Task SendAsync(string fromEmail, string password, MimeMessage message, CancellationToken ct = default)
        {
            using var smtp = new SmtpClient();
            // 587 + StartTls — стандартний Gmail SMTP (на відміну від IMAP, де 993/SSL)
            await smtp.ConnectAsync("smtp.gmail.com", 587, SecureSocketOptions.StartTls, ct);
            // те саме що й в IMAP: прибираю XOAUTH2, бо Google пропонує його першим, а він не підходить для App Password
            smtp.AuthenticationMechanisms.Remove("XOAUTH2");
            await smtp.AuthenticateAsync(fromEmail, password, ct);
            await smtp.SendAsync(message, ct);
            await smtp.DisconnectAsync(true, ct);
        }
    }
}
