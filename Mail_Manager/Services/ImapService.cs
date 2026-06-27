using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MailKit.Security;
using MimeKit;

namespace Mail_Manager.Services
{
    // instance-клас, бо IMAP — stateful протокол: одне підключення живе весь сеанс, ре-коннект на кожну операцію — це ~1-2 с затримки
    public class ImapService : IAsyncDisposable
    {
        private readonly ImapClient _client = new();
        // надаю доступ до клієнта зовні, бо MainWindow читає Inbox безпосередньо
        public ImapClient Client => _client;



        public async Task ConnectAsync(string email, string password, CancellationToken ct = default)
        {
            // якщо почомусь підключений — відключаюсь спочатку, щоб не злітати повторний connect
            if (_client.IsConnected)
                await _client.DisconnectAsync(true, ct);

            await _client.ConnectAsync("imap.gmail.com", 993, SecureSocketOptions.SslOnConnect, ct);

            //google wtf????
            _client.AuthenticationMechanisms.Remove("XOAUTH2");

            await _client.AuthenticateAsync(email, password, ct);
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                // try/catch тут обов’язковий: dispose не повинен кидати винятки, відрив між операціями — звичайна ситуація
                if (_client.IsConnected)
                    await _client.DisconnectAsync(true);
                _client.Dispose();
            }
            catch
            {

            }
        }


        public async Task<IList<IMailFolder>> GetAllSelectableFoldersAsync(CancellationToken ct = default)
        {
            var result = new List<IMailFolder>();

            // Inbox додаю окремо, бо це спеціальна папка, яка може не потрапити в PersonalNamespaces
            result.Add(_client.Inbox);


            string rootPath = string.Empty;


            if (_client.PersonalNamespaces.Count > 0)
            {
                // отримую корень особистого простору імен (Gmail повертає порожню рядку)
                var ns = _client.PersonalNamespaces[0];


                if (!string.IsNullOrEmpty(ns.Path))
                    rootPath = ns.Path;
            }


            var personal = await _client.GetFolderAsync(rootPath, ct);



            async Task RecurseAsync(IMailFolder folder)
            {
                // NoSelect означає папку-контейнер (не відкривається), такі пропускаю
                if (!folder.Attributes.HasFlag(FolderAttributes.NoSelect))
                    result.Add(folder);


                foreach (var sub in await folder.GetSubfoldersAsync(false, ct))
                {
                    await RecurseAsync(sub);
                }
            }


            foreach (var top in await personal.GetSubfoldersAsync(false, ct))
                await RecurseAsync(top);


            // без DistinctBy Inbox може потрапити двічі: доданий вручну на початку і знову через RecurseAsync
            result = result
                .DistinctBy(f => f.FullName)
                .ToList();

            // відкриваю всі папки ReadOnly сейчас, щоб пізніше GetPageAsync не відкривав кожну заново
            foreach (var f in result)
            {
                if (!f.IsOpen)
                {
                    try
                    {
                        await f.OpenAsync(FolderAccess.ReadOnly, ct);
                    }
                    catch
                    {

                    }
                }
            }

            return result;
        }

        public static (int start, int end) GetPageRange(int totalCount, int pageIndex, int pageSize)
        {
            // нові листи мають найбільший індекс, тому читаю з кінця масиву:
            // end = total-1 для першої сторінки, total-pageSize-1 для другої і т.d.
            var end = totalCount - pageIndex * pageSize - 1;
            var start = Math.Max(0, end - (pageSize - 1));
            if (end < 0) return (0, -1);
            return (start, end);
        }


        public async Task<IList<Models.EmailItem>> GetPageAsync(
            IMailFolder folder, int pageIndex, int pageSize, CancellationToken ct = default)
        {
            if (!folder.IsOpen)
                await folder.OpenAsync(FolderAccess.ReadOnly, ct);

            int total = folder.Count;
            var (start, end) = GetPageRange(total, pageIndex, pageSize);
            if (end < start) return new List<Models.EmailItem>();

            // завантажую тільки зведення (без тіла!): UniqueId + Envelope + InternalDate —
            // це в десятки разів бюджетніше ніж завантажувати повні повідомлення для списку
            var summaries = await folder.FetchAsync(start, end,
                MessageSummaryItems.UniqueId |
                MessageSummaryItems.Envelope |
                MessageSummaryItems.InternalDate, ct);

            // сортую за Index DESC, щоб новіші листи були зверху (як у звичайних email-клієнтах)
            var items = summaries
                .OrderByDescending(s => s.Index)
                .Select(s => new Models.EmailItem
                {
                    UniqueId = s.UniqueId,
                    From = s.Envelope.From?.ToString() ?? "",
                    Subject = s.Envelope.Subject ?? "",
                    Date = s.InternalDate ?? DateTimeOffset.MinValue
                })
                .ToList();

            return items;
        }


        public async Task<MimeMessage> GetMessageAsync(IMailFolder folder, UniqueId uid, CancellationToken ct = default)
        {
            if (!folder.IsOpen)
                await folder.OpenAsync(FolderAccess.ReadOnly, ct);

            return await folder.GetMessageAsync(uid, ct);
        }


        public async Task<bool> DeleteAsync(IMailFolder folder, UniqueId uid, CancellationToken ct = default)
        {
            // видалення потребує ReadWrite, бо за IMAP-стандартом прапори ставляться тільки в ReadWrite
            await folder.OpenAsync(FolderAccess.ReadWrite, ct);
            await folder.AddFlagsAsync(uid, MessageFlags.Deleted, true, ct);
            // Expunge — фізично видаляє повідомлення з сервера, flag \Deleted сам по собі не видаляє
            await folder.ExpungeAsync(ct);
            await folder.CloseAsync(true, ct);

            // повертаю папку в ReadOnly, щоб подальші операції читання працювали без повторного відкриття
            await folder.OpenAsync(FolderAccess.ReadOnly, ct);
            return true;
        }
    }
}
