using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebNangCao.Data;
using WebNangCao.Models;

namespace WebNangCao.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PaymentController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("status/{invoiceId}")]
        [AllowAnonymous] // Hoặc yêu cầu Auth tùy logic, nhưng vì chỉ trả status nên để AllowAnonymous cũng được
        public async Task<IActionResult> CheckStatus(int invoiceId)
        {
            var invoice = await _context.Invoices.FirstOrDefaultAsync(i => i.Id == invoiceId);
            if (invoice == null)
            {
                return NotFound();
            }

            var previousAmounts = await _context.Transactions
                .Where(t => t.InvoiceId == invoice.Id && !t.IsDeleted)
                .Select(t => t.Amount)
                .ToListAsync();
            var totalPaid = previousAmounts.Sum();

            return Ok(new
            {
                isPaid = invoice.Status == InvoiceStatus.Paid,
                status = invoice.Status.ToString(),
                totalPaid = totalPaid
            });
        }
    }
}
