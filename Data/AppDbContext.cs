using Microsoft.EntityFrameworkCore;
using Winedge.Models;

namespace Winedge.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Device> Devices { get; set; }
    }
}
