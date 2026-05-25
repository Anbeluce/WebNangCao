using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebNangCao.Data;
using WebNangCao.Models;

namespace WebNangCao.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AppDbContext _context;

        public DashboardService(UserManager<ApplicationUser> userManager, AppDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public async Task<DashboardStatsDto> GetDashboardStatsAsync()
        {
            var dto = new DashboardStatsDto();

            dto.TotalUsers = await _userManager.Users.CountAsync();
            dto.ActiveUsers = await _userManager.Users.CountAsync(u => u.IsActive);
            dto.TotalApartments = await _context.Apartments.CountAsync(a => !a.IsDeleted);
            dto.TotalInvoices = await _context.Invoices.CountAsync(i => !i.IsDeleted);

            dto.OccupiedCount = await _context.Apartments.CountAsync(a => a.OwnerId != null && !a.IsDeleted);
            dto.EmptyCount = await _context.Apartments.CountAsync(a => a.OwnerId == null && !a.IsDeleted);

            var dbInvoices = await _context.Invoices
                .Where(i => !i.IsDeleted)
                .Select(i => new {
                    i.Status,
                    i.Month,
                    i.Year,
                    TotalAmount = (i.WaterUsage * i.WaterUnitPrice) + i.ManagementFee + i.WasteFee + i.ParkingFee
                })
                .ToListAsync();

            dto.TotalPaidRevenue = dbInvoices
                .Where(i => i.Status == InvoiceStatus.Paid)
                .Sum(i => i.TotalAmount);

            dto.TotalUnpaidDebt = dbInvoices
                .Where(i => i.Status != InvoiceStatus.Paid)
                .Sum(i => i.TotalAmount);

            dto.LatestResidents = await _context.Apartments
                .Include(a => a.Owner)
                .Where(a => a.OwnerId != null && !a.IsDeleted)
                .OrderByDescending(a => a.UpdatedAt ?? a.CreatedAt)
                .Take(5)
                .ToListAsync();

            dto.LatestInvoices = await _context.Invoices
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

            foreach (var monthYear in last6Months)
            {
                dto.MonthlyLabels.Add($"T{monthYear.Month}/{monthYear.Year}");
                
                var paid = dbInvoices
                    .Where(i => i.Month == monthYear.Month && i.Year == monthYear.Year && i.Status == InvoiceStatus.Paid)
                    .Sum(i => (double)i.TotalAmount);

                var unpaid = dbInvoices
                    .Where(i => i.Month == monthYear.Month && i.Year == monthYear.Year && i.Status != InvoiceStatus.Paid)
                    .Sum(i => (double)i.TotalAmount);

                dto.MonthlyPaid.Add(paid);
                dto.MonthlyUnpaid.Add(unpaid);
            }

            return dto;
        }
    }
}
