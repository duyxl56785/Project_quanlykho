using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using quanlykho.Common;
using quanlykho.Common.Loi;
using quanlykho.Common.PhanQuyen;
using quanlykho.Domain;
using quanlykho.Features.Kho;
using quanlykho.Tests.HaTang;

namespace quanlykho.Tests.Integration;

[Collection(CsdlCollection.Ten)]
public class ViTriTests(CsdlFixture csdl) : IAsyncLifetime
{
    private string _chuoi = "";
    private UngDungFactory _app = null!;

    public async Task InitializeAsync()
    {
        _chuoi = await csdl.TaoCsdlCoDuLieuThuAsync();
        _app = new UngDungFactory { ChuoiKetNoi = _chuoi };
    }

    public async Task DisposeAsync() => await _app.DisposeAsync();

    private async Task<T> Voi<T>(Func<ViTriService, Task<T>> lam, string ma = "ND002", string vai = Vai.QuanLyKho)
    {
        await using var db = CsdlFixture.TaoDbContext(_chuoi);
        var hienTai = NguoiThu.HienTai(db, ma, vai);
        return await lam(new ViTriService(db, hienTai, NguoiThu.NhatKy(db, hienTai)));
    }

    private Task Voi(Func<ViTriService, Task> lam, string ma = "ND002", string vai = Vai.QuanLyKho) =>
        Voi(async s => { await lam(s); return 0; }, ma, vai);

    private Task<string> Them(string maCha, int so, string ten, LoaiKhu? khu = null, DieuKienBaoQuan? baoQuan = null, decimal? kichThuoc = null) =>
        Voi(s => s.ThemAsync(new ViTriForm
        {
            MaCha = maCha, So = so, Ten = ten, LoaiKhu = khu, DieuKienBaoQuan = baoQuan,
            Dai = kichThuoc, Rong = kichThuoc, Cao = kichThuoc, TaiTrongToiDa = kichThuoc * 100,
        }));

    [Fact]
    public async Task SinhMaDu7Cap_KeThuaKhu_Cap8BiChan()
    {
        var ma = "K01";
        ma = await Them(ma, 2, "Tòa 2");
        ma = await Them(ma, 3, "Tầng 3");
        ma = await Them(ma, 4, "Phòng 4", LoaiKhu.LuuTru, DieuKienBaoQuan.Lanh);
        ma = await Them(ma, 5, "Hàng 5");
        ma = await Them(ma, 6, "Kệ 6");
        ma = await Them(ma, 7, "Ô 7", kichThuoc: 1.2m);
        Assert.Equal("K01-TA02-TG03-P04-H05-KE06-O07", ma);

        await using (var db = CsdlFixture.TaoDbContext(_chuoi))
        {
            var o = await db.ViTri.SingleAsync(v => v.MaViTri == ma);
            Assert.Equal((7, LoaiKhu.LuuTru, DieuKienBaoQuan.Lanh, "K01"), ((int)o.Cap, o.LoaiKhu!.Value, o.DieuKienBaoQuan!.Value, o.MaKho));
            Assert.Equal("Kho Hà Nội / Tòa 2 / Tầng 3 / Phòng 4 / Hàng 5 / Kệ 6 / Ô 7", o.TenDayDu);
        }

        var loi = await Assert.ThrowsAsync<NghiepVuException>(() => Them(ma, 1, "Cấp 8"));
        Assert.Contains("cấp cuối", loi.Message);
        await Assert.ThrowsAsync<NghiepVuException>(() => Them("K01", 2, "Trùng"));
        await Assert.ThrowsAsync<NghiepVuException>(() => Them("K01-TA02-TG03", 5, "Thiếu khu"));
        Assert.Equal("K01-TA03", await Voi(s => s.ThemAsync(new ViTriForm { MaCha = "K01", Ten = "Tòa tự đánh số" })));
    }

    [Fact]
    public async Task OThieuKichThuoc_BiChan()
    {
        var ke = "K01-TA01-TG01-P02-H01-KE01";
        var loi = await Assert.ThrowsAsync<NghiepVuException>(() => Them(ke, 20, "Ô thiếu"));
        Assert.Contains("tải trọng", loi.Message);
        await Assert.ThrowsAsync<NghiepVuException>(() => Them(ke, 20, "Ô âm", kichThuoc: -1));
        Assert.Equal($"{ke}-O20", await Them(ke, 20, "Ô đủ", kichThuoc: 1));
    }

