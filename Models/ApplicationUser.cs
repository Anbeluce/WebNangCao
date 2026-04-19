using Microsoft.AspNetCore.Identity;

namespace WebNangCao.Models
{
    public class ApplicationUser : IdentityUser
    {
        // Các trường Email, PhoneNumber đã có sẵn trong IdentityUser

        public string FullName { get; set; } = string.Empty;

        public string? IdentityCardNumber { get; set; } // Số CCCD/CMND

        public DateTime? DateOfBirth { get; set; } // Ngày sinh

        public string? AvatarUrl { get; set; } // Đường dẫn ảnh đại diện

        public bool IsActive { get; set; } = true; // Trạng thái tài khoản (còn ở chung cư hay không)

        // Navigation Property (Không dùng virtual)
        public ICollection<Apartment> Apartments { get; set; } = new List<Apartment>();
    }
}
