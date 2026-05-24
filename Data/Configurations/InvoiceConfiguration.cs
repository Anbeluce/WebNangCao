using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WebNangCao.Models;

namespace WebNangCao.Data.Configurations
{
    public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
    {
        public void Configure(EntityTypeBuilder<Invoice> builder)
        {
            builder.HasKey(i => i.Id);

            // Stored columns
            builder.Property(i => i.ElectricityUsage).HasColumnType("decimal(18,2)");
            builder.Property(i => i.ElectricityUnitPrice).HasColumnType("decimal(18,2)");
            builder.Property(i => i.WaterUsage).HasColumnType("decimal(18,2)");
            builder.Property(i => i.WaterUnitPrice).HasColumnType("decimal(18,2)");
            builder.Property(i => i.ManagementFee).HasColumnType("decimal(18,2)");
            builder.Property(i => i.WasteFee).HasColumnType("decimal(18,2)");
            builder.Property(i => i.ParkingFee).HasColumnType("decimal(18,2)");

            // Computed properties - không lưu vào DB
            builder.Ignore(i => i.ElectricityFee);
            builder.Ignore(i => i.WaterFee);
            builder.Ignore(i => i.ServiceFee);
            builder.Ignore(i => i.TotalAmount);

            builder.HasOne(i => i.Apartment)
                   .WithMany(a => a.Invoices)
                   .HasForeignKey(i => i.ApartmentId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
