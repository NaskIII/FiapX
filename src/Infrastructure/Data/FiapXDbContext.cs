using FiapX.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace FiapX.Infrastructure.Data;

public class FiapXDbContext : DbContext
{

    public DbSet<VideoBatch> VideoBatches => Set<VideoBatch>();
    public DbSet<Video> Videos => Set<Video>();

    public FiapXDbContext(DbContextOptions<FiapXDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FiapXDbContext).Assembly);  
    }
}