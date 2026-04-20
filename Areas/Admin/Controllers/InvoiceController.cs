using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebNangCao.Data;
using WebNangCao.Models;
using WebNangCao.Models.ViewModels.Admin;

namespace WebNangCao.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class InvoiceController : Controller
    {
        private readonly AppDbContext _context;

        // Đơn giá mặc định
        private const decimal DefaultElectricityPrice = 3500;  // VNĐ/kWh
        private const decimal DefaultWaterPrice = 15000;       // VNĐ/m³
        private const decimal DefaultServiceFeePerM2 = 15000;  // VNĐ/m²

        public InvoiceController(AppDbContext context)
        {
            _context = context;
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

            TempData["SuccessMessage"] = $"Đã tạo hóa đơn tháng {model.Month}/{model.Year} — Tổng: {invoice.TotalAmount:N0}đ";
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

            TempData["SuccessMessage"] = $"Đã cập nhật hóa đơn tháng {model.Month}/{model.Year} — Tổng: {invoice.TotalAmount:N0}đ";
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
