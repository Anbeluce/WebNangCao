using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebNangCao.Data;
using WebNangCao.Models;
using WebNangCao.Models.Dtos;
using WebNangCao.Models.ViewModels.Admin;

namespace WebNangCao.Services
{
    public class ApartmentService : IApartmentService
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ApartmentService(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<List<ApartmentListVM>> GetApartmentListWithStatsAsync()
        {
            return await _context.Apartments
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
        }

        public async Task<ServiceResult> CreateApartmentAsync(CreateApartmentVM model)
        {
            var exists = await _context.Apartments
                .AnyAsync(a => a.ApartmentNumber == model.ApartmentNumber && !a.IsDeleted);
            if (exists)
            {
                return ServiceResult.FailureResult("Số căn hộ này đã tồn tại.");
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

            return ServiceResult.SuccessResult($"Đã thêm căn hộ {apartment.ApartmentNumber} thành công!");
        }

        public async Task<EditApartmentVM?> GetApartmentForEditAsync(int id)
        {
            var apartment = await _context.Apartments
                .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted);

            if (apartment == null) return null;

            return new EditApartmentVM
            {
                Id = apartment.Id,
                ApartmentNumber = apartment.ApartmentNumber,
                Area = apartment.Area,
                Floor = apartment.Floor,
                OwnerId = apartment.OwnerId
            };
        }

        public async Task<string?> GetOwnerNameAndEmailAsync(string? ownerId)
        {
            if (string.IsNullOrEmpty(ownerId)) return null;
            var owner = await _userManager.FindByIdAsync(ownerId);
            return owner != null ? $"{owner.FullName} ({owner.Email})" : null;
        }

        public async Task<ServiceResult> UpdateApartmentAsync(EditApartmentVM model)
        {
            var apartment = await _context.Apartments
                .FirstOrDefaultAsync(a => a.Id == model.Id && !a.IsDeleted);

            if (apartment == null)
            {
                return ServiceResult.FailureResult("Không tìm thấy căn hộ.");
            }

            var exists = await _context.Apartments
                .AnyAsync(a => a.ApartmentNumber == model.ApartmentNumber && a.Id != model.Id && !a.IsDeleted);
            if (exists)
            {
                return ServiceResult.FailureResult("Số căn hộ này đã tồn tại.");
            }

            apartment.ApartmentNumber = model.ApartmentNumber;
            apartment.Area = model.Area;
            apartment.Floor = model.Floor;
            apartment.OwnerId = model.OwnerId;
            apartment.UpdatedAt = DateTime.UtcNow.AddHours(7);

            await _context.SaveChangesAsync();

            return ServiceResult.SuccessResult($"Đã cập nhật căn hộ {apartment.ApartmentNumber} thành công!");
        }

        public async Task<ServiceResult> DeleteApartmentAsync(int id)
        {
            var apartment = await _context.Apartments
                .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted);

            if (apartment == null)
            {
                return ServiceResult.FailureResult("Không tìm thấy căn hộ.");
            }

            apartment.IsDeleted = true;
            apartment.UpdatedAt = DateTime.UtcNow.AddHours(7);
            await _context.SaveChangesAsync();

            return ServiceResult.SuccessResult($"Đã xóa căn hộ {apartment.ApartmentNumber}.");
        }

        public async Task<List<object>> SearchUsersAsync(string? q)
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

            return users.Cast<object>().ToList();
        }

        public async Task<List<OwnerDropdownItemDto>> GetOwnerDropdownAsync()
        {
            return await _userManager.Users
                .Where(u => u.IsActive)
                .OrderBy(u => u.FullName)
                .Select(u => new OwnerDropdownItemDto
                {
                    Id = u.Id,
                    Display = u.FullName + " (" + u.Email + ")"
                })
                .ToListAsync();
        }
    }
}
