using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
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
    public class ApartmentController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ApartmentController(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Admin/Apartment
        public async Task<IActionResult> Index()
        {
            var apartments = await _context.Apartments
                .Include(a => a.Owner)
                .Where(a => !a.IsDeleted)
                .OrderBy(a => a.Floor)
                .ThenBy(a => a.ApartmentNumber)
                .Select(a => new ApartmentListVM
                {
                    Id = a.Id,
                    ApartmentNumber = a.ApartmentNumber,
                    Area = a.Area,
                    Floor = a.Floor,
                    OwnerName = a.Owner != null ? a.Owner.FullName : null,
                    OwnerEmail = a.Owner != null ? a.Owner.Email : null,
                    InvoiceCount = a.Invoices.Count(i => !i.IsDeleted)
                })
                .ToListAsync();

            // Tính toán số liệu thống kê KPI
            int total = apartments.Count;
            int occupied = apartments.Count(a => a.OwnerName != null);
            int empty = total - occupied;
            double occupancyRate = total > 0 ? Math.Round((double)occupied / total * 100, 1) : 0;

            ViewBag.TotalCount = total;
            ViewBag.OccupiedCount = occupied;
            ViewBag.EmptyCount = empty;
            ViewBag.OccupancyRate = occupancyRate;

            return View(apartments);
        }

        // GET: Admin/Apartment/Create
        public async Task<IActionResult> Create()
        {
            await PopulateOwnerDropdown();
            return View(new CreateApartmentVM());
        }

        // POST: Admin/Apartment/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateApartmentVM model)
        {
            if (!ModelState.IsValid)
            {
                await PopulateOwnerDropdown(model.OwnerId);
                return View(model);
            }

            // Kiểm tra số căn hộ đã tồn tại
            var exists = await _context.Apartments
                .AnyAsync(a => a.ApartmentNumber == model.ApartmentNumber && !a.IsDeleted);
            if (exists)
            {
                ModelState.AddModelError("ApartmentNumber", "Số căn hộ này đã tồn tại.");
                await PopulateOwnerDropdown(model.OwnerId);
                return View(model);
            }

            var apartment = new Apartment
            {
                ApartmentNumber = model.ApartmentNumber,
                Area = model.Area,
                Floor = model.Floor,
                OwnerId = model.OwnerId
            };

            _context.Apartments.Add(apartment);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã thêm căn hộ {apartment.ApartmentNumber} thành công!";
            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/Apartment/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var apartment = await _context.Apartments
                .Include(a => a.Owner)
                .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted);

            if (apartment == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy căn hộ.";
                return RedirectToAction(nameof(Index));
            }

            var model = new EditApartmentVM
            {
                Id = apartment.Id,
                ApartmentNumber = apartment.ApartmentNumber,
                Area = apartment.Area,
                Floor = apartment.Floor,
                OwnerId = apartment.OwnerId
            };

            // Pass the current owner's text to ViewBag for Select2 initialization
            ViewBag.CurrentOwnerName = apartment.Owner != null ? $"{apartment.Owner.FullName} ({apartment.Owner.Email})" : null;

            await PopulateOwnerDropdown(model.OwnerId);
            return View(model);
        }

        // POST: Admin/Apartment/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditApartmentVM model)
        {
            if (!ModelState.IsValid)
            {
                if (!string.IsNullOrEmpty(model.OwnerId))
                {
                    var owner = await _userManager.FindByIdAsync(model.OwnerId);
                    ViewBag.CurrentOwnerName = owner != null ? $"{owner.FullName} ({owner.Email})" : null;
                }
                await PopulateOwnerDropdown(model.OwnerId);
                return View(model);
            }

            var apartment = await _context.Apartments
                .FirstOrDefaultAsync(a => a.Id == model.Id && !a.IsDeleted);

            if (apartment == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy căn hộ.";
                return RedirectToAction(nameof(Index));
            }

            // Kiểm tra trùng số căn hộ (trừ chính nó)
            var exists = await _context.Apartments
                .AnyAsync(a => a.ApartmentNumber == model.ApartmentNumber && a.Id != model.Id && !a.IsDeleted);
            if (exists)
            {
                ModelState.AddModelError("ApartmentNumber", "Số căn hộ này đã tồn tại.");
                if (!string.IsNullOrEmpty(model.OwnerId))
                {
                    var owner = await _userManager.FindByIdAsync(model.OwnerId);
                    ViewBag.CurrentOwnerName = owner != null ? $"{owner.FullName} ({owner.Email})" : null;
                }
                await PopulateOwnerDropdown(model.OwnerId);
                return View(model);
            }

            apartment.ApartmentNumber = model.ApartmentNumber;
            apartment.Area = model.Area;
            apartment.Floor = model.Floor;
            apartment.OwnerId = model.OwnerId;
            apartment.UpdatedAt = DateTime.UtcNow.AddHours(7);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã cập nhật căn hộ {apartment.ApartmentNumber} thành công!";
            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/Apartment/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var apartment = await _context.Apartments
                .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted);

            if (apartment == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy căn hộ.";
                return RedirectToAction(nameof(Index));
            }

            // Soft delete
            apartment.IsDeleted = true;
            apartment.UpdatedAt = DateTime.UtcNow.AddHours(7);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã xóa căn hộ {apartment.ApartmentNumber}.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> SearchUsers(string? q)
        {
            var query = q?.Trim().ToLower();

            var usersQuery = _userManager.Users.Where(u => u.IsActive);

            if (!string.IsNullOrEmpty(query))
            {
                usersQuery = usersQuery.Where(u =>
                    u.FullName.ToLower().Contains(query) ||
                    (u.Email != null && u.Email.ToLower().Contains(query)) ||
                    (u.PhoneNumber != null && u.PhoneNumber.ToLower().Contains(query)) ||
                    (u.IdentityCardNumber != null && u.IdentityCardNumber.ToLower().Contains(query))
                );
            }

            var users = await usersQuery
                .OrderBy(u => u.FullName)
                .Take(20)
                .Select(u => new
                {
                    id = u.Id,
                    text = u.FullName + " (" + u.Email + ")" + (u.PhoneNumber != null ? " - SĐT: " + u.PhoneNumber : "") + (u.IdentityCardNumber != null ? " - CCCD: " + u.IdentityCardNumber : "")
                })
                .ToListAsync();

            return Json(users);
        }

        private async Task PopulateOwnerDropdown(string? selectedId = null)
        {
            var users = await _userManager.Users
                .Where(u => u.IsActive)
                .OrderBy(u => u.FullName)
                .Select(u => new { u.Id, Display = u.FullName + " (" + u.Email + ")" })
                .ToListAsync();

            ViewBag.Owners = new SelectList(users, "Id", "Display", selectedId);
        }
    }
}
