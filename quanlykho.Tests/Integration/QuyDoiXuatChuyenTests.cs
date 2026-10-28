using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using quanlykho.Common;
using quanlykho.Common.PhanQuyen;
using quanlykho.Common.GiaoDien;
using quanlykho.Data;
using quanlykho.Domain;
using quanlykho.Features.BaoCao;
using quanlykho.Features.DanhMuc;
using quanlykho.Features.DieuChuyen;
using quanlykho.Features.XuatKho;
using quanlykho.Tests.HaTang;

namespace quanlykho.Tests.Integration;

/// <summary>Quy đổi ở xuất kho, chuyển kho, báo cáo: lô sữa LS 10 thùng = 240 hộp ở ô lạnh K01, thùng có mã vạch THUNG-SUA.</summary>
[Collection(CsdlCollection.Ten)]
public class QuyDoiXuatChuyenTests(CsdlFixture csdl) : IAsyncLifetime
{
    private const string Sua = "SUATUOI-01";
    private const string OLanh = "K01-TA01-TG01-P03-H01-KE01-O01";
    private static readonly FakeTimeProvider Gio = new(new DateTimeOffset(2026, 11, 1, 3, 0, 0, TimeSpan.Zero));
    private string _chuoi = "";

    public async Task InitializeAsync()
    {
        _chuoi = await csdl.TaoCsdlCoDuLieuThuAsync();
        await using var db = CsdlFixture.TaoDbContext(_chuoi);
        db.DonViQuyDoi.Add(new DonViQuyDoi { MaBienThe = Sua, TenDonVi = "thùng", HeSo = 24, MaVach = "THUNG-SUA", TrangThai = TrangThaiSuDung.DangSuDung });
        db.Lo.Add(new Lo { MaLo = "LS", MaBienThe = Sua, HanDung = new DateOnly(2027, 1, 1), NgayNhap = Gio.GetUtcNow() });
        await db.SaveChangesAsync();
        await using var tx = await db.Database.BeginTransactionAsync();
        await new CongGhiTon(db, NguoiThu.HienTai(db, "ND001", Vai.QuanTri), Gio).GhiAsync("nap", "Thu", "T1", [new("LS", OLanh, 240)]);
        await tx.CommitAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<T> Voi<T>(Func<AppDbContext, NguoiDungHienTai, Task<T>> lam, string ma = "ND002", string vai = Vai.QuanLyKho)
    {
        await using var db = CsdlFixture.TaoDbContext(_chuoi);
        return await lam(db, NguoiThu.HienTai(db, ma, vai));
    }

    private static PhieuXuatService PhieuXuat(AppDbContext db, NguoiDungHienTai h) =>
        new(db, h, new MaChungTu(db), new CongGhiTon(db, h, Gio), NguoiThu.NhatKy(db, h), Gio);

    private static DieuChuyenService DieuChuyen(AppDbContext db, NguoiDungHienTai h) =>
        new(db, h, new MaChungTu(db), new CongGhiTon(db, h, Gio), new TonKhaDung(db, h, Gio), NguoiThu.NhatKy(db, h), Gio);

    private Task<decimal> TonLo() => Voi((db, _) => db.TonKho.Where(t => t.MaLo == "LS").SumAsync(t => t.SoLuong));

    private Task<(string, KetQuaGhi)> XuatKhac(string khoa, decimal soLuong, string? donVi) => Voi((db, h) => PhieuXuat(db, h).TaoKhacAsync(new PhieuXuatForm
    {
        MaKho = "K01", LoaiPhieu = LoaiPhieuXuat.XuatKhac, LyDo = "Thử quy đổi", KhoaChongLap = khoa,
        Dong = [new DongPhieuXuatForm { LoO = $"LS|{OLanh}", TenDonVi = donVi, SoLuong = soLuong }],
    }));

    [Fact]
    public async Task XuatNamHopTuLoMuoiThung_Con235_BaoCaoHienThungLe()
    {
        var (ma, _) = await XuatKhac("x1", 5, null);
        Assert.Equal(235, await TonLo());
        var (ma2, _) = await XuatKhac("x2", 1, "thùng");
        Assert.Equal(211, await TonLo());

        var px = await Voi((db, h) => PhieuXuat(db, h).LayAsync(ma2));
        var d = Assert.Single(px.Dong);
        Assert.Equal(("thùng", 24, 1m, 24m, "hộp"), (d.TenDonVi, d.HeSo, d.SoLuongGoc, d.SoLuong, d.DonViChinh));
        Assert.Equal(("hộp", 5m), (Assert.Single((await Voi((db, h) => PhieuXuat(db, h).LayAsync(ma))).Dong) is var a ? (a.TenDonVi, a.SoLuongGoc) : default));

        // Báo cáo theo đơn vị chính, hiện kèm thùng lẻ; tổng biến động bằng tồn.
        var tongHop = await Voi((db, h) => new BaoCaoService(db, h, new TonKhaDung(db, h, Gio)).TongHopAsync(new LocBaoCao { TuKhoa = Sua }, 1));
        var sua = Assert.Single(tongHop.Trang.Muc);
        Assert.Equal((211m, "8 thùng 19 hộp"), (sua.Tong, QuyDoiDonVi.HienThiNeuCo(sua.DonVi, sua.Tong)));
        Assert.Equal(211, await Voi((db, _) => db.SoBienDong.Where(s => s.MaLo == "LS").SumAsync(s => s.SoLuongThayDoi)));
        var theKho = await Voi((db, h) => new BaoCaoService(db, h, new TonKhaDung(db, h, Gio)).TheKhoAsync(new LocBaoCao { MaBienThe = Sua }, 1));
        Assert.Equal((211m, "8 thùng 19 hộp"), (theKho.CuoiKy, QuyDoiDonVi.HienThiNeuCo(theKho.DonVi, theKho.CuoiKy)));
    }

    [Fact]
    public async Task ChuyenHaiThung_Tru48Hop_TongHeThongKhongDoi()
    {
        var ma = await Voi((db, h) => DieuChuyen(db, h).TaoAsync(new DieuChuyenForm
        {
            MaKhoDi = "K01", MaKhoDen = "K02", KhoaChongLap = "dc", Dong = [new DongDieuChuyenForm { LoO = $"LS|{OLanh}", TenDonVi = "thùng", SoLuong = 2 }],
        }, true));
        Assert.Equal(192, await TonLo());

        var p = await Voi((db, h) => DieuChuyen(db, h).LayAsync(ma));
        var d = Assert.Single(p.Dong);
        Assert.Equal(("thùng", 2m, 24, 48m, "hộp"), (d.TenDonVi, d.SoLuongGoc, d.HeSo, d.SoLuong, d.DonViChinh));

        await Voi((db, h) => DieuChuyen(db, h).NhanAsync(ma, [new DongNhanForm { STT = 1, SoLuongNhan = 48 }], "nhan"), "ND006", Vai.ThuKho);
        Assert.Equal(240, await TonLo());
        Assert.Equal(48, await Voi((db, _) => db.TonKho.Where(t => t.MaLo == "LS" && t.MaViTri.StartsWith("K02")).SumAsync(t => t.SoLuong)));
        Assert.Equal(240, await Voi((db, _) => db.SoBienDong.Where(s => s.MaLo == "LS").SumAsync(s => s.SoLuongThayDoi)));
    }

    [Fact]
    public async Task SuaPhieuChuyen_GiuDonViDaChon()
    {
        var ma = await Voi((db, h) => DieuChuyen(db, h).TaoAsync(new DieuChuyenForm
        {
            MaKhoDi = "K01", MaKhoDen = "K02", KhoaChongLap = "dc2", Dong = [new DongDieuChuyenForm { LoO = $"LS|{OLanh}", TenDonVi = "thùng", SoLuong = 3 }],
        }, false));
        var f = await Voi((db, h) => DieuChuyen(db, h).LayFormAsync(ma));
        Assert.Equal(("thùng", 3m), (f.Dong[0].TenDonVi, f.Dong[0].SoLuong));
        Assert.Equal(["hộp", "thùng"], f.DonViHang[$"LS|{OLanh}"].Select(x => x.Ten));
    }

    [Fact]
    public async Task LayHangBangMaThung_TinhTheoHop()
    {
        var maDon = await Voi(async (db, h) =>
        {
            var ton = new TonKhaDung(db, h, Gio);
            var giu = new GiuHangService(db, h, new CongGhiTon(db, h, Gio), new GoiYService(db, h, ton, Gio), NguoiThu.NhatKy(db, h), Gio);
            return await new DonDatService(db, h, new MaChungTu(db), NguoiThu.NhatKy(db, h), ton, giu, Gio).TaoAsync(new DonDatForm
            {
                MaKho = "K01", MaNoiNhan = "NN01", Dong = [new DongDonDatForm { MaBienThe = Sua, TenDonVi = "thùng", SoLuong = 2 }],
            }, guiDuyet: true);
        }, "ND007", Vai.BanHang);
        LenhXuatService Lenh(AppDbContext db, NguoiDungHienTai h)
        {
            var ton = new TonKhaDung(db, h, Gio);
            return new LenhXuatService(db, h, new MaChungTu(db), new CongGhiTon(db, h, Gio), new GoiYService(db, h, ton, Gio), ton, NguoiThu.NhatKy(db, h), Gio);
        }
        await Voi(async (db, h) =>
        {
            var ton = new TonKhaDung(db, h, Gio);
            await new GiuHangService(db, h, new CongGhiTon(db, h, Gio), new GoiYService(db, h, ton, Gio), NguoiThu.NhatKy(db, h), Gio).DuyetAsync([maDon]);
            return 0;
        });
        var maLenh = await Voi((db, h) => Lenh(db, h).TaoAsync(new LapLenhYeuCau(maDon, "ND004")));

        await Voi((db, h) => Lenh(db, h).LayHangAsync(maLenh, new LayHangYeuCau(OLanh, "THUNG-SUA", 2)), "ND004", Vai.ThuKho);
        Assert.Equal(48, await Voi((db, _) => db.DongLayHang.Where(d => d.MaLenh == maLenh).SumAsync(d => d.SoLuong)));
        var ct = await Voi((db, h) => Lenh(db, h).LayAsync(maLenh), "ND004", Vai.ThuKho);
        Assert.Equal(new DonViChon("thùng", 24), ct.MaVachDonVi!["THUNG-SUA"]);

        await using var app = new UngDungFactory { ChuoiKetNoi = _chuoi, Gio = Gio };
        Assert.Equal(HttpStatusCode.OK, (await app.TaoClient(Vai.ThuKho, "ND004").GetAsync($"/LenhXuat/{maLenh}")).StatusCode);
    }
}
