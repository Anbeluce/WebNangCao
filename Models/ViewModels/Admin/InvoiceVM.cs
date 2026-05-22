using System.ComponentModel.DataAnnotations;

namespace WebNangCao.Models.ViewModels.Admin
{
    public class InvoiceListVM
    {
        public int Id { get; set; }
        public string ApartmentNumber { get; set; } = string.Empty;
        public string? OwnerName { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
        public decimal ElectricityUsage { get; set; }
        public decimal ElectricityUnitPrice { get; set; }
        public decimal ElectricityFee => 0;
        public decimal WaterUsage { get; set; }
        public decimal WaterUnitPrice { get; set; }
        public decimal WaterFee => WaterUsage * WaterUnitPrice;
        public decimal ServiceFee { get; set; }
        public decimal TotalAmount => WaterFee + ServiceFee;
        public InvoiceStatus Status { get; set; }
        public DateTime DueDate { get; set; }
    }

    public class CreateInvoiceVM
    {
        [Required(ErrorMessage = "Vui lòng chọn căn hộ")]
        [Display(Name = "Căn hộ")]
        public int ApartmentId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn tháng")]
        [Display(Name = "Tháng")]
        [Range(1, 12, ErrorMessage = "Tháng phải từ 1 đến 12")]
        public int Month { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập năm")]
        [Display(Name = "Năm")]
        [Range(2020, 2099, ErrorMessage = "Năm không hợp lệ")]
        public int Year { get; set; }

        [Display(Name = "Số điện tiêu thụ (kWh)")]
        [Range(0, 99999, ErrorMessage = "Số điện không hợp lệ")]
        public decimal ElectricityUsage { get; set; } = 0;

        [Display(Name = "Đơn giá điện (VNĐ/kWh)")]
        [Range(0, 100000)]
        public decimal ElectricityUnitPrice { get; set; } = 0;

        [Required(ErrorMessage = "Vui lòng nhập số nước tiêu thụ")]
        [Display(Name = "Số nước tiêu thụ (m³)")]
        [Range(0, 99999, ErrorMessage = "Số nước không hợp lệ")]
        public decimal WaterUsage { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập đơn giá nước")]
        [Display(Name = "Đơn giá nước (VNĐ/m³)")]
        [Range(0, 1000000)]
        public decimal WaterUnitPrice { get; set; } = 15000;

        [Required(ErrorMessage = "Vui lòng nhập phí dịch vụ")]
        [Display(Name = "Phí dịch vụ (VNĐ)")]
        [Range(0, 100000000, ErrorMessage = "Số tiền không hợp lệ")]
        public decimal ServiceFee { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn hạn thanh toán")]
        [Display(Name = "Hạn thanh toán")]
        [DataType(DataType.Date)]
        public DateTime DueDate { get; set; } = DateTime.UtcNow.AddHours(7).AddDays(30);
    }

    public class EditInvoiceVM
    {
        public int Id { get; set; }

        [Display(Name = "Căn hộ")]
        public int ApartmentId { get; set; }
        public string ApartmentNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn tháng")]
        [Display(Name = "Tháng")]
        [Range(1, 12)]
        public int Month { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập năm")]
        [Display(Name = "Năm")]
        [Range(2020, 2099)]
        public int Year { get; set; }

        [Display(Name = "Số điện tiêu thụ (kWh)")]
        [Range(0, 99999)]
        public decimal ElectricityUsage { get; set; } = 0;

        [Display(Name = "Đơn giá điện (VNĐ/kWh)")]
        [Range(0, 100000)]
        public decimal ElectricityUnitPrice { get; set; } = 0;

        [Required]
        [Display(Name = "Số nước tiêu thụ (m³)")]
        [Range(0, 99999)]
        public decimal WaterUsage { get; set; }

        [Required]
        [Display(Name = "Đơn giá nước (VNĐ/m³)")]
        [Range(0, 1000000)]
        public decimal WaterUnitPrice { get; set; }

        [Required]
        [Display(Name = "Phí dịch vụ (VNĐ)")]
        [Range(0, 100000000)]
        public decimal ServiceFee { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn hạn thanh toán")]
        [Display(Name = "Hạn thanh toán")]
        [DataType(DataType.Date)]
        public DateTime DueDate { get; set; }

        [Display(Name = "Trạng thái")]
        public InvoiceStatus Status { get; set; }
    }
}
