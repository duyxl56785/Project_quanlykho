using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using quanlykho.Common;
using quanlykho.Common.PhanQuyen;
using quanlykho.Data;
using quanlykho.Domain;
using quanlykho.Features.BaoCao;
using quanlykho.Features.DieuChuyen;
using quanlykho.Features.TongQuan;
using quanlykho.Features.XuatKho;
using quanlykho.Tests.HaTang;

namespace quanlykho.Tests.Integration;

[Collection(CsdlCollection.Ten)]
public class BaoCaoTests(CsdlFixture csdl) : IAsyncLifetime
{
    private const string TiepNhan = "K01-TA01-TG01-P01-H01-KE01-O01";
    private const string Lanh = "K01-TA01-TG01-P03-H01-KE01-O01";
    private const string Ke = "K01-TA01-TG01-P02-H01-KE01-O01";
    private readonly FakeTimeProvider _gio = new(new DateTimeOffset(2026, 10, 15, 3, 0, 0, TimeSpan.Zero));
    private string _chuoi = "";

    /// <summary>
    /// K01: 15/10 nhập sữa LS1 50 (khu tiếp nhận), áo LA 30 (ô thường); 05/11 xếp 40 sữa vào ô lạnh; 10/11 xuất 5 sữa; giữ 3 sữa;
    /// 20/11 chuyển 10 áo sang K02 chưa nhận. Hôm nay 30/11: sữa 10 tiếp nhận + 35 ô lạnh (giữ 3), áo 20, đang vận chuyển 10 áo.
    /// </summary>
    public async Task InitializeAsync()
    {
        _chuoi = await csdl.TaoCsdlCoDuLieuThuAsync();
        await using (var db = CsdlFixture.TaoDbContext(_chuoi))
        {
            db.Lo.AddRange(new Lo { MaLo = "LS1", MaBienThe = "SUATUOI-01", HanDung = new DateOnly(2027, 1, 1), NgayNhap = _gio.GetUtcNow() },
                new Lo { MaLo = "LA", MaBienThe = "AOTHUN-01", NgayNhap = _gio.GetUtcNow() });
            await db.SaveChangesAsync();
        }
        await Ghi("PhieuNhap", "PNT1", new DongGhiTon("LS1", TiepNhan, 50), new DongGhiTon("LA", Ke, 30));
        _gio.SetUtcNow(new DateTimeOffset(2026, 11, 5, 3, 0, 0, TimeSpan.Zero));
        await Ghi("XepHang", "CVT1", new DongGhiTon("LS1", TiepNhan, -40), new DongGhiTon("LS1", Lanh, 40));
        _gio.SetUtcNow(new DateTimeOffset(2026, 11, 10, 3, 0, 0, TimeSpan.Zero));
        await Ghi("PhieuXuat", "PXT1", new DongGhiTon("LS1", Lanh, -5));
        await using (var db = CsdlFixture.TaoDbContext(_chuoi))
        {
            await using var tx = await db.Database.BeginTransactionAsync();
            await new CongGhiTon(db, NguoiThu.HienTai(db, "ND002", Vai.QuanLyKho), _gio).GiuAsync([new DongGiu("LS1", Lanh, 3)]);
            await tx.CommitAsync();
        }
        _gio.SetUtcNow(new DateTimeOffset(2026, 11, 20, 3, 0, 0, TimeSpan.Zero));
        await using (var db = CsdlFixture.TaoDbContext(_chuoi))
        {
            var h = NguoiThu.HienTai(db, "ND002", Vai.QuanLyKho);
            await new DieuChuyenService(db, h, new MaChungTu(db), new CongGhiTon(db, h, _gio), new TonKhaDung(db, h, _gio), NguoiThu.NhatKy(db, h), _gio)
                .TaoAsync(new DieuChuyenForm { MaKhoDi = "K01", MaKhoDen = "K02", KhoaChongLap = "dc", Dong = [new DongDieuChuyenForm { LoO = $"LA|{Ke}", SoLuong = 10 }] }, true);
        }
        _gio.SetUtcNow(new DateTimeOffset(2026, 11, 30, 3, 0, 0, TimeSpan.Zero));
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task Ghi(string loai, string ma, params DongGhiTon[] dong)
    {
        await using var db = CsdlFixture.TaoDbContext(_chuoi);
        await using var tx = await db.Database.BeginTransactionAsync();
        await new CongGhiTon(db, NguoiThu.HienTai(db, "ND002", Vai.QuanLyKho), _gio).GhiAsync(ma, loai, ma, [.. dong]);
        await tx.CommitAsync();
    }

    private async Task<T> Voi<T>(Func<BaoCaoService, Task<T>> lam, string ma = "ND002", string vai = Vai.QuanLyKho)
    {
        await using var db = CsdlFixture.TaoDbContext(_chuoi);
        var h = NguoiThu.HienTai(db, ma, vai);
        return await lam(new BaoCaoService(db, h, new TonKhaDung(db, h, _gio)));
    }

    [Fact]
    public async Task TongHop_TungKhoCanhTong_TachKhuVaVanChuyen_KhopSoBienDong()
    {
        var k01 = await Voi(s => s.TongHopAsync(new LocBaoCao(), 1));
        Assert.Equal(["K01"], k01.Kho);
        var sua = k01.Trang.Muc.Single(d => d.MaBienThe == "SUATUOI-01");
        Assert.Equal((45m, 32m, 10m, 0m, 0m), (sua.Tong, sua.KhaDung, sua.TiepNhan, sua.CachLy, sua.VanChuyen));
        var ao = k01.Trang.Muc.Single(d => d.MaBienThe == "AOTHUN-01");
        Assert.Equal((20m, 20m, 10m), (ao.Tong, ao.KhaDung, ao.VanChuyen));

        var tatCa = await Voi(s => s.TongHopAsync(new LocBaoCao(), 1), "ND001", Vai.QuanTri);
        Assert.Equal(["K01", "K02"], tatCa.Kho);
        // Tổng hệ thống = tổng các kho + đang vận chuyển = 80 nhập − 5 xuất.
        Assert.Equal(75, tatCa.TongCong.Tong + tatCa.TongCong.VanChuyen);

        // Quy tắc 6: tồn từng kho trong báo cáo bằng tổng sổ biến động của kho đó.
        await using var db = CsdlFixture.TaoDbContext(_chuoi);
        var soBienDong = await db.SoBienDong.Join(db.ViTri, s => s.MaViTri, v => v.MaViTri, (s, v) => new { v.MaKho, s.SoLuongThayDoi })
            .GroupBy(x => x.MaKho).ToDictionaryAsync(g => g.Key, g => g.Sum(x => x.SoLuongThayDoi));
        Assert.All(tatCa.Kho, k => Assert.Equal(soBienDong.GetValueOrDefault(k), tatCa.TongCong.TheoKho[k]));

        // Tổng quan và báo cáo dùng chung tồn khả dụng nên khớp nhau.
        await using (var dbTq = CsdlFixture.TaoDbContext(_chuoi))
        {
            var h = NguoiThu.HienTai(dbTq, "ND002", Vai.QuanLyKho);
            var duoi = await new TongQuanService(dbTq, h, new TonKhaDung(dbTq, h, _gio), _gio).TheAsync(LoaiThe.DuoiToiThieu, 30);
            Assert.Equal(sua.KhaDung, duoi.Dong.Single(d => d.MaBienThe == "SUATUOI-01").SoLuong);
        }

        var loc = await Voi(s => s.TongHopAsync(new LocBaoCao { TuKhoa = "sữa" }, 1));
        Assert.Equal(["SUATUOI-01"], loc.Trang.Muc.Select(d => d.MaBienThe));
    }

    [Fact]
    public async Task TheoLoVaTheoViTri()
    {
        var lo = Assert.Single((await Voi(s => s.TheoLoAsync(new LocBaoCao(), 1))).Trang.Muc);
        Assert.Equal(("LS1", 45m, 3m, 32m, 10m), (lo.MaLo, lo.SoLuong, lo.DaGiu, lo.KhaDung, lo.TiepNhan));

        var tiepNhan = Assert.Single((await Voi(s => s.TheoViTriAsync(new LocBaoCao { LoaiKhu = LoaiKhu.TiepNhan }, 1))).Trang.Muc);
        Assert.Equal((TiepNhan, 10m), (tiepNhan.MaViTri, tiepNhan.SoLuong));
        var lanh = Assert.Single((await Voi(s => s.TheoViTriAsync(new LocBaoCao { MaViTri = "K01-TA01-TG01-P03" }, 1))).Trang.Muc);
        Assert.Equal((Lanh, 35m, 3m), (lanh.MaViTri, lanh.SoLuong, lanh.DaGiu));
    }

    [Fact]
    public async Task NhapXuatTon_DauKyCongNhapTruXuatBangCuoiKy()
    {
        var thang11 = await Voi(s => s.NhapXuatTonAsync(new LocBaoCao { TuNgay = new(2026, 11, 1), DenNgay = new(2026, 11, 30) }, 1));
        Assert.Equal([("K01", "AOTHUN-01", 30m, 0m, 10m, 20m), ("K01", "SUATUOI-01", 50m, 0m, 5m, 45m)],
            thang11.Trang.Muc.Select(d => (d.MaKho, d.MaBienThe, d.DauKy, d.Nhap, d.Xuat, d.CuoiKy)));

        var tuDau = await Voi(s => s.NhapXuatTonAsync(new LocBaoCao { TuNgay = new(2026, 10, 1), DenNgay = new(2026, 11, 30) }, 1));
        Assert.Equal((0m, 80m, 15m, 65m), (tuDau.TongCong.DauKy, tuDau.TongCong.Nhap, tuDau.TongCong.Xuat, tuDau.TongCong.CuoiKy));

        // Mặc định: đầu tháng đến hôm nay.
        var macDinh = await Voi(s => s.NhapXuatTonAsync(new LocBaoCao(), 1));
        Assert.Equal((new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 30)), (macDinh.Loc.TuNgay!.Value, macDinh.Loc.DenNgay!.Value));

        var sai = await Voi(s => s.NhapXuatTonAsync(new LocBaoCao { TuNgay = new(2026, 11, 30), DenNgay = new(2026, 11, 1) }, 1));
        Assert.Equal("Ngày bắt đầu không được sau ngày kết thúc.", sai.Loc.Loi);
    }

