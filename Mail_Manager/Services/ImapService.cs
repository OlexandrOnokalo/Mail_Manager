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
    public class ImapService : IAsyncDisposable
    {
        private readonly ImapClient _client = new();
        public ImapClient Client => _client;



        public async Task ConnectAsync(string email, string password, CancellationToken ct = default)
        {
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

            result.Add(_client.Inbox);


            string rootPath = string.Empty;


            if (_client.PersonalNamespaces.Count > 0)
            {

                var ns = _client.PersonalNamespaces[0];


                if (!string.IsNullOrEmpty(ns.Path))
                    rootPath = ns.Path;
            }


            var personal = await _client.GetFolderAsync(rootPath, ct);



            async Task RecurseAsync(IMailFolder folder)
            {

                if (!folder.Attributes.HasFlag(FolderAttributes.NoSelect))
                    result.Add(folder);


                foreach (var sub in await folder.GetSubfoldersAsync(false, ct))
                {
                    await RecurseAsync(sub);
                }
            }


            foreach (var top in await personal.GetSubfoldersAsync(false, ct))
                await RecurseAsync(top);


            result = result
                .DistinctBy(f => f.FullName)
                .ToList();

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


            var summaries = await folder.FetchAsync(start, end,
                MessageSummaryItems.UniqueId |
                MessageSummaryItems.Envelope |
                MessageSummaryItems.InternalDate, ct);


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
            await folder.OpenAsync(FolderAccess.ReadWrite, ct);
            await folder.AddFlagsAsync(uid, MessageFlags.Deleted, true, ct);
            await folder.ExpungeAsync(ct);
            await folder.CloseAsync(true, ct);


            await folder.OpenAsync(FolderAccess.ReadOnly, ct);
            return true;
        }
    }
}
