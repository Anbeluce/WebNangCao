using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebNangCao.Models;
using WebNangCao.Models.ViewModels.Auth;
using WebNangCao.Services;

namespace WebNangCao.Controllers
{
    public class AuthController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IAuthService _authService;

        public AuthController(
            SignInManager<ApplicationUser> signInManager,
            IAuthService authService)
        {
            _signInManager = signInManager;
            _authService = authService;
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

            var result = await _authService.LoginAsync(model);
            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.ErrorMessage!);
                return View(model);
            }

            if (result.Data == "Admin")
            {
                return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
            }

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Index", "Home");
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

            var result = await _authService.SendRegisterOtpAsync(model, HttpContext.Session);
            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Có lỗi xảy ra.");
                return View(model);
            }

            if (result.Message != null && result.Message.Contains("Mã test"))
            {
                TempData["ErrorMessage"] = result.Message;
            }
            else
            {
                TempData["SuccessMessage"] = result.Message;
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

            var result = await _authService.VerifyOtpAndCreateUserAsync(model, HttpContext.Session);
            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Xác thực thất bại.");
                if (result.ErrorMessage == "Mã xác thực không chính xác.")
                {
                    TempData["ErrorMessage"] = "Mã OTP bạn vừa nhập không đúng. Vui lòng kiểm tra lại email.";
                }
                return View(model);
            }

            TempData["SuccessMessage"] = result.Message;
            return RedirectToAction("Index", "Home");
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