    [Fact]
    public async Task TheKho_SoDuLuyKe_KhopTon()
    {
        var the = await Voi(s => s.TheKhoAsync(new LocBaoCao { MaBienThe = "SUATUOI-01", MaKho = "K01", TuNgay = new(2026, 11, 1) }, 1));
        Assert.Equal((50m, 45m), (the.DauKy, the.CuoiKy));
        Assert.Equal([("CVT1", 0m, 40m, 10m), ("CVT1", 40m, 0m, 50m), ("PXT1", 0m, 5m, 45m)], the.Trang.Muc.Select(d => (d.MaChungTu, d.Nhap, d.Xuat, d.Ton)));

        await using var db = CsdlFixture.TaoDbContext(_chuoi);
        Assert.Equal(the.CuoiKy, await db.TonKho.Where(t => t.MaLo == "LS1").SumAsync(t => t.SoLuong));

        var oLanh = await Voi(s => s.TheKhoAsync(new LocBaoCao { MaBienThe = "SUATUOI-01", MaViTri = "K01-TA01-TG01-P03" }, 1));
        Assert.Equal((0m, 35m, 2), (oLanh.DauKy, oLanh.CuoiKy, oLanh.Trang.TongSo));

        Assert.Equal("Không có biến thể KHONGCO.", (await Voi(s => s.TheKhoAsync(new LocBaoCao { MaBienThe = "KHONGCO" }, 1))).Loc.Loi);
    }

