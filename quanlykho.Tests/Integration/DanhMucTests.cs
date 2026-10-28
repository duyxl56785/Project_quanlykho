using System.Net;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using quanlykho.Common.Loi;
using quanlykho.Common.PhanQuyen;
using quanlykho.Domain;
using quanlykho.Features.DanhMuc;
using quanlykho.Tests.HaTang;

namespace quanlykho.Tests.Integration;

[Collection(CsdlCollection.Ten)]
public class DanhMucTests(CsdlFixture csdl) : IAsyncLifetime
{
    private string _chuoi = "";
    private UngDungFactory _app = null!;

    public async Task InitializeAsync()
    {
        _chuoi = await csdl.TaoCsdlCoDuLieuThuAsync();
        _app = new UngDungFactory { ChuoiKetNoi = _chuoi };
    }

    public async Task DisposeAsync() => await _app.DisposeAsync();

    private DanhMucService Service(out Data.AppDbContext db)
    {
        db = CsdlFixture.TaoDbContext(_chuoi);
        return new DanhMucService(db, NguoiThu.NhatKy(db, NguoiThu.HienTai(db, "ND001", Vai.QuanTri)), new QuyDoiDonVi(db));
    }

    private static async Task<HttpResponseMessage> Post(HttpClient client, string url, string trangLayToken, Dictionary<string, string> duLieu)
    {
        var html = await client.GetStringAsync(trangLayToken);
        duLieu["__RequestVerificationToken"] = Regex.Match(html, "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"").Groups[1].Value;
        var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = new FormUrlEncodedContent(duLieu) };
        request.Headers.Referrer = new Uri(client.BaseAddress!, trangLayToken);
        return await client.SendAsync(request);
    }

    [Fact]
    public async Task ThemNganhHangMoi_TrenGiaoDien_KhongSuaMa()
    {
        var admin = _app.TaoClient(Vai.QuanTri, "ND001");

        Assert.Equal(HttpStatusCode.Redirect, (await Post(admin, "/DanhMuc/NhomHang/Them", "/DanhMuc/NhomHang/Them",
            new() { ["Ma"] = "gd", ["Ten"] = "Đồ gia dụng" })).StatusCode);
        await Post(admin, "/DanhMuc/NhomHang/GD/ThuocTinh", "/DanhMuc/NhomHang/Sua/GD", new() { ["ten"] = "Công suất", ["giaTriGoiY"] = "500W, 1000W" });
        await Post(admin, "/DanhMuc/NhomHang/GD/ThuocTinh", "/DanhMuc/NhomHang/Sua/GD", new() { ["ten"] = "Bảo hành", ["giaTriGoiY"] = "12 tháng, 24 tháng" });

        var them = await Post(admin, "/DanhMuc/SanPham/Them", "/DanhMuc/SanPham/Them", new()
        {
            ["Ma"] = "noicom", ["Ten"] = "Nồi cơm điện", ["DonViTinh"] = "cái", ["MaNhom"] = "GD", ["DieuKienBaoQuan"] = "Thuong"
        });
        Assert.Equal("/DanhMuc/SanPham/NOICOM", them.Headers.Location?.OriginalString);

        var sinh = new Dictionary<string, string>
        {
            ["GiaTri[Công suất]"] = "500W, 1000W", ["GiaTri[Bảo hành]"] = "12 tháng, 24 tháng",
            ["Dai"] = "0.4", ["Rong"] = "0.35", ["Cao"] = "0.3", ["KhoiLuong"] = "3.5"
        };
        await Post(admin, "/DanhMuc/SanPham/NOICOM/SinhBienThe", "/DanhMuc/SanPham/NOICOM", new(sinh));
        await Post(admin, "/DanhMuc/SanPham/NOICOM/SinhBienThe", "/DanhMuc/SanPham/NOICOM", new(sinh));

        await using var db = CsdlFixture.TaoDbContext(_chuoi);
        var bienThe = await db.BienThe.Where(b => b.MaSanPham == "NOICOM").OrderBy(b => b.MaBienThe).ToListAsync();
        Assert.Equal(["NOICOM-01", "NOICOM-02", "NOICOM-03", "NOICOM-04"], bienThe.Select(b => b.MaBienThe));
        Assert.All(bienThe, b => Assert.Equal(3.5m, b.KhoiLuong));
        Assert.Contains(bienThe, b => b.ThuocTinh["Công suất"] == "1000W" && b.ThuocTinh["Bảo hành"] == "24 tháng");
        Assert.Contains("bỏ qua 4 tổ hợp đã có", await admin.GetStringAsync("/DanhMuc/SanPham/NOICOM"));
    }

    [Fact]
    public async Task ThemGiaTriMoi_ChiTaoToHopMoi()
    {
        var service = Service(out var db);
        await using var _ = db;

        var (taoMoi, daCo) = await service.SinhBienTheAsync("AOTHUN", new SinhBienTheForm
        {
            GiaTri = new() { ["Size"] = "S, M, L, XL", ["Màu"] = "Trắng, Đen, Xanh" }, Dai = 0.3m, Rong = 0.25m, Cao = 0.03m, KhoiLuong = 0.2m
        });

        Assert.Equal((4, 8), (taoMoi, daCo));
        Assert.Equal("AOTHUN-12", await db.BienThe.Where(b => b.MaSanPham == "AOTHUN").MaxAsync(b => b.MaBienThe));
    }

