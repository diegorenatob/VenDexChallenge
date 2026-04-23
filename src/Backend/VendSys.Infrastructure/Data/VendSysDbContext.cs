using Microsoft.EntityFrameworkCore;
using VendSys.Domain;

namespace VendSys.Infrastructure.Data;

public sealed class VendSysDbContext : DbContext
{
    public VendSysDbContext(DbContextOptions<VendSysDbContext> options) : base(options) { }

    public DbSet<DexMeter> DexMeters => Set<DexMeter>();
    public DbSet<DexLaneMeter> DexLaneMeters => Set<DexLaneMeter>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(VendSysDbContext).Assembly);
    }
}