    [Fact]
    public async Task ManHinh_PhanQuyen()
    {
        await using var app = new UngDungFactory { ChuoiKetNoi = _chuoi, Gio = _gio };
        var thuKho = app.TaoClient(Vai.ThuKho, "ND004");
        foreach (var trang in new[] { "", "/TongHop", "/TheoLo", "/TheoViTri", "/NhapXuatTon", "/TheKho?MaBienThe=SUATUOI-01" })
            Assert.Equal(HttpStatusCode.OK, (await thuKho.GetAsync("/BaoCao" + trang)).StatusCode);
        Assert.Contains("SUATUOI-01", await thuKho.GetStringAsync("/BaoCao/TongHop"));
        Assert.Contains("Ngày bắt đầu không được sau", await thuKho.GetStringAsync("/BaoCao/NhapXuatTon?TuNgay=2026-11-30&DenNgay=2026-11-01"));
        Assert.Contains("\"tongSo\":2", await thuKho.GetStringAsync("/api/bao-cao/tong-hop"));
        Assert.Equal(HttpStatusCode.Forbidden, (await app.TaoClient(Vai.BanHang, "ND007").GetAsync("/api/bao-cao/tong-hop")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await app.TaoClient(Vai.ThuKho, "ND006").GetAsync("/api/bao-cao/tong-hop?MaKho=K01")).StatusCode);
    }
}
