using System.Net;
using Microsoft.Extensions.Time.Testing;
using quanlykho.Common;
using quanlykho.Common.Loi;
using quanlykho.Common.PhanQuyen;
using quanlykho.Data;
using quanlykho.Domain;
using quanlykho.Features.KiemKe;
using quanlykho.Features.XuatKho;
using quanlykho.Tests.HaTang;

namespace quanlykho.Tests.Integration;

/// <summary>Kiểm kê đếm theo thùng và số lẻ: sữa thùng = 24 hộp, sổ sách lô LS 235 hộp ở ô lạnh O01; áo LA 5 cái ở O02 (không có đơn vị quy đổi).</summary>
[Collection(CsdlCollection.Ten)]
public class QuyDoiKiemKeTests(CsdlFixture csdl) : IAsyncLifetime
{
    private const string Ke = "K01-TA01-TG01-P03-H01-KE01";
    private static readonly FakeTimeProvider Gio = new(new DateTimeOffset(2026, 11, 1, 3, 0, 0, TimeSpan.Zero));
    private string _chuoi = "";

    public async Task InitializeAsync()
    {
        _chuoi = await csdl.TaoCsdlCoDuLieuThuAsync();
        await using var db = CsdlFixture.TaoDbContext(_chuoi);
        db.DonViQuyDoi.Add(new DonViQuyDoi { MaBienThe = "SUATUOI-01", TenDonVi = "thùng", HeSo = 24, TrangThai = TrangThaiSuDung.DangSuDung });
        db.Lo.AddRange(new Lo { MaLo = "LS", MaBienThe = "SUATUOI-01", HanDung = new DateOnly(2027, 1, 1), NgayNhap = Gio.GetUtcNow() },
            new Lo { MaLo = "LA", MaBienThe = "AOTHUN-01", NgayNhap = Gio.GetUtcNow() });
        await db.SaveChangesAsync();
        await using var tx = await db.Database.BeginTransactionAsync();
        await new CongGhiTon(db, NguoiThu.HienTai(db, "ND001", Vai.QuanTri), Gio).GhiAsync("nap", "Thu", "T1", [new("LS", Ke + "-O01", 235), new("LA", Ke + "-O02", 5)]);
        await tx.CommitAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<T> Voi<T>(Func<KiemKeService, Task<T>> lam, string ma = "ND002", string vai = Vai.QuanLyKho)
    {
        await using var db = CsdlFixture.TaoDbContext(_chuoi);
        var h = NguoiThu.HienTai(db, ma, vai);
        return await lam(new KiemKeService(db, h, new MaChungTu(db), new TonKhaDung(db, h, Gio), new CongGhiTon(db, h, Gio), NguoiThu.NhatKy(db, h), Gio));
    }

    private Task Dem(string ma, params DongDemForm[] dem) => Voi(async s => { await s.NhapSoDemAsync(ma, dem); return 0; }, "ND004", Vai.ThuKho);

    [Fact]
    public async Task DemChinThungMuoiChinHop_KhopSoSach235()
    {
        var ma = await Voi(s => s.TaoAsync(new KiemKeForm { MaKho = "K01", PhamVi = PhamViKiemKe.ViTri, DoiTuong = [Ke], ThanhVien = ["ND004"] }));
        await Voi(async s => { await s.BatDauAsync(ma, false); return 0; });
        var p = await Voi(s => s.LayAsync(ma));
        var sua = p.Dong.Single(d => d.MaLo == "LS");
        var ao = p.Dong.Single(d => d.MaLo == "LA");
        Assert.Equal(("thùng", 24), (sua.DonViLon!.Ten, sua.DonViLon.HeSo));
        Assert.Null(ao.DonViLon);

        await Dem(ma, new DongDemForm { STT = sua.STT, SoLon = 9, SoLuongThucTe = 19 });
        Assert.Equal((235m, 0m), (await Voi(s => s.LayAsync(ma))).Dong.Single(d => d.MaLo == "LS") is var a ? (a.ThucTe!.Value, a.ChenhLech!.Value) : default);

        await Dem(ma, new DongDemForm { STT = sua.STT, SoLon = 9, SoLuongThucTe = 18 });
        Assert.Equal(-1, (await Voi(s => s.LayAsync(ma))).Dong.Single(d => d.MaLo == "LS").ChenhLech);

        Assert.Contains("không có đơn vị quy đổi", (await Assert.ThrowsAsync<NghiepVuException>(() => Dem(ma, new DongDemForm { STT = ao.STT, SoLon = 1, SoLuongThucTe = 0 }))).Message);
        await Assert.ThrowsAsync<NghiepVuException>(() => Dem(ma, new DongDemForm { STT = sua.STT, SoLon = -1, SoLuongThucTe = 30 }));

        // Hàng ngoài danh sách: 1 thùng 2 hộp ở ô O03.
        var stt = await Voi(s => s.ThemDongAsync(ma, new ThemDongKiemKe(Ke + "-O03", "SUATUOI-01", 2, 1)), "ND004", Vai.ThuKho);
        Assert.Equal(26, (await Voi(s => s.LayAsync(ma))).Dong.Single(d => d.STT == stt).ThucTe);

        await using var app = new UngDungFactory { ChuoiKetNoi = _chuoi, Gio = Gio };
        var html = await app.TaoClient(Vai.ThuKho, "ND004").GetStringAsync($"/KiemKe/{ma}");
        Assert.Contains($"dem[0].SoLon", html.Replace("&#x5B;", "[").Replace("&#x5D;", "]"));
        Assert.Contains("9 thùng 19 hộp", html);
    }
}
