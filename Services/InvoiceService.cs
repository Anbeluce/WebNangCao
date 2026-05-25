using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Http;
using MiniExcelLibs;
using System.IO;
using WebNangCao.Data;
using WebNangCao.Models;
using WebNangCao.Models.Configs;
using WebNangCao.Models.Dtos;
using WebNangCao.Models.ViewModels.Admin;

namespace WebNangCao.Services
{
    public class InvoiceService : IInvoiceService
    {
        private readonly AppDbContext _context;
        private readonly IEmailService _emailService;
        private readonly InvoiceSettings _settings;

        public InvoiceService(AppDbContext context, IEmailService emailService, IOptions<InvoiceSettings> settings)
        {
            _context = context;
            _emailService = emailService;
            _settings = settings.Value;
        }

        public async Task<List<InvoiceListVM>> GetInvoiceListAsync(int? month, int? year, int? apartmentId, InvoiceStatus? status)
        {
            var query = _context.Invoices
                .Include(i => i.Apartment)
                    .ThenInclude(a => a.Owner)
                .Where(i => !i.IsDeleted);

            if (month.HasValue) query = query.Where(i => i.Month == month.Value);
            if (year.HasValue) query = query.Where(i => i.Year == year.Value);
            if (apartmentId.HasValue) query = query.Where(i => i.ApartmentId == apartmentId.Value);
            if (status.HasValue) query = query.Where(i => i.Status == status.Value);

            return await query
                .OrderByDescending(i => i.Year)
                .ThenByDescending(i => i.Month)
                .ThenBy(i => i.Apartment.ApartmentNumber)
                .Select(i => new InvoiceListVM
                {
                    Id = i.Id,
                    ApartmentNumber = i.Apartment.ApartmentNumber,
                    OwnerName = i.Apartment.Owner != null ? i.Apartment.Owner.FullName : null,
                    Month = i.Month,
                    Year = i.Year,
                    ElectricityUsage = i.ElectricityUsage,
                    ElectricityUnitPrice = i.ElectricityUnitPrice,
                    WaterUsage = i.WaterUsage,
                    WaterUnitPrice = i.WaterUnitPrice,
                    ManagementFee = i.ManagementFee,
                    WasteFee = i.WasteFee,
                    ParkingFee = i.ParkingFee,
                    Status = i.Status,
                    DueDate = i.DueDate
                })
                .ToListAsync();
        }

        public async Task<ServiceResult> CreateInvoiceAsync(CreateInvoiceVM model)
        {
            var apartment = await _context.Apartments
                .Include(a => a.Owner)
                .FirstOrDefaultAsync(a => a.Id == (model.ApartmentId ?? 0) && !a.IsDeleted);
            if (apartment == null)
            {
                return ServiceResult.FailureResult("Căn hộ không tồn tại hoặc đã bị xóa.");
            }
            if (string.IsNullOrEmpty(apartment.OwnerId))
            {
                return ServiceResult.FailureResult($"Căn hộ '{apartment.ApartmentNumber}' chưa có cư dân đăng ký sở hữu, không thể tạo hóa đơn.");
            }

            // Kiểm tra trùng hóa đơn
            var exists = await _context.Invoices
                .AnyAsync(i => i.ApartmentId == (model.ApartmentId ?? 0)
                    && i.Month == model.Month && i.Year == model.Year && !i.IsDeleted);
            if (exists)
            {
                return ServiceResult.FailureResult($"Căn hộ này đã có hóa đơn tháng {model.Month}/{model.Year}.");
            }

            var invoice = new Invoice
            {
                ApartmentId = model.ApartmentId ?? 0,
                Month = model.Month,
                Year = model.Year,
                ElectricityUsage = model.ElectricityUsage,
                ElectricityUnitPrice = model.ElectricityUnitPrice,
                WaterUsage = model.WaterUsage,
                WaterUnitPrice = model.WaterUnitPrice,
                ManagementFee = model.ManagementFee,
                WasteFee = model.WasteFee,
                ParkingFee = model.ParkingFee,
                DueDate = model.DueDate,
                Status = InvoiceStatus.Unpaid
            };

            _context.Invoices.Add(invoice);
            await _context.SaveChangesAsync();

            // Gửi mail thông báo cho cư dân
            try
            {
                if (apartment.Owner?.Email != null)
                {
                    var subject = $"[Thông báo] Hóa đơn tiền nước & phí dịch vụ tháng {model.Month}/{model.Year}";
                    var body = BuildInvoiceEmailBody(apartment.Owner.FullName, apartment.ApartmentNumber, invoice);
                    await _emailService.SendEmailAsync(apartment.Owner.Email, subject, body);
                }
            }
            catch (Exception)
            {
                // Bỏ qua lỗi gửi mail để tránh roll-back DB
            }

            return ServiceResult.SuccessResult($"Đã tạo hóa đơn tháng {model.Month}/{model.Year} — Tổng: {invoice.TotalAmount:N0}đ. Đã gửi email thông báo.");
        }

