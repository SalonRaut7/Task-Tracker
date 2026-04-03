using Microsoft.EntityFrameworkCore;
using TaskTracker.Domain.Entities;
namespace TaskTracker.Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // Define your DbSets here, for example:
        // public DbSet<Task> Tasks { get; set; }
        public DbSet<TaskItem> Tasks { get; set; } = null!;
    }
}
