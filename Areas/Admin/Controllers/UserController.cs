using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebNangCao.Models;
using WebNangCao.Models.ViewModels.Admin;

namespace WebNangCao.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class UserController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public UserController(UserManager<ApplicationUser> userManager,
                              RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // GET: /Admin/User
        public async Task<IActionResult> Index(string? search, string? roleFilter, bool? activeFilter)
        {
            var usersQuery = _userManager.Users.AsQueryable();

            // Lọc theo search
            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim().ToLower();
                usersQuery = usersQuery.Where(u =>
                    u.FullName.ToLower().Contains(search) ||
                    u.Email!.ToLower().Contains(search) ||
                    (u.PhoneNumber != null && u.PhoneNumber.Contains(search)));
            }

            // Lọc theo trạng thái
            if (activeFilter.HasValue)
            {
                usersQuery = usersQuery.Where(u => u.IsActive == activeFilter.Value);
            }

            var users = await usersQuery.OrderBy(u => u.FullName).ToListAsync();

            // Build ViewModel
            var userVMs = new List<UserListVM>();
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);

                // Lọc theo role
                if (!string.IsNullOrWhiteSpace(roleFilter) && !roles.Contains(roleFilter))
                    continue;

                userVMs.Add(new UserListVM
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Email = user.Email ?? "",
                    PhoneNumber = user.PhoneNumber,
                    IdentityCardNumber = user.IdentityCardNumber,
                    DateOfBirth = user.DateOfBirth,
                    IsActive = user.IsActive,
                    Roles = roles,
                    CreatedAt = DateTime.UtcNow.AddHours(7) // Placeholder vì Identity không lưu CreatedAt mặc định
                });
            }

            ViewBag.Search = search;
            ViewBag.RoleFilter = roleFilter;
            ViewBag.ActiveFilter = activeFilter;
            ViewBag.AllRoles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();

            return View(userVMs);
        }

        // GET: /Admin/User/Create
        public async Task<IActionResult> Create()
        {
            ViewBag.AllRoles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();
            return View(new CreateUserVM());
        }

        // POST: /Admin/User/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateUserVM model)
        {
            ViewBag.AllRoles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();

            if (!ModelState.IsValid)
                return View(model);

            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError("Email", "Email này đã được sử dụng.");
                return View(model);
            }

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName,
                PhoneNumber = model.PhoneNumber,
                IdentityCardNumber = model.IdentityCardNumber,
                DateOfBirth = model.DateOfBirth,
                IsActive = model.IsActive,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, model.Role);
                TempData["SuccessMessage"] = $"Tạo tài khoản '{model.FullName}' thành công!";
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            return View(model);
        }

        // GET: /Admin/User/Edit/{id}
        public async Task<IActionResult> Edit(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound();

            var roles = await _userManager.GetRolesAsync(user);
            ViewBag.AllRoles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();

            var model = new EditUserVM
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? "",
                PhoneNumber = user.PhoneNumber,
                IdentityCardNumber = user.IdentityCardNumber,
                DateOfBirth = user.DateOfBirth,
                IsActive = user.IsActive,
                Role = roles.FirstOrDefault() ?? "Resident"
            };

            return View(model);
        }

        // POST: /Admin/User/Edit/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, EditUserVM model)
        {
            ViewBag.AllRoles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();

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

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound();

            // Cập nhật thông tin
            user.FullName = model.FullName;
            user.PhoneNumber = model.PhoneNumber;
            user.IdentityCardNumber = model.IdentityCardNumber;
            user.DateOfBirth = model.DateOfBirth;
            user.IsActive = model.IsActive;

            // Cập nhật email nếu thay đổi
            if (user.Email != model.Email)
            {
                var emailExists = await _userManager.FindByEmailAsync(model.Email);
                if (emailExists != null && emailExists.Id != id)
                {
                    ModelState.AddModelError("Email", "Email này đã được sử dụng.");
                    return View(model);
                }
                user.Email = model.Email;
                user.UserName = model.Email;
                user.NormalizedEmail = model.Email.ToUpper();
                user.NormalizedUserName = model.Email.ToUpper();
            }

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                foreach (var error in updateResult.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);
                return View(model);
            }

            // Cập nhật role
            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
            await _userManager.AddToRoleAsync(user, model.Role);

            // Đổi mật khẩu nếu có
            if (!string.IsNullOrEmpty(model.NewPassword))
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var passResult = await _userManager.ResetPasswordAsync(user, token, model.NewPassword);
                if (!passResult.Succeeded)
                {
                    foreach (var error in passResult.Errors)
                        ModelState.AddModelError(string.Empty, error.Description);
                    return View(model);
                }
            }

            TempData["SuccessMessage"] = $"Cập nhật tài khoản '{user.FullName}' thành công!";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Admin/User/Details/{id}
        public async Task<IActionResult> Details(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound();

            var roles = await _userManager.GetRolesAsync(user);

            var vm = new UserListVM
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? "",
                PhoneNumber = user.PhoneNumber,
                IdentityCardNumber = user.IdentityCardNumber,
                DateOfBirth = user.DateOfBirth,
                IsActive = user.IsActive,
                Roles = roles
            };

            return View(vm);
        }

        // POST: /Admin/User/ToggleActive/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound();

            // Không cho phép khóa chính tài khoản của mình
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser?.Id == id)
            {
                TempData["ErrorMessage"] = "Không thể thay đổi trạng thái tài khoản của chính mình!";
                return RedirectToAction(nameof(Index));
            }

            user.IsActive = !user.IsActive;
            await _userManager.UpdateAsync(user);

            var status = user.IsActive ? "kích hoạt" : "vô hiệu hóa";
            TempData["SuccessMessage"] = $"Đã {status} tài khoản '{user.FullName}'.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Admin/User/Delete/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return NotFound();

            // Không cho phép xóa tài khoản của chính mình
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser?.Id == id)
            {
                TempData["ErrorMessage"] = "Không thể xóa tài khoản của chính mình!";
                return RedirectToAction(nameof(Index));
            }

            await _userManager.DeleteAsync(user);
            TempData["SuccessMessage"] = $"Đã xóa tài khoản '{user.FullName}'.";
            return RedirectToAction(nameof(Index));
        }
    }
}
