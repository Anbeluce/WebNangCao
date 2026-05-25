using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WebNangCao.Data;
using WebNangCao.Models;
using WebNangCao.Models.Configs;
using WebNangCao.Models.ViewModels.Admin;
using WebNangCao.Services;

namespace WebNangCao.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class InvoiceController : Controller
    {
        private readonly IInvoiceService _invoiceService;

        public InvoiceController(IInvoiceService invoiceService)
        {
            _invoiceService = invoiceService;
        }

        // GET: Admin/Invoice
        public async Task<IActionResult> Index(int? month, int? year, int? apartmentId, InvoiceStatus? status)
        {
            var invoices = await _invoiceService.GetInvoiceListAsync(month, year, apartmentId, status);

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
            var model = await _invoiceService.GetCreateDefaultsAsync();
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

            var result = await _invoiceService.CreateInvoiceAsync(model);
            if (!result.Success)
            {
                ModelState.AddModelError("", result.ErrorMessage ?? "Lỗi tạo hóa đơn.");
                await PopulateApartmentDropdown(model.ApartmentId);
                return View(model);
            }

            TempData["SuccessMessage"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/Invoice/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var model = await _invoiceService.GetInvoiceForEditAsync(id);
            if (model == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy hóa đơn.";
                return RedirectToAction(nameof(Index));
            }

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

            var result = await _invoiceService.UpdateInvoiceAsync(model);
            if (!result.Success)
            {
                TempData["ErrorMessage"] = result.ErrorMessage;
                return RedirectToAction(nameof(Index));
            }

            TempData["SuccessMessage"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/Invoice/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _invoiceService.DeleteInvoiceAsync(id);
            if (!result.Success)
            {
                TempData["ErrorMessage"] = result.ErrorMessage;
            }
            else
            {
                TempData["SuccessMessage"] = result.Message;
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/Invoice/DownloadTemplate
        public async Task<IActionResult> DownloadTemplate()
        {
            var fileBytes = await _invoiceService.DownloadTemplateAsync();
            return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Mau_Nhap_Hoa_Don.xlsx");
        }

        // POST: Admin/Invoice/ImportExcel
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ImportExcel(IFormFile excelFile)
        {
            if (excelFile == null || excelFile.Length == 0)
            {
                TempData["ErrorMessage"] = "Vui lòng chọn tệp Excel để nhập.";
                return RedirectToAction(nameof(Index));
            }

            var result = await _invoiceService.ImportExcelAsync(excelFile);
            var importResult = result.Data!;

            ViewBag.Errors = importResult.Errors;
            ViewBag.Warnings = importResult.Warnings;
            ViewBag.SuccessCount = importResult.SuccessCount;

            return View("ImportResults");
        }

        private async Task PopulateApartmentDropdown(int? selectedId = null)
        {
            var apartments = await _invoiceService.GetApartmentDropdownAsync();

            ViewBag.ApartmentsList = apartments;
            ViewBag.Apartments = new SelectList(apartments, "Id", "ApartmentNumber", selectedId);
            ViewBag.DefaultManagementFeePerM2 = await _invoiceService.GetDefaultManagementFeePerM2Async();
        }
    }
}
