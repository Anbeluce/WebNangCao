namespace WebNangCao.Models
{
    public class Apartment : BaseEntity
    {
        public string ApartmentNumber { get; set; } = string.Empty;
        public double Area { get; set; }
        public int Floor { get; set; }

        public string? OwnerId { get; set; }

        // Navigation Properties (Không dùng virtual)
        public ApplicationUser? Owner { get; set; }
        public ICollection<UtilityReading> UtilityReadings { get; set; } = new List<UtilityReading>();
        public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
    }
}
