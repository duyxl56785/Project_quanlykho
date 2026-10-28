using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using quanlykho.Common;
using quanlykho.Common.Loi;
using quanlykho.Common.PhanQuyen;
using quanlykho.Domain;
using quanlykho.Features.DieuChuyen;
using quanlykho.Features.XuatKho;
using quanlykho.Tests.HaTang;

namespace quanlykho.Tests.Integration;

[Collection(CsdlCollection.Ten)]
public class DieuChuyenTests(CsdlFixture csdl) : IAsyncLifetime
{
    private const string Thuong = "K01-TA01-TG01-P02-H01-KE01-O";
    private const string TiepNhanK02 = "K02-TA01-TG01-P01-H01-KE01-O01";
    private static readonly FakeTimeProvider Gio = new(new DateTimeOffset(2026, 11, 1, 3, 0, 0, TimeSpan.Zero));
    private static readonly DateOnly HomNay = new(2026, 11, 1);
    private string _chuoi = "";
    private UngDungFactory _app = null!;

    /// <summary>K01: áo L1 10 ở O01, L2 4 ở O02 (khả dụng); sữa LX hết hạn ở ô lạnh, LC ở khu cách ly (không khả dụng).</summary>
    public async Task InitializeAsync()
    {
        _chuoi = await csdl.TaoCsdlCoDuLieuThuAsync();
        _app = new UngDungFactory { ChuoiKetNoi = _chuoi, Gio = Gio };
        await using var db = CsdlFixture.TaoDbContext(_chuoi);
        var lo = new (string Ma, string BienThe, DateOnly? Han, string O, decimal SoLuong)[]
        {
            ("L1", "AOTHUN-01", null, Thuong + "01", 10),
            ("L2", "AOTHUN-01", null, Thuong + "02", 4),
            ("LX", "SUATUOI-01", HomNay.AddDays(-1), "K01-TA01-TG01-P03-H01-KE01-O01", 5),
            ("LC", "SUATUOI-01", HomNay.AddDays(9), "K01-TA01-TG01-P04-H01-KE01-O01", 5),
        };
        db.Lo.AddRange(lo.Select(l => new Lo { MaLo = l.Ma, MaBienThe = l.BienThe, HanDung = l.Han, NgayNhap = Gio.GetUtcNow() }));
        await db.SaveChangesAsync();
        await using var tx = await db.Database.BeginTransactionAsync();
        await new CongGhiTon(db, NguoiThu.HienTai(db, "ND001", Vai.QuanTri), Gio).GhiAsync("nap", "Thu", "T1", lo.Select(l => new DongGhiTon(l.Ma, l.O, l.SoLuong)).ToList());
        await tx.CommitAsync();
    }

    public async Task DisposeAsync() => await _app.DisposeAsync();

    private async Task<T> Voi<T>(string ma, string vai, Func<DieuChuyenService, Task<T>> lam)
    {
        await using var db = CsdlFixture.TaoDbContext(_chuoi);
        var h = NguoiThu.HienTai(db, ma, vai);
        return await lam(new DieuChuyenService(db, h, new MaChungTu(db), new CongGhiTon(db, h, Gio), new TonKhaDung(db, h, Gio), NguoiThu.NhatKy(db, h), Gio));
    }

    private Task Voi(string ma, string vai, Func<DieuChuyenService, Task> lam) => Voi(ma, vai, async s => { await lam(s); return 0; });

    private static DieuChuyenForm Phieu(string khoa, params (string Lo, string O, decimal SoLuong)[] dong) => new()
    {
        MaKhoDi = "K01", MaKhoDen = "K02", PhuongTien = "Xe tải", KhoaChongLap = khoa,
        Dong = dong.Select(d => new DongDieuChuyenForm { LoO = $"{d.Lo}|{d.O}", SoLuong = d.SoLuong }).ToList(),
    };

    private Task<string> Lap(bool luu, string khoa, params (string Lo, string O, decimal SoLuong)[] dong) =>
        Voi("ND002", Vai.QuanLyKho, s => s.TaoAsync(Phieu(khoa, dong), luu));

    private Task<KetQuaGhi> Nhan(string ma, string khoa, params (int STT, decimal SoLuong)[] nhan) =>
        Voi("ND006", Vai.ThuKho, s => s.NhanAsync(ma, nhan.Select(n => new DongNhanForm { STT = n.STT, SoLuongNhan = n.SoLuong }).ToList(), khoa));

