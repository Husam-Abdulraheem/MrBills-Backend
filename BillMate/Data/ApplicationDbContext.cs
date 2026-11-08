using BillMate.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;

namespace BillMate.Data
{
    public class ApplicationDbContext : IdentityDbContext<User>
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var conn = Environment.GetEnvironmentVariable("DATABASE_URL");
            optionsBuilder.UseMySQL(conn);
        }
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<User>? User { get; set; }
        public DbSet<Group>? Groups { get; set; }
        public DbSet<GroupUser>? GroupUsers { get; set; }
        public DbSet<Bill>? Bills { get; set; }
        public DbSet<Debt>? Debts { get; set; }
        public DbSet<ActivityLog>? ActivityLogs { get; set; }
        public DbSet<Subscription>? Subscriptions { get; set; }
        public DbSet<Notification>? Notification { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);


            modelBuilder.Entity<Bill>()
                .HasMany(b => b.Debts)
                .WithOne(d => d.Bill)
                .HasForeignKey(d => d.BillId)
                .OnDelete(DeleteBehavior.Cascade);
        }

    }
}