        public async Task<EditInvoiceVM?> GetInvoiceForEditAsync(int id)
        {
            var invoice = await _context.Invoices
                .Include(i => i.Apartment)
                .FirstOrDefaultAsync(i => i.Id == id && !i.IsDeleted);

            if (invoice == null) return null;

            return new EditInvoiceVM
            {
                Id = invoice.Id,
                ApartmentId = invoice.ApartmentId,
                ApartmentNumber = invoice.Apartment.ApartmentNumber,
                Month = invoice.Month,
                Year = invoice.Year,
                ElectricityUsage = invoice.ElectricityUsage,
                ElectricityUnitPrice = invoice.ElectricityUnitPrice,
                WaterUsage = invoice.WaterUsage,
                WaterUnitPrice = invoice.WaterUnitPrice,
                ManagementFee = invoice.ManagementFee,
                WasteFee = invoice.WasteFee,
                ParkingFee = invoice.ParkingFee,
                DueDate = invoice.DueDate,
                Status = invoice.Status
            };
        }

        public async Task<ServiceResult> UpdateInvoiceAsync(EditInvoiceVM model)
        {
            var invoice = await _context.Invoices
                .FirstOrDefaultAsync(i => i.Id == model.Id && !i.IsDeleted);

            if (invoice == null)
            {
                return ServiceResult.FailureResult("Không tìm thấy hóa đơn.");
            }

            var oldStatus = invoice.Status;
            
            invoice.Month = model.Month;
            invoice.Year = model.Year;
            invoice.ElectricityUsage = model.ElectricityUsage;
            invoice.ElectricityUnitPrice = model.ElectricityUnitPrice;
            invoice.WaterUsage = model.WaterUsage;
            invoice.WaterUnitPrice = model.WaterUnitPrice;
            invoice.ManagementFee = model.ManagementFee;
            invoice.WasteFee = model.WasteFee;
            invoice.ParkingFee = model.ParkingFee;
            invoice.DueDate = model.DueDate;
            invoice.Status = model.Status;
            invoice.UpdatedAt = DateTime.UtcNow.AddHours(7);

            await _context.SaveChangesAsync();

            // Nếu trạng thái đổi từ Unpaid sang Paid thì gửi mail cảm ơn/xác nhận
            if (oldStatus == InvoiceStatus.Unpaid && model.Status == InvoiceStatus.Paid)
            {
                try
                {
                    var apartment = await _context.Apartments
                        .Include(a => a.Owner)
                        .FirstOrDefaultAsync(a => a.Id == invoice.ApartmentId);

                    if (apartment?.Owner?.Email != null)
                    {
                        var subject = $"[Xác nhận] Thanh toán hóa đơn tháng {model.Month}/{model.Year} thành công";
                        var body = BuildPaymentConfirmationEmailBody(apartment.Owner.FullName, apartment.ApartmentNumber, invoice);
                        await _emailService.SendEmailAsync(apartment.Owner.Email, subject, body);
                    }
                }
                catch (Exception) { /* Bỏ qua lỗi gửi mail */ }
            }

            return ServiceResult.SuccessResult($"Đã cập nhật hóa đơn tháng {model.Month}/{model.Year}.");
        }

        public async Task<ServiceResult> DeleteInvoiceAsync(int id)
        {
            var invoice = await _context.Invoices
                .Include(i => i.Apartment)
                .FirstOrDefaultAsync(i => i.Id == id && !i.IsDeleted);

            if (invoice == null)
            {
                return ServiceResult.FailureResult("Không tìm thấy hóa đơn.");
            }

            invoice.IsDeleted = true;
            invoice.UpdatedAt = DateTime.UtcNow.AddHours(7);
            await _context.SaveChangesAsync();

            return ServiceResult.SuccessResult($"Đã xóa hóa đơn T{invoice.Month}/{invoice.Year} - {invoice.Apartment.ApartmentNumber}.");
        }

