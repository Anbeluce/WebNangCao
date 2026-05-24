using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;
using WebNangCao.Data;
using WebNangCao.Models;
using WebNangCao.Models.Configs;

namespace WebNangCao.Services
{
    public class SepayService : ISepayService
    {
        private readonly AppDbContext _context;
        private readonly SepaySettings _settings;
        private readonly ILogger<SepayService> _logger;
        private readonly IEmailService _emailService;

        public SepayService(AppDbContext context, IOptions<SepaySettings> settings, ILogger<SepayService> logger, IEmailService emailService)
        {
            _context = context;
            _settings = settings.Value;
            _logger = logger;
            _emailService = emailService;
        }

        public string GenerateQrUrl(decimal amount, string content)
        {
            return $"https://qr.sepay.vn/img?acc={_settings.AccountNumber}&bank={_settings.BankCode}&amount={(int)amount}&des={content}&template=compact";
        }

        public bool VerifyWebhookSecret(string apiKeyHeader)
        {
            // Verifies the incoming API key matches the one we set in our config
            return string.Equals(_settings.WebhookSecret, apiKeyHeader, StringComparison.Ordinal);
        }

        public async Task<bool> ProcessWebhookAsync(SepayWebhookPayload payload)
        {
            // 1. Chống duplicate (trùng lặp) dựa trên transaction id của SePay
            bool exists = await _context.Transactions.AnyAsync(t => t.SepayTransactionId == payload.Id);
            if (exists)
            {
                _logger.LogInformation($"Giao dịch {payload.Id} đã được xử lý trước đó.");
                return true; // Return true to tell SePay we received it
            }

            // 2. Chỉ xử lý giao dịch cộng tiền (in)
            if (payload.TransferType != "in")
            {
                return true;
            }

            // 3. Tìm mã hóa đơn (VD: nội dung chuyển khoản là "Thanh toan HD 12" hoặc "HD12")
            int? invoiceId = ExtractInvoiceIdFromContent(payload.Content);
            if (invoiceId == null)
            {
                _logger.LogWarning($"Không thể nhận diện mã hóa đơn từ nội dung: {payload.Content}");
                return true; // Vẫn return true để SePay không gửi lại
            }

            // 4. Tìm hóa đơn trong DB
            var invoice = await _context.Invoices
                .Include(i => i.Apartment)
                .ThenInclude(a => a.Owner)
                .FirstOrDefaultAsync(i => i.Id == invoiceId.Value);

            if (invoice == null)
            {
                _logger.LogWarning($"Không tìm thấy hóa đơn ID {invoiceId} cho giao dịch {payload.Id}");
                return true;
            }

            // 5. Kiểm tra số tiền (chỉ update trạng thái nếu đủ tiền, hoặc xử lý theo logic kinh doanh)
            // Ở đây đơn giản hóa: Tạo Transaction và cập nhật thành Paid
            DateTime parsedDate = DateTime.UtcNow.AddHours(7);
            if (!string.IsNullOrEmpty(payload.TransactionDate) && DateTime.TryParse(payload.TransactionDate, out var dt))
            {
                parsedDate = dt;
            }

            var transaction = new Transaction
            {
                InvoiceId = invoice.Id,
                Amount = payload.TransferAmount,
                PaymentDate = parsedDate,
                PaymentMethod = "Chuyển khoản (SePay QR)",
                Note = $"Auto-paid via SePay. Ref: {payload.ReferenceCode}",
                SepayTransactionId = payload.Id
            };

            _context.Transactions.Add(transaction);

            // Tạm tính tổng số tiền đã thanh toán cho hóa đơn này (fix lỗi SQLite không hỗ trợ Sum kiểu decimal)
            var previousAmounts = await _context.Transactions
                .Where(t => t.InvoiceId == invoice.Id)
                .Select(t => t.Amount)
                .ToListAsync();
            
            var currentTotalPaid = previousAmounts.Sum();

            if (currentTotalPaid + transaction.Amount >= invoice.TotalAmount)
            {
                invoice.Status = InvoiceStatus.Paid;

                // Gửi email xác nhận thanh toán thành công qua SePay
                try
                {
                    if (invoice.Apartment?.Owner?.Email != null)
                    {
                        var subject = $"[Xác nhận] Thanh toán hóa đơn tháng {invoice.Month}/{invoice.Year} thành công";
                        var body = $@"
                            <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; border: 1px solid #e0e0e0; padding: 20px;'>
                                <div style='text-align: center; color: #28a745; margin-bottom: 20px;'>
                                    <h2 style='margin: 0;'>Thanh toán thành công!</h2>
                                    <p style='font-size: 16px;'>Cảm ơn bạn đã hoàn thành nghĩa vụ thanh toán qua SePay QR.</p>
                                </div>
                                <p>Kính gửi ông/bà <strong>{invoice.Apartment.Owner.FullName}</strong>,</p>
                                <p>Hệ thống đã ghi nhận thanh toán tự động cho hóa đơn tháng <strong>{invoice.Month}/{invoice.Year}</strong> của căn hộ <strong>{invoice.Apartment.ApartmentNumber}</strong>.</p>
                                <div style='background: #f8f9fa; padding: 20px; border-radius: 4px; margin: 20px 0;'>
                                    <p style='margin: 5px 0;'>Mã hóa đơn: <strong>#{invoice.Id}</strong></p>
                                    <p style='margin: 5px 0;'>Số tiền thanh toán: <strong style='color: #04a9f5;'>{payload.TransferAmount:N0}đ</strong></p>
                                    <p style='margin: 5px 0;'>Ngày thanh toán: <strong>{parsedDate:dd/MM/yyyy HH:mm}</strong></p>
                                    <p style='margin: 5px 0;'>Phương thức: <strong>Chuyển khoản (SePay QR)</strong></p>
                                    <p style='margin: 5px 0;'>Trạng thái hóa đơn: <strong style='color: #28a745;'>Đã thanh toán đủ</strong></p>
                                </div>
                                <p>Nếu có bất kỳ thắc mắc nào, vui lòng liên hệ Ban quản lý để được hỗ trợ.</p>
                                <hr style='border: none; border-top: 1px solid #eee; margin: 20px 0;'/>
                                <p style='font-size: 12px; color: #999;'>Ban quản lý Chung cư Smart</p>
                            </div>";

                        await _emailService.SendEmailAsync(invoice.Apartment.Owner.Email, subject, body);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi khi gửi email xác nhận thanh toán tự động qua SePay.");
                }
            }
            else
            {
                invoice.Status = InvoiceStatus.Partial;

                // Gửi email xác nhận thanh toán một phần qua SePay
                try
                {
                    if (invoice.Apartment?.Owner?.Email != null)
                    {
                        var subject = $"[Xác nhận] Ghi nhận thanh toán một phần hóa đơn tháng {invoice.Month}/{invoice.Year}";
                        var body = $@"
                            <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; border: 1px solid #e0e0e0; padding: 20px;'>
                                <div style='text-align: center; color: #ffc107; margin-bottom: 20px;'>
                                    <h2 style='margin: 0;'>Thanh toán một phần thành công!</h2>
                                    <p style='font-size: 16px;'>Cảm ơn bạn đã thực hiện thanh toán qua SePay QR.</p>
                                </div>
                                <p>Kính gửi ông/bà <strong>{invoice.Apartment.Owner.FullName}</strong>,</p>
                                <p>Hệ thống đã ghi nhận thanh toán một phần cho hóa đơn tháng <strong>{invoice.Month}/{invoice.Year}</strong> của căn hộ <strong>{invoice.Apartment.ApartmentNumber}</strong>.</p>
                                <div style='background: #f8f9fa; padding: 20px; border-radius: 4px; margin: 20px 0;'>
                                    <p style='margin: 5px 0;'>Mã hóa đơn: <strong>#{invoice.Id}</strong></p>
                                    <p style='margin: 5px 0;'>Số tiền thanh toán đợt này: <strong style='color: #04a9f5;'>{payload.TransferAmount:N0}đ</strong></p>
                                    <p style='margin: 5px 0;'>Tổng số tiền đã nhận: <strong>{(currentTotalPaid + transaction.Amount):N0}đ</strong> / {invoice.TotalAmount:N0}đ</p>
                                    <p style='margin: 5px 0;'>Ngày thanh toán: <strong>{parsedDate:dd/MM/yyyy HH:mm}</strong></p>
                                    <p style='margin: 5px 0;'>Phương thức: <strong>Chuyển khoản (SePay QR)</strong></p>
                                    <p style='margin: 5px 0;'>Trạng thái hóa đơn: <strong style='color: #ffc107;'>Thanh toán một phần</strong></p>
                                </div>
                                <p>Vui lòng chuyển khoản nốt số tiền còn thiếu trước hạn thanh toán để hoàn tất hóa đơn này.</p>
                                <p>Nếu có bất kỳ thắc mắc nào, vui lòng liên hệ Ban quản lý để được hỗ trợ.</p>
                                <hr style='border: none; border-top: 1px solid #eee; margin: 20px 0;'/>
                                <p style='font-size: 12px; color: #999;'>Ban quản lý Chung cư Smart</p>
                            </div>";

                        await _emailService.SendEmailAsync(invoice.Apartment.Owner.Email, subject, body);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi khi gửi email xác nhận thanh toán một phần qua SePay.");
                }
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation($"Đã xử lý thành công thanh toán {payload.TransferAmount} cho hóa đơn {invoice.Id}");

            return true;
        }

        private int? ExtractInvoiceIdFromContent(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return null;

            // Chuyển nội dung về chữ hoa để dễ tìm
            content = content.ToUpper();

            // Tìm chuỗi có dạng HD theo sau là các chữ số (VD: HD123 hoặc HD 123)
            var match = Regex.Match(content, @"HD\s*(\d+)");
            if (match.Success && int.TryParse(match.Groups[1].Value, out int id))
            {
                return id;
            }

            return null;
        }
    }
}
