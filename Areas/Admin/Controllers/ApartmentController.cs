using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebNangCao.Models;
using WebNangCao.Models.ViewModels.Admin;
using WebNangCao.Services;

namespace WebNangCao.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class ApartmentController : Controller
    {
        private readonly IApartmentService _apartmentService;

        public ApartmentController(IApartmentService apartmentService)
        {
            _apartmentService = apartmentService;
        }

        // GET: Admin/Apartment
        public async Task<IActionResult> Index()
        {
            var apartments = await _apartmentService.GetApartmentListWithStatsAsync();

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
            ViewBag.Owners = new SelectList(await _apartmentService.GetOwnerDropdownAsync(), "Id", "Display");
            return View(new CreateApartmentVM());
        }

        // POST: Admin/Apartment/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateApartmentVM model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Owners = new SelectList(await _apartmentService.GetOwnerDropdownAsync(), "Id", "Display", model.OwnerId);
                return View(model);
            }

            var result = await _apartmentService.CreateApartmentAsync(model);
            if (!result.Success)
            {
                ModelState.AddModelError("ApartmentNumber", result.ErrorMessage ?? "Lỗi tạo căn hộ.");
                ViewBag.Owners = new SelectList(await _apartmentService.GetOwnerDropdownAsync(), "Id", "Display", model.OwnerId);
                return View(model);
            }

            TempData["SuccessMessage"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/Apartment/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var model = await _apartmentService.GetApartmentForEditAsync(id);
            if (model == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy căn hộ.";
                return RedirectToAction(nameof(Index));
            }

            // Pass the current owner's text to ViewBag for Select2 initialization
            ViewBag.CurrentOwnerName = await _apartmentService.GetOwnerNameAndEmailAsync(model.OwnerId);

            ViewBag.Owners = new SelectList(await _apartmentService.GetOwnerDropdownAsync(), "Id", "Display", model.OwnerId);
            return View(model);
        }

        // POST: Admin/Apartment/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditApartmentVM model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.CurrentOwnerName = await _apartmentService.GetOwnerNameAndEmailAsync(model.OwnerId);
                ViewBag.Owners = new SelectList(await _apartmentService.GetOwnerDropdownAsync(), "Id", "Display", model.OwnerId);
                return View(model);
            }

            var result = await _apartmentService.UpdateApartmentAsync(model);
            if (!result.Success)
            {
                ModelState.AddModelError("ApartmentNumber", result.ErrorMessage ?? "Lỗi cập nhật căn hộ.");
                ViewBag.CurrentOwnerName = await _apartmentService.GetOwnerNameAndEmailAsync(model.OwnerId);
                ViewBag.Owners = new SelectList(await _apartmentService.GetOwnerDropdownAsync(), "Id", "Display", model.OwnerId);
                return View(model);
            }

            TempData["SuccessMessage"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/Apartment/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _apartmentService.DeleteApartmentAsync(id);
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

        [HttpGet]
        public async Task<IActionResult> SearchUsers(string? q)
        {
            var users = await _apartmentService.SearchUsersAsync(q);
            return Json(users);
        }

    }
}
