using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Time.Testing;
using quanlykho.Common.Loi;
using quanlykho.Common.PhanQuyen;
using quanlykho.Data.Seed;
using quanlykho.Domain;
using quanlykho.Tests.HaTang;

namespace quanlykho.Tests.Integration;

/// <summary>Đăng nhập thật bằng cookie trên CSDL đã nạp dữ liệu thử (tài khoản ND001–ND007).</summary>
[Collection(CsdlCollection.Ten)]
public class PhanQuyenTests(CsdlFixture csdl) : IAsyncLifetime
{
    private const string MatKhau = "MatKhauThu@1";
    private string _chuoi = "";
    private readonly FakeTimeProvider _gio = new(DateTimeOffset.UtcNow);
    private UngDungFactory _app = null!;

    public async Task InitializeAsync()
    {
        _chuoi = await csdl.TaoCsdlAsync();
        await using var db = CsdlFixture.TaoDbContext(_chuoi);
        await new DuLieuThu(db, new PasswordHasher<NguoiDung>(), new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["DuLieuThu:MatKhau"] = MatKhau }).Build()).NapAsync();
        _app = new UngDungFactory { ChuoiKetNoi = _chuoi, DangNhapGia = false, Gio = _gio };
    }

    public async Task DisposeAsync() => await _app.DisposeAsync();

    // ---------- Tiện ích ----------

    private HttpClient Client() => _app.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    private static async Task<string> Token(HttpClient client, string url) =>
        Regex.Match(await client.GetStringAsync(url), "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"").Groups[1].Value;

    private static async Task<HttpResponseMessage> Post(HttpClient client, string url, string trangLayToken, Dictionary<string, string>? duLieu = null)
    {
        duLieu ??= [];
        duLieu["__RequestVerificationToken"] = await Token(client, trangLayToken);
        var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = new FormUrlEncodedContent(duLieu) };
        request.Headers.Referrer = new Uri(client.BaseAddress!, trangLayToken);
        return await client.SendAsync(request);
    }

    private async Task<(HttpClient Client, HttpResponseMessage KetQua)> DangNhap(string email, string matKhau = MatKhau)
    {
        var client = Client();
        var ketQua = await Post(client, "/DangNhap", "/DangNhap", new() { ["Email"] = email, ["MatKhau"] = matKhau });
        return (client, ketQua);
    }

    private async Task<HttpClient> DaDangNhap(string email)
    {
        var (client, ketQua) = await DangNhap(email);
        Assert.Equal(HttpStatusCode.Redirect, ketQua.StatusCode);
        return client;
    }

    // ---------- Đăng nhập ----------

    [Fact]
    public async Task ChuaDangNhap_TrangChuyenVeDangNhap_ApiTra401()
    {
        var client = Client();

        var trang = await client.GetAsync("/NguoiDung");
        Assert.Equal(HttpStatusCode.Redirect, trang.StatusCode);
        Assert.StartsWith("/DangNhap", trang.Headers.Location!.PathAndQuery);

        var api = await client.GetAsync("/api/tai-khoan/toi");
        Assert.Equal(HttpStatusCode.Unauthorized, api.StatusCode);
        Assert.NotNull((await api.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("loi").GetString());

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/DangNhap")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/css/site.css")).StatusCode);
    }

    [Fact]
    public async Task DangNhapDung_EmailKhongPhanBietHoaThuong()
    {
        var client = await DaDangNhap("ADMIN@Kho.Test");

        var toi = await client.GetFromJsonAsync<JsonElement>("/api/tai-khoan/toi");
        Assert.Equal("ND001", toi.GetProperty("ma").GetString());
        Assert.Equal(["K01", "K02"], toi.GetProperty("kho").EnumerateArray().Select(k => k.GetString()));
    }

    [Fact]
    public async Task SaiMatKhau_BaoLoiChung()
    {
        var (_, saiMatKhau) = await DangNhap("admin@kho.test", "sai");
        var (_, khongCoEmail) = await DangNhap("khongco@kho.test", "sai");

        Assert.Contains("Email hoặc mật khẩu không đúng", await saiMatKhau.Content.ReadAsStringAsync());
        Assert.Contains("Email hoặc mật khẩu không đúng", await khongCoEmail.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task SaiNamLan_KhoaTam15Phut_RoiMoLai()
    {
        for (var i = 0; i < 5; i++)
            await DangNhap("tk1.k01@kho.test", "sai");

        var (_, dungMatKhau) = await DangNhap("tk1.k01@kho.test");
        Assert.Equal(HttpStatusCode.OK, dungMatKhau.StatusCode); // vẫn ở trang đăng nhập
        await using (var db = CsdlFixture.TaoDbContext(_chuoi))
            Assert.True(await db.NhatKy.AnyAsync(n => n.LoaiThaoTac == "KhoaTamDangNhap" && n.MaDoiTuong == "ND004"));

        _gio.Advance(TimeSpan.FromMinutes(16));
        var (_, sauKhoa) = await DangNhap("tk1.k01@kho.test");
        Assert.Equal(HttpStatusCode.Redirect, sauKhoa.StatusCode);
    }

    // ---------- Phân quyền theo vai và theo kho ----------

    [Theory]
    [InlineData("admin@kho.test", true, "K01,K02")]
    [InlineData("ql.k01@kho.test", false, "K01")]
    [InlineData("tk.k02@kho.test", false, "K02")]
    [InlineData("banhang@kho.test", false, "K01,K02")]
    public async Task MaTranVai(string email, bool laQuanTri, string kho)
    {
        var client = await DaDangNhap(email);

        foreach (var url in new[] { "/NguoiDung", "/NguoiDung/Them", "/Styleguide" })
        {
            var r = await client.GetAsync(url);
            if (laQuanTri)
                Assert.Equal(HttpStatusCode.OK, r.StatusCode);
            else
                Assert.StartsWith("/TuChoi", r.Headers.Location?.PathAndQuery);
        }
        var toi = await client.GetFromJsonAsync<JsonElement>("/api/tai-khoan/toi");
        Assert.Equal(kho, string.Join(",", toi.GetProperty("kho").EnumerateArray().Select(k => k.GetString())));

        // Menu chỉ hiện mục được phép.
        var trangChu = await client.GetStringAsync("/");
        Assert.Equal(laQuanTri, trangChu.Contains("href=\"/NguoiDung\""));
    }

    [Fact]
    public async Task DamBaoQuyenKho_ChanKhoKhongDuocGiao()
    {
        await using var db = CsdlFixture.TaoDbContext(_chuoi);
        var http = new HttpContextAccessor { HttpContext = new DefaultHttpContext { User = CauHinhPhanQuyen.TaoPrincipal(await db.NguoiDung.SingleAsync(n => n.MaNguoiDung == "ND002")) } };
        var hienTai = new NguoiDungHienTai(http, db);

        await hienTai.DamBaoQuyenKhoAsync("K01");
        await Assert.ThrowsAsync<KhongCoQuyenException>(() => hienTai.DamBaoQuyenKhoAsync("K02"));
    }

    // ---------- Quản lý tài khoản ----------

    [Fact]
    public async Task KhoaTaiKhoan_PhienCuMatQuyenNgay_VaKhongDangNhapLaiDuoc()
    {
        var thuKho = await DaDangNhap("tk1.k01@kho.test");
        var admin = await DaDangNhap("admin@kho.test");

        var khoa = await Post(admin, "/NguoiDung/Khoa/ND004", "/NguoiDung/Sua/ND004");
        Assert.Equal(HttpStatusCode.Redirect, khoa.StatusCode);

        Assert.Equal(HttpStatusCode.Unauthorized, (await thuKho.GetAsync("/api/tai-khoan/toi")).StatusCode);
        var (_, dangNhapLai) = await DangNhap("tk1.k01@kho.test");
        Assert.Equal(HttpStatusCode.OK, dangNhapLai.StatusCode);
        await using var db = CsdlFixture.TaoDbContext(_chuoi);
        Assert.True(await db.NhatKy.AnyAsync(n => n.LoaiThaoTac == "KhoaTaiKhoan" && n.MaDoiTuong == "ND004" && n.MaNguoiDung == "ND001"));
    }

    [Fact]
    public async Task KhongKhoaDuocQuanTriVienCuoiCung()
    {
        var admin = await DaDangNhap("admin@kho.test");

        var r = await Post(admin, "/NguoiDung/Khoa/ND001", "/NguoiDung/Sua/ND001");

        Assert.Equal("/NguoiDung/Sua/ND001", r.Headers.Location?.OriginalString);
        Assert.Contains("Phải luôn còn ít nhất một quản trị viên đang hoạt động.", await admin.GetStringAsync("/NguoiDung/Sua/ND001"));
        await using var db = CsdlFixture.TaoDbContext(_chuoi);
        Assert.Equal(TrangThaiTaiKhoan.DangHoatDong, (await db.NguoiDung.SingleAsync(n => n.MaNguoiDung == "ND001")).TrangThai);
    }

    [Fact]
    public async Task DoiKhoDuocGiao_PhienCuHetHieuLuc()
    {
        var quanLy = await DaDangNhap("ql.k01@kho.test");
        var admin = await DaDangNhap("admin@kho.test");

        var r = await Post(admin, "/NguoiDung/Sua/ND002", "/NguoiDung/Sua/ND002", new()
        {
            ["Ten"] = "Quản lý kho Hà Nội", ["Email"] = "ql.k01@kho.test", ["VaiTro"] = "QuanLyKho", ["Kho"] = "K02"
        });
        Assert.Equal(HttpStatusCode.Redirect, r.StatusCode);

        Assert.Equal(HttpStatusCode.Unauthorized, (await quanLy.GetAsync("/api/tai-khoan/toi")).StatusCode);
        var moi = await DaDangNhap("ql.k01@kho.test");
        var toi = await moi.GetFromJsonAsync<JsonElement>("/api/tai-khoan/toi");
        Assert.Equal("K02", toi.GetProperty("kho")[0].GetString());
    }

    [Fact]
    public async Task ThemNguoiDung_KiemLuat_RoiDangNhapDuoc()
    {
        var admin = await DaDangNhap("admin@kho.test");
        Dictionary<string, string> Form(string email, string vai, string? kho = null) => new()
        {
            ["Ten"] = "Thủ kho mới", ["Email"] = email, ["VaiTro"] = vai, ["MatKhau"] = "MoiVao2026", ["Kho"] = kho ?? ""
        };

        var thieuKho = await Post(admin, "/NguoiDung/Them", "/NguoiDung/Them", Form("moi@kho.test", "ThuKho"));
        Assert.Contains("phải được giao ít nhất một kho", await thieuKho.Content.ReadAsStringAsync());

        var trungEmail = await Post(admin, "/NguoiDung/Them", "/NguoiDung/Them", Form("tk1.k01@kho.test", "ThuKho", "K01"));
        Assert.Contains("đã được dùng cho tài khoản khác", await trungEmail.Content.ReadAsStringAsync());

        var dung = await Post(admin, "/NguoiDung/Them", "/NguoiDung/Them", Form("Moi@Kho.Test", "ThuKho", "K01"));
        Assert.Equal(HttpStatusCode.Redirect, dung.StatusCode);
        Assert.Matches("^/NguoiDung/Sua/ND\\d{6}$", dung.Headers.Location!.OriginalString);

        var (_, dangNhap) = await DangNhap("moi@kho.test", "MoiVao2026");
        Assert.Equal(HttpStatusCode.Redirect, dangNhap.StatusCode);
    }

    [Fact]
    public async Task DatLaiMatKhauYeu_BaoLoiQuaTempData()
    {
        var admin = await DaDangNhap("admin@kho.test");

        var r = await Post(admin, "/NguoiDung/DatLaiMatKhau/ND004", "/NguoiDung/Sua/ND004", new() { ["matKhauMoi"] = "123" });

        Assert.Equal("/NguoiDung/Sua/ND004", r.Headers.Location?.OriginalString);
        Assert.Contains("Mật khẩu phải có ít nhất 8 ký tự", await admin.GetStringAsync("/NguoiDung/Sua/ND004"));
    }

    [Fact]
    public async Task DangXuat_XoaPhien()
    {
        var client = await DaDangNhap("banhang@kho.test");

        var r = await Post(client, "/DangXuat", "/");

        Assert.Equal(HttpStatusCode.Redirect, r.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/tai-khoan/toi")).StatusCode);
    }
}
