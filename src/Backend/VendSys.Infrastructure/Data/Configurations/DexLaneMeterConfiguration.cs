using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VendSys.Domain;

namespace VendSys.Infrastructure.Data.Configurations;

internal sealed class DexLaneMeterConfiguration : IEntityTypeConfiguration<DexLaneMeter>
{
    public void Configure(EntityTypeBuilder<DexLaneMeter> builder)
    {
        builder.ToTable("DEXLaneMeter", "dbo");

        builder.HasKey(e => e.DexLaneMeterId);

        builder.Property(e => e.DexLaneMeterId)
               .UseIdentityColumn();

        builder.Property(e => e.DexMeterId)
               .IsRequired();

        builder.Property(e => e.ProductIdentifier)
               .HasColumnType("nvarchar(50)")
               .IsRequired();

        builder.Property(e => e.Price)
               .HasColumnType("decimal(10,2)")
               .IsRequired();

        builder.Property(e => e.NumberOfVends)
               .HasColumnType("int")
               .IsRequired();

        builder.Property(e => e.ValueOfPaidSales)
               .HasColumnType("decimal(10,2)")
               .IsRequired();

        builder.HasOne<DexMeter>()
               .WithMany()
               .HasForeignKey(e => e.DexMeterId)
               .HasConstraintName("FK_DEXLaneMeter_DEXMeter")
               .OnDelete(DeleteBehavior.Cascade);
    }
}
