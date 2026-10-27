using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using quanlykho.Common.PhanQuyen;
using quanlykho.Data;

namespace quanlykho.Tests.HaTang;

/// <summary>
/// App trong bộ nhớ cho test; không tự migrate CSDL dev của máy đang chạy test.
/// Mặc định dùng đăng nhập giả theo header X-Vai (không cần CSDL): TaoClient(Vai.QuanTri).
/// Test cần đăng nhập thật bằng cookie: new UngDungFactory { ChuoiKetNoi = ..., DangNhapGia = false }.
/// </summary>
public class UngDungFactory : WebApplicationFactory<Program>
{
    public string? ChuoiKetNoi { get; init; }
    public bool DangNhapGia { get; init; } = true;
    public FakeTimeProvider? Gio { get; init; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration(c => c.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Csdl:TuDongMigrate"] = "false"
        }));

        builder.ConfigureTestServices(services =>
        {
            if (ChuoiKetNoi is not null)
            {
                services.RemoveAll<DbContextOptions<AppDbContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
                services.AddDbContext<AppDbContext>(o => o.UseNpgsql(ChuoiKetNoi));
            }
            if (Gio is not null)
                services.Replace(ServiceDescriptor.Singleton<TimeProvider>(Gio));
            if (DangNhapGia)
                services.AddAuthentication(o =>
                {
                    o.DefaultScheme = XacThucGia.Ten;
                    o.DefaultChallengeScheme = XacThucGia.Ten;
                    o.DefaultForbidScheme = XacThucGia.Ten;
                }).AddScheme<AuthenticationSchemeOptions, XacThucGia>(XacThucGia.Ten, _ => { });
        });
    }

    /// <summary>Client không tự theo chuyển hướng; vai null là chưa đăng nhập.</summary>
    public HttpClient TaoClient(string? vai = Vai.QuanTri, string ma = "ND_TEST")
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        if (vai is not null)
        {
            client.DefaultRequestHeaders.Add(XacThucGia.HeaderVai, vai);
            client.DefaultRequestHeaders.Add(XacThucGia.HeaderMa, ma);
        }
        return client;
    }
}

/// <summary>Đăng nhập giả: header X-Vai là vai, X-Ma là mã người dùng. Không có header thì là chưa đăng nhập.</summary>
public class XacThucGia(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string Ten = "Gia";
    public const string HeaderVai = "X-Vai";
    public const string HeaderMa = "X-Ma";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(HeaderVai, out var vai))
            return Task.FromResult(AuthenticateResult.NoResult());
        var identity = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, Request.Headers[HeaderMa].ToString()),
            new Claim(ClaimTypes.Name, "Người thử"),
            new Claim(ClaimTypes.Role, vai.ToString()),
        ], Ten);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Ten)));
    }
}
