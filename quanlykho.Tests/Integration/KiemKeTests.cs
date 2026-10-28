using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using quanlykho.Common;
using quanlykho.Common.Loi;
using quanlykho.Common.PhanQuyen;
using quanlykho.Domain;
using quanlykho.Features.DieuChuyen;
using quanlykho.Features.KiemKe;
using quanlykho.Features.Kho;
using quanlykho.Features.XepHang;
using quanlykho.Features.XuatKho;
using quanlykho.Tests.HaTang;

namespace quanlykho.Tests.Integration;

[Collection(CsdlCollection.Ten)]
public class KiemKeTests(CsdlFixture csdl) : IAsyncLifetime
{
    private const string Ke = "K01-TA01-TG01-P02-H01-KE01";
    private const string Lanh = "K01-TA01-TG01-P03-H01-KE01-O02";
    private const string TiepNhan = "K01-TA01-TG01-P01-H01-KE01-O01";
    private static readonly FakeTimeProvider Gio = new(new DateTimeOffset(2026, 11, 1, 3, 0, 0, TimeSpan.Zero));
    private string _chuoi = "";
    private UngDungFactory _app = null!;

    /// <summary>K01: áo L1 10 ở KE01-O01, L2 4 ở KE01-O02, điện thoại L4 3 ở KE01-O03, sữa L3 5 ở ô lạnh (giữ 2); L5 2 ở khu tiếp nhận.</summary>
    public async Task InitializeAsync()
    {
        _chuoi = await csdl.TaoCsdlCoDuLieuThuAsync();
        _app = new UngDungFactory { ChuoiKetNoi = _chuoi, Gio = Gio };
        await using var db = CsdlFixture.TaoDbContext(_chuoi);
        var lo = new (string Ma, string BienThe, string O, decimal SoLuong)[]
        {
            ("L1", "AOTHUN-01", Ke + "-O01", 10), ("L2", "AOTHUN-01", Ke + "-O02", 4), ("L4", "DTA-01", Ke + "-O03", 3),
            ("L3", "SUATUOI-01", Lanh, 5), ("L5", "AOTHUN-01", TiepNhan, 2),
        };
        db.Lo.AddRange(lo.Select(l => new Lo { MaLo = l.Ma, MaBienThe = l.BienThe, HanDung = new DateOnly(2027, 1, 1), NgayNhap = Gio.GetUtcNow() }));
        await db.SaveChangesAsync();
        await using var tx = await db.Database.BeginTransactionAsync();
        var cong = new CongGhiTon(db, NguoiThu.HienTai(db, "ND001", Vai.QuanTri), Gio);
        await cong.GhiAsync("nap", "Thu", "T1", lo.Select(l => new DongGhiTon(l.Ma, l.O, l.SoLuong)).ToList());
        await cong.GiuAsync([new DongGiu("L3", Lanh, 2)]);
        await tx.CommitAsync();
    }

    public async Task DisposeAsync() => await _app.DisposeAsync();

    private async Task<T> Voi<T>(string ma, string vai, Func<KiemKeService, Task<T>> lam)
    {
        await using var db = CsdlFixture.TaoDbContext(_chuoi);
        var h = NguoiThu.HienTai(db, ma, vai);
        return await lam(new KiemKeService(db, h, new MaChungTu(db), new TonKhaDung(db, h, Gio), NguoiThu.NhatKy(db, h), Gio));
    }

    private Task Voi(string ma, string vai, Func<KiemKeService, Task> lam) => Voi(ma, vai, async s => { await lam(s); return 0; });

    private Task<string> Lap(PhamViKiemKe phamVi, params string[] doiTuong) => Voi("ND002", Vai.QuanLyKho, s => s.TaoAsync(new KiemKeForm
    {
        MaKho = "K01", PhamVi = phamVi, DoiTuong = [.. doiTuong], ThanhVien = ["ND004"], MucDich = "Kiểm thử",
    }));

    private Task Dem(string ma, params (int STT, decimal SoLuong)[] dem) =>
        Voi("ND004", Vai.ThuKho, s => s.NhapSoDemAsync(ma, dem.Select(d => new DongDemForm { STT = d.STT, SoLuongThucTe = d.SoLuong }).ToList()));

