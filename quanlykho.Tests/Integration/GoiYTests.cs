using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using quanlykho.Common;
using quanlykho.Common.Loi;
using quanlykho.Common.PhanQuyen;
using quanlykho.Domain;
using quanlykho.Features.XuatKho;
using quanlykho.Tests.HaTang;

namespace quanlykho.Tests.Integration;

[Collection(CsdlCollection.Ten)]
public class GoiYTests(CsdlFixture csdl) : IAsyncLifetime
{
    private const string Lanh = "K01-TA01-TG01-P03-H01-KE01-O";
    private static readonly FakeTimeProvider Gio = new(new DateTimeOffset(2026, 11, 1, 3, 0, 0, TimeSpan.Zero));
    private static readonly DateOnly HomNay = new(2026, 11, 1);
    private string _chuoi = "";
    private UngDungFactory _app = null!;

    public async Task InitializeAsync()
    {
        _chuoi = await csdl.TaoCsdlCoDuLieuThuAsync();
        _app = new UngDungFactory { ChuoiKetNoi = _chuoi, Gio = Gio };
        await using var db = CsdlFixture.TaoDbContext(_chuoi);
        var lo = new (string Ma, DateOnly Han, string O, decimal SoLuong)[]
        {
            ("LX", HomNay.AddDays(-1), Lanh + "01", 20),
            ("L1", HomNay.AddDays(10), Lanh + "02", 3),
            ("L2", HomNay.AddDays(30), Lanh + "03", 10),
            ("L3", HomNay.AddDays(5), "K01-TA01-TG01-P01-H01-KE01-O01", 50),
        };
        db.Lo.AddRange(lo.Select(l => new Lo { MaLo = l.Ma, MaBienThe = "SUATUOI-01", HanDung = l.Han, NgayNhap = Gio.GetUtcNow() }));
        await db.SaveChangesAsync();
        await using var tx = await db.Database.BeginTransactionAsync();
        await new CongGhiTon(db, NguoiThu.HienTai(db, "ND001", Vai.QuanTri), Gio).GhiAsync("nap", "Thu", "T1", lo.Select(l => new DongGhiTon(l.Ma, l.O, l.SoLuong)).ToList());
        await tx.CommitAsync();
    }

    public async Task DisposeAsync() => await _app.DisposeAsync();

    private async Task<T> Voi<T>(Func<GoiYService, Task<T>> lam, string ma = "ND004", string vai = Vai.ThuKho)
    {
        await using var db = CsdlFixture.TaoDbContext(_chuoi);
        var hienTai = NguoiThu.HienTai(db, ma, vai);
        return await lam(new GoiYService(db, hienTai, new TonKhaDung(db, hienTai, Gio), Gio));
    }

    [Fact]
    public async Task GoiYTuTonKhaDung_GhiLichSu_KhongDoiTon()
    {
        var kq = await Voi(s => s.LayHangAsync("K01", "SUATUOI-01", 8));
        Assert.Equal([("L1", Lanh + "02", 3m), ("L2", Lanh + "03", 5m)], kq.Dong.Select(d => (d.MaLo, d.MaViTri, d.SoLuong)));
        Assert.Equal(0, kq.ConThieu);
        Assert.Equal(87, (await Voi(s => s.LayHangAsync("K01", "SUATUOI-01", 100))).ConThieu);

        await using var db = CsdlFixture.TaoDbContext(_chuoi);
        var ls = await db.LichSuGoiY.SingleAsync(x => x.MaGoiY == kq.MaGoiY);
        Assert.Equal((LoaiGoiY.LayHang, "K01", "SUATUOI-01", "ND004", (bool?)null), (ls.Loai, ls.MaKho, ls.MaBienThe, ls.NguoiDung, ls.ChapNhan));
        Assert.Contains("\"L1\"", ls.DauVao);
        Assert.DoesNotContain("\"LX\"", ls.DauVao);
        Assert.Equal(0, JsonDocument.Parse(ls.KetQua).RootElement.GetProperty("conThieu").GetDecimal());
        Assert.Equal(83, await db.TonKho.SumAsync(t => t.SoLuong));
        Assert.Equal(0, await db.TonKho.SumAsync(t => t.SoLuongDaGiu));
    }

    [Fact]
    public async Task PhanHoi_ChiGhiLanDau_VaPhanQuyen()
    {
        var kq = await Voi(s => s.LayHangAsync("K01", "SUATUOI-01", 2));
        await Voi(async s => { await s.PhanHoiAsync(kq.MaGoiY, true, "LX000001"); return 0; });
        await Voi(async s => { await s.PhanHoiAsync(kq.MaGoiY, false); return 0; });
        await using (var db = CsdlFixture.TaoDbContext(_chuoi))
        {
            var ls = await db.LichSuGoiY.SingleAsync(x => x.MaGoiY == kq.MaGoiY);
            Assert.Equal((true, "LX000001"), (ls.ChapNhan, ls.MaChungTu));
        }

        await Assert.ThrowsAsync<KhongCoQuyenException>(() => Voi(s => s.LayHangAsync("K01", "SUATUOI-01", 1), "ND006"));
        await Assert.ThrowsAsync<NghiepVuException>(() => Voi(s => s.LayHangAsync("K01", "SUATUOI-01", 0)));

        var thuKho = _app.TaoClient(Vai.ThuKho, "ND004");
        var api = await thuKho.GetFromJsonAsync<JsonElement>("/api/goi-y/lay-hang?maKho=K01&maBienThe=SUATUOI-01&soLuong=4");
        Assert.Equal("L1", api.GetProperty("dong")[0].GetProperty("maLo").GetString());
        Assert.StartsWith("hết hạn sớm nhất 11/11/2026", api.GetProperty("dong")[0].GetProperty("lyDo").GetString());
        Assert.Equal(HttpStatusCode.Forbidden, (await _app.TaoClient(Vai.ThuKho, "ND006").GetAsync("/api/goi-y/lay-hang?maKho=K01&maBienThe=SUATUOI-01&soLuong=4")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await _app.TaoClient(Vai.BanHang, "ND007").GetAsync("/api/goi-y/lay-hang?maKho=K01&maBienThe=SUATUOI-01&soLuong=4")).StatusCode);

        var token = await NguoiThu.TokenAsync(thuKho, "/XepHang");
        var post = new HttpRequestMessage(HttpMethod.Post, $"/api/goi-y/{api.GetProperty("maGoiY").GetInt64()}/phan-hoi") { Content = JsonContent.Create(new { chapNhan = false }) };
        post.Headers.Add("RequestVerificationToken", token);
        Assert.Equal(HttpStatusCode.NoContent, (await thuKho.SendAsync(post)).StatusCode);
    }
}
