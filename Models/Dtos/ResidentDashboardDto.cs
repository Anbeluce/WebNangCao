using System.Collections.Generic;

namespace WebNangCao.Models.Dtos
{
    public class ResidentDashboardDto
    {
        public string UserName { get; set; } = string.Empty;
        public List<Apartment> Apartments { get; set; } = new();
        public int TotalInvoices { get; set; }
        public int UnpaidInvoices { get; set; }
        public decimal TotalUnpaid { get; set; }
    }
}
