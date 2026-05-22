using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebNangCao.Models;

namespace WebNangCao.Data
{
    public static class SeedData
    {
        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var context = serviceProvider.GetRequiredService<AppDbContext>();

            // Tạo roles nếu chưa có
            string[] roles = { "Admin", "Resident", "Staff" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // Tạo tài khoản Admin mặc định nếu chưa có
            const string adminEmail = "admin@webnangcao.com";
            const string adminPassword = "Admin@123";

            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FullName = "Quản trị viên",
                    EmailConfirmed = true,
                    IsActive = true,
                    LockoutEnabled = false
                };

                var result = await userManager.CreateAsync(adminUser, adminPassword);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }

            // Tạo danh sách cư dân mẫu (Resident) để admin dễ dàng chọn gán phòng
            var dummyResidents = new List<(string Email, string Name)>
            {
                ("nguyenvana@gmail.com", "Nguyễn Văn An"),
                ("tranthingoc@gmail.com", "Trần Thị Ngọc"),
                ("levanbinh@gmail.com", "Lê Văn Bình"),
                ("phamthithao@gmail.com", "Phạm Thị Thảo"),
                ("hoangvanthanh@gmail.com", "Hoàng Văn Thanh")
            };

            foreach (var res in dummyResidents)
            {
                var user = await userManager.FindByEmailAsync(res.Email);
                if (user == null)
                {
                    user = new ApplicationUser
                    {
                        UserName = res.Email,
                        Email = res.Email,
                        FullName = res.Name,
                        EmailConfirmed = true,
                        IsActive = true,
                        LockoutEnabled = false
                    };
                    var resCreate = await userManager.CreateAsync(user, "Resident@123");
                    if (resCreate.Succeeded)
                    {
                        await userManager.AddToRoleAsync(user, "Resident");
                    }
                }
            }

            // Khởi tạo 180 căn hộ mẫu nếu số lượng căn hộ hiện tại không chính xác (tránh lỗi lệch dữ liệu cũ)
            int currentApartmentCount = await context.Apartments.CountAsync(a => !a.IsDeleted);
            if (currentApartmentCount != 180)
            {
                // Dọn dẹp các căn hộ cũ bị lệch tầng/số phòng để đảm bảo tính nhất quán của sơ đồ mới
                var oldApartments = await context.Apartments.ToListAsync();
                if (oldApartments.Any())
                {
                    context.Apartments.RemoveRange(oldApartments);
                    await context.SaveChangesAsync();
                }

                var apartments = new List<Apartment>();
                string[] buildings = { "A", "B" };
                
                foreach (var bld in buildings)
                {
                    for (int floor = 1; floor <= 9; floor++)
                    {
                        for (int room = 1; room <= 10; room++)
                        {
                            // Định dạng số phòng chuẩn không gạch ngang: A101 -> A110; A901 -> A910
                            var roomSuffix = room < 10 ? $"0{room}" : $"{room}";
                            var apartmentNumber = $"{bld}{floor}{roomSuffix}";
                            
                            // Phân bổ diện tích căn hộ thực tế và khoa học theo vị trí phòng
                            double area = 75.0; // Phòng thường tiêu chuẩn (phòng số 2, 5, 6, 9)
                            if (room == 1 || room == 10)
                            {
                                area = 90.0; // Phòng góc lớn cao cấp (phòng số 1, 10)
                            }
                            else if (room == 3 || room == 4 || room == 7 || room == 8)
                            {
                                area = 65.0; // Phòng studio/central nhỏ gọn (phòng số 3, 4, 7, 8)
                            }

                            apartments.Add(new Apartment
                            {
                                ApartmentNumber = apartmentNumber,
                                Floor = floor,
                                Area = area,
                                OwnerId = null,
                                IsDeleted = false,
                                CreatedAt = DateTime.UtcNow.AddHours(7)
                            });
                        }
                    }
                }

                await context.Apartments.AddRangeAsync(apartments);
                await context.SaveChangesAsync();
            }
        }
    }
}
