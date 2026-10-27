using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
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
public class DonDatTests(CsdlFixture csdl) : IAsyncLifetime
{
    private const string LuuTru = "K01-TA01-TG01-P02-H01-KE01-O";
    private static readonly FakeTimeProvider Gio = new(new DateTimeOffset(2026, 11, 1, 3, 0, 0, TimeSpan.Zero));
    private static readonly DateOnly HomNay = new(2026, 11, 1);
    private string _chuoi = "";
    private UngDungFactory _app = null!;

    public async Task InitializeAsync()
    {
        _chuoi = await csdl.TaoCsdlCoDuLieuThuAsync();
        _app = new UngDungFactory { ChuoiKetNoi = _chuoi, Gio = Gio };
    }

    public async Task DisposeAsync() => await _app.DisposeAsync();

    private static TonKhaDung Ton(AppDbContext db, string ma, string vai) => new(db, NguoiThu.HienTai(db, ma, vai), Gio);

    private async Task<T> Voi<T>(Func<DonDatService, Task<T>> lam, string ma = "ND007", string vai = Vai.BanHang)
    {
        await using var db = CsdlFixture.TaoDbContext(_chuoi);
        var hienTai = NguoiThu.HienTai(db, ma, vai);
        return await lam(new DonDatService(db, hienTai, new MaChungTu(db), NguoiThu.NhatKy(db, hienTai), Ton(db, ma, vai), Gio));
    }

    private Task Voi(Func<DonDatService, Task> lam, string ma = "ND007", string vai = Vai.BanHang) => Voi(async s => { await lam(s); return 0; }, ma, vai);

    private static DonDatForm Don(params (string BienThe, decimal SoLuong)[] dong) => new()
    {
        MaKho = "K01", MaNoiNhan = "NN01", HanGiao = HomNay.AddDays(3),
        Dong = dong.Select(d => new DongDonDatForm { MaBienThe = d.BienThe, SoLuong = d.SoLuong }).ToList(),
    };

    /// <summary>Đặt hàng vào đủ mọi loại chỗ cần loại trừ; chỉ LA (10), LE (8 − 2 giữ), LH (hết hạn hôm nay, còn dùng được) và LG ở K02 là khả dụng.</summary>
    private async Task NapTonAsync()
    {
        await using var db = CsdlFixture.TaoDbContext(_chuoi);
        var lo = new (string Ma, DateOnly? HanDung, string O, decimal SoLuong)[]
        {
            ("LA", null, LuuTru + "01", 10),
            ("LB", null, "K01-TA01-TG01-P01-H01-KE01-O01", 5),
            ("LC", null, "K01-TA01-TG01-P04-H01-KE01-O01", 3),
            ("LD", HomNay.AddDays(-1), LuuTru + "02", 7),
            ("LE", null, LuuTru + "03", 8),
            ("LF", null, LuuTru + "04", 4),
            ("LH", HomNay, LuuTru + "05", 1),
            ("LG", null, "K02-TA01-TG01-P02-H01-KE01-O01", 9),
        };
        db.Lo.AddRange(lo.Select(l => new Lo { MaLo = l.Ma, MaBienThe = "AOTHUN-01", HanDung = l.HanDung, NgayNhap = Gio.GetUtcNow() }));
        await db.SaveChangesAsync();
        await using var tx = await db.Database.BeginTransactionAsync();
        var cong = new CongGhiTon(db, NguoiThu.HienTai(db, "ND001", Vai.QuanTri), Gio);
        await cong.GhiAsync("nap", "Thu", "T1", lo.Select(l => new DongGhiTon(l.Ma, l.O, l.SoLuong)).ToList());
        await cong.GiuAsync([new("LE", LuuTru + "03", 2)]);
        await tx.CommitAsync();
        await db.ViTri.Where(v => v.MaViTri == LuuTru + "04").ExecuteUpdateAsync(s => s.SetProperty(v => v.MaPhieuKiemKe, "KK000001"));
    }