    [Fact]
    public async Task BatDau_KhoaO_ChotSoSach_ChanMoiThaoTac()
    {
        var ma = await Lap(PhamViKiemKe.ViTri, Ke);
        await Voi("ND002", Vai.QuanLyKho, s => s.BatDauAsync(ma, false));

        var p = await Voi("ND004", Vai.ThuKho, s => s.LayAsync(ma));
        Assert.Equal(TrangThaiKiemKe.DangKiemKe, p.TrangThai);
        Assert.Equal(6, p.CacO.Count);
        Assert.Equal([("L1", 10m), ("L2", 4m), ("L4", 3m)], p.Dong.Select(d => (d.MaLo!, d.SoSach)));

        await using var db = CsdlFixture.TaoDbContext(_chuoi);
        var h = NguoiThu.HienTai(db, "ND002", Vai.QuanLyKho);
        await using (var tx = await db.Database.BeginTransactionAsync())
            Assert.Contains("kiểm kê", (await Assert.ThrowsAsync<NghiepVuException>(() => new CongGhiTon(db, h, Gio).GhiAsync("x", "Thu", "T", [new("L1", Ke + "-O01", -1)]))).Message);
        Assert.Equal(0, (await new TonKhaDung(db, h, Gio).TheoBienTheAsync("K01", ["AOTHUN-01"])).GetValueOrDefault("AOTHUN-01"));
        Assert.DoesNotContain((await new OPhuHop(db, h).TimAsync("L5", 1, "K01")), o => o.MaViTri.StartsWith(Ke));
        var xep = new XepHangService(db, h, new MaChungTu(db), new CongGhiTon(db, h, Gio), Gio);
        Assert.Contains("kiểm kê", (await Assert.ThrowsAsync<NghiepVuException>(() => xep.XepAsync(new XepHangYeuCau("L5", TiepNhan, Ke + "-O04", 1), "xep"))).Message);
        var chuyen = new DieuChuyenService(db, h, new MaChungTu(db), new CongGhiTon(db, h, Gio), new TonKhaDung(db, h, Gio), NguoiThu.NhatKy(db, h), Gio);
        await Assert.ThrowsAsync<NghiepVuException>(() => chuyen.TaoAsync(new DieuChuyenForm
        {
            MaKhoDi = "K01", MaKhoDen = "K02", KhoaChongLap = "dc", Dong = [new() { LoO = $"L1|{Ke}-O01", SoLuong = 1 }],
        }, true));

        var trung = await Lap(PhamViKiemKe.ViTri, Ke + "-O01");
        Assert.Contains($"đang thuộc phiếu kiểm kê {ma}", (await Assert.ThrowsAsync<NghiepVuException>(() => Voi("ND002", Vai.QuanLyKho, s => s.BatDauAsync(trung, false)))).Message);
    }

    [Fact]
    public async Task Dem_TinhChenhLech_ThemHangNgoaiDanhSach_ChuaHoanThanhKhiConLech()
    {
        var ma = await Lap(PhamViKiemKe.ViTri, Ke);
        await Voi("ND002", Vai.QuanLyKho, s => s.BatDauAsync(ma, false));

        await Assert.ThrowsAsync<KhongCoQuyenException>(() => Voi("ND005", Vai.ThuKho, s => s.NhapSoDemAsync(ma, [new() { STT = 1, SoLuongThucTe = 1 }])));
        await Assert.ThrowsAsync<NghiepVuException>(() => Dem(ma, (1, -1)));
        await Assert.ThrowsAsync<NghiepVuException>(() => Voi("ND002", Vai.QuanLyKho, s => s.HoanThanhAsync(ma)));
        await Dem(ma, (1, 8), (2, 4), (3, 3));
        Assert.Equal(4, await Voi("ND004", Vai.ThuKho, s => s.ThemDongAsync(ma, new ThemDongKiemKe(Ke + "-O05", "AOTHUN-02", 2))));
        await Assert.ThrowsAsync<NghiepVuException>(() => Voi("ND004", Vai.ThuKho, s => s.ThemDongAsync(ma, new ThemDongKiemKe(Lanh, "L3", 1))));

        var p = await Voi("ND004", Vai.ThuKho, s => s.LayAsync(ma));
        Assert.Equal([-2m, 0m, 0m, 2m], p.Dong.Select(d => d.ChenhLech!.Value));
        Assert.Equal((0, 2), (p.ChuaDem, p.CoChenhLech));
        Assert.Contains("chênh lệch", (await Assert.ThrowsAsync<NghiepVuException>(() => Voi("ND002", Vai.QuanLyKho, s => s.HoanThanhAsync(ma)))).Message);

        await using (var db = CsdlFixture.TaoDbContext(_chuoi))
            Assert.Equal(10, (await db.TonKho.SingleAsync(t => t.MaLo == "L1")).SoLuong);
        await Voi("ND002", Vai.QuanLyKho, s => s.HuyAsync(ma));
        await using var kt = CsdlFixture.TaoDbContext(_chuoi);
        Assert.False(await kt.ViTri.AnyAsync(v => v.MaPhieuKiemKe != null));
    }

