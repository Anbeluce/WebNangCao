using System.Security.Claims;
using WebNangCao.Models;
using WebNangCao.Models.Dtos;
using WebNangCao.Models.ViewModels;

namespace WebNangCao.Services
{
    public interface IResidentService
    {
        Task<ServiceResult<ResidentDashboardDto>> GetDashboardAsync(ClaimsPrincipal userPrincipal);
        Task<ServiceResult<List<Apartment>>> GetApartmentsAndInvoicesAsync(ClaimsPrincipal userPrincipal);
        Task<ServiceResult<Apartment>> GetApartmentInvoicesAsync(int id, ClaimsPrincipal userPrincipal);
        Task<ServiceResult<Invoice>> GetInvoiceDetailAsync(int id, ClaimsPrincipal userPrincipal);
        Task<ServiceResult<object>> CheckPaymentStatusAsync(int id, ClaimsPrincipal userPrincipal);
        Task<ServiceResult<ProfileVM>> GetProfileAsync(ClaimsPrincipal userPrincipal);
        Task<ServiceResult> UpdateProfileAsync(ProfileVM model, ClaimsPrincipal userPrincipal);
        Task<ServiceResult> ChangePasswordAsync(ChangePasswordVM model, ClaimsPrincipal userPrincipal);
    }
}

