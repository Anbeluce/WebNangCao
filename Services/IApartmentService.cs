using WebNangCao.Models;
using WebNangCao.Models.Dtos;
using WebNangCao.Models.ViewModels.Admin;

namespace WebNangCao.Services
{
    public interface IApartmentService
    {
        Task<List<ApartmentListVM>> GetApartmentListWithStatsAsync();
        Task<ServiceResult> CreateApartmentAsync(CreateApartmentVM model);
        Task<EditApartmentVM?> GetApartmentForEditAsync(int id);
        Task<ServiceResult> UpdateApartmentAsync(EditApartmentVM model);
        Task<ServiceResult> DeleteApartmentAsync(int id);
        Task<List<object>> SearchUsersAsync(string? q);
        Task<string?> GetOwnerNameAndEmailAsync(string? ownerId);
        Task<List<OwnerDropdownItemDto>> GetOwnerDropdownAsync();
    }
}
