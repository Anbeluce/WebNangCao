using System.ComponentModel.DataAnnotations;

namespace WebNangCao.Models.ViewModels.Auth
{
    public class VerifyOtpVM
    {
        [Required(ErrorMessage = "Vui lòng nhập mã OTP")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "Mã OTP phải có 6 chữ số")]
        public string OtpCode { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;
    }
}
