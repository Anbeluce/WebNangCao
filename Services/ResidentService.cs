using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebNangCao.Data;
using WebNangCao.Models;
using WebNangCao.Models.Dtos;
using WebNangCao.Models.ViewModels;

namespace WebNangCao.Services
{
    public class ResidentService : IResidentService
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public ResidentService(AppDbContext context, UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
        }

        public async Task<ServiceResult<ResidentDashboardDto>> GetDashboardAsync(ClaimsPrincipal userPrincipal)
        {
            var user = await _userManager.GetUserAsync(userPrincipal);
            if (user == null) return ServiceResult<ResidentDashboardDto>.FailureResult("Người dùng chưa đăng nhập.");

            var apartments = await _context.Apartments
                .Where(a => a.OwnerId == user.Id && !a.IsDeleted)
                .Include(a => a.Invoices.Where(i => !i.IsDeleted))
                .OrderBy(a => a.ApartmentNumber)
                .ToListAsync();

            var allInvoices = apartments.SelectMany(a => a.Invoices).ToList();

            var dashboardDto = new ResidentDashboardDto
            {
                UserName = user.FullName,
                Apartments = apartments,
                TotalInvoices = allInvoices.Count,
                UnpaidInvoices = allInvoices.Count(i => i.Status == InvoiceStatus.Unpaid),
                TotalUnpaid = allInvoices.Where(i => i.Status != InvoiceStatus.Paid).Sum(i => i.TotalAmount)
            };

            return ServiceResult<ResidentDashboardDto>.SuccessResult(dashboardDto);
        }

        public async Task<ServiceResult<List<Apartment>>> GetApartmentsAndInvoicesAsync(ClaimsPrincipal userPrincipal)
        {
            var user = await _userManager.GetUserAsync(userPrincipal);
            if (user == null) return ServiceResult<List<Apartment>>.FailureResult("Người dùng chưa đăng nhập.");

            var apartments = await _context.Apartments
                .Where(a => a.OwnerId == user.Id && !a.IsDeleted)
                .Include(a => a.Invoices.Where(i => !i.IsDeleted))
                .OrderBy(a => a.ApartmentNumber)
                .ToListAsync();

            return ServiceResult<List<Apartment>>.SuccessResult(apartments);
        }

        public async Task<ServiceResult<Apartment>> GetApartmentInvoicesAsync(int id, ClaimsPrincipal userPrincipal)
        {
            var user = await _userManager.GetUserAsync(userPrincipal);
            if (user == null) return ServiceResult<Apartment>.FailureResult("Người dùng chưa đăng nhập.");

            var apartment = await _context.Apartments
                .Include(a => a.Invoices.Where(i => !i.IsDeleted))
                    .ThenInclude(i => i.Transactions)
                .FirstOrDefaultAsync(a => a.Id == id && a.OwnerId == user.Id && !a.IsDeleted);

            if (apartment == null)
            {
                return ServiceResult<Apartment>.FailureResult("Không tìm thấy căn hộ hoặc bạn không có quyền truy cập.");
            }

            return ServiceResult<Apartment>.SuccessResult(apartment);
        }

        public async Task<ServiceResult<Invoice>> GetInvoiceDetailAsync(int id, ClaimsPrincipal userPrincipal)
        {
            var user = await _userManager.GetUserAsync(userPrincipal);
            if (user == null) return ServiceResult<Invoice>.FailureResult("Người dùng chưa đăng nhập.");

            var invoice = await _context.Invoices
                .Include(i => i.Apartment)
                .Include(i => i.Transactions.Where(t => !t.IsDeleted))
                .FirstOrDefaultAsync(i => i.Id == id && !i.IsDeleted && i.Apartment.OwnerId == user.Id);

            if (invoice == null)
            {
                return ServiceResult<Invoice>.FailureResult("Không tìm thấy hóa đơn hoặc bạn không có quyền truy cập.");
            }

            return ServiceResult<Invoice>.SuccessResult(invoice);
        }

        public async Task<ServiceResult<object>> CheckPaymentStatusAsync(int id, ClaimsPrincipal userPrincipal)
        {
            var user = await _userManager.GetUserAsync(userPrincipal);
            if (user == null) return ServiceResult<object>.FailureResult("Người dùng chưa đăng nhập.");

            var invoice = await _context.Invoices
                .Include(i => i.Apartment)
                .FirstOrDefaultAsync(i => i.Id == id && i.Apartment.OwnerId == user.Id && !i.IsDeleted);
            
            if (invoice == null) return ServiceResult<object>.FailureResult("Hóa đơn không hợp lệ.");

            var previousAmounts = await _context.Transactions
                .Where(t => t.InvoiceId == invoice.Id && !t.IsDeleted)
                .Select(t => t.Amount)
                .ToListAsync();
            var totalPaid = previousAmounts.Sum();

            var result = new
            {
                isPaid = invoice.Status == InvoiceStatus.Paid,
                status = invoice.Status.ToString(),
                totalPaid = totalPaid
            };

            return ServiceResult<object>.SuccessResult(result);
        }

        public async Task<ServiceResult<ProfileVM>> GetProfileAsync(ClaimsPrincipal userPrincipal)
        {
            var user = await _userManager.GetUserAsync(userPrincipal);
            if (user == null) return ServiceResult<ProfileVM>.FailureResult("Người dùng chưa đăng nhập.");

            var apartments = await _context.Apartments
                .Where(a => a.OwnerId == user.Id && !a.IsDeleted)
                .Include(a => a.Invoices.Where(i => !i.IsDeleted))
                .ToListAsync();

            var allInvoices = apartments.SelectMany(a => a.Invoices).ToList();

            var model = new ProfileVM
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? "",
                PhoneNumber = user.PhoneNumber,
                IdentityCardNumber = user.IdentityCardNumber,
                DateOfBirth = user.DateOfBirth,
                ApartmentCount = apartments.Count,
                InvoiceCount = allInvoices.Count,
                UnpaidInvoiceCount = allInvoices.Count(i => i.Status == InvoiceStatus.Unpaid),
                TotalDebt = allInvoices.Where(i => i.Status != InvoiceStatus.Paid).Sum(i => i.TotalAmount)
            };

            return ServiceResult<ProfileVM>.SuccessResult(model);
        }

        public async Task<ServiceResult> UpdateProfileAsync(ProfileVM model, ClaimsPrincipal userPrincipal)
        {
            var user = await _userManager.GetUserAsync(userPrincipal);
            if (user == null) return ServiceResult.FailureResult("Người dùng chưa đăng nhập.");

            user.FullName = model.FullName;
            user.PhoneNumber = model.PhoneNumber;
            user.IdentityCardNumber = model.IdentityCardNumber;
            user.DateOfBirth = model.DateOfBirth;

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                return ServiceResult.SuccessResult("Cập nhật thông tin thành công!");
            }

            return ServiceResult.FailureResult(string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        public async Task<ServiceResult> ChangePasswordAsync(ChangePasswordVM model, ClaimsPrincipal userPrincipal)
        {
            var user = await _userManager.GetUserAsync(userPrincipal);
            if (user == null) return ServiceResult.FailureResult("Người dùng chưa đăng nhập.");

            var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
            if (result.Succeeded)
            {
                await _signInManager.RefreshSignInAsync(user);
                return ServiceResult.SuccessResult("Đổi mật khẩu thành công!");
            }

            // Map standard password mismatch error
            var errors = result.Errors.Select(e => 
                e.Code == "PasswordMismatch" ? "Mật khẩu hiện tại không đúng." : e.Description
            );
            return ServiceResult.FailureResult(string.Join("; ", errors));
        }
    }
}
