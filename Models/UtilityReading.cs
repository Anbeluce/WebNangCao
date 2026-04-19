namespace WebNangCao.Models
{
    public class UtilityReading : BaseEntity
    {
        public int ApartmentId { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
        public UtilityType Type { get; set; }

        public decimal PreviousReading { get; set; }
        public decimal CurrentReading { get; set; }

        public Apartment Apartment { get; set; } = null!;
    }

    public enum UtilityType { Electricity = 1, Water = 2 }
}