    [Fact]
    public async Task TonKhaDung_LoaiTruChoKiem_CachLy_HetHan_DaGiu_KhoaKiemKe()
    {
        await NapTonAsync();
        await using var db = CsdlFixture.TaoDbContext(_chuoi);

        var banHang = (await Ton(db, "ND007", Vai.BanHang).TraCuuAsync("AOTHUN-01", null, false, 1)).Muc.Single();
        Assert.Equal(new Dictionary<string, decimal> { ["K01"] = 17, ["K02"] = 9 }, banHang.TheoKho);
        Assert.Equal(26, banHang.Tong);
        Assert.Equal(["LA", "LE", "LH"], await Ton(db, "ND007", Vai.BanHang).TruyVan().Where(d => d.MaKho == "K01").Select(d => d.MaLo).OrderBy(m => m).ToListAsync());

        var thuKho = (await Ton(db, "ND004", Vai.ThuKho).TraCuuAsync("AOTHUN-01", null, false, 1)).Muc.Single();
        Assert.Equal(["K01"], thuKho.TheoKho.Keys);
        await Assert.ThrowsAsync<KhongCoQuyenException>(() => Ton(db, "ND004", Vai.ThuKho).TraCuuAsync(null, "K02", false, 1));

        var maVach = (await db.BienThe.SingleAsync(b => b.MaBienThe == "AOTHUN-01")).MaVach!;
        Assert.Equal("AOTHUN-01", (await Ton(db, "ND007", Vai.BanHang).TraCuuAsync(maVach, null, false, 1)).Muc.Single().MaBienThe);
        Assert.Equal(["AOTHUN-01"], (await Ton(db, "ND007", Vai.BanHang).TraCuuAsync("AOTHUN", null, true, 1)).Muc.Select(m => m.MaBienThe));
    }

    [Fact]
    public async Task TaoDon_GuiDuyet_ChiNguoiTaoVaKhoDuocGiaoThay()
    {
        await NapTonAsync();
        var ma = await Voi(s => s.TaoAsync(Don(("AOTHUN-01", 20), ("DTA-01", 2)), guiDuyet: true));
        Assert.Equal("DD000001", ma);

        var d = await Voi(s => s.LayAsync(ma), "ND002", Vai.QuanLyKho);
        Assert.Equal(TrangThaiDonDat.ChoDuyet, d.TrangThai);
        Assert.Equal((20m, 17m), (d.Dong[0].SoLuongDat, d.Dong[0].KhaDung));
        Assert.Equal(1, (await Voi(s => s.DanhSachAsync(null, null, null, 1), "ND002", Vai.QuanLyKho)).TongSo);
        Assert.Equal(0, (await Voi(s => s.DanhSachAsync(null, null, null, 1), "ND003", Vai.QuanLyKho)).TongSo);
        await Assert.ThrowsAsync<NghiepVuException>(() => Voi(s => s.LayAsync(ma), "ND003", Vai.QuanLyKho));
        await Assert.ThrowsAsync<KhongCoQuyenException>(() => Voi(s => s.SuaAsync(ma, Don(("AOTHUN-01", 1))), "ND002", Vai.QuanLyKho));

        await Assert.ThrowsAsync<NghiepVuException>(() => Voi(s => s.TaoAsync(Don(("AOTHUN-01", 1), ("AOTHUN-01", 2)), false)));
        await Assert.ThrowsAsync<NghiepVuException>(() => Voi(s => s.TaoAsync(Don(("AOTHUN-01", 0)), false)));
        var quaHan = Don(("AOTHUN-01", 1));
        quaHan.HanGiao = HomNay.AddDays(-1);
        Assert.Contains("Hạn giao", (await Assert.ThrowsAsync<NghiepVuException>(() => Voi(s => s.TaoAsync(quaHan, false)))).Message);

        await using var db = CsdlFixture.TaoDbContext(_chuoi);
        Assert.True(await db.NhatKy.AnyAsync(n => n.LoaiThaoTac == "GuiDuyetDonDat" && n.MaDoiTuong == ma));
    }

