using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using Microsoft.Extensions.WebEncoders;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using quanlykho.Common;
using quanlykho.Common.Loi;
using quanlykho.Common.PhanQuyen;
using quanlykho.Data;
using quanlykho.Data.Seed;
using quanlykho.Domain;
using quanlykho.Features.DanhMuc;
using quanlykho.Features.NhapKho;
using quanlykho.Features.QuanLyNguoiDung;
using quanlykho.Features.TaiKhoan;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add<XuLyLoiFilter>();
    // Chống CSRF cho mọi POST, PUT, DELETE: form có token sẵn, fetch gửi header RequestVerificationToken (site.js).
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
}).AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("QuanLyKho")));
// Giữ nguyên chữ tiếng Việt trong HTML thay vì mã hóa thành &#x...; (vẫn mã hóa ký tự nguy hiểm như < > " &).
builder.Services.Configure<WebEncoderOptions>(o => o.TextEncoderSettings = new TextEncoderSettings(UnicodeRanges.All));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<MaChungTu>();
builder.Services.AddScoped<IPasswordHasher<NguoiDung>, PasswordHasher<NguoiDung>>();
builder.Services.AddScoped<DuLieuThu>();
builder.Services.AddPhanQuyen();
builder.Services.AddScoped<GhiNhatKy>();
builder.Services.AddScoped<CongGhiTon>();
builder.Services.AddScoped<TaiKhoanService>();
builder.Services.AddScoped<NguoiDungService>();
builder.Services.AddScoped<DanhMucService>();
builder.Services.AddScoped<DoiTacService>();
builder.Services.AddScoped<DonMuaService>();
builder.Services.AddScoped<PhieuNhapService>();

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

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets().AllowAnonymous();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapKiemTraCsdl().AllowAnonymous();


app.Run();

// Cho dự án test dùng WebApplicationFactory<Program>.
public partial class Program;