    [Fact]
    public async Task ViTriCoHang_ChiKhoa_KhongXoa()
    {
        var ke = "K01-TA01-TG01-P02-H01-KE01";
        var o = await Them(ke, 20, "Ô mới", kichThuoc: 1);
        var trong = await Them(ke, 21, "Ô trống", kichThuoc: 1);
        await using (var db = CsdlFixture.TaoDbContext(_chuoi))
        {
            db.Lo.Add(new Lo { MaLo = "LT1", MaBienThe = "AOTHUN-01", NgayNhap = DateTimeOffset.UtcNow });
            await db.SaveChangesAsync();
            await using var tx = await db.Database.BeginTransactionAsync();
            var cong = new CongGhiTon(db, NguoiThu.HienTai(db, "ND002", Vai.QuanLyKho), TimeProvider.System);
            await cong.GhiAsync("vao", "Thu", "T1", [new("LT1", o, 5)]);
            await cong.GhiAsync("ra", "Thu", "T2", [new("LT1", o, -5)]);
            await tx.CommitAsync();
        }

        var loi = await Assert.ThrowsAsync<NghiepVuException>(() => Voi(s => s.XoaAsync(o)));
        Assert.Contains("từng có hàng", loi.Message);
        await Assert.ThrowsAsync<NghiepVuException>(() => Voi(s => s.XoaAsync(ke)));
        await Assert.ThrowsAsync<NghiepVuException>(() => Voi(s => s.XoaAsync("K01-TA01-TG01-P01-H01-KE01-O01")));
        await Voi(s => s.XoaAsync(trong));

        await Voi(s => s.KhoaAsync(ke, true));
        await Assert.ThrowsAsync<NghiepVuException>(() => Voi(s => s.KhoaAsync(o, false)));
        await using (var db = CsdlFixture.TaoDbContext(_chuoi))
        {
            Assert.True(await db.ViTri.Where(v => v.MaViTri.StartsWith(ke)).AllAsync(v => v.DangKhoa));
            Assert.False(await db.ViTri.AnyAsync(v => v.MaViTri == trong));
            Assert.True(await db.NhatKy.AnyAsync(n => n.LoaiThaoTac == "KhoaViTri" && n.MaDoiTuong == ke));
        }
        await Voi(s => s.KhoaAsync(ke, false));
        Assert.False((await Voi(s => s.LayAsync(o))).Nut.DangKhoa);
    }

    [Fact]
    public async Task SuaPhongDay_DoiTenVaKhuChoCaNhanh_ChanKhiCoHang()
    {
        var phong = "K01-TA01-TG01-P05";
        await Voi(s => s.SuaAsync(phong, new ViTriForm { Ten = "Khu đóng gói", LoaiKhu = LoaiKhu.LuuTru, DieuKienBaoQuan = DieuKienBaoQuan.Lanh }));

        await using (var db = CsdlFixture.TaoDbContext(_chuoi))
        {
            var o = await db.ViTri.SingleAsync(v => v.MaViTri == phong + "-H01-KE01-O01");
            Assert.Equal((LoaiKhu.LuuTru, DieuKienBaoQuan.Lanh), (o.LoaiKhu!.Value, o.DieuKienBaoQuan!.Value));
            Assert.Equal("Kho Hà Nội / Tòa 1 / Tầng 1 / Khu đóng gói / Hàng 1 / Kệ 1 / Ô 1", o.TenDayDu);
        }
        await Assert.ThrowsAsync<NghiepVuException>(() =>
            Voi(s => s.SuaAsync("K01-TA01-TG01-P01", new ViTriForm { Ten = "Khu tiếp nhận", LoaiKhu = LoaiKhu.LuuTru, DieuKienBaoQuan = DieuKienBaoQuan.Thuong })));
    }

    [Fact]
    public async Task PhanQuyenKho_VaApi()
    {
        await Assert.ThrowsAsync<KhongCoQuyenException>(() => Voi(s => s.ThemAsync(new ViTriForm { MaCha = "K01", Ten = "X" }), "ND003"));

        var quanLy = _app.TaoClient(Vai.QuanLyKho, "ND002");
        var cay = await quanLy.GetFromJsonAsync<JsonElement>("/api/vi-tri/cay?maKho=K01");
        Assert.Equal("K01", cay[0].GetProperty("ma").GetString());
        Assert.Equal("K01-TA01", cay[0].GetProperty("con")[0].GetProperty("ma").GetString());
        Assert.Equal(HttpStatusCode.Forbidden, (await _app.TaoClient(Vai.QuanLyKho, "ND003").GetAsync("/api/vi-tri/cay?maKho=K01")).StatusCode);

        var token = await NguoiThu.TokenAsync(quanLy, "/ViTri");
        var post = new HttpRequestMessage(HttpMethod.Post, "/api/vi-tri") { Content = JsonContent.Create(new { maCha = "K01-TA01", ten = "Tầng 2" }) };
        post.Headers.Add("RequestVerificationToken", token);
        var tao = await quanLy.SendAsync(post);
        Assert.Equal(HttpStatusCode.Created, tao.StatusCode);
        Assert.Equal("K01-TA01-TG02", (await tao.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("ma").GetString());

        var put = new HttpRequestMessage(HttpMethod.Put, "/api/vi-tri/K01-TA01-TG02") { Content = JsonContent.Create(new { ten = "Tầng hai" }) };
        put.Headers.Add("RequestVerificationToken", token);
        Assert.Equal(HttpStatusCode.NoContent, (await quanLy.SendAsync(put)).StatusCode);

        var thuKho = _app.TaoClient(Vai.ThuKho, "ND004");
        Assert.Contains("K01-TA01-TG02", await thuKho.GetStringAsync("/ViTri?maKho=K01"));
        Assert.Equal(HttpStatusCode.Forbidden, (await thuKho.GetAsync("/ViTri/Them?maCha=K01")).StatusCode);

        var r = await NguoiThu.PostFormAsync(quanLy, "/ViTri/Them", "/ViTri/Them?maCha=K01-TA01-TG02", new() { ["MaCha"] = "K01-TA01-TG02", ["So"] = "1", ["Ten"] = "Phòng A" });
        Assert.Contains("Chọn loại khu", await r.Content.ReadAsStringAsync());
    }
}
