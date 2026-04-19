using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WebNangCao.Data.Configurations;
using WebNangCao.Models;

namespace WebNangCao.Data
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Apartment> Apartments { get; set; }
        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<UtilityReading> UtilityReadings { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<BlogPost> BlogPosts { get; set; }
        public DbSet<BlogComment> BlogComments { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.ApplyConfiguration(new ApartmentConfiguration());
            builder.ApplyConfiguration(new InvoiceConfiguration());
            builder.ApplyConfiguration(new UtilityReadingConfiguration());

            // Đổi tên bảng Identity sang tiếng Việt thân thiện hơn (tùy chọn)
            builder.Entity<ApplicationUser>().ToTable("Users");
        }
    }
}