        public async Task<byte[]> DownloadTemplateAsync()
        {
            var apartments = await _context.Apartments
                .Where(a => !a.IsDeleted && a.OwnerId != null)
                .Include(a => a.Owner)
                .Take(3)
                .ToListAsync();

            var rows = new List<Dictionary<string, object>>();

            if (apartments.Any())
            {
                foreach (var apt in apartments)
                {
                    rows.Add(new Dictionary<string, object>
                    {
                        { "Số căn hộ", apt.ApartmentNumber },
                        { "Tháng", DateTime.UtcNow.AddHours(7).Month },
                        { "Năm", DateTime.UtcNow.AddHours(7).Year },
                        { "Số nước tiêu thụ (m³)", 12.5 },
                        { "Đơn giá nước (đ/m³)", _settings.DefaultWaterPrice },
                        { "Phí quản lý vận hành (đ)", (decimal)apt.Area * _settings.DefaultManagementFeePerM2 },
                        { "Phí vệ sinh (đ)", _settings.DefaultWasteFee },
                        { "Phí gửi xe (đ)", _settings.DefaultParkingFee },
                        { "Hạn thanh toán (dd/MM/yyyy)", DateTime.UtcNow.AddHours(7).AddDays(15).ToString("dd/MM/yyyy") }
                    });
                }
            }
            else
            {
                rows.Add(new Dictionary<string, object>
                {
                    { "Số căn hộ", "A101" },
                    { "Tháng", 5 },
                    { "Năm", 2026 },
                    { "Số nước tiêu thụ (m³)", 15.0 },
                    { "Đơn giá nước (đ/m³)", _settings.DefaultWaterPrice },
                    { "Phí quản lý vận hành (đ)", 750000 },
                    { "Phí vệ sinh (đ)", _settings.DefaultWasteFee },
                    { "Phí gửi xe (đ)", _settings.DefaultParkingFee },
                    { "Hạn thanh toán (dd/MM/yyyy)", "15/06/2026" }
                });
            }

            using (var memoryStream = new MemoryStream())
            {
                memoryStream.SaveAs(rows);
                return memoryStream.ToArray();
            }
        }

