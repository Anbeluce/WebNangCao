using System.Security.Claims;
using WebNangCao.Models;
using WebNangCao.Models.ViewModels.Admin;

namespace WebNangCao.Services
{
    public interface IUserService
    {
        Task<List<UserListVM>> GetUserListAsync(string? search, string? roleFilter, bool? activeFilter);
        Task<List<string>> GetAllRolesAsync();
        Task<ServiceResult> CreateUserAsync(CreateUserVM model);
        Task<EditUserVM?> GetUserForEditAsync(string id);
        Task<ServiceResult> UpdateUserAsync(string id, EditUserVM model);
        Task<UserListVM?> GetUserDetailsAsync(string id);
        Task<ServiceResult> ToggleUserActiveAsync(string id, ClaimsPrincipal currentUserPrincipal);
        Task<ServiceResult> DeleteUserAsync(string id, ClaimsPrincipal currentUserPrincipal);
    }
}
