using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Winedge.Models;

namespace Winedge.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UserDevice>()
                .HasIndex(u => new { u.User, u.Device })
                .IsUnique(); // impede duplicações do mesmo par User/Device
        }

        public DbSet<UserDevice> UserDevices { get; set; }
    }
}
