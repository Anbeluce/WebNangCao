using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WebNangCao.Models;

namespace WebNangCao.Data.Configurations
{
    public class UtilityReadingConfiguration : IEntityTypeConfiguration<UtilityReading>
    {
        public void Configure(EntityTypeBuilder<UtilityReading> builder)
        {
            builder.HasKey(u => u.Id);
            builder.Property(u => u.PreviousReading).HasColumnType("decimal(18,2)");
            builder.Property(u => u.CurrentReading).HasColumnType("decimal(18,2)");
            builder.HasOne(u => u.Apartment)
                   .WithMany(a => a.UtilityReadings)
                   .HasForeignKey(u => u.ApartmentId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
