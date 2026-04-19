namespace WebNangCao.Models
{
    public class Transaction : BaseEntity
    {
        public int InvoiceId { get; set; }
        public decimal Amount { get; set; }
        public DateTime PaymentDate { get; set; } = DateTime.UtcNow;
        public string PaymentMethod { get; set; } = "Chuyển khoản";
        public string? Note { get; set; }

        public Invoice Invoice { get; set; } = null!;
    }
}
