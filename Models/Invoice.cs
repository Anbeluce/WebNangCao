namespace WebNangCao.Models
{
    public class Invoice : BaseEntity
    {
        public int ApartmentId { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }

        public decimal ElectricityFee { get; set; } // Tiền điện
        public decimal WaterFee { get; set; }       // Tiền nước
        public decimal ManagementFee { get; set; }  // Phí quản lý chung

        public decimal TotalAmount => ElectricityFee + WaterFee + ManagementFee;
        public InvoiceStatus Status { get; set; } = InvoiceStatus.Unpaid;
        public DateTime DueDate { get; set; }

        public Apartment Apartment { get; set; } = null!;
        public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    }

    public enum InvoiceStatus
    {
        Unpaid = 0,
        Partial = 1,
        Paid = 2
    }
}
