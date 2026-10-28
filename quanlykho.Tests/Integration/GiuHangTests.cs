using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using quanlykho.Common;
using quanlykho.Common.Loi;
using quanlykho.Common.PhanQuyen;
using quanlykho.Data;
using quanlykho.Domain;
using quanlykho.Features.XuatKho;
using quanlykho.Tests.HaTang;

namespace quanlykho.Tests.Integration;

[Collection(CsdlCollection.Ten)]
public class GiuHangTests(CsdlFixture csdl) : IAsyncLifetime
{
    private const string Thuong = "K01-TA01-TG01-P02-H01-KE01-O";
    private const string Lanh = "K01-TA01-TG01-P03-H01-KE01-O";
    private static readonly FakeTimeProvider Gio = new(new DateTimeOffset(2026, 11, 1, 3, 0, 0, TimeSpan.Zero));
    private static readonly DateOnly HomNay = new(2026, 11, 1);
    private string _chuoi = "";
    private UngDungFactory _app = null!;

    /// <summary>Áo: L1 10 (nhập trước), L2 8. Sữa: LX hết hạn 20, L3 còn hạn 5, LC 50 ở khu cách ly.</summary>
    public async Task InitializeAsync()
    {
        _chuoi = await csdl.TaoCsdlCoDuLieuThuAsync();
        _app = new UngDungFactory { ChuoiKetNoi = _chuoi, Gio = Gio };
        await using var db = CsdlFixture.TaoDbContext(_chuoi);
        var lo = new (string Ma, string BienThe, DateOnly? Han, int NhapSau, string O, decimal SoLuong)[]
        {
            ("L1", "AOTHUN-01", null, 0, Thuong + "01", 10),
            ("L2", "AOTHUN-01", null, 1, Thuong + "02", 8),
            ("LX", "SUATUOI-01", HomNay.AddDays(-1), 0, Lanh + "01", 20),
            ("L3", "SUATUOI-01", HomNay.AddDays(10), 0, Lanh + "02", 5),
            ("LC", "SUATUOI-01", HomNay.AddDays(10), 0, "K01-TA01-TG01-P04-H01-KE01-O01", 50),
        };
        db.Lo.AddRange(lo.Select(l => new Lo { MaLo = l.Ma, MaBienThe = l.BienThe, HanDung = l.Han, NgayNhap = Gio.GetUtcNow().AddDays(l.NhapSau) }));
        await db.SaveChangesAsync();
        await using var tx = await db.Database.BeginTransactionAsync();
        await new CongGhiTon(db, NguoiThu.HienTai(db, "ND001", Vai.QuanTri), Gio).GhiAsync("nap", "Thu", "T1", lo.Select(l => new DongGhiTon(l.Ma, l.O, l.SoLuong)).ToList());
        await tx.CommitAsync();
    }

    public async Task DisposeAsync() => await _app.DisposeAsync();

    private static (DonDatService Don, GiuHangService Giu) DichVu(AppDbContext db, string ma, string vai)
    {
        var h = NguoiThu.HienTai(db, ma, vai);
        var nhatKy = NguoiThu.NhatKy(db, h);
        var ton = new TonKhaDung(db, h, Gio);
        var giu = new GiuHangService(db, h, new CongGhiTon(db, h, Gio), new GoiYService(db, h, ton, Gio), nhatKy, Gio);
        return (new DonDatService(db, h, new MaChungTu(db), nhatKy, ton, giu, Gio), giu);
    }

    private async Task<string> TaoDon(params (string BienThe, decimal SoLuong)[] dong)
    {
        await using var db = CsdlFixture.TaoDbContext(_chuoi);
        return await DichVu(db, "ND007", Vai.BanHang).Don.TaoAsync(new DonDatForm
        {
            MaKho = "K01", MaNoiNhan = "NN01",
            Dong = dong.Select(d => new DongDonDatForm { MaBienThe = d.BienThe, SoLuong = d.SoLuong }).ToList(),
        }, guiDuyet: true);
    }

    private async Task Giu(Func<GiuHangService, Task> lam, string ma = "ND002", string vai = Vai.QuanLyKho)
    {
        await using var db = CsdlFixture.TaoDbContext(_chuoi);
        await lam(DichVu(db, ma, vai).Giu);
    }

    private async Task<DonDatChiTiet> Lay(string ma)
    {
        await using var db = CsdlFixture.TaoDbContext(_chuoi);
        return await DichVu(db, "ND002", Vai.QuanLyKho).Don.LayAsync(ma);
    }

    private async Task<Dictionary<string, decimal>> DaGiuTheoLo()
    {
        await using var db = CsdlFixture.TaoDbContext(_chuoi);
        return await db.TonKho.Where(t => t.SoLuongDaGiu > 0).ToDictionaryAsync(t => t.MaLo, t => t.SoLuongDaGiu);
    }

