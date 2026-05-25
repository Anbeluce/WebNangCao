using Microsoft.AspNetCore.Http;
using WebNangCao.Models;
using WebNangCao.Models.Dtos;
using WebNangCao.Models.ViewModels.Admin;

namespace WebNangCao.Services
{
    public class ImportResultDto
    {
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public int SuccessCount { get; set; }
    }

    public interface IInvoiceService
    {
        Task<List<InvoiceListVM>> GetInvoiceListAsync(int? month, int? year, int? apartmentId, InvoiceStatus? status);
        Task<ServiceResult> CreateInvoiceAsync(CreateInvoiceVM model);
        Task<EditInvoiceVM?> GetInvoiceForEditAsync(int id);
        Task<ServiceResult> UpdateInvoiceAsync(EditInvoiceVM model);
        Task<ServiceResult> DeleteInvoiceAsync(int id);
        Task<byte[]> DownloadTemplateAsync();
        Task<ServiceResult<ImportResultDto>> ImportExcelAsync(IFormFile excelFile);
        Task<List<ApartmentDropdownItemDto>> GetApartmentDropdownAsync();
        Task<CreateInvoiceVM> GetCreateDefaultsAsync();
        Task<decimal> GetDefaultManagementFeePerM2Async();
    }
}
