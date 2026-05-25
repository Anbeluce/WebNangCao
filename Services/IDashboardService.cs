using WebNangCao.Models;

namespace WebNangCao.Services
{
    public class DashboardStatsDto
    {
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int TotalApartments { get; set; }
        public int TotalInvoices { get; set; }
        public int OccupiedCount { get; set; }
        public int EmptyCount { get; set; }
        public decimal TotalPaidRevenue { get; set; }
        public decimal TotalUnpaidDebt { get; set; }
        public List<Apartment> LatestResidents { get; set; } = new();
        public List<Invoice> LatestInvoices { get; set; } = new();
        public List<string> MonthlyLabels { get; set; } = new();
        public List<double> MonthlyPaid { get; set; } = new();
        public List<double> MonthlyUnpaid { get; set; } = new();
    }

    public interface IDashboardService
    {
        Task<DashboardStatsDto> GetDashboardStatsAsync();
    }
}
