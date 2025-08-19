using Microsoft.EntityFrameworkCore;
using Api_TaskManager.Models;

public class ApplicationDbContext : DbContext {
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) {}
    public DbSet<User> Users { get; set; }
    public DbSet<TaskItem> TaskItems { get; set; }
}
