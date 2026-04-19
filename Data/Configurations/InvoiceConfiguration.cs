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
            builder.Property(i => i.ElectricityFee).HasColumnType("decimal(18,2)");
            builder.Property(i => i.WaterFee).HasColumnType("decimal(18,2)");
            builder.Property(i => i.ManagementFee).HasColumnType("decimal(18,2)");
            builder.Ignore(i => i.TotalAmount); // Computed property
            builder.HasOne(i => i.Apartment)
                   .WithMany(a => a.Invoices)
                   .HasForeignKey(i => i.ApartmentId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
