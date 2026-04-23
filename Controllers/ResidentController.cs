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

        // POST: /Resident/ConfirmPaymentRequest
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmPaymentRequest(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Auth");

            // Kiểm tra hóa đơn tồn tại và thuộc quyền sở hữu của User
            var invoice = await _context.Invoices
                .Include(i => i.Apartment)
                .Include(i => i.Transactions.Where(t => !t.IsDeleted))
                .FirstOrDefaultAsync(i => i.Id == id && !i.IsDeleted
                    && i.Apartment.OwnerId == user.Id);

            if (invoice == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy hóa đơn hoặc bạn không có quyền thực hiện.";
                return RedirectToAction(nameof(Index));
            }

            // Nếu đã thanh toán xong rồi thì không cần gửi yêu cầu nữa
            if (invoice.Status == InvoiceStatus.Paid)
            {
                TempData["InfoMessage"] = "Hóa đơn này đã được thanh toán hoàn tất.";
                return RedirectToAction(nameof(InvoiceDetail), new { id = id });
            }

            // Tính số tiền còn lại cần trả
            var paidAmount = invoice.Transactions.Sum(t => t.Amount);
            var remaining = invoice.TotalAmount - paidAmount;

            // 1. Tạo một Transaction mới với trạng thái "Chờ duyệt"
            var transaction = new Transaction
            {
                InvoiceId = id,
                Amount = remaining,
                PaymentDate = DateTime.Now,
                PaymentMethod = "Chuyển khoản VietQR",
                Note = "Cư dân báo đã chuyển khoản - Chờ duyệt", // Chuỗi này để Admin dễ lọc
                IsDeleted = false
            };

            // 2. Cập nhật trạng thái hóa đơn sang "Một phần" hoặc giữ nguyên để chờ Admin xác nhận chính thức
            // Ở đây mình giữ nguyên InvoiceStatus để Admin bấm Duyệt mới đổi màu trạng thái

            _context.Transactions.Add(transaction);

            try
            {
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã gửi thông báo xác nhận thanh toán! Vui lòng chờ Ban quản lý đối soát tài khoản.";
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi gửi yêu cầu. Vui lòng thử lại sau.";
            }

            return RedirectToAction(nameof(InvoiceDetail), new { id = id });
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