        public async Task<ServiceResult<ImportResultDto>> ImportExcelAsync(IFormFile excelFile)
        {
            var errors = new List<string>();
            var warnings = new List<string>();
            var importedInvoices = new List<Invoice>();
            var allApartments = new List<Apartment>();

            try
            {
                using (var stream = excelFile.OpenReadStream())
                {
                    var rows = stream.Query(useHeaderRow: true).ToList();
                    
                    if (!rows.Any())
                    {
                        errors.Add("Tệp Excel trống hoặc không đúng định dạng mẫu.");
                    }
                    else
                    {
                        allApartments = await _context.Apartments
                            .Where(a => !a.IsDeleted)
                            .Include(a => a.Owner)
                            .ToListAsync();

                        var apartmentDict = allApartments.ToDictionary(a => a.ApartmentNumber.Trim().ToUpper(), a => a);

                        var existingInvoices = await _context.Invoices
                            .Where(i => !i.IsDeleted)
                            .Select(i => new { i.ApartmentId, i.Month, i.Year })
                            .ToListAsync();

                        var invoiceSet = new HashSet<string>(existingInvoices.Select(i => $"{i.ApartmentId}_{i.Month}_{i.Year}"));

                        int rowNum = 1;
                        foreach (IDictionary<string, object> row in rows)
                        {
                            rowNum++;
                            
                            if (row.Values.All(v => v == null || string.IsNullOrWhiteSpace(v.ToString())))
                            {
                                continue;
                            }

                            if (!row.TryGetValue("Số căn hộ", out var aptObj) || aptObj == null || string.IsNullOrWhiteSpace(aptObj.ToString()))
                            {
                                errors.Add($"Dòng {rowNum}: Cột 'Số căn hộ' không được để trống.");
                                continue;
                            }

                            var aptNo = aptObj.ToString()!.Trim();
                            if (!apartmentDict.TryGetValue(aptNo.ToUpper(), out var apartment))
                            {
                                errors.Add($"Dòng {rowNum}: Căn hộ '{aptNo}' không tồn tại trong hệ thống.");
                                continue;
                            }

                            if (!row.TryGetValue("Tháng", out var monthObj) || monthObj == null || !int.TryParse(monthObj.ToString(), out int month) || month < 1 || month > 12)
                            {
                                errors.Add($"Dòng {rowNum}: Cột 'Tháng' phải là số nguyên từ 1 đến 12.");
                                continue;
                            }

                            if (!row.TryGetValue("Năm", out var yearObj) || yearObj == null || !int.TryParse(yearObj.ToString(), out int year) || year < 2020 || year > 2099)
                            {
                                errors.Add($"Dòng {rowNum}: Cột 'Năm' phải là số nguyên hợp lệ (2020-2099).");
                                continue;
                            }

                            var invoiceKey = $"{apartment.Id}_{month}_{year}";
                            if (invoiceSet.Contains(invoiceKey) || importedInvoices.Any(i => i.ApartmentId == apartment.Id && i.Month == month && i.Year == year))
                            {
                                errors.Add($"Dòng {rowNum}: Căn hộ '{aptNo}' đã tồn tại hóa đơn kỳ tháng {month}/{year}.");
                                continue;
                            }

                            if (!row.TryGetValue("Số nước tiêu thụ (m³)", out var waterObj) || waterObj == null || !decimal.TryParse(waterObj.ToString(), out decimal waterUsage) || waterUsage < 0)
                            {
                                errors.Add($"Dòng {rowNum}: Cột 'Số nước tiêu thụ (m³)' phải là số lớn hơn hoặc bằng 0.");
                                continue;
                            }

                            decimal waterPrice = _settings.DefaultWaterPrice;
                            if (row.TryGetValue("Đơn giá nước (đ/m³)", out var waterPriceObj) && waterPriceObj != null && !string.IsNullOrWhiteSpace(waterPriceObj.ToString()))
                            {
                                if (!decimal.TryParse(waterPriceObj.ToString(), out waterPrice) || waterPrice < 0)
                                {
                                    errors.Add($"Dòng {rowNum}: Cột 'Đơn giá nước (đ/m³)' không hợp lệ.");
                                    continue;
                                }
                            }

                            decimal managementFee = (decimal)apartment.Area * _settings.DefaultManagementFeePerM2;
                            if (row.TryGetValue("Phí quản lý vận hành (đ)", out var mFeeObj) && mFeeObj != null && !string.IsNullOrWhiteSpace(mFeeObj.ToString()))
                            {
                                if (!decimal.TryParse(mFeeObj.ToString(), out managementFee) || managementFee < 0)
                                {
                                    errors.Add($"Dòng {rowNum}: Cột 'Phí quản lý vận hành (đ)' không hợp lệ.");
                                    continue;
                                }
                            }

                            decimal wasteFee = _settings.DefaultWasteFee;
                            if (row.TryGetValue("Phí vệ sinh (đ)", out var wasteFeeObj) && wasteFeeObj != null && !string.IsNullOrWhiteSpace(wasteFeeObj.ToString()))
                            {
                                if (!decimal.TryParse(wasteFeeObj.ToString(), out wasteFee) || wasteFee < 0)
                                {
                                    errors.Add($"Dòng {rowNum}: Cột 'Phí vệ sinh (đ)' không hợp lệ.");
                                    continue;
                                }
                            }

                            decimal parkingFee = _settings.DefaultParkingFee;
                            if (row.TryGetValue("Phí gửi xe (đ)", out var parkingFeeObj) && parkingFeeObj != null && !string.IsNullOrWhiteSpace(parkingFeeObj.ToString()))
                            {
                                if (!decimal.TryParse(parkingFeeObj.ToString(), out parkingFee) || parkingFee < 0)
                                {
                                    errors.Add($"Dòng {rowNum}: Cột 'Phí gửi xe (đ)' không hợp lệ.");
                                    continue;
                                }
                            }

                            DateTime dueDate = new DateTime(year, month, 1).AddMonths(1).AddDays(14);
                            if (row.TryGetValue("Hạn thanh toán (dd/MM/yyyy)", out var dateObj) && dateObj != null && !string.IsNullOrWhiteSpace(dateObj.ToString()))
                            {
                                var dateStr = dateObj.ToString()!.Trim();
                                if (!DateTime.TryParseExact(dateStr, "dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out dueDate))
                                {
                                    if (!DateTime.TryParse(dateStr, out dueDate))
                                    {
                                        errors.Add($"Dòng {rowNum}: Định dạng 'Hạn thanh toán' không hợp lệ (mẫu: dd/MM/yyyy).");
                                        continue;
                                    }
                                }
                            }

                            if (string.IsNullOrEmpty(apartment.OwnerId))
                            {
                                errors.Add($"Dòng {rowNum}: Căn hộ '{aptNo}' chưa có cư dân đăng ký sở hữu, không thể tạo hóa đơn.");
                                continue;
                            }

                            var invoice = new Invoice
                            {
                                ApartmentId = apartment.Id,
                                Month = month,
                                Year = year,
                                WaterUsage = waterUsage,
                                WaterUnitPrice = waterPrice,
                                ManagementFee = managementFee,
                                WasteFee = wasteFee,
                                ParkingFee = parkingFee,
                                DueDate = dueDate,
                                Status = InvoiceStatus.Unpaid
                            };

                            importedInvoices.Add(invoice);
                        }
                    }
                }

                if (!errors.Any() && importedInvoices.Any())
                {
                    await _context.Invoices.AddRangeAsync(importedInvoices);
                    await _context.SaveChangesAsync();

                    // Gửi email thông báo cho từng hộ cư dân sau khi nhập thành công
                    foreach (var inv in importedInvoices)
                    {
                        try
                        {
                            var apartment = allApartments.FirstOrDefault(a => a.Id == inv.ApartmentId);
                            if (apartment?.Owner?.Email != null)
                            {
                                var subject = $"[Thông báo] Hóa đơn tiền nước & phí dịch vụ tháng {inv.Month}/{inv.Year}";
                                var body = BuildInvoiceEmailBody(apartment.Owner.FullName, apartment.ApartmentNumber, inv, true);
                                await _emailService.SendEmailAsync(apartment.Owner.Email, subject, body);
                            }
                        }
                        catch (Exception)
                        {
                            // Bỏ qua lỗi gửi mail lẻ để tránh hủy giao dịch thành công
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                errors.Add($"Lỗi hệ thống khi đọc tệp Excel: {ex.Message}");
            }

            var result = new ImportResultDto
            {
                Errors = errors,
                Warnings = warnings,
                SuccessCount = errors.Any() ? 0 : importedInvoices.Count
            };

            return ServiceResult<ImportResultDto>.SuccessResult(result);
        }

        private string BuildInvoiceEmailBody(string fullName, string apartmentNumber, Invoice invoice, bool isExcel = false)
        {
            var sourceStr = isExcel ? "được khởi tạo từ tệp Excel" : "đã được khởi tạo";
            return $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; border: 1px solid #e0e0e0; padding: 20px;'>
                    <h2 style='color: #04a9f5;'>Thông báo hóa đơn mới</h2>
                    <p>Kính gửi ông/bà <strong>{fullName}</strong>,</p>
                    <p>Ban quản lý chung cư xin thông báo hóa đơn tháng <strong>{invoice.Month}/{invoice.Year}</strong> của căn hộ <strong>{apartmentNumber}</strong> {sourceStr}.</p>
                    <table style='width: 100%; border-collapse: collapse; margin: 20px 0;'>
                        <tr style='background: #f4f7fa;'>
                            <th style='padding: 10px; border: 1px solid #eee; text-align: left;'>Hạng mục</th>
                            <th style='padding: 10px; border: 1px solid #eee; text-align: right;'>Thành tiền</th>
                        </tr>
                        <tr>
                            <td style='padding: 10px; border: 1px solid #eee;'>Tiền nước ({invoice.WaterUsage} m³)</td>
                            <td style='padding: 10px; border: 1px solid #eee; text-align: right;'>{(invoice.WaterUsage * invoice.WaterUnitPrice):N0}đ</td>
                        </tr>
                        <tr>
                            <td style='padding: 10px; border: 1px solid #eee;'>Phí quản lý vận hành</td>
                            <td style='padding: 10px; border: 1px solid #eee; text-align: right;'>{invoice.ManagementFee:N0}đ</td>
                        </tr>
                        <tr>
                            <td style='padding: 10px; border: 1px solid #eee;'>Phí vệ sinh (cố định)</td>
                            <td style='padding: 10px; border: 1px solid #eee; text-align: right;'>{invoice.WasteFee:N0}đ</td>
                        </tr>
                        <tr>
                            <td style='padding: 10px; border: 1px solid #eee;'>Phí gửi xe (cố định)</td>
                            <td style='padding: 10px; border: 1px solid #eee; text-align: right;'>{invoice.ParkingFee:N0}đ</td>
                        </tr>
                        <tr style='font-weight: bold; font-size: 16px; color: #04a9f5;'>
                            <td style='padding: 10px; border: 1px solid #eee;'>TỔNG CỘNG</td>
                            <td style='padding: 10px; border: 1px solid #eee; text-align: right;'>{invoice.TotalAmount:N0}đ</td>
                        </tr>
                    </table>
                    <p>Hạn thanh toán: <strong>{invoice.DueDate:dd/MM/yyyy}</strong></p>
                    <p>Vui lòng đăng nhập vào hệ thống để xem chi tiết và thực hiện thanh toán.</p>
                    <div style='text-align: center; margin: 30px 0;'>
                        <a href='#' style='background: #04a9f5; color: white; padding: 12px 25px; text-decoration: none; border-radius: 4px; font-weight: bold;'>XEM HÓA ĐƠN</a>
                    </div>
                    <hr style='border: none; border-top: 1px solid #eee;'/>
                    <p style='font-size: 12px; color: #999;'>Ban quản lý Chung cư Smart</p>
                </div>";
        }

        private string BuildPaymentConfirmationEmailBody(string fullName, string apartmentNumber, Invoice invoice)
        {
            return $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; border: 1px solid #e0e0e0; padding: 20px;'>
                    <div style='text-align: center; color: #28a745; margin-bottom: 20px;'>
                        <h2 style='margin: 0;'>Thanh toán thành công!</h2>
                        <p style='font-size: 16px;'>Cảm ơn bạn đã hoàn thành nghĩa vụ thanh toán.</p>
                    </div>
                    <p>Kính gửi ông/bà <strong>{fullName}</strong>,</p>
                    <p>Hệ thống đã ghi nhận thanh toán cho hóa đơn tháng <strong>{invoice.Month}/{invoice.Year}</strong> của căn hộ <strong>{apartmentNumber}</strong>.</p>
                    <div style='background: #f8f9fa; padding: 20px; border-radius: 4px; margin: 20px 0;'>
                        <p style='margin: 5px 0;'>Mã hóa đơn: <strong>#{invoice.Id}</strong></p>
                        <p style='margin: 5px 0;'>Số tiền: <strong style='color: #04a9f5;'>{invoice.TotalAmount:N0}đ</strong></p>
                        <p style='margin: 5px 0;'>Ngày thanh toán: <strong>{DateTime.UtcNow.AddHours(7):dd/MM/yyyy HH:mm}</strong></p>
                        <p style='margin: 5px 0;'>Trạng thái: <strong style='color: #28a745;'>Đã thanh toán</strong></p>
                    </div>
                    <p>Nếu có bất kỳ thắc mắc nào, vui lòng liên hệ Ban quản lý để được hỗ trợ.</p>
                    <hr style='border: none; border-top: 1px solid #eee; margin: 20px 0;'/>
                    <p style='font-size: 12px; color: #999;'>Ban quản lý Chung cư Smart</p>
                </div>";
        }

        public async Task<List<ApartmentDropdownItemDto>> GetApartmentDropdownAsync()
        {
            return await _context.Apartments
                .Where(a => !a.IsDeleted && a.OwnerId != null)
                .Include(a => a.Owner)
                .OrderBy(a => a.ApartmentNumber)
                .Select(a => new ApartmentDropdownItemDto
                {
                    Id = a.Id,
                    ApartmentNumber = a.ApartmentNumber,
                    Floor = a.Floor,
                    Area = a.Area,
                    OwnerName = a.Owner != null ? a.Owner.FullName : ""
                })
                .ToListAsync();
        }

        public Task<CreateInvoiceVM> GetCreateDefaultsAsync()
        {
            var localNow = DateTime.UtcNow.AddHours(7);
            var result = new CreateInvoiceVM
            {
                Month = localNow.Month,
                Year = localNow.Year,
                ElectricityUnitPrice = 0,
                WaterUnitPrice = _settings.DefaultWaterPrice,
                ManagementFee = 0,
                WasteFee = _settings.DefaultWasteFee,
                ParkingFee = _settings.DefaultParkingFee,
                DueDate = new DateTime(localNow.Year, localNow.Month, 1).AddMonths(1).AddDays(14)
            };
            return Task.FromResult(result);
        }

        public Task<decimal> GetDefaultManagementFeePerM2Async()
        {
            return Task.FromResult(_settings.DefaultManagementFeePerM2);
        }
    }
}