    /// <summary>Tồn theo kho và hàng đang vận chuyển (phần chưa nhận của phiếu Chờ nhập kho, Nhận một phần).</summary>
    private async Task<(decimal K01, decimal K02, decimal VanChuyen)> SoLieu()
    {
        await using var db = CsdlFixture.TaoDbContext(_chuoi);
        var ton = await db.TonKho.Join(db.ViTri, t => t.MaViTri, v => v.MaViTri, (t, v) => new { v.MaKho, t.SoLuong })
            .GroupBy(x => x.MaKho).ToDictionaryAsync(g => g.Key, g => g.Sum(x => x.SoLuong));
        var vanChuyen = await db.ChiTietDieuChuyen
            .Join(db.PhieuDieuChuyen.Where(p => p.TrangThai == TrangThaiDieuChuyen.ChoNhapKho || p.TrangThai == TrangThaiDieuChuyen.NhanMotPhan),
                c => c.MaPhieu, p => p.MaPhieu, (c, p) => c.SoLuong - (c.SoLuongNhan ?? 0))
            .SumAsync();
        return (ton.GetValueOrDefault("K01"), ton.GetValueOrDefault("K02"), vanChuyen);
    }

    private static decimal Tong((decimal K01, decimal K02, decimal VanChuyen) s) => s.K01 + s.K02 + s.VanChuyen;

    [Fact]
    public async Task LuuTam_Luu_NhanDu_TongTonCongVanChuyenKhongDoi()
    {
        var dau = await SoLieu();
        var ma = await Lap(false, "k0", ("L1", Thuong + "01", 6));
        Assert.Equal("DC000001", ma);
        Assert.Equal(dau, await SoLieu());

        Assert.Equal(KetQuaGhi.DaGhi, await Voi("ND002", Vai.QuanLyKho, s => s.LuuAsync(ma, "luu-1")));
        Assert.Equal(KetQuaGhi.DaGhiTruocDo, await Voi("ND002", Vai.QuanLyKho, s => s.LuuAsync(ma, "luu-1")));
        var sauLuu = await SoLieu();
        Assert.Equal((dau.K01 - 6, dau.K02, 6m), sauLuu);
        Assert.Equal(TrangThaiDieuChuyen.ChoNhapKho, (await Voi("ND006", Vai.ThuKho, s => s.LayAsync(ma))).TrangThai);
        Assert.Equal(1, await Voi("ND006", Vai.ThuKho, s => s.DemChoNhanAsync()));

        Assert.Equal(KetQuaGhi.DaGhi, await Nhan(ma, "nhan-1", (1, 6)));
        Assert.Equal(KetQuaGhi.DaGhiTruocDo, await Nhan(ma, "nhan-1", (1, 6)));
        var cuoi = await SoLieu();
        Assert.Equal((dau.K01 - 6, dau.K02 + 6, 0m), cuoi);
        Assert.Equal(Tong(dau), Tong(cuoi));

        var p = await Voi("ND002", Vai.QuanLyKho, s => s.LayAsync(ma));
        Assert.Equal(TrangThaiDieuChuyen.HoanThanh, p.TrangThai);
        await using var db = CsdlFixture.TaoDbContext(_chuoi);
        Assert.Equal(6, (await db.TonKho.SingleAsync(t => t.MaLo == "L1" && t.MaViTri == TiepNhanK02)).SoLuong);
        Assert.Equal(0, await db.SoBienDong.Where(s => s.MaChungTu == ma).SumAsync(s => s.SoLuongThayDoi));
    }

    [Fact]
    public async Task NhanThieu_NhanMotPhan_SinhChenhLech()
    {
        var dau = await SoLieu();
        var ma = await Lap(true, "luu", ("L1", Thuong + "01", 3), ("L2", Thuong + "02", 4));
        Assert.Equal(KetQuaGhi.DaGhi, await Nhan(ma, "nhan", (1, 1), (2, 4)));

        var p = await Voi("ND002", Vai.QuanLyKho, s => s.LayAsync(ma));
        Assert.Equal(TrangThaiDieuChuyen.NhanMotPhan, p.TrangThai);
        Assert.Equal([2m, 0m], p.Dong.Select(d => d.ChenhLech!.Value));
        var sau = await SoLieu();
        Assert.Equal((dau.K01 - 7, dau.K02 + 5, 2m), sau);
        Assert.Equal(Tong(dau), Tong(sau));
        await using var db = CsdlFixture.TaoDbContext(_chuoi);
        Assert.True(await db.NhatKy.AnyAsync(n => n.LoaiThaoTac == "NhanDieuChuyen" && n.MaDoiTuong == ma));
        await Assert.ThrowsAsync<NghiepVuException>(() => Voi("ND002", Vai.QuanLyKho, s => s.HuyAsync(ma)));
    }

    [Fact]
    public async Task HuyKhiChuaNhan_CongTraVeDungO()
    {
        var dau = await SoLieu();
        var tam = await Lap(false, "tam", ("L1", Thuong + "01", 2));
        await Voi("ND002", Vai.QuanLyKho, s => s.HuyAsync(tam));

        var ma = await Lap(true, "luu", ("L2", Thuong + "02", 4));
        await Voi("ND002", Vai.QuanLyKho, s => s.HuyAsync(ma));
        Assert.Equal(dau, await SoLieu());
        Assert.Equal(TrangThaiDieuChuyen.DaHuy, (await Voi("ND002", Vai.QuanLyKho, s => s.LayAsync(ma))).TrangThai);
        await Assert.ThrowsAsync<NghiepVuException>(() => Nhan(ma, "nhan", (1, 4)));
        await using var db = CsdlFixture.TaoDbContext(_chuoi);
        Assert.Equal(4, (await db.TonKho.SingleAsync(t => t.MaLo == "L2" && t.MaViTri == Thuong + "02")).SoLuong);
    }

