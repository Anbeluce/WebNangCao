using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebNangCao.Data;
using WebNangCao.Models;
using WebNangCao.Models.ViewModels;

namespace WebNangCao.Controllers
{
    [Authorize]
    public class ResidentController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public ResidentController(AppDbContext context, UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
        }

        // GET: /Resident - Trang tổng quan hóa đơn
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Auth");

            var apartments = await _context.Apartments
                .Where(a => a.OwnerId == user.Id && !a.IsDeleted)
                .Include(a => a.Invoices.Where(i => !i.IsDeleted))
                .OrderBy(a => a.ApartmentNumber)
                .ToListAsync();

            var allInvoices = apartments.SelectMany(a => a.Invoices).ToList();
            ViewBag.UserName = user.FullName;
            ViewBag.TotalApartments = apartments.Count;
            ViewBag.TotalInvoices = allInvoices.Count;
            ViewBag.UnpaidInvoices = allInvoices.Count(i => i.Status == InvoiceStatus.Unpaid);
            ViewBag.TotalUnpaid = allInvoices
                .Where(i => i.Status != InvoiceStatus.Paid)
                .Sum(i => i.TotalAmount);

            return View(apartments);
        }

        // GET: /Resident/Invoices/5
        public async Task<IActionResult> Invoices(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Auth");

            var apartment = await _context.Apartments
                .Include(a => a.Invoices.Where(i => !i.IsDeleted))
                    .ThenInclude(i => i.Transactions)
                .FirstOrDefaultAsync(a => a.Id == id && a.OwnerId == user.Id && !a.IsDeleted);

            if (apartment == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy căn hộ hoặc bạn không có quyền truy cập.";
                return RedirectToAction(nameof(Index));
            }

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
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Auth");

            var invoice = await _context.Invoices
                .Include(i => i.Apartment)
                .Include(i => i.Transactions.Where(t => !t.IsDeleted))
                .FirstOrDefaultAsync(i => i.Id == id && !i.IsDeleted
                    && i.Apartment.OwnerId == user.Id);

            if (invoice == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy hóa đơn.";
                return RedirectToAction(nameof(Index));
            }

            return View(invoice);
        }

        // GET: /Resident/CheckPaymentStatus/5
        [HttpGet]
        public async Task<IActionResult> CheckPaymentStatus(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Json(null);

            var invoice = await _context.Invoices
                .Include(i => i.Apartment)
                .FirstOrDefaultAsync(i => i.Id == id && i.Apartment.OwnerId == user.Id && !i.IsDeleted);
            
            if (invoice == null) return Json(null);

            var previousAmounts = await _context.Transactions
                .Where(t => t.InvoiceId == invoice.Id && !t.IsDeleted)
                .Select(t => t.Amount)
                .ToListAsync();
            var totalPaid = previousAmounts.Sum();

            return Json(new
            {
                isPaid = invoice.Status == InvoiceStatus.Paid,
                status = invoice.Status.ToString(),
                totalPaid = totalPaid
            });
        }

        // GET: /Resident/Profile - Trang quản lý tài khoản
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Auth");

            var apartments = await _context.Apartments
                .Where(a => a.OwnerId == user.Id && !a.IsDeleted)
                .Include(a => a.Invoices.Where(i => !i.IsDeleted))
                .ToListAsync();

            var allInvoices = apartments.SelectMany(a => a.Invoices).ToList();

            var model = new ProfileVM
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? "",
                PhoneNumber = user.PhoneNumber,
                IdentityCardNumber = user.IdentityCardNumber,
                DateOfBirth = user.DateOfBirth,
                ApartmentCount = apartments.Count,
                InvoiceCount = allInvoices.Count,
                UnpaidInvoiceCount = allInvoices.Count(i => i.Status == InvoiceStatus.Unpaid),
                TotalDebt = allInvoices.Where(i => i.Status != InvoiceStatus.Paid).Sum(i => i.TotalAmount)
            };

            return View(model);
        }

        // POST: /Resident/UpdateProfile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(ProfileVM model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Auth");

            // Chỉ validate các trường cần thiết
            ModelState.Remove("Email");
            if (!ModelState.IsValid)
            {
                // Reload thống kê
                var apts = await _context.Apartments
                    .Where(a => a.OwnerId == user.Id && !a.IsDeleted)
                    .Include(a => a.Invoices.Where(i => !i.IsDeleted))
                    .ToListAsync();
                var invs = apts.SelectMany(a => a.Invoices).ToList();
                model.Email = user.Email ?? "";
                model.ApartmentCount = apts.Count;
                model.InvoiceCount = invs.Count;
                model.UnpaidInvoiceCount = invs.Count(i => i.Status == InvoiceStatus.Unpaid);
                model.TotalDebt = invs.Where(i => i.Status != InvoiceStatus.Paid).Sum(i => i.TotalAmount);
                return View("Profile", model);
            }

            user.FullName = model.FullName;
            user.PhoneNumber = model.PhoneNumber;
            user.IdentityCardNumber = model.IdentityCardNumber;
            user.DateOfBirth = model.DateOfBirth;

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Cập nhật thông tin thành công!";
            }
            else
            {
                TempData["ErrorMessage"] = "Có lỗi xảy ra: " + string.Join(", ", result.Errors.Select(e => e.Description));
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

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Auth");

            var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
            if (result.Succeeded)
            {
                await _signInManager.RefreshSignInAsync(user);
                TempData["SuccessMessage"] = "Đổi mật khẩu thành công!";
                return RedirectToAction(nameof(Profile));
            }
            else
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Code == "PasswordMismatch" ? "Mật khẩu hiện tại không đúng." : error.Description);
                }
                return View(model);
            }
        }
    }
}
