using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebNangCao.Models;
using WebNangCao.Models.ViewModels;
using WebNangCao.Services;

namespace WebNangCao.Controllers
{
    [Authorize]
    public class ResidentController : Controller
    {
        private readonly IResidentService _residentService;

        public ResidentController(IResidentService residentService)
        {
            _residentService = residentService;
        }

        // GET: /Resident - Trang tổng quan hóa đơn
        public async Task<IActionResult> Index()
        {
            var result = await _residentService.GetDashboardAsync(User);
            if (!result.Success) return RedirectToAction("Login", "Auth");

            var dashboard = result.Data!;
            ViewBag.UserName = dashboard.UserName;
            ViewBag.TotalApartments = dashboard.Apartments.Count;
            ViewBag.TotalInvoices = dashboard.TotalInvoices;
            ViewBag.UnpaidInvoices = dashboard.UnpaidInvoices;
            ViewBag.TotalUnpaid = dashboard.TotalUnpaid;

            return View(dashboard.Apartments);
        }

        // GET: /Resident/Invoices/5
        public async Task<IActionResult> Invoices(int id)
        {
            var result = await _residentService.GetApartmentInvoicesAsync(id, User);
            if (!result.Success)
            {
                TempData["ErrorMessage"] = result.ErrorMessage;
                return RedirectToAction(nameof(Index));
            }

            var apartment = result.Data!;
            ViewBag.ApartmentNumber = apartment.ApartmentNumber;

            var invoices = apartment.Invoices
                .OrderByDescending(i => i.Year)
                .ThenByDescending(i => i.Month)
                .ToList();

            return View(invoices);
        }

        // GET: /Resident/InvoiceDetail/5
        public async Task<IActionResult> InvoiceDetail(int id)
        {
            var result = await _residentService.GetInvoiceDetailAsync(id, User);
            if (!result.Success)
            {
                TempData["ErrorMessage"] = result.ErrorMessage;
                return RedirectToAction(nameof(Index));
            }

            return View(result.Data);
        }

        // GET: /Resident/CheckPaymentStatus/5
        [HttpGet]
        public async Task<IActionResult> CheckPaymentStatus(int id)
        {
            var result = await _residentService.CheckPaymentStatusAsync(id, User);
            if (!result.Success) return Json(null);

            return Json(result.Data);
        }

        // GET: /Resident/Profile - Trang quản lý tài khoản
        public async Task<IActionResult> Profile()
        {
            var result = await _residentService.GetProfileAsync(User);
            if (!result.Success) return RedirectToAction("Login", "Auth");

            return View(result.Data);
        }

        // POST: /Resident/UpdateProfile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(ProfileVM model)
        {
            // Chỉ validate các trường cần thiết
            ModelState.Remove("Email");
            if (!ModelState.IsValid)
            {
                // Reload thống kê bằng cách lấy dữ liệu hiện tại
                var profileResult = await _residentService.GetProfileAsync(User);
                if (profileResult.Success && profileResult.Data != null)
                {
                    model.Email = profileResult.Data.Email;
                    model.ApartmentCount = profileResult.Data.ApartmentCount;
                    model.InvoiceCount = profileResult.Data.InvoiceCount;
                    model.UnpaidInvoiceCount = profileResult.Data.UnpaidInvoiceCount;
                    model.TotalDebt = profileResult.Data.TotalDebt;
                }
                return View("Profile", model);
            }

            var result = await _residentService.UpdateProfileAsync(model, User);
            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
            }
            else
            {
                TempData["ErrorMessage"] = "Có lỗi xảy ra: " + result.ErrorMessage;
            }

            return RedirectToAction(nameof(Profile));
        }

        // GET: /Resident/ChangePassword
        public IActionResult ChangePassword()
        {
            return View(new ChangePasswordVM());
        }

        // POST: /Resident/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordVM model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result = await _residentService.ChangePasswordAsync(model, User);
            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
                return RedirectToAction(nameof(Profile));
            }
            else
            {
                ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Đổi mật khẩu thất bại.");
                return View(model);
            }
        }
    }
}
