using System;
using MailKit;
using MimeKit;

namespace Mail_Manager.Models
{
    // ДТО для рядка у ListView — тільки те, що потрібно для перегляду (без тіла листа)
    public class EmailItem
    {
        // стабільний IMAP-ідентифікатор на сервері, зберігаю це щоб потім змогти завантажити повне повідомлення або видалити його
        public UniqueId UniqueId { get; set; }
        public string From { get; set; } = "";
        public string Subject { get; set; } = "";
        public DateTimeOffset Date { get; set; }
    }
}
