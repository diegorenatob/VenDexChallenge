using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VendSys.Domain;

namespace VendSys.Infrastructure.Data.Configurations;

internal sealed class DexMeterConfiguration : IEntityTypeConfiguration<DexMeter>
{
    public void Configure(EntityTypeBuilder<DexMeter> builder)
    {
        builder.ToTable("DEXMeter", "dbo");

        builder.HasKey(e => e.DexMeterId);

        builder.Property(e => e.DexMeterId)
               .UseIdentityColumn();

        builder.Property(e => e.Machine)
               .HasColumnType("nvarchar(1)")
               .IsRequired();

        builder.Property(e => e.DEXDateTime)
               .HasColumnType("datetime2")
               .IsRequired();

        builder.Property(e => e.MachineSerialNumber)
               .HasColumnType("nvarchar(50)")
               .IsRequired();

        builder.Property(e => e.ValueOfPaidVends)
               .HasColumnType("decimal(10,2)")
               .IsRequired();

        builder.HasIndex(e => new { e.Machine, e.DEXDateTime })
               .IsUnique()
               .HasDatabaseName("UQ_DEXMeter_Machine_DEXDateTime");
    }
}
