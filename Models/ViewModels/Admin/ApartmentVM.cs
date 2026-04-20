using System.ComponentModel.DataAnnotations;

namespace WebNangCao.Models.ViewModels.Admin
{
    public class ApartmentListVM
    {
        public int Id { get; set; }
        public string ApartmentNumber { get; set; } = string.Empty;
        public double Area { get; set; }
        public int Floor { get; set; }
        public string? OwnerName { get; set; }
        public string? OwnerEmail { get; set; }
        public int InvoiceCount { get; set; }
    }

    public class CreateApartmentVM
    {
        [Required(ErrorMessage = "Vui lòng nhập số căn hộ")]
        [Display(Name = "Số căn hộ")]
        [StringLength(20)]
        public string ApartmentNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập diện tích")]
        [Display(Name = "Diện tích (m²)")]
        [Range(10, 500, ErrorMessage = "Diện tích phải từ 10 đến 500 m²")]
        public double Area { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tầng")]
        [Display(Name = "Tầng")]
        [Range(1, 100, ErrorMessage = "Tầng phải từ 1 đến 100")]
        public int Floor { get; set; }

        [Display(Name = "Chủ sở hữu")]
        public string? OwnerId { get; set; }
    }

    public class EditApartmentVM
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số căn hộ")]
        [Display(Name = "Số căn hộ")]
        [StringLength(20)]
        public string ApartmentNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập diện tích")]
        [Display(Name = "Diện tích (m²)")]
        [Range(10, 500, ErrorMessage = "Diện tích phải từ 10 đến 500 m²")]
        public double Area { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tầng")]
        [Display(Name = "Tầng")]
        [Range(1, 100, ErrorMessage = "Tầng phải từ 1 đến 100")]
        public int Floor { get; set; }

        [Display(Name = "Chủ sở hữu")]
        public string? OwnerId { get; set; }
    }
}