    [Fact]
    public async Task Duyet_GiuTheoG1_ChiLoDatChuaHetHan()
    {
        var ma = await TaoDon(("AOTHUN-01", 12), ("SUATUOI-01", 4));
        await Giu(s => s.DuyetAsync([ma]));

        var d = await Lay(ma);
        Assert.Equal(TrangThaiDonDat.DaDuyet, d.TrangThai);
        Assert.Equal([(1, "L1", Thuong + "01", 10m), (2, "L2", Thuong + "02", 2m), (3, "L3", Lanh + "02", 4m)],
            d.DaGiu.Select(g => (g.ThuTu, g.MaLo, g.MaViTri, g.SoLuong)));
        Assert.Equal(new Dictionary<string, decimal> { ["L1"] = 10, ["L2"] = 2, ["L3"] = 4 }, await DaGiuTheoLo());
        Assert.Equal(6, d.Dong.Single(x => x.MaBienThe == "AOTHUN-01").KhaDung);

        await using var db = CsdlFixture.TaoDbContext(_chuoi);
        Assert.Equal(2, await db.LichSuGoiY.CountAsync(x => x.MaChungTu == ma && x.ChapNhan == true));
        Assert.True(await db.NhatKy.AnyAsync(n => n.LoaiThaoTac == "DuyetDonDat" && n.MaDoiTuong == ma));
        await Assert.ThrowsAsync<NghiepVuException>(() => Giu(s => s.DuyetAsync([ma])));
    }

    [Fact]
    public async Task Thieu_Bao422CanConY_KhongGiuGi()
    {
        var thieu = await TaoDon(("SUATUOI-01", 6));
        var du = await TaoDon(("AOTHUN-01", 3));
        var loi = await Assert.ThrowsAsync<NghiepVuException>(() => Giu(s => s.DuyetAsync([thieu, du])));
        Assert.Equal($"Đơn {thieu}, SUATUOI-01: cần 6, còn 5", loi.Message);

        Assert.Empty(await DaGiuTheoLo());
        Assert.Equal(TrangThaiDonDat.ChoDuyet, (await Lay(du)).TrangThai);
        await using var db = CsdlFixture.TaoDbContext(_chuoi);
        Assert.False(await db.GiuHang.AnyAsync());
        Assert.False(await db.LichSuGoiY.AnyAsync());
    }

    [Fact]
    public async Task HaiLanDuyetSongSong_KhongGiuTrung()
    {
        var a = await TaoDon(("AOTHUN-01", 10));
        var b = await TaoDon(("AOTHUN-01", 10));
        var kq = await Task.WhenAll(new[] { a, b }.Select(async ma =>
        {
            try { await Giu(s => s.DuyetAsync([ma])); return "ok"; }
            catch (NghiepVuException e) { return e.Message; }
        }));
        Assert.Single(kq, "ok");
        Assert.Contains(kq, k => k.EndsWith("AOTHUN-01: cần 10, còn 8"));
        Assert.Equal(10, (await DaGiuTheoLo()).Values.Sum());
    }

    [Fact]
    public async Task BoGiu_VaHuyDonDaDuyet_TraLaiTonKhaDung()
    {
        var ma = await TaoDon(("AOTHUN-01", 12));
        await Giu(s => s.DuyetAsync([ma]));
        await Giu(s => s.BoGiuAsync(ma));
        Assert.Equal(TrangThaiDonDat.ChoDuyet, (await Lay(ma)).TrangThai);
        Assert.Empty(await DaGiuTheoLo());
        Assert.Empty((await Lay(ma)).DaGiu);

        await Giu(s => s.DuyetAsync([ma]));
        await Assert.ThrowsAsync<KhongCoQuyenException>(() => Giu(s => s.HuyDonDaDuyetAsync(ma), "ND007", Vai.BanHang));
        await Assert.ThrowsAsync<KhongCoQuyenException>(() => Giu(s => s.BoGiuAsync(ma), "ND003", Vai.QuanLyKho));
        await using (var db = CsdlFixture.TaoDbContext(_chuoi))
            await DichVu(db, "ND002", Vai.QuanLyKho).Don.HuyAsync(ma);
        Assert.Equal(TrangThaiDonDat.DaHuy, (await Lay(ma)).TrangThai);
        Assert.Empty(await DaGiuTheoLo());
    }

    [Fact]
    public async Task ManHinh_VaApi()
    {
        var a = await TaoDon(("AOTHUN-01", 2));
        var quanLy = _app.TaoClient(Vai.QuanLyKho, "ND002");
        Assert.Contains("value=\"" + a + "\" form=\"formDuyet\"", await quanLy.GetStringAsync("/DonDat"));
        var r = await NguoiThu.PostFormAsync(quanLy, "/DonDat/Duyet", "/DonDat", new() { ["ma"] = a });
        Assert.Equal(HttpStatusCode.Redirect, r.StatusCode);
        Assert.Contains("Hàng đã giữ", await quanLy.GetStringAsync($"/DonDat/{a}"));

        var b = await TaoDon(("SUATUOI-01", 100));
        var token = await NguoiThu.TokenAsync(quanLy, "/DonDat");
        HttpRequestMessage Duyet(string ma)
        {
            var yc = new HttpRequestMessage(HttpMethod.Post, "/api/don-dat/duyet") { Content = JsonContent.Create(new { ma = new[] { ma } }) };
            yc.Headers.Add("RequestVerificationToken", token);
            return yc;
        }
        var thieu = await quanLy.SendAsync(Duyet(b));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, thieu.StatusCode);
        Assert.Contains("\"con\":5", await thieu.Content.ReadAsStringAsync());

        var banHang = _app.TaoClient(Vai.BanHang, "ND007");
        var yc = new HttpRequestMessage(HttpMethod.Post, "/api/don-dat/duyet") { Content = JsonContent.Create(new { ma = new[] { b } }) };
        yc.Headers.Add("RequestVerificationToken", await NguoiThu.TokenAsync(banHang, "/DonDat"));
        Assert.Equal(HttpStatusCode.Forbidden, (await banHang.SendAsync(yc)).StatusCode);
    }
}
