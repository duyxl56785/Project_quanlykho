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
public class OPhuHopTests(CsdlFixture csdl) : IAsyncLifetime
{
    private const string TiepNhan = "K01-TA01-TG01-P01-H01-KE01-O01";
    private const string Ke = "K01-TA01-TG01-P02-H01-KE01";
    private string _chuoi = "";
    private UngDungFactory _app = null!;

    public async Task InitializeAsync()
    {
        _chuoi = await csdl.TaoCsdlCoDuLieuThuAsync();
        _app = new UngDungFactory { ChuoiKetNoi = _chuoi };
        await using var db = CsdlFixture.TaoDbContext(_chuoi);
        foreach (var (lo, bienThe) in new[] { ("LT1", "AOTHUN-01"), ("LT2", "AOTHUN-01"), ("LT3", "DTA-01"), ("LS1", "SUATUOI-01"), ("LX", "AOTHUN-01") })
            db.Lo.Add(new Lo { MaLo = lo, MaBienThe = bienThe, NgayNhap = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync();
        await using var tx = await db.Database.BeginTransactionAsync();
        var cong = new CongGhiTon(db, NguoiThu.HienTai(db, "ND002", Vai.QuanLyKho), TimeProvider.System);
        await cong.GhiAsync("thu", "Thu", "T1", [new("LT1", TiepNhan, 600), new("LS1", TiepNhan, 20), new("LT2", Ke + "-O01", 50), new("LT3", Ke + "-O02", 10)]);
        await tx.CommitAsync();
        await db.ViTri.Where(v => v.MaViTri == Ke + "-O03").ExecuteUpdateAsync(s => s.SetProperty(v => v.DangKhoa, true));
    }

    public async Task DisposeAsync() => await _app.DisposeAsync();

    private async Task<List<OPhuHopDong>> Tim(string maLo, decimal soLuong, string ma = "ND004", string vai = Vai.ThuKho)
    {
        await using var db = CsdlFixture.TaoDbContext(_chuoi);
        return await new OPhuHop(db, NguoiThu.HienTai(db, ma, vai)).TimAsync(maLo, soLuong);
    }

    [Fact]
    public async Task ChiOLuuTruDungBaoQuanKhongKhoa_TruHangDangCo()
    {
        var ds = await Tim("LT1", 500);
        Assert.Equal(11, ds.Count);
        Assert.All(ds, o => Assert.StartsWith("K01-TA01-TG01-P02-", o.MaViTri));
        Assert.DoesNotContain(ds, o => o.MaViTri == Ke + "-O03");
        Assert.Equal(ds.Select(o => o.MaViTri).Order(StringComparer.Ordinal), ds.Select(o => o.MaViTri));

        var o1 = ds.Single(o => o.MaViTri == Ke + "-O01");
        Assert.Equal((478m, false), (o1.QMax, o1.DuSoLuong));
        Assert.Equal(448, ds.Single(o => o.MaViTri == Ke + "-O02").QMax);
        var o4 = ds.Single(o => o.MaViTri == Ke + "-O04");
        Assert.Equal((528m, true, 300m), (o4.QMax, o4.DuSoLuong, o4.TaiTrongConLai));

        var lanh = await Tim("LS1", 10);
        Assert.Equal(4, lanh.Count);
        Assert.All(lanh, o => Assert.StartsWith("K01-TA01-TG01-P03-", o.MaViTri));
    }

    [Fact]
    public async Task LoiVaPhanQuyen()
    {
        Assert.Contains("không còn tồn", (await Assert.ThrowsAsync<NghiepVuException>(() => Tim("LX", 1))).Message);
        await Assert.ThrowsAsync<NghiepVuException>(() => Tim("LT1", 0));
        await Assert.ThrowsAsync<KhongCoQuyenException>(() => Tim("LT1", 1, "ND003", Vai.QuanLyKho));

        var ds = await _app.TaoClient(Vai.ThuKho, "ND004").GetFromJsonAsync<JsonElement>("/api/vi-tri/o-phu-hop?maLo=LT1&soLuong=500");
        Assert.Equal(11, ds.GetArrayLength());
        Assert.Equal(478, ds[0].GetProperty("qMax").GetDecimal());
        Assert.Equal(HttpStatusCode.Forbidden, (await _app.TaoClient(Vai.QuanLyKho, "ND003").GetAsync("/api/vi-tri/o-phu-hop?maLo=LT1&soLuong=1")).StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await _app.TaoClient(Vai.ThuKho, "ND004").GetAsync("/api/vi-tri/o-phu-hop?maLo=LX&soLuong=1")).StatusCode);
    }
}
