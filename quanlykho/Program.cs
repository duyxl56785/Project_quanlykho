using System.Globalization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using quanlykho.Common;
using quanlykho.Common.Loi;
using quanlykho.Data;
using quanlykho.Data.Seed;
using quanlykho.Domain;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews(options => options.Filters.Add<XuLyLoiFilter>());
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("QuanLyKho")));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<MaChungTu>();
builder.Services.AddScoped<IPasswordHasher<NguoiDung>, PasswordHasher<NguoiDung>>();
builder.Services.AddScoped<DuLieuThu>();

var app = builder.Build();

// dotnet run --project quanlykho -- --seed: migrate, nạp dữ liệu thử rồi thoát.
if (args.Contains("--seed"))
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
    await scope.ServiceProvider.GetRequiredService<DuLieuThu>().NapAsync();
    app.Logger.LogInformation("Đã nạp dữ liệu thử.");
    return;
}

if (app.Configuration.GetValue<bool>("Csdl:TuDongMigrate"))
{
    using var scope = app.Services.CreateScope();
    scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.Migrate();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
// Đọc số và ngày gửi lên bằng InvariantCulture ("1.5", "2026-10-27") trên mọi máy; hiển thị kiểu Việt Nam do helper định dạng lo.
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(CultureInfo.InvariantCulture),
    SupportedCultures = [CultureInfo.InvariantCulture],
    SupportedUICultures = [CultureInfo.InvariantCulture],
    RequestCultureProviders = []
});
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapKiemTraCsdl();


app.Run();

// Cho dự án test dùng WebApplicationFactory<Program>.
public partial class Program;
