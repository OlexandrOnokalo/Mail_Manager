using System;
using MailKit;
using MimeKit;

namespace Mail_Manager.Models
{
    public class EmailItem
    {
        public UniqueId UniqueId { get; set; }
        public string From { get; set; } = "";
        public string Subject { get; set; } = "";
        public DateTimeOffset Date { get; set; }
    }
}
