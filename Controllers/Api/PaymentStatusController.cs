using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebNangCao.Data;
using WebNangCao.Models;

namespace WebNangCao.Controllers.Api
{
    [Route("api/payment/[action]")]
    [ApiController]
    public class PaymentStatusController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PaymentStatusController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("{invoiceId}")]
        [Authorize(Roles = "Resident,Admin")]
        public async Task<IActionResult> Status(int invoiceId)
        {
            var invoice = await _context.Invoices.FirstOrDefaultAsync(i => i.Id == invoiceId);
            if (invoice == null)
            {
                return NotFound();
            }

            var totalPaid = await _context.Transactions
                .Where(t => t.InvoiceId == invoiceId)
                .SumAsync(t => t.Amount);

            return Ok(new
            {
                status = invoice.Status.ToString(),
                totalAmount = invoice.TotalAmount,
                paidAmount = totalPaid,
                remaining = invoice.TotalAmount - totalPaid,
                isPaid = invoice.Status == InvoiceStatus.Paid
            });
        }
    }
}
