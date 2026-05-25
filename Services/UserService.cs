using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebNangCao.Data;
using WebNangCao.Models;
using WebNangCao.Models.ViewModels.Admin;

namespace WebNangCao.Services
{
    public class UserService : IUserService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly AppDbContext _context;

        public UserService(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            AppDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
        }

        public async Task<List<UserListVM>> GetUserListAsync(string? search, string? roleFilter, bool? activeFilter)
        {
            var usersQuery = _userManager.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim().ToLower();
                usersQuery = usersQuery.Where(u =>
                    u.FullName.ToLower().Contains(search) ||
                    u.Email!.ToLower().Contains(search) ||
                    (u.PhoneNumber != null && u.PhoneNumber.Contains(search)));
            }

            if (activeFilter.HasValue)
            {
                usersQuery = usersQuery.Where(u => u.IsActive == activeFilter.Value);
            }

            var users = await usersQuery.OrderBy(u => u.FullName).ToListAsync();

            var userVMs = new List<UserListVM>();
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);

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
                    CreatedAt = user.CreatedAt
                });
            }

            return userVMs;
        }

        public async Task<List<string>> GetAllRolesAsync()
        {
            return await _roleManager.Roles.Select(r => r.Name ?? "").ToListAsync();
        }

        public async Task<ServiceResult> CreateUserAsync(CreateUserVM model)
        {
            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                return ServiceResult.FailureResult("Email này đã được sử dụng.");
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
                return ServiceResult.SuccessResult($"Tạo tài khoản '{model.FullName}' thành công!");
            }

            return ServiceResult.FailureResult(string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        public async Task<EditUserVM?> GetUserForEditAsync(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return null;

            var roles = await _userManager.GetRolesAsync(user);

            return new EditUserVM
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
        }

        public async Task<ServiceResult> UpdateUserAsync(string id, EditUserVM model)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return ServiceResult.FailureResult("Không tìm thấy người dùng.");
            }

            user.FullName = model.FullName;
            user.PhoneNumber = model.PhoneNumber;
            user.IdentityCardNumber = model.IdentityCardNumber;
            user.DateOfBirth = model.DateOfBirth;
            user.IsActive = model.IsActive;

            if (user.Email != model.Email)
            {
                var emailExists = await _userManager.FindByEmailAsync(model.Email);
                if (emailExists != null && emailExists.Id != id)
                {
                    return ServiceResult.FailureResult("Email này đã được sử dụng.");
                }
                user.Email = model.Email;
                user.UserName = model.Email;
                user.NormalizedEmail = model.Email.ToUpper();
                user.NormalizedUserName = model.Email.ToUpper();
            }

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                return ServiceResult.FailureResult(string.Join(", ", updateResult.Errors.Select(e => e.Description)));
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
                    return ServiceResult.FailureResult("Lỗi đổi mật khẩu: " + string.Join(", ", passResult.Errors.Select(e => e.Description)));
                }
            }

            return ServiceResult.SuccessResult($"Cập nhật tài khoản '{user.FullName}' thành công!");
        }

        public async Task<UserListVM?> GetUserDetailsAsync(string id)
        {
            var user = await _context.Users
                .Include(u => u.Apartments)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null) return null;

            var roles = await _userManager.GetRolesAsync(user);

            return new UserListVM
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? "",
                PhoneNumber = user.PhoneNumber,
                IdentityCardNumber = user.IdentityCardNumber,
                DateOfBirth = user.DateOfBirth,
                IsActive = user.IsActive,
                Roles = roles,
                Apartments = user.Apartments
                    .Where(a => !a.IsDeleted)
                    .OrderBy(a => a.ApartmentNumber)
                    .Select(a => a.ApartmentNumber)
                    .ToList(),
                ApartmentDetails = user.Apartments
                    .Where(a => !a.IsDeleted)
                    .OrderBy(a => a.ApartmentNumber)
                    .ToList()
            };
        }

        public async Task<ServiceResult> ToggleUserActiveAsync(string id, ClaimsPrincipal currentUserPrincipal)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return ServiceResult.FailureResult("Không tìm thấy người dùng.");

            var currentUser = await _userManager.GetUserAsync(currentUserPrincipal);
            if (currentUser?.Id == id)
            {
                return ServiceResult.FailureResult("Không thể thay đổi trạng thái tài khoản của chính mình!");
            }

            user.IsActive = !user.IsActive;
            await _userManager.UpdateAsync(user);

            var status = user.IsActive ? "kích hoạt" : "vô hiệu hóa";
            return ServiceResult.SuccessResult($"Đã {status} tài khoản '{user.FullName}'.");
        }

        public async Task<ServiceResult> DeleteUserAsync(string id, ClaimsPrincipal currentUserPrincipal)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return ServiceResult.FailureResult("Không tìm thấy người dùng.");

            var currentUser = await _userManager.GetUserAsync(currentUserPrincipal);
            if (currentUser?.Id == id)
            {
                return ServiceResult.FailureResult("Không thể xóa tài khoản của chính mình!");
            }

            await _userManager.DeleteAsync(user);
            return ServiceResult.SuccessResult($"Đã xóa tài khoản '{user.FullName}'.");
        }
    }
}
