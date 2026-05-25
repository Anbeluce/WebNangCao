using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebNangCao.Services;

namespace WebNangCao.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class DashboardController : Controller
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        public async Task<IActionResult> Index()
        {
            var stats = await _dashboardService.GetDashboardStatsAsync();

            ViewBag.TotalUsers = stats.TotalUsers;
            ViewBag.ActiveUsers = stats.ActiveUsers;
            ViewBag.TotalApartments = stats.TotalApartments;
            ViewBag.TotalInvoices = stats.TotalInvoices;
            ViewBag.OccupiedCount = stats.OccupiedCount;
            ViewBag.EmptyCount = stats.EmptyCount;
            ViewBag.TotalPaidRevenue = stats.TotalPaidRevenue;
            ViewBag.TotalUnpaidDebt = stats.TotalUnpaidDebt;
            ViewBag.LatestResidents = stats.LatestResidents;
            ViewBag.LatestInvoices = stats.LatestInvoices;
            ViewBag.MonthlyLabels = stats.MonthlyLabels;
            ViewBag.MonthlyPaid = stats.MonthlyPaid;
            ViewBag.MonthlyUnpaid = stats.MonthlyUnpaid;

            return View();
        }
    }
}