    [Fact]
    public async Task DoiTheoDoiHanDung_KhiCoTon_BiChan()
    {
        await using (var db = CsdlFixture.TaoDbContext(_chuoi))
        {
            db.Lo.Add(new Lo { MaLo = "L1", MaBienThe = "MIGOI-01", NgayNhap = DateTimeOffset.UtcNow });
            db.TonKho.Add(new TonKho { MaLo = "L1", MaViTri = "K01-TA01-TG01-P02-H01-KE01-O01", SoLuong = 5 });
            await db.SaveChangesAsync();
        }
        var service = Service(out var db2);
        await using var _ = db2;
        var form = await service.LaySanPhamAsync("MIGOI");
        form.TheoDoiHanDung = false;

        var loi = await Assert.ThrowsAsync<NghiepVuException>(() => service.SuaSanPhamAsync("MIGOI", form));
        Assert.Contains("đang có tồn", loi.Message);

        var khongTon = await service.LaySanPhamAsync("BUTBI");
        khongTon.TheoDoiHanDung = true;
        await service.SuaSanPhamAsync("BUTBI", khongTon);
    }

    [Fact]
    public async Task DoiNhom_KhiDaCoBienThe_BiChan()
    {
        var service = Service(out var db);
        await using var _ = db;
        var form = await service.LaySanPhamAsync("AOTHUN");
        form.MaNhom = "DT";

        await Assert.ThrowsAsync<NghiepVuException>(() => service.SuaSanPhamAsync("AOTHUN", form));
    }

    [Fact]
    public async Task MaVachTrung_VaMucTonSai_BiChan()
    {
        var service = Service(out var db);
        await using var _ = db;
        var form = await service.LayBienTheAsync("AOTHUN-02");

        form.MaVach = "8930000000001";
        Assert.Contains("đã dùng cho hàng khác", (await Assert.ThrowsAsync<NghiepVuException>(() => service.SuaBienTheAsync("AOTHUN-02", form))).Message);

        form.MaVach = "8939999999999";
        form.MucTon[0].ToiThieu = 50;
        form.MucTon[0].ToiDa = 10;
        Assert.Contains("tồn tối đa phải lớn hơn", (await Assert.ThrowsAsync<NghiepVuException>(() => service.SuaBienTheAsync("AOTHUN-02", form))).Message);

        form.MucTon[0].ToiDa = 100;
        await service.SuaBienTheAsync("AOTHUN-02", form);
        Assert.Equal(100, (await db.MucTon.SingleAsync(m => m.MaBienThe == "AOTHUN-02" && m.MaKho == "K01")).ToiDa);
    }

    [Fact]
    public async Task XoaNhom_CoSanPhamHoacThuocTinhDangDung_BiChan()
    {
        var service = Service(out var db);
        await using var _ = db;

        await Assert.ThrowsAsync<NghiepVuException>(() => service.XoaNhomAsync("TT"));
        await Assert.ThrowsAsync<NghiepVuException>(() => service.XoaThuocTinhAsync("TT", "Size"));
        await Assert.ThrowsAsync<NghiepVuException>(() => service.SuaNhomAsync("TT", new NhomForm { Ten = "Thời trang", MaCha = "TT" }));

        await service.ThemNhomAsync(new NhomForm { Ma = "TRONG", Ten = "Nhóm trống", MaCha = "TT" });
        await service.XoaNhomAsync("TRONG");
    }

    [Theory]
    [InlineData("/DanhMuc/SanPham")]
    [InlineData("/DanhMuc/SanPham/AOTHUN")]
    [InlineData("/DanhMuc/SanPham/BienThe/AOTHUN-01")]
    [InlineData("/DanhMuc/NhomHang")]
    [InlineData("/DanhMuc/NhaCungCap")]
    [InlineData("/DanhMuc/NoiNhan")]
    public async Task ThuKho_XemDuoc(string url) =>
        Assert.Equal(HttpStatusCode.OK, (await _app.TaoClient(Vai.ThuKho).GetAsync(url)).StatusCode);

    [Theory]
    [InlineData("/DanhMuc/SanPham/Them")]
    [InlineData("/DanhMuc/NhomHang/Them")]
    [InlineData("/DanhMuc/NhaCungCap/Them")]
    [InlineData("/DanhMuc/NhaCungCap/NCC01")]
    public async Task ThuKho_KhongSuaDuoc(string url) =>
        Assert.Equal(HttpStatusCode.Forbidden, (await _app.TaoClient(Vai.ThuKho).GetAsync(url)).StatusCode);

    [Theory]
    [InlineData("/DanhMuc/SanPham/Them")]
    [InlineData("/DanhMuc/NhomHang/Them")]
    [InlineData("/DanhMuc/NhomHang/Sua/TT")]
    [InlineData("/DanhMuc/NhaCungCap/Them")]
    [InlineData("/DanhMuc/NoiNhan/NN01")]
    public async Task QuanTri_MoTrangSua(string url) =>
        Assert.Equal(HttpStatusCode.OK, (await _app.TaoClient(Vai.QuanTri).GetAsync(url)).StatusCode);

    [Fact]
    public async Task NhaCungCap_ThemSuaNgungDung()
    {
        var admin = _app.TaoClient(Vai.QuanTri, "ND001");

        var trung = await Post(admin, "/DanhMuc/NhaCungCap/Them", "/DanhMuc/NhaCungCap/Them", new() { ["Ma"] = "NCC01", ["Ten"] = "Trùng" });
        Assert.Contains("Mã NCC01 đã có.", await trung.Content.ReadAsStringAsync());

        await Post(admin, "/DanhMuc/NhaCungCap/NCC02", "/DanhMuc/NhaCungCap/NCC02", new() { ["Ten"] = "Điện tử Sao Mai", ["TrangThai"] = "NgungSuDung" });
        await using var db = CsdlFixture.TaoDbContext(_chuoi);
        Assert.Equal(TrangThaiSuDung.NgungSuDung, (await db.NhaCungCap.SingleAsync(n => n.MaNhaCungCap == "NCC02")).TrangThai);
    }
}
