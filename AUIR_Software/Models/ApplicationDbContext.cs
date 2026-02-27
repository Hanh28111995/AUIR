using Microsoft.EntityFrameworkCore;
using AUIR_Software.Models;
namespace ApplicationDbContext.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<Contact> Contacts { get; set; }
    public DbSet<Content> Contents { get; set; }
}
