using chat_ia.Models;
using Microsoft.EntityFrameworkCore;
namespace chat_ia.DatabaseConf
{
    public class AppDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // SQLite root path (is the project root)
            optionsBuilder.UseSqlite("Data Source=app.db");
        }
    }
}