    [Fact]
    public async Task ChanTheoQuyTac_ChiChuyenHangKhaDung_PhanQuyenKho()
    {
        var cungKho = Phieu("a", ("L1", Thuong + "01", 1));
        cungKho.MaKhoDen = "K01";
        await Assert.ThrowsAsync<NghiepVuException>(() => Voi("ND002", Vai.QuanLyKho, s => s.TaoAsync(cungKho, false)));
        Assert.Contains("còn khả dụng 0", (await Assert.ThrowsAsync<NghiepVuException>(() => Lap(true, "b", ("LX", "K01-TA01-TG01-P03-H01-KE01-O01", 1)))).Message);
        Assert.Contains("còn khả dụng 0", (await Assert.ThrowsAsync<NghiepVuException>(() => Lap(true, "c", ("LC", "K01-TA01-TG01-P04-H01-KE01-O01", 1)))).Message);
        Assert.Contains("cần 11", (await Assert.ThrowsAsync<NghiepVuException>(() => Lap(true, "d", ("L1", Thuong + "01", 11)))).Message);
        await Assert.ThrowsAsync<KhongCoQuyenException>(() => Voi("ND003", Vai.QuanLyKho, s => s.TaoAsync(Phieu("e", ("L1", Thuong + "01", 1)), false)));

        var tam = await Lap(false, "f", ("L1", Thuong + "01", 1));
        await Assert.ThrowsAsync<KhongCoQuyenException>(() => Voi("ND004", Vai.ThuKho, s => s.LuuAsync(tam, "g")));
        var ma = await Lap(true, "h", ("L1", Thuong + "01", 2));
        await Assert.ThrowsAsync<KhongCoQuyenException>(() => Voi("ND004", Vai.ThuKho, s => s.NhanAsync(ma, [new() { STT = 1, SoLuongNhan = 2 }], "i")));
        await Assert.ThrowsAsync<NghiepVuException>(() => Nhan(ma, "j", (1, 3)));
        await Assert.ThrowsAsync<NghiepVuException>(() => Nhan(ma, "k"));
    }

    [Fact]
    public async Task ManHinh_Api_SoChoNhanTrenMenu()
    {
        var quanLy = _app.TaoClient(Vai.QuanLyKho, "ND002");
        Assert.Contains($"L1|{Thuong}01", await quanLy.GetStringAsync("/DieuChuyen/Them"));
        var r = await NguoiThu.PostFormAsync(quanLy, "/DieuChuyen/Them", "/DieuChuyen/Them", new()
        {
            ["MaKhoDi"] = "K01", ["MaKhoDen"] = "K02", ["KhoaChongLap"] = "form-1", ["Dong[0].LoO"] = $"L1|{Thuong}01", ["Dong[0].SoLuong"] = "5", ["luu"] = "true",
        });
        Assert.Equal(HttpStatusCode.Redirect, r.StatusCode);
        var ma = r.Headers.Location!.ToString().Split('/').Last();

        var thuKho = _app.TaoClient(Vai.ThuKho, "ND006");
        Assert.Equal(1, (await thuKho.GetFromJsonAsync<JsonElement>("/api/dieu-chuyen/dem-cho-nhan")).GetProperty("so").GetInt32());
        Assert.Contains("Xác nhận nhận hàng", await thuKho.GetStringAsync($"/DieuChuyen/{ma}"));
        Assert.Contains("data-dem=\"/api/dieu-chuyen/dem-cho-nhan\"", await thuKho.GetStringAsync("/DieuChuyen"));

        var token = await NguoiThu.TokenAsync(thuKho, $"/DieuChuyen/{ma}");
        var nhan = new HttpRequestMessage(HttpMethod.Post, $"/api/dieu-chuyen/{ma}/nhan") { Content = JsonContent.Create(new[] { new { stt = 1, soLuongNhan = 5 } }) };
        nhan.Headers.Add("RequestVerificationToken", token);
        nhan.Headers.Add("Idempotency-Key", "api-nhan");
        Assert.Equal(HttpStatusCode.OK, (await thuKho.SendAsync(nhan)).StatusCode);
        Assert.Equal("HoanThanh", (await thuKho.GetFromJsonAsync<JsonElement>($"/api/dieu-chuyen/{ma}")).GetProperty("trangThai").GetString());
        Assert.Equal(HttpStatusCode.Forbidden, (await thuKho.GetAsync("/DieuChuyen/Them")).StatusCode);
    }
}
