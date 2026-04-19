using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WebNangCao.Models;

namespace WebNangCao.Data.Configurations
{
    public class ApartmentConfiguration : IEntityTypeConfiguration<Apartment>
    {
        public void Configure(EntityTypeBuilder<Apartment> builder)
        {
            builder.HasKey(a => a.Id);
            builder.Property(a => a.ApartmentNumber).IsRequired().HasMaxLength(20);
            builder.HasOne(a => a.Owner)
                   .WithMany(u => u.Apartments)
                   .HasForeignKey(a => a.OwnerId)
                   .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
