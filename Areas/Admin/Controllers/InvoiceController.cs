using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebNangCao.Data;
using WebNangCao.Models;
using WebNangCao.Models.ViewModels.Admin;
using WebNangCao.Services;


namespace WebNangCao.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class InvoiceController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IEmailService _emailService;

        // Đơn giá mặc định
        private const decimal DefaultElectricityPrice = 3500;  // VNĐ/kWh
        private const decimal DefaultWaterPrice = 15000;       // VNĐ/m³
        private const decimal DefaultServiceFeePerM2 = 15000;  // VNĐ/m²

        public InvoiceController(AppDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        // GET: Admin/Invoice
        public async Task<IActionResult> Index(int? month, int? year, int? apartmentId, InvoiceStatus? status)
        {
            var query = _context.Invoices
                .Include(i => i.Apartment)
                    .ThenInclude(a => a.Owner)
                .Where(i => !i.IsDeleted);

            // Filters
            if (month.HasValue) query = query.Where(i => i.Month == month.Value);
            if (year.HasValue) query = query.Where(i => i.Year == year.Value);
            if (apartmentId.HasValue) query = query.Where(i => i.ApartmentId == apartmentId.Value);
            if (status.HasValue) query = query.Where(i => i.Status == status.Value);

            var invoices = await query
                .OrderByDescending(i => i.Year)
                .ThenByDescending(i => i.Month)
                .ThenBy(i => i.Apartment.ApartmentNumber)
                .Select(i => new InvoiceListVM
                {
                    Id = i.Id,
                    ApartmentNumber = i.Apartment.ApartmentNumber,
                    OwnerName = i.Apartment.Owner != null ? i.Apartment.Owner.FullName : null,
                    Month = i.Month,
                    Year = i.Year,
                    ElectricityUsage = i.ElectricityUsage,
                    ElectricityUnitPrice = i.ElectricityUnitPrice,
                    WaterUsage = i.WaterUsage,
                    WaterUnitPrice = i.WaterUnitPrice,
                    ServiceFee = i.ServiceFee,
                    Status = i.Status,
                    DueDate = i.DueDate
                })
                .ToListAsync();

            // Thống kê nhanh
            ViewBag.TotalInvoices = invoices.Count;
            ViewBag.UnpaidCount = invoices.Count(i => i.Status == InvoiceStatus.Unpaid);
            ViewBag.PaidCount = invoices.Count(i => i.Status == InvoiceStatus.Paid);
            ViewBag.TotalRevenue = invoices.Where(i => i.Status == InvoiceStatus.Paid).Sum(i => i.TotalAmount);

            // Filter values cho view
            ViewBag.FilterMonth = month;
            ViewBag.FilterYear = year;
            ViewBag.FilterApartmentId = apartmentId;
            ViewBag.FilterStatus = status;

            await PopulateApartmentDropdown(apartmentId);

            return View(invoices);
        }

        // GET: Admin/Invoice/Create
        public async Task<IActionResult> Create()
        {
            await PopulateApartmentDropdown();
            var model = new CreateInvoiceVM
            {
                Month = DateTime.Now.Month,
                Year = DateTime.Now.Year,
                ElectricityUnitPrice = DefaultElectricityPrice,
                WaterUnitPrice = DefaultWaterPrice,
                DueDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(1).AddDays(14)
            };
            return View(model);
        }

        // POST: Admin/Invoice/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateInvoiceVM model)
        {
            if (!ModelState.IsValid)
            {
                await PopulateApartmentDropdown(model.ApartmentId);
                return View(model);
            }

            // Kiểm tra trùng hóa đơn
            var exists = await _context.Invoices
                .AnyAsync(i => i.ApartmentId == model.ApartmentId
                    && i.Month == model.Month && i.Year == model.Year && !i.IsDeleted);
            if (exists)
            {
                ModelState.AddModelError("", $"Căn hộ này đã có hóa đơn tháng {model.Month}/{model.Year}.");
                await PopulateApartmentDropdown(model.ApartmentId);
                return View(model);
            }

            var invoice = new Invoice
            {
                ApartmentId = model.ApartmentId,
                Month = model.Month,
                Year = model.Year,
                ElectricityUsage = model.ElectricityUsage,
                ElectricityUnitPrice = model.ElectricityUnitPrice,
                WaterUsage = model.WaterUsage,
                WaterUnitPrice = model.WaterUnitPrice,
                ServiceFee = model.ServiceFee,
                DueDate = model.DueDate,
                Status = InvoiceStatus.Unpaid
            };

            _context.Invoices.Add(invoice);
            await _context.SaveChangesAsync();

            // Gửi mail thông báo cho cư dân
            try
            {
                var apartment = await _context.Apartments
                    .Include(a => a.Owner)
                    .FirstOrDefaultAsync(a => a.Id == model.ApartmentId);

                if (apartment?.Owner?.Email != null)
                {
                    var subject = $"[Thông báo] Hóa đơn tiền điện/nước tháng {model.Month}/{model.Year}";
                    var body = $@"
                        <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; border: 1px solid #e0e0e0; padding: 20px;'>
                            <h2 style='color: #04a9f5;'>Thông báo hóa đơn mới</h2>
                            <p>Kính gửi ông/bà <strong>{apartment.Owner.FullName}</strong>,</p>
                            <p>Ban quản lý chung cư xin thông báo hóa đơn tháng <strong>{model.Month}/{model.Year}</strong> của căn hộ <strong>{apartment.ApartmentNumber}</strong> đã được khởi tạo.</p>
                            <table style='width: 100%; border-collapse: collapse; margin: 20px 0;'>
                                <tr style='background: #f4f7fa;'>
                                    <th style='padding: 10px; border: 1px solid #eee; text-align: left;'>Hạng mục</th>
                                    <th style='padding: 10px; border: 1px solid #eee; text-align: right;'>Thành tiền</th>
                                </tr>
                                <tr>
                                    <td style='padding: 10px; border: 1px solid #eee;'>Tiền điện ({model.ElectricityUsage} kWh)</td>
                                    <td style='padding: 10px; border: 1px solid #eee; text-align: right;'>{(model.ElectricityUsage * model.ElectricityUnitPrice):N0}đ</td>
                                </tr>
                                <tr>
                                    <td style='padding: 10px; border: 1px solid #eee;'>Tiền nước ({model.WaterUsage} m³)</td>
                                    <td style='padding: 10px; border: 1px solid #eee; text-align: right;'>{(model.WaterUsage * model.WaterUnitPrice):N0}đ</td>
                                </tr>
                                <tr>
                                    <td style='padding: 10px; border: 1px solid #eee;'>Phí dịch vụ</td>
                                    <td style='padding: 10px; border: 1px solid #eee; text-align: right;'>{model.ServiceFee:N0}đ</td>
                                </tr>
                                <tr style='font-weight: bold; font-size: 16px; color: #04a9f5;'>
                                    <td style='padding: 10px; border: 1px solid #eee;'>TỔNG CỘNG</td>
                                    <td style='padding: 10px; border: 1px solid #eee; text-align: right;'>{invoice.TotalAmount:N0}đ</td>
                                </tr>
                            </table>
                            <p>Hạn thanh toán: <strong>{model.DueDate:dd/MM/yyyy}</strong></p>
                            <p>Vui lòng đăng nhập vào hệ thống để xem chi tiết và thực hiện thanh toán.</p>
                            <div style='text-align: center; margin: 30px 0;'>
                                <a href='#' style='background: #04a9f5; color: white; padding: 12px 25px; text-decoration: none; border-radius: 4px; font-weight: bold;'>XEM HÓA ĐƠN</a>
                            </div>
                            <hr style='border: none; border-top: 1px solid #eee;'/>
                            <p style='font-size: 12px; color: #999;'>Ban quản lý Chung cư Smart</p>
                        </div>";

                    await _emailService.SendEmailAsync(apartment.Owner.Email, subject, body);
                }
            }
            catch (Exception)
            {
                // Bỏ qua lỗi gửi mail để không làm gián đoạn việc lưu hóa đơn
            }

            TempData["SuccessMessage"] = $"Đã tạo hóa đơn tháng {model.Month}/{model.Year} — Tổng: {invoice.TotalAmount:N0}đ. Đã gửi email thông báo.";
            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/Invoice/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var invoice = await _context.Invoices
                .Include(i => i.Apartment)
                .FirstOrDefaultAsync(i => i.Id == id && !i.IsDeleted);

            if (invoice == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy hóa đơn.";
                return RedirectToAction(nameof(Index));
            }

            var model = new EditInvoiceVM
            {
                Id = invoice.Id,
                ApartmentId = invoice.ApartmentId,
                ApartmentNumber = invoice.Apartment.ApartmentNumber,
                Month = invoice.Month,
                Year = invoice.Year,
                ElectricityUsage = invoice.ElectricityUsage,
                ElectricityUnitPrice = invoice.ElectricityUnitPrice,
                WaterUsage = invoice.WaterUsage,
                WaterUnitPrice = invoice.WaterUnitPrice,
                ServiceFee = invoice.ServiceFee,
                DueDate = invoice.DueDate,
                Status = invoice.Status
            };

            return View(model);
        }

        // POST: Admin/Invoice/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditInvoiceVM model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var invoice = await _context.Invoices
                .FirstOrDefaultAsync(i => i.Id == model.Id && !i.IsDeleted);

            if (invoice == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy hóa đơn.";
                return RedirectToAction(nameof(Index));
            }

            var oldStatus = invoice.Status;
            
            invoice.Month = model.Month;
            invoice.Year = model.Year;
            invoice.ElectricityUsage = model.ElectricityUsage;
            invoice.ElectricityUnitPrice = model.ElectricityUnitPrice;
            invoice.WaterUsage = model.WaterUsage;
            invoice.WaterUnitPrice = model.WaterUnitPrice;
            invoice.ServiceFee = model.ServiceFee;
            invoice.DueDate = model.DueDate;
            invoice.Status = model.Status;
            invoice.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Nếu trạng thái đổi từ Unpaid sang Paid thì gửi mail cảm ơn/xác nhận
            if (oldStatus == InvoiceStatus.Unpaid && model.Status == InvoiceStatus.Paid)
            {
                try
                {
                    var apartment = await _context.Apartments
                        .Include(a => a.Owner)
                        .FirstOrDefaultAsync(a => a.Id == invoice.ApartmentId);

                    if (apartment?.Owner?.Email != null)
                    {
                        var subject = $"[Xác nhận] Thanh toán hóa đơn tháng {model.Month}/{model.Year} thành công";
                        var body = $@"
                            <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; border: 1px solid #e0e0e0; padding: 20px;'>
                                <div style='text-align: center; color: #28a745; margin-bottom: 20px;'>
                                    <h2 style='margin: 0;'>Thanh toán thành công!</h2>
                                    <p style='font-size: 16px;'>Cảm ơn bạn đã hoàn thành nghĩa vụ thanh toán.</p>
                                </div>
                                <p>Kính gửi ông/bà <strong>{apartment.Owner.FullName}</strong>,</p>
                                <p>Hệ thống đã ghi nhận thanh toán cho hóa đơn tháng <strong>{model.Month}/{model.Year}</strong> của căn hộ <strong>{apartment.ApartmentNumber}</strong>.</p>
                                <div style='background: #f8f9fa; padding: 20px; border-radius: 4px; margin: 20px 0;'>
                                    <p style='margin: 5px 0;'>Mã hóa đơn: <strong>#{invoice.Id}</strong></p>
                                    <p style='margin: 5px 0;'>Số tiền: <strong style='color: #04a9f5;'>{invoice.TotalAmount:N0}đ</strong></p>
                                    <p style='margin: 5px 0;'>Ngày thanh toán: <strong>{DateTime.Now:dd/MM/yyyy HH:mm}</strong></p>
                                    <p style='margin: 5px 0;'>Trạng thái: <strong style='color: #28a745;'>Đã thanh toán</strong></p>
                                </div>
                                <p>Nếu có bất kỳ thắc mắc nào, vui lòng liên hệ Ban quản lý để được hỗ trợ.</p>
                                <hr style='border: none; border-top: 1px solid #eee; margin: 20px 0;'/>
                                <p style='font-size: 12px; color: #999;'>Ban quản lý Chung cư Smart</p>
                            </div>";

                        await _emailService.SendEmailAsync(apartment.Owner.Email, subject, body);
                    }
                }
                catch (Exception) { /* Bỏ qua lỗi gửi mail */ }
            }

            TempData["SuccessMessage"] = $"Đã cập nhật hóa đơn tháng {model.Month}/{model.Year}.";
            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/Invoice/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var invoice = await _context.Invoices
                .Include(i => i.Apartment)
                .FirstOrDefaultAsync(i => i.Id == id && !i.IsDeleted);

            if (invoice == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy hóa đơn.";
                return RedirectToAction(nameof(Index));
            }

            invoice.IsDeleted = true;
            invoice.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã xóa hóa đơn T{invoice.Month}/{invoice.Year} - {invoice.Apartment.ApartmentNumber}.";
            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/Invoice/CreateBatch - Tạo hóa đơn hàng loạt
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateBatch(int month, int year, decimal serviceFeePerM2)
        {
            var apartments = await _context.Apartments
                .Where(a => !a.IsDeleted && a.OwnerId != null)
                .ToListAsync();

            int created = 0;
            foreach (var apt in apartments)
            {
                var exists = await _context.Invoices
                    .AnyAsync(i => i.ApartmentId == apt.Id && i.Month == month && i.Year == year && !i.IsDeleted);
                if (exists) continue;

                var invoice = new Invoice
                {
                    ApartmentId = apt.Id,
                    Month = month,
                    Year = year,
                    ElectricityUsage = 0,
                    ElectricityUnitPrice = DefaultElectricityPrice,
                    WaterUsage = 0,
                    WaterUnitPrice = DefaultWaterPrice,
                    ServiceFee = (decimal)apt.Area * serviceFeePerM2,
                    DueDate = new DateTime(year, month, 1).AddMonths(1).AddDays(14),
                    Status = InvoiceStatus.Unpaid
                };

                _context.Invoices.Add(invoice);
                created++;
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã tạo {created} hóa đơn tháng {month}/{year} (phí DV: {serviceFeePerM2:N0}đ/m²). Vui lòng cập nhật số điện/nước cho từng căn hộ.";
            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateApartmentDropdown(int? selectedId = null)
        {
            var apartments = await _context.Apartments
                .Where(a => !a.IsDeleted)
                .Include(a => a.Owner)
                .OrderBy(a => a.ApartmentNumber)
                .Select(a => new
                {
                    a.Id,
                    Display = a.ApartmentNumber + " - Tầng " + a.Floor
                        + (a.Owner != null ? " (" + a.Owner.FullName + ")" : " (Chưa có chủ)")
                })
                .ToListAsync();

            ViewBag.Apartments = new SelectList(apartments, "Id", "Display", selectedId);
        }
    }
}
