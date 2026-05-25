using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using System.Text.Json;
using WebNangCao.Models;
using WebNangCao.Models.ViewModels.Auth;

namespace WebNangCao.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IEmailService _emailService;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IEmailService emailService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailService = emailService;
        }

        public async Task<ServiceResult> SendRegisterOtpAsync(RegisterVM model, ISession session)
        {
            // Kiểm tra xem email đã tồn tại chưa
            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                return ServiceResult.FailureResult("Email này đã được đăng ký. Vui lòng đăng nhập hoặc sử dụng email khác.");
            }

            // Tạo mã OTP ngẫu nhiên 6 số
            var otpCode = new Random().Next(100000, 999999).ToString();

            // Lưu thông tin đăng ký và OTP vào Session
            session.SetString("RegistrationData", JsonSerializer.Serialize(model));
            session.SetString("RegistrationOTP", otpCode);

            // Gửi OTP qua Email
            try
            {
                var subject = "Mã xác thực đăng ký - Chung cư Smart";
                var body = $@"
                    <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; border: 1px solid #e0e0e0; padding: 20px;'>
                        <h2 style='color: #04a9f5;'>Xác thực đăng ký tài khoản</h2>
                        <p>Chào <strong>{model.FullName}</strong>,</p>
                        <p>Cảm ơn bạn đã đăng ký tài khoản tại hệ thống quản lý Chung cư Smart.</p>
                        <p>Mã xác thực (OTP) của bạn là:</p>
                        <div style='background: #f4f7fa; padding: 15px; text-align: center; font-size: 24px; font-weight: bold; letter-spacing: 5px; color: #04a9f5; border: 1px dashed #04a9f5;'>
                            {otpCode}
                        </div>
                        <p style='margin-top: 20px; font-size: 13px; color: #777;'>Mã này sẽ hết hạn sau 10 phút. Nếu bạn không thực hiện yêu cầu này, vui lòng bỏ qua email này.</p>
                        <hr style='border: none; border-top: 1px solid #eee; margin: 20px 0;'/>
                        <p style='font-size: 12px; color: #999;'>Đây là email tự động, vui lòng không phản hồi.</p>
                    </div>";
                
                await _emailService.SendEmailAsync(model.Email, subject, body);
                return ServiceResult.SuccessResult("Mã OTP đã được gửi đến email của bạn. Vui lòng kiểm tra hộp thư.");
            }
            catch (Exception)
            {
                // Fallback để có thể test nếu lỗi mail
                return ServiceResult.SuccessResult("Có lỗi khi gửi email xác thực. (Mã test: " + otpCode + ")");
            }
        }

        public async Task<ServiceResult> VerifyOtpAndCreateUserAsync(VerifyOtpVM model, ISession session)
        {
            var sessionOtp = session.GetString("RegistrationOTP");
            var registrationJson = session.GetString("RegistrationData");

            if (string.IsNullOrEmpty(sessionOtp) || string.IsNullOrEmpty(registrationJson))
            {
                return ServiceResult.FailureResult("Phiên làm việc đã hết hạn. Vui lòng đăng ký lại.");
            }

            if (model.OtpCode != sessionOtp)
            {
                return ServiceResult.FailureResult("Mã xác thực không chính xác.");
            }

            // OTP đúng -> Tiến hành tạo tài khoản
            var regData = JsonSerializer.Deserialize<RegisterVM>(registrationJson);
            if (regData == null)
            {
                return ServiceResult.FailureResult("Dữ liệu đăng ký không hợp lệ.");
            }

            // Kiểm tra xem người dùng đã được tạo chưa (race condition)
            var existingUser = await _userManager.FindByEmailAsync(regData.Email);
            if (existingUser != null)
            {
                session.Remove("RegistrationOTP");
                session.Remove("RegistrationData");

                await _signInManager.SignInAsync(existingUser, isPersistent: false);
                return ServiceResult.SuccessResult("Tài khoản đã được xác thực thành công.");
            }

            var user = new ApplicationUser
            {
                UserName = regData.Email,
                Email = regData.Email,
                FullName = regData.FullName,
                PhoneNumber = regData.PhoneNumber,
                IdentityCardNumber = regData.IdentityCardNumber,
                DateOfBirth = regData.DateOfBirth,
                IsActive = true,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, regData.Password);

            if (result.Succeeded)
            {
                session.Remove("RegistrationOTP");
                session.Remove("RegistrationData");

                await _userManager.AddToRoleAsync(user, "Resident");
                await _signInManager.SignInAsync(user, isPersistent: false);
                return ServiceResult.SuccessResult("Đăng ký và xác thực thành công! Chào mừng bạn.");
            }

            // Xử lý race condition trùng lặp
            if (result.Errors.Any(e => e.Code == "DuplicateEmail" || e.Code == "DuplicateUserName"))
            {
                var userAgain = await _userManager.FindByEmailAsync(regData.Email);
                if (userAgain != null)
                {
                    session.Remove("RegistrationOTP");
                    session.Remove("RegistrationData");
                    await _signInManager.SignInAsync(userAgain, isPersistent: false);
                    return ServiceResult.SuccessResult("Đăng ký thành công.");
                }
            }

            return ServiceResult.FailureResult(string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        public async Task<ServiceResult<string>> LoginAsync(LoginVM model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
                return ServiceResult<string>.FailureResult("Email hoặc mật khẩu không đúng.");
            if (!user.IsActive)
                return ServiceResult<string>.FailureResult("Tài khoản bị khóa. Vui lòng liên hệ Ban quản lý.");

            var result = await _signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, lockoutOnFailure: true);
            if (result.Succeeded)
            {
                var target = await _userManager.IsInRoleAsync(user, "Admin") ? "Admin" : "Home";
                return ServiceResult<string>.SuccessResult(target);
            }
            if (result.IsLockedOut)
                return ServiceResult<string>.FailureResult("Tài khoản bị khóa tạm thời. Vui lòng thử lại sau.");

            return ServiceResult<string>.FailureResult("Email hoặc mật khẩu không đúng.");
        }
    }
}
