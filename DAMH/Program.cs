using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using DAMH.Data;
using DAMH.Models;

var builder = WebApplication.CreateBuilder(args);

// 1. Thêm dịch vụ vào Container
builder.Services.AddControllersWithViews();

// Đăng ký DbContext (KẾT NỐI DATABASE)
builder.Services.AddDbContext<LibraryContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// Đăng ký Identity (HỆ THỐNG TÀI KHOẢN)
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options => {
    // Cấu hình mật khẩu (tùy chọn)
    options.Password.RequireDigit = false; // Không bắt buộc số
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false; // Không bắt buộc ký tự đặc biệt
    options.Password.RequiredLength = 6; // Độ dài tối thiểu 6
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<LibraryContext>()
.AddDefaultTokenProviders()
.AddDefaultUI();

// Cần thiết cho trang Login/Register của Identity
builder.Services.AddRazorPages();

var app = builder.Build();

// 2. Tự động tạo Admin khi chạy web (Seeding)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        // Gọi hàm khởi tạo dữ liệu mẫu
        await DbInitializer.Initialize(services);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Lỗi khi khởi tạo dữ liệu Admin.");
    }
}

// 3. Cấu hình Pipeline (Luồng xử lý)
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// QUAN TRỌNG: Phải có 2 dòng này để đăng nhập hoạt động
app.UseAuthentication(); // Xác thực (Bạn là ai?)
app.UseAuthorization();  // Phân quyền (Bạn được làm gì?)

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages(); // Map các trang Identity

app.Run();