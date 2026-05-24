namespace WebNangCao.Models
{
    public class Invoice : BaseEntity
    {
        public int ApartmentId { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }

        // Điện
        public decimal ElectricityUsage { get; set; }    // Số kWh tiêu thụ
        public decimal ElectricityUnitPrice { get; set; } // Đơn giá (VNĐ/kWh)
        public decimal ElectricityFee => 0;

        // Nước
        public decimal WaterUsage { get; set; }           // Số m³ tiêu thụ
        public decimal WaterUnitPrice { get; set; }       // Đơn giá (VNĐ/m³)
        public decimal WaterFee => WaterUsage * WaterUnitPrice;

        // Phí dịch vụ chi tiết
        public decimal ManagementFee { get; set; } // Phí quản lý vận hành (tính theo diện tích)
        public decimal WasteFee { get; set; }      // Phí vệ sinh rác thải (cố định)
        public decimal ParkingFee { get; set; }    // Phí gửi xe (cố định)

        // Phí dịch vụ tổng hợp
        public decimal ServiceFee => ManagementFee + WasteFee + ParkingFee;

        public decimal TotalAmount => WaterFee + ServiceFee;
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