    [Fact]
    public async Task Nhap_GuiDuyet_Sua_Huy()
    {
        var ma = await Voi(s => s.TaoAsync(Don(("AOTHUN-01", 5)), guiDuyet: false));
        Assert.Equal(TrangThaiDonDat.Nhap, (await Voi(s => s.LayAsync(ma))).TrangThai);
        await Voi(s => s.SuaAsync(ma, Don(("AOTHUN-02", 3), ("DTA-01", 1))));
        Assert.Equal(["AOTHUN-02", "DTA-01"], (await Voi(s => s.LayAsync(ma))).Dong.Select(x => x.MaBienThe));

        await Voi(s => s.GuiDuyetAsync(ma));
        await Assert.ThrowsAsync<NghiepVuException>(() => Voi(s => s.GuiDuyetAsync(ma)));
        await Voi(s => s.HuyAsync(ma));
        Assert.Equal(TrangThaiDonDat.DaHuy, (await Voi(s => s.LayAsync(ma))).TrangThai);
        await Assert.ThrowsAsync<NghiepVuException>(() => Voi(s => s.HuyAsync(ma)));
        await Assert.ThrowsAsync<NghiepVuException>(() => Voi(s => s.SuaAsync(ma, Don(("AOTHUN-01", 1)))));

        await using var db = CsdlFixture.TaoDbContext(_chuoi);
        await db.DonDatHang.Where(x => x.MaDonDat == ma).ExecuteUpdateAsync(s => s.SetProperty(x => x.TrangThai, TrangThaiDonDat.DaDuyet));
        Assert.Contains("bỏ giữ", (await Assert.ThrowsAsync<NghiepVuException>(() => Voi(s => s.HuyAsync(ma)))).Message);
    }

    [Fact]
    public async Task ManHinh_Api_PhanQuyenTheoVai()
    {
        await NapTonAsync();
        var banHang = _app.TaoClient(Vai.BanHang, "ND007");
        Assert.Contains("AOTHUN-01", await banHang.GetStringAsync("/TonKhaDung?tuKhoa=AOTHUN-01"));
        Assert.Equal(HttpStatusCode.OK, (await banHang.GetAsync("/DonDat/Them")).StatusCode);

        var r = await NguoiThu.PostFormAsync(banHang, "/DonDat/Them", "/DonDat/Them", new()
        {
            ["MaKho"] = "K01", ["MaNoiNhan"] = "NN01", ["Dong[0].MaBienThe"] = "AOTHUN-01", ["Dong[0].SoLuong"] = "2", ["guiDuyet"] = "true",
        });
        Assert.Equal(HttpStatusCode.Redirect, r.StatusCode);
        Assert.Contains("Chờ duyệt", await banHang.GetStringAsync(r.Headers.Location!.ToString()));

        var token = await NguoiThu.TokenAsync(banHang, "/DonDat");
        var post = new HttpRequestMessage(HttpMethod.Post, "/api/don-dat")
        {
            Content = JsonContent.Create(new { maKho = "K02", maNoiNhan = "NN02", dong = new[] { new { maBienThe = "AOTHUN-01", soLuong = 1 } }, guiDuyet = true }),
        };
        post.Headers.Add("RequestVerificationToken", token);
        Assert.Equal(HttpStatusCode.Created, (await banHang.SendAsync(post)).StatusCode);
        var ds = await banHang.GetFromJsonAsync<JsonElement>("/api/don-dat");
        Assert.Equal(2, ds.GetProperty("tongSo").GetInt32());
        Assert.Equal("ChoDuyet", ds.GetProperty("muc")[0].GetProperty("trangThai").GetString());

        var ton = await banHang.GetFromJsonAsync<JsonElement>("/api/ton-kha-dung?tuKhoa=AOTHUN-01");
        Assert.Equal(17, ton.GetProperty("muc")[0].GetProperty("theoKho").GetProperty("K01").GetDecimal());

        Assert.Equal(HttpStatusCode.Forbidden, (await _app.TaoClient(Vai.ThuKho, "ND004").GetAsync("/DonDat")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await _app.TaoClient(Vai.QuanLyKho, "ND002").GetAsync("/DonDat/Them")).StatusCode);
    }
}
