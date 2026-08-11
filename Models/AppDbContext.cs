using Microsoft.EntityFrameworkCore;

namespace RescueLink.Api.Models;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Case> Cases { get; set; }
    public DbSet<User> Users { get; set; }
}