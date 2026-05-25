using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebNangCao.Models.ViewModels.Admin;
using WebNangCao.Services;

namespace WebNangCao.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class UserController : Controller
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        // GET: /Admin/User
        public async Task<IActionResult> Index(string? search, string? roleFilter, bool? activeFilter)
        {
            var userVMs = await _userService.GetUserListAsync(search, roleFilter, activeFilter);

            ViewBag.Search = search;
            ViewBag.RoleFilter = roleFilter;
            ViewBag.ActiveFilter = activeFilter;
            ViewBag.AllRoles = await _userService.GetAllRolesAsync();

            return View(userVMs);
        }

        // GET: /Admin/User/Create
        public async Task<IActionResult> Create()
        {
            ViewBag.AllRoles = await _userService.GetAllRolesAsync();
            return View(new CreateUserVM());
        }

        // POST: /Admin/User/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateUserVM model)
        {
            ViewBag.AllRoles = await _userService.GetAllRolesAsync();

            if (!ModelState.IsValid)
                return View(model);

            var result = await _userService.CreateUserAsync(model);
            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
                return RedirectToAction(nameof(Index));
            }

            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Lỗi tạo tài khoản.");
            return View(model);
        }

        // GET: /Admin/User/Edit/{id}
        public async Task<IActionResult> Edit(string id)
        {
            var model = await _userService.GetUserForEditAsync(id);
            if (model == null)
                return NotFound();

            ViewBag.AllRoles = await _userService.GetAllRolesAsync();
            return View(model);
        }

        // POST: /Admin/User/Edit/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, EditUserVM model)
        {
            ViewBag.AllRoles = await _userService.GetAllRolesAsync();

            if (id != model.Id)
                return BadRequest();

            // Bỏ validate password nếu không đổi
            if (string.IsNullOrEmpty(model.NewPassword))
            {
                ModelState.Remove("NewPassword");
                ModelState.Remove("ConfirmNewPassword");
            }

            if (!ModelState.IsValid)
                return View(model);

            var result = await _userService.UpdateUserAsync(id, model);
            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
                return RedirectToAction(nameof(Index));
            }

            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Lỗi cập nhật tài khoản.");
            return View(model);
        }

        // GET: /Admin/User/Details/{id}
        public async Task<IActionResult> Details(string id)
        {
            var vm = await _userService.GetUserDetailsAsync(id);
            if (vm == null)
                return NotFound();

            return View(vm);
        }

        // POST: /Admin/User/ToggleActive/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(string id)
        {
            var result = await _userService.ToggleUserActiveAsync(id, User);
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

        // POST: /Admin/User/Delete/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            var result = await _userService.DeleteUserAsync(id, User);
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
    }
}
