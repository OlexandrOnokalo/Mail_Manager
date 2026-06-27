using Mail_Data_Access.Models;
using Microsoft.EntityFrameworkCore;

namespace Mail_Data_Access
{
    public class MailDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
            // локальний варіант залишив на випадок якщо Somee ляже або буду тестувати офлайн
            //optionsBuilder.UseSqlServer(@"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=MailDatabase;Integrated Security=True;Connect Timeout=30;Encrypt=False;Trust Server Certificate=False;Application Intent=ReadWrite;Multi Subnet Failover=False");
            // хардкод — свідоме рішення для навчального проєкту, конфіг-файлів не робили
            optionsBuilder.UseSqlServer(@"workstation id=MailDatabase.mssql.somee.com;packet size=4096;user id=student_itstep_SQLLogin_1;pwd=9bcxnzefb4;data source=MailDatabase.mssql.somee.com;persist security info=False;initial catalog=MailDatabase;TrustServerCertificate=True");
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            // seed-data: без цього після міграції таблиця порожня і логін одразу падає
            // пароль тут — Google App Password (не звичайний), Gmail вимагає саме його для IMAP/SMTP
            modelBuilder.Entity<User>().HasData(new User[]
            {
                new User { Id = 1, Mail = "lenailyshun@gmail.com", Password = "dqmq yyqu uxfb ikfc" }
            });
        }
    }
}