    [Fact]
    public async Task TheoLo_OGiuHangPhaiXacNhan_KhongLechThiHoanThanhMoKhoa()
    {
        var ma = await Lap(PhamViKiemKe.Lo, "L3");
        Assert.Contains("giữ hàng", (await Assert.ThrowsAsync<NghiepVuException>(() => Voi("ND002", Vai.QuanLyKho, s => s.BatDauAsync(ma, false)))).Message);
        await Voi("ND002", Vai.QuanLyKho, s => s.BatDauAsync(ma, true));
        var p = await Voi("ND004", Vai.ThuKho, s => s.LayAsync(ma));
        Assert.Equal([Lanh], p.CacO);
        Assert.Equal([("L3", 5m)], p.Dong.Select(d => (d.MaLo!, d.SoSach)));

        await Dem(ma, (1, 5));
        await Voi("ND002", Vai.QuanLyKho, s => s.HoanThanhAsync(ma));
        await using var db = CsdlFixture.TaoDbContext(_chuoi);
        Assert.Equal(TrangThaiKiemKe.HoanThanh, (await db.PhieuKiemKe.SingleAsync(x => x.MaPhieu == ma)).TrangThai);
        Assert.Null((await db.ViTri.SingleAsync(v => v.MaViTri == Lanh)).MaPhieuKiemKe);
        Assert.True(await db.NhatKy.AnyAsync(n => n.LoaiThaoTac == "HoanThanhKiemKe" && n.MaDoiTuong == ma));
    }

    [Fact]
    public async Task ToanKho_KhoaMoiOCuaKho_PhanQuyen()
    {
        await Assert.ThrowsAsync<KhongCoQuyenException>(() => Voi("ND003", Vai.QuanLyKho, s => s.TaoAsync(new KiemKeForm { MaKho = "K01", ThanhVien = ["ND004"] })));
        await Assert.ThrowsAsync<NghiepVuException>(() => Voi("ND002", Vai.QuanLyKho, s => s.TaoAsync(new KiemKeForm { MaKho = "K01", ThanhVien = ["ND006"] })));
        var ma = await Lap(PhamViKiemKe.ToanKho);
        await Voi("ND002", Vai.QuanLyKho, s => s.BatDauAsync(ma, true));
        await using var db = CsdlFixture.TaoDbContext(_chuoi);
        Assert.Equal(19, await db.ViTri.CountAsync(v => v.MaPhieuKiemKe == ma));
        Assert.False(await db.ViTri.AnyAsync(v => v.MaKho == "K02" && v.MaPhieuKiemKe != null));
        Assert.Equal(5, await db.ChiTietKiemKe.CountAsync(c => c.MaPhieu == ma));
    }

    [Fact]
    public async Task ManHinh_LapPhieu_DemTrenMayTinhBang()
    {
        var quanLy = _app.TaoClient(Vai.QuanLyKho, "ND002");
        Assert.Contains($"value=\"{Ke}\"", await quanLy.GetStringAsync("/KiemKe/Them"));
        var r = await NguoiThu.PostFormAsync(quanLy, "/KiemKe/Them", "/KiemKe/Them", new()
        {
            ["MaKho"] = "K01", ["PhamVi"] = "ViTri", ["DoiTuong"] = Ke, ["ThanhVien"] = "ND004",
        });
        Assert.Equal(HttpStatusCode.Redirect, r.StatusCode);
        var ma = r.Headers.Location!.ToString().Split('/').Last();
        Assert.Equal(HttpStatusCode.Redirect, (await NguoiThu.PostFormAsync(quanLy, $"/KiemKe/{ma}/BatDau", $"/KiemKe/{ma}")).StatusCode);

        var thuKho = _app.TaoClient(Vai.ThuKho, "ND004");
        Assert.Contains("man-hinh-quet", await thuKho.GetStringAsync($"/KiemKe/{ma}"));
        var dem = await NguoiThu.PostFormAsync(thuKho, $"/KiemKe/{ma}/Dem", $"/KiemKe/{ma}", new() { ["dem[0].STT"] = "1", ["dem[0].SoLuongThucTe"] = "9.5" });
        Assert.Equal(HttpStatusCode.Redirect, dem.StatusCode);
        Assert.Equal(-0.5m, (await Voi("ND004", Vai.ThuKho, s => s.LayAsync(ma))).Dong[0].ChenhLech);
        Assert.Equal(HttpStatusCode.Forbidden, (await thuKho.GetAsync("/KiemKe/Them")).StatusCode);
    }
}
