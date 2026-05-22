using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebNangCao.Models;
using WebNangCao.Models.ViewModels.Auth;
using System.Text.Json;
using WebNangCao.Services;


namespace WebNangCao.Controllers
{
    public class AuthController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailService _emailService;

        public AuthController(SignInManager<ApplicationUser> signInManager,
                              UserManager<ApplicationUser> userManager,
                              IEmailService emailService)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _emailService = emailService;
        }

        // GET: /Auth/Login
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // POST: /Auth/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginVM model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Email hoặc mật khẩu không đúng.");
                return View(model);
            }

            if (!user.IsActive)
            {
                ModelState.AddModelError(string.Empty, "Tài khoản của bạn đã bị khóa hoặc ngừng hoạt động. Vui lòng liên hệ Ban quản lý.");
                return View(model);
            }

            var result = await _signInManager.PasswordSignInAsync(
                user, model.Password, model.RememberMe, lockoutOnFailure: true);

            if (result.Succeeded)
            {
                // Nếu là Admin thì chuyển đến trang Admin
                if (await _userManager.IsInRoleAsync(user, "Admin"))
                {
                    return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
                }

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);

                return RedirectToAction("Index", "Home");
            }

            if (result.IsLockedOut)
            {
                ModelState.AddModelError(string.Empty, "Tài khoản bị khóa tạm thời. Vui lòng thử lại sau.");
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Email hoặc mật khẩu không đúng.");
            }

            return View(model);
        }

        // GET: /Auth/Register
        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");

            return View();
        }

        // POST: /Auth/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterVM model)
        {
            // Kiểm tra ConfirmPassword
            if (string.IsNullOrEmpty(model.ConfirmPassword))
            {
                ModelState.AddModelError("ConfirmPassword", "Vui lòng xác nhận mật khẩu");
            }

            if (!ModelState.IsValid)
                return View(model);

            // Kiểm tra xem email đã tồn tại chưa
            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError("Email", "Email này đã được đăng ký. Vui lòng đăng nhập hoặc sử dụng email khác.");
                return View(model);
            }

            // Tạo mã OTP ngẫu nhiên 6 số
            var otpCode = new Random().Next(100000, 999999).ToString();

            // Lưu thông tin đăng ký và OTP vào Session
            HttpContext.Session.SetString("RegistrationData", JsonSerializer.Serialize(model));
            HttpContext.Session.SetString("RegistrationOTP", otpCode);

            // Gửi OTP qua Brevo Email
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
                TempData["SuccessMessage"] = "Mã OTP đã được gửi đến email của bạn. Vui lòng kiểm tra hộp thư.";
            }
            catch (Exception ex)
            {
                // Fallback nếu lỗi gửi mail để vẫn có thể test
                TempData["ErrorMessage"] = "Có lỗi khi gửi email xác thực. (Mã test: " + otpCode + ")";
            }
            
            return RedirectToAction("VerifyOtp", new { email = model.Email });
        }

        // GET: /Auth/VerifyOtp
        [HttpGet]
        public IActionResult VerifyOtp(string email)
        {
            var otp = HttpContext.Session.GetString("RegistrationOTP");
            if (string.IsNullOrEmpty(otp))
            {
                return RedirectToAction("Register");
            }

            var model = new VerifyOtpVM { Email = email };
            return View(model);
        }

        // POST: /Auth/VerifyOtp
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyOtp(VerifyOtpVM model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var sessionOtp = HttpContext.Session.GetString("RegistrationOTP");
            var registrationJson = HttpContext.Session.GetString("RegistrationData");

            if (string.IsNullOrEmpty(sessionOtp) || string.IsNullOrEmpty(registrationJson))
            {
                ModelState.AddModelError(string.Empty, "Phiên làm việc đã hết hạn. Vui lòng đăng ký lại.");
                return View(model);
            }

            if (model.OtpCode != sessionOtp)
            {
                ModelState.AddModelError("OtpCode", "Mã xác thực không chính xác.");
                TempData["ErrorMessage"] = "Mã OTP bạn vừa nhập không đúng. Vui lòng kiểm tra lại email.";
                return View(model);
            }

            // OTP đúng -> Tiến hành tạo tài khoản
            var regData = JsonSerializer.Deserialize<RegisterVM>(registrationJson);
            if (regData == null) return RedirectToAction("Register");

            // Kiểm tra xem người dùng đã được tạo chưa (trường hợp click đúp/double-submit)
            var existingUser = await _userManager.FindByEmailAsync(regData.Email);
            if (existingUser != null)
            {
                // Nếu đã tồn tại, có thể là do double-submit hoặc đã đăng ký xong ở tab khác
                // Xóa session và cho phép đăng nhập luôn
                HttpContext.Session.Remove("RegistrationOTP");
                HttpContext.Session.Remove("RegistrationData");

                if (!(User.Identity?.IsAuthenticated == true))
                {
                    await _signInManager.SignInAsync(existingUser, isPersistent: false);
                }
                
                TempData["SuccessMessage"] = "Tài khoản đã được xác thực thành công.";
                return RedirectToAction("Index", "Home");
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
                EmailConfirmed = true // Đã xác thực qua OTP
            };

            var result = await _userManager.CreateAsync(user, regData.Password);

            if (result.Succeeded)
            {
                // Xóa session sau khi thành công
                HttpContext.Session.Remove("RegistrationOTP");
                HttpContext.Session.Remove("RegistrationData");

                await _userManager.AddToRoleAsync(user, "Resident");
                await _signInManager.SignInAsync(user, isPersistent: false);
                TempData["SuccessMessage"] = "Đăng ký và xác thực thành công! Chào mừng bạn.";
                return RedirectToAction("Index", "Home");
            }

            // Nếu thất bại vì email đã tồn tại (lại một lần nữa check race condition)
            if (result.Errors.Any(e => e.Code == "DuplicateEmail" || e.Code == "DuplicateUserName"))
            {
                var userAgain = await _userManager.FindByEmailAsync(regData.Email);
                if (userAgain != null)
                {
                    HttpContext.Session.Remove("RegistrationOTP");
                    HttpContext.Session.Remove("RegistrationData");
                    await _signInManager.SignInAsync(userAgain, isPersistent: false);
                    return RedirectToAction("Index", "Home");
                }
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        // POST: /Auth/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login", "Auth");
        }

        // GET: /Auth/AccessDenied
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
