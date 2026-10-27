using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using quanlykho.Data;
using quanlykho.Domain;

namespace quanlykho.Common.PhanQuyen;

public static class CauHinhPhanQuyen
{
    public const string ClaimSecurityStamp = "stamp";

    /// <summary>
    /// Đăng nhập bằng cookie (KE_HOACH A4). Mỗi request đọc lại TrangThai, SecurityStamp, VaiTro trong CSDL:
    /// khóa tài khoản, đổi vai, đổi kho, đổi mật khẩu có hiệu lực ngay. /api trả 401, 403 thay vì chuyển trang.
    /// Mặc định mọi trang cần đăng nhập; trang công khai đánh [AllowAnonymous].
    /// </summary>
    public static IServiceCollection AddPhanQuyen(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<NguoiDungHienTai>();

        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(o =>
            {
                o.LoginPath = "/DangNhap";
                o.LogoutPath = "/DangXuat";
                o.AccessDeniedPath = "/TuChoi";
                o.Cookie.Name = "quanlykho.dangnhap";
                o.Cookie.HttpOnly = true;
                o.Cookie.SameSite = SameSiteMode.Lax;
                o.ExpireTimeSpan = TimeSpan.FromHours(8);
                o.SlidingExpiration = true;
                o.Events.OnRedirectToLogin = ctx => LaApi(ctx.Request) ? TraMa(ctx.Response, 401) : Chuyen(ctx);
                o.Events.OnRedirectToAccessDenied = ctx => LaApi(ctx.Request) ? TraMa(ctx.Response, 403) : Chuyen(ctx);
                o.Events.OnValidatePrincipal = KiemTraConHieuLuc;
            });

        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
        return services;
    }

    public static ClaimsPrincipal TaoPrincipal(NguoiDung nd) => new(new ClaimsIdentity(
    [
        new Claim(ClaimTypes.NameIdentifier, nd.MaNguoiDung),
        new Claim(ClaimTypes.Name, nd.Ten),
        new Claim(ClaimTypes.Role, nd.VaiTro.ToString()),
        new Claim(ClaimSecurityStamp, nd.SecurityStamp),
    ], CookieAuthenticationDefaults.AuthenticationScheme));

    private static async Task KiemTraConHieuLuc(CookieValidatePrincipalContext ctx)
    {
        var ma = ctx.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
        var stamp = ctx.Principal?.FindFirstValue(ClaimSecurityStamp);
        var db = ctx.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
        var conHieuLuc = ma is not null && await db.NguoiDung.AnyAsync(n =>
            n.MaNguoiDung == ma && n.SecurityStamp == stamp && n.TrangThai == TrangThaiTaiKhoan.DangHoatDong);
        if (!conHieuLuc)
        {
            ctx.RejectPrincipal();
            await ctx.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }
    }

    private static bool LaApi(HttpRequest request) => request.Path.StartsWithSegments("/api");

    private static Task TraMa(HttpResponse response, int ma)
    {
        response.StatusCode = ma;
        return response.WriteAsJsonAsync(new { loi = ma == 401 ? "Bạn chưa đăng nhập hoặc phiên đã hết." : "Bạn không có quyền thực hiện thao tác này." });
    }

    private static Task Chuyen(RedirectContext<CookieAuthenticationOptions> ctx)
    {
        ctx.Response.Redirect(ctx.RedirectUri);
        return Task.CompletedTask;
    }
}
