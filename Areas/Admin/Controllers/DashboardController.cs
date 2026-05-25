using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebNangCao.Data;
using WebNangCao.Models;

namespace WebNangCao.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class DashboardController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AppDbContext _context;

        public DashboardController(UserManager<ApplicationUser> userManager, AppDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.TotalUsers = await _userManager.Users.CountAsync();
            ViewBag.ActiveUsers = await _userManager.Users.CountAsync(u => u.IsActive);
            ViewBag.TotalApartments = await _context.Apartments.CountAsync(a => !a.IsDeleted);
            ViewBag.TotalInvoices = await _context.Invoices.CountAsync(i => !i.IsDeleted);

            ViewBag.OccupiedCount = await _context.Apartments.CountAsync(a => a.OwnerId != null && !a.IsDeleted);
            ViewBag.EmptyCount = await _context.Apartments.CountAsync(a => a.OwnerId == null && !a.IsDeleted);
            
            // Lấy danh sách hóa đơn rút gọn dưới dạng bộ nhớ để tính toán calculated properties
            var dbInvoices = await _context.Invoices
                .Where(i => !i.IsDeleted)
                .Select(i => new {
                    i.Status,
                    i.Month,
                    i.Year,
                    TotalAmount = (i.WaterUsage * i.WaterUnitPrice) + i.ManagementFee + i.WasteFee + i.ParkingFee
                })
                .ToListAsync();

            ViewBag.TotalPaidRevenue = dbInvoices
                .Where(i => i.Status == InvoiceStatus.Paid)
                .Sum(i => i.TotalAmount);

            ViewBag.TotalUnpaidDebt = dbInvoices
                .Where(i => i.Status != InvoiceStatus.Paid)
                .Sum(i => i.TotalAmount);

            // 5 cư dân đăng ký căn hộ mới nhất
            ViewBag.LatestResidents = await _context.Apartments
                .Include(a => a.Owner)
                .Where(a => a.OwnerId != null && !a.IsDeleted)
                .OrderByDescending(a => a.UpdatedAt ?? a.CreatedAt)
                .Take(5)
                .ToListAsync();

            // 5 hóa đơn mới tạo gần nhất
            ViewBag.LatestInvoices = await _context.Invoices
                .Include(i => i.Apartment)
                    .ThenInclude(a => a.Owner)
                .Where(i => !i.IsDeleted)
                .OrderByDescending(i => i.Id)
                .Take(5)
                .ToListAsync();

            // Thống kê doanh thu 6 tháng qua
            var localNow = DateTime.UtcNow.AddHours(7);
            var last6Months = Enumerable.Range(0, 6)
                .Select(i => localNow.AddMonths(-i))
                .Select(d => new { d.Month, d.Year })
                .Reverse()
                .ToList();

            var monthlyLabels = new List<string>();
            var monthlyPaid = new List<double>();
            var monthlyUnpaid = new List<double>();

            foreach (var monthYear in last6Months)
            {
                monthlyLabels.Add($"T{monthYear.Month}/{monthYear.Year}");
                
                var paid = dbInvoices
                    .Where(i => i.Month == monthYear.Month && i.Year == monthYear.Year && i.Status == InvoiceStatus.Paid)
                    .Sum(i => (double)i.TotalAmount);

                var unpaid = dbInvoices
                    .Where(i => i.Month == monthYear.Month && i.Year == monthYear.Year && i.Status != InvoiceStatus.Paid)
                    .Sum(i => (double)i.TotalAmount);

                monthlyPaid.Add(paid);
                monthlyUnpaid.Add(unpaid);
            }

            ViewBag.MonthlyLabels = monthlyLabels;
            ViewBag.MonthlyPaid = monthlyPaid;
            ViewBag.MonthlyUnpaid = monthlyUnpaid;

            return View();
        }
    }
}
