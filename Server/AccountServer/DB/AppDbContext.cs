using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AccountServer.DB
{
    public class AppDbContext : DbContext
    {
        public DbSet<AccountDb> Accounts { get; set; }
        
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {

        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            // AccountName에 인덱스를 걸어줌
            // IsUnique =>  AccountName에 중복된 데이터가 들어어지 못하도록 방지
            builder.Entity<AccountDb>()
                .HasIndex(a => a.AccountName)
                .IsUnique();
        }
        
    }
}