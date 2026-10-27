using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using quanlykho.Common;
using quanlykho.Common.Loi;
using quanlykho.Common.PhanQuyen;
using quanlykho.Data;
using quanlykho.Data.Seed;
using quanlykho.Domain;
using quanlykho.Tests.HaTang;

namespace quanlykho.Tests.Integration;

[Collection(CsdlCollection.Ten)]
public class CongGhiTonTests(CsdlFixture csdl) : IAsyncLifetime
{
    private const string O1 = "K01-TA01-TG01-P02-H01-KE01-O01";
    private const string O2 = "K01-TA01-TG01-P02-H01-KE01-O02";
    private string _chuoi = "";

    public async Task InitializeAsync()
    {
        _chuoi = await csdl.TaoCsdlAsync();
        await using var db = CsdlFixture.TaoDbContext(_chuoi);
        await new DuLieuThu(db, new PasswordHasher<NguoiDung>(), new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["DuLieuThu:MatKhau"] = "MatKhauThu@1" }).Build()).NapAsync();
        db.Lo.AddRange(
            new Lo { MaLo = "L1", MaBienThe = "AOTHUN-01", NgayNhap = DateTimeOffset.UtcNow },
            new Lo { MaLo = "L2", MaBienThe = "AOTHUN-02", NgayNhap = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private static CongGhiTon Cong(AppDbContext db)
    {
        var http = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "ND004"), new Claim(ClaimTypes.Role, Vai.ThuKho)], "thu"))
            }
        };
        return new CongGhiTon(db, new NguoiDungHienTai(http, db), TimeProvider.System);
    }

    private async Task<T> TrongTransaction<T>(Func<CongGhiTon, Task<T>> lam)
    {
        await using var db = CsdlFixture.TaoDbContext(_chuoi);
        await using var tx = await db.Database.BeginTransactionAsync();
        var ketQua = await lam(Cong(db));
        await tx.CommitAsync();
        return ketQua;
    }

    private static DongGhiTon D(string lo, string viTri, decimal soLuong) => new(lo, viTri, soLuong);

    private Task<KetQuaGhi> Ghi(string khoa, params DongGhiTon[] dong) =>
        TrongTransaction(c => c.GhiAsync(khoa, "Thu", "THU001", dong));

    private Task Giu(params DongGiu[] dong) => TrongTransaction(async c => { await c.GiuAsync(dong); return 0; });

    private Task BoGiu(params DongGiu[] dong) => TrongTransaction(async c => { await c.BoGiuAsync(dong); return 0; });

    private async Task<(decimal SoLuong, decimal DaGiu, decimal TongBienDong)> Ton(string lo, string viTri)
    {
        await using var db = CsdlFixture.TaoDbContext(_chuoi);
        var t = await db.TonKho.SingleOrDefaultAsync(x => x.MaLo == lo && x.MaViTri == viTri);
        var tong = await db.SoBienDong.Where(s => s.MaLo == lo && s.MaViTri == viTri).SumAsync(s => s.SoLuongThayDoi);
        return (t?.SoLuong ?? 0, t?.SoLuongDaGiu ?? 0, tong);
    }

    [Fact]
    public async Task NhapRoiChuyen_TongBienDongBangTon()
    {
        Assert.Equal(KetQuaGhi.DaGhi, await Ghi("k1", D("L1", O1, 10)));
        Assert.Equal(KetQuaGhi.DaGhi, await Ghi("k2", D("L1", O1, -3), D("L1", O2, 3)));

        Assert.Equal((7m, 0m, 7m), await Ton("L1", O1));
        Assert.Equal((3m, 0m, 3m), await Ton("L1", O2));
        await using var db = CsdlFixture.TaoDbContext(_chuoi);
        var dong = await db.SoBienDong.Where(s => s.MaKhoaChongLap.StartsWith("k2#")).OrderBy(s => s.MaKhoaChongLap).ToListAsync();
        Assert.Equal(["k2#1", "k2#2"], dong.Select(s => s.MaKhoaChongLap));
        Assert.All(dong, s => Assert.Equal("ND004", s.NguoiThucHien));
    }

    [Fact]
    public async Task CungKhoa_GoiHaiLanTuanTu_ChiGhiMotLan()
    {
        await Ghi("lap", D("L1", O1, 10));
        Assert.Equal(KetQuaGhi.DaGhiTruocDo, await Ghi("lap", D("L1", O1, 10)));

        Assert.Equal((10m, 0m, 10m), await Ton("L1", O1));
    }

    [Fact]
    public async Task CungKhoa_GoiSongSong_ChiGhiMotLan()
    {
        var ketQua = await Task.WhenAll(Ghi("song-song", D("L1", O1, 5)), Ghi("song-song", D("L1", O1, 5)));

        Assert.Equal([KetQuaGhi.DaGhi, KetQuaGhi.DaGhiTruocDo], ketQua.Order());
        Assert.Equal((5m, 0m, 5m), await Ton("L1", O1));
    }

    [Fact]
    public async Task XuatVuotTon_BiTuChoi_KhongDoiGi()
    {
        await Ghi("nhap", D("L1", O1, 5));

        var loi = await Assert.ThrowsAsync<NghiepVuException>(() => Ghi("xuat", D("L1", O1, -6)));

        Assert.Contains("cần 6, còn 5", loi.Message);
        Assert.Equal((5m, 0m, 5m), await Ton("L1", O1));
    }

    [Fact]
    public async Task HaiNguoiTruSongSong_VuotTong_MotNguoiBiChan_TonKhongAm()
    {
        await Ghi("nhap", D("L1", O1, 10));

        var ketQua = await Task.WhenAll(
            Task.Run(async () => { try { await Ghi("a", D("L1", O1, -7)); return true; } catch (NghiepVuException) { return false; } }),
            Task.Run(async () => { try { await Ghi("b", D("L1", O1, -7)); return true; } catch (NghiepVuException) { return false; } }));

        Assert.Single(ketQua, true);
        Assert.Equal((3m, 0m, 3m), await Ton("L1", O1));
    }

    [Fact]
    public async Task GiuHang_KhongVuotTon_VaHangDaGiuKhongXuatDuoc()
    {
        await Ghi("nhap", D("L1", O1, 10));
        await Giu(new DongGiu("L1", O1, 8));

        var giuThem = await Assert.ThrowsAsync<NghiepVuException>(() => Giu(new DongGiu("L1", O1, 3)));
        Assert.Contains("còn 2", giuThem.Message);
        await Assert.ThrowsAsync<NghiepVuException>(() => Ghi("xuat", D("L1", O1, -3)));

        await BoGiu(new DongGiu("L1", O1, 8));
        await Ghi("xuat", D("L1", O1, -3));
        Assert.Equal((7m, 0m, 7m), await Ton("L1", O1));
    }

    [Fact]
    public async Task GiuSongSong_KhongGiuTrung()
    {
        await Ghi("nhap", D("L1", O1, 10));

        var ketQua = await Task.WhenAll(
            Task.Run(async () => { try { await Giu(new DongGiu("L1", O1, 6)); return true; } catch (NghiepVuException) { return false; } }),
            Task.Run(async () => { try { await Giu(new DongGiu("L1", O1, 6)); return true; } catch (NghiepVuException) { return false; } }));

        Assert.Single(ketQua, true);
        Assert.Equal(6m, (await Ton("L1", O1)).DaGiu);
    }

    [Fact]
    public async Task OKhoaKiemKe_ChiPhieuKiemKeDoGhiDuoc()
    {
        await Ghi("nhap", D("L1", O1, 4));
        await using (var db = CsdlFixture.TaoDbContext(_chuoi))
            await db.ViTri.Where(v => v.MaViTri == O1).ExecuteUpdateAsync(s => s.SetProperty(v => v.MaPhieuKiemKe, "KK000001"));

        var loi = await Assert.ThrowsAsync<NghiepVuException>(() => Ghi("xuat", D("L1", O1, -1)));
        Assert.Contains("đang khóa để kiểm kê", loi.Message);
        await Assert.ThrowsAsync<NghiepVuException>(() => Giu(new DongGiu("L1", O1, 1)));

        await TrongTransaction(c => c.GhiAsync("dieu-chinh", "KiemKe", "KK000001", [new("L1", O1, -1)], maPhieuKiemKe: "KK000001"));
        Assert.Equal(3m, (await Ton("L1", O1)).SoLuong);
    }

    [Fact]
    public async Task ViTriKhongPhaiO_BiTuChoi()
    {
        var loi = await Assert.ThrowsAsync<NghiepVuException>(() => Ghi("nhap", D("L1", "K01", 1)));
        Assert.Contains("không phải ô", loi.Message);
    }

    [Fact]
    public async Task KhongCoTransaction_BaoLoi()
    {
        await using var db = CsdlFixture.TaoDbContext(_chuoi);
        await Assert.ThrowsAsync<InvalidOperationException>(() => Cong(db).GhiAsync("k", "Thu", "THU001", [new("L1", O1, 1)]));
    }
}
