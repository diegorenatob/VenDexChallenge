using Microsoft.EntityFrameworkCore;
using VendSys.Domain;

namespace VendSys.Infrastructure.Data;

public sealed class VenDexDbContext : DbContext
{
    public VenDexDbContext(DbContextOptions<VenDexDbContext> options) : base(options) { }

    public DbSet<DexMeter> DexMeters => Set<DexMeter>();
    public DbSet<DexLaneMeter> DexLaneMeters => Set<DexLaneMeter>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(VenDexDbContext).Assembly);
    }
}
