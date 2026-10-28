using System.Net;
using Microsoft.Extensions.Time.Testing;
using quanlykho.Common;
using quanlykho.Common.PhanQuyen;
using quanlykho.Domain;
using quanlykho.Features.TongQuan;
using quanlykho.Features.XuatKho;
using quanlykho.Tests.HaTang;

namespace quanlykho.Tests.Integration;

[Collection(CsdlCollection.Ten)]
public class TongQuanTests(CsdlFixture csdl) : IAsyncLifetime
{
    // 10:00 ngày 30/11/2026 giờ Việt Nam.
    private static readonly FakeTimeProvider Gio = new(new DateTimeOffset(2026, 11, 30, 3, 0, 0, TimeSpan.Zero));
    private const string Lanh = "K01-TA01-TG01-P03-H01-KE01-O0";
    private const string Thuong = "K01-TA01-TG01-P02-H01-KE01-O0";
    private const string TiepNhan = "K01-TA01-TG01-P01-H01-KE01-O01";
    private string _chuoi = "";
    private UngDungFactory _app = null!;

    /// <summary>
    /// Mức tồn dữ liệu thử K01: sữa 50–500, áo M 20–200, điện thoại 5–50, giấy 10–100.
    /// Sữa: LS1 20 hạn 10/12 (còn 10 ngày), LS2 5 hạn 29/11 (quá hạn), LS3 10 hạn 01/03/2027 ở khu tiếp nhận, LS4 2 hạn hôm nay.
    /// Áo M (không theo dõi hạn) 250, điện thoại 6, giấy 0.
    /// </summary>
    public async Task InitializeAsync()
    {
        _chuoi = await csdl.TaoCsdlCoDuLieuThuAsync();
        _app = new UngDungFactory { ChuoiKetNoi = _chuoi, Gio = Gio };
        await using var db = CsdlFixture.TaoDbContext(_chuoi);
        var lo = new (string Ma, string BienThe, DateOnly? Han, string O, decimal SoLuong)[]
        {
            ("LS1", "SUATUOI-01", new(2026, 12, 10), Lanh + "1", 20), ("LS2", "SUATUOI-01", new(2026, 11, 29), Lanh + "2", 5),
            ("LS3", "SUATUOI-01", new(2027, 3, 1), TiepNhan, 10), ("LS4", "SUATUOI-01", new(2026, 11, 30), Lanh + "3", 2),
            ("LA", "AOTHUN-03", new(2026, 12, 1), Thuong + "1", 250), ("LD", "DTA-01", null, Thuong + "2", 6),
        };
        db.Lo.AddRange(lo.Select(l => new Lo { MaLo = l.Ma, MaBienThe = l.BienThe, HanDung = l.Han, NgayNhap = Gio.GetUtcNow().AddDays(-60) }));
        await db.SaveChangesAsync();
        await using var tx = await db.Database.BeginTransactionAsync();
        await new CongGhiTon(db, NguoiThu.HienTai(db, "ND001", Vai.QuanTri), Gio).GhiAsync("nap", "Thu", "T1", lo.Select(l => new DongGhiTon(l.Ma, l.O, l.SoLuong)).ToList());
        await tx.CommitAsync();
    }

    public async Task DisposeAsync() => await _app.DisposeAsync();

    private async Task<TheCanhBao> The(LoaiThe loai, int soNgay = 30, string ma = "ND002", string vai = Vai.QuanLyKho)
    {
        await using var db = CsdlFixture.TaoDbContext(_chuoi);
        var h = NguoiThu.HienTai(db, ma, vai);
        return await new TongQuanService(db, h, new TonKhaDung(db, h, Gio), Gio).TheAsync(loai, soNgay);
    }

    [Fact]
    public async Task MucTon_SoVoiTonKhaDung()
    {
        // Sữa khả dụng = LS1 20 + LS4 2 (LS2 quá hạn, LS3 ở khu tiếp nhận không tính).
        var duoi = await The(LoaiThe.DuoiToiThieu);
        Assert.Equal([("K01", "GIAYA4-01", 0m, 10m), ("K01", "SUATUOI-01", 22m, 50m)], duoi.Dong.Select(d => (d.MaKho, d.MaBienThe, d.SoLuong, d.Muc!.Value)));
        Assert.Equal([("K01", "AOTHUN-03", 250m, 200m)], (await The(LoaiThe.VuotToiDa)).Dong.Select(d => (d.MaKho, d.MaBienThe, d.SoLuong, d.Muc!.Value)));

        // Quản trị viên thấy cả K02.
        Assert.Equal(["K01", "K01", "K02", "K02"], (await The(LoaiThe.DuoiToiThieu, ma: "ND001", vai: Vai.QuanTri)).Dong.Select(d => d.MaKho));
        Assert.DoesNotContain((await The(LoaiThe.DuoiToiThieu, ma: "ND003")).Dong, d => d.MaKho == "K01");
    }

    [Fact]
    public async Task HanDung_SapHetHanVaQuaHan()
    {
        var sap = await The(LoaiThe.SapHetHan);
        Assert.Equal([("LS4", 2m), ("LS1", 20m)], sap.Dong.Select(d => (d.MaLo!, d.SoLuong)));
        Assert.Equal(["LS4"], (await The(LoaiThe.SapHetHan, soNgay: 9)).Dong.Select(d => d.MaLo));
        Assert.Equal([("LS2", new DateOnly(2026, 11, 29), 5m)], (await The(LoaiThe.QuaHan)).Dong.Select(d => (d.MaLo!, d.HanDung!.Value, d.SoLuong)));
        Assert.Empty((await The(LoaiThe.QuaHan, ma: "ND003")).Dong);
    }

    [Fact]
    public async Task ManHinh_ThamSoLuuCookie_PhanQuyen()
    {
        var ql = _app.TaoClient(Vai.QuanLyKho, "ND002");
        Assert.Contains("trong 7 ngày", await ql.GetStringAsync("/?soNgay=7"));
        var trang = await ql.GetStringAsync("/");
        Assert.Contains("trong 7 ngày", trang);
        Assert.Contains("data-tai=\"/TongQuan/The/QuaHan?soNgay=7\"", trang);
        var the = await ql.GetStringAsync("/TongQuan/The/QuaHan?soNgay=7");
        Assert.Contains("LS2", the);
        Assert.Contains("Số liệu tính đến", the);

        var thuKho = _app.TaoClient(Vai.ThuKho, "ND004");
        Assert.DoesNotContain("data-tai=", await thuKho.GetStringAsync("/"));
        Assert.Equal(HttpStatusCode.Forbidden, (await thuKho.GetAsync("/TongQuan/The/QuaHan")).StatusCode);
    }
}
