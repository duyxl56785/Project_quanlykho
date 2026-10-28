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
public class LenhXuatTests(CsdlFixture csdl) : IAsyncLifetime
{
    private const string Thuong = "K01-TA01-TG01-P02-H01-KE01-O";
    private const string Lanh = "K01-TA01-TG01-P03-H01-KE01-O";
    private const string CachLy = "K01-TA01-TG01-P04-H01-KE01-O01";
    private static readonly FakeTimeProvider Gio = new(new DateTimeOffset(2026, 11, 1, 3, 0, 0, TimeSpan.Zero));
    private static readonly DateOnly HomNay = new(2026, 11, 1);
    private string _chuoi = "";
    private UngDungFactory _app = null!;

    /// <summary>Áo L1 10 ở O01 (nhập trước), L2 8 ở O02, L4 5 ở O04; điện thoại LD 3 ở O03; sữa LX hết hạn ở lạnh O01, L3 5 ở lạnh O02, LC 50 ở cách ly.</summary>
    public async Task InitializeAsync()
    {
        _chuoi = await csdl.TaoCsdlCoDuLieuThuAsync();
        _app = new UngDungFactory { ChuoiKetNoi = _chuoi, Gio = Gio };
        await using var db = CsdlFixture.TaoDbContext(_chuoi);
        var lo = new (string Ma, string BienThe, DateOnly? Han, int NhapSau, string O, decimal SoLuong)[]
        {
            ("L1", "AOTHUN-01", null, 0, Thuong + "01", 10),
            ("L2", "AOTHUN-01", null, 1, Thuong + "02", 8),
            ("L4", "AOTHUN-01", null, 2, Thuong + "04", 5),
            ("LD", "DTA-01", null, 0, Thuong + "03", 3),
            ("LX", "SUATUOI-01", HomNay.AddDays(-1), 0, Lanh + "01", 20),
            ("L3", "SUATUOI-01", HomNay.AddDays(10), 0, Lanh + "02", 5),
            ("LC", "SUATUOI-01", HomNay.AddDays(10), 0, CachLy, 50),
        };
        db.Lo.AddRange(lo.Select(l => new Lo { MaLo = l.Ma, MaBienThe = l.BienThe, HanDung = l.Han, NgayNhap = Gio.GetUtcNow().AddDays(l.NhapSau) }));
        await db.SaveChangesAsync();
        await using var tx = await db.Database.BeginTransactionAsync();
        await new CongGhiTon(db, NguoiThu.HienTai(db, "ND001", Vai.QuanTri), Gio).GhiAsync("nap", "Thu", "T1", lo.Select(l => new DongGhiTon(l.Ma, l.O, l.SoLuong)).ToList());
        await tx.CommitAsync();
    }

    public async Task DisposeAsync() => await _app.DisposeAsync();

    private sealed record DichVu(DonDatService Don, GiuHangService Giu, LenhXuatService Lenh, PhieuXuatService Phieu);

    private static DichVu Tao(AppDbContext db, string ma, string vai)
    {
        var h = NguoiThu.HienTai(db, ma, vai);
        var nhatKy = NguoiThu.NhatKy(db, h);
        var ton = new TonKhaDung(db, h, Gio);
        var cong = new CongGhiTon(db, h, Gio);
        var goiY = new GoiYService(db, h, ton, Gio);
        var giu = new GiuHangService(db, h, cong, goiY, nhatKy, Gio);
        return new DichVu(new DonDatService(db, h, new MaChungTu(db), nhatKy, ton, giu, Gio), giu,
            new LenhXuatService(db, h, new MaChungTu(db), cong, goiY, ton, nhatKy, Gio),
            new PhieuXuatService(db, h, new MaChungTu(db), cong, nhatKy, Gio));
    }

    private async Task<T> Voi<T>(string ma, string vai, Func<DichVu, Task<T>> lam)
    {
        await using var db = CsdlFixture.TaoDbContext(_chuoi);
        return await lam(Tao(db, ma, vai));
    }

    private Task Voi(string ma, string vai, Func<DichVu, Task> lam) => Voi(ma, vai, async d => { await lam(d); return 0; });

    private Task<long> Lay(string lenh, string o, string hang, decimal soLuong, string thuKho = "ND004") =>
        Voi(thuKho, Vai.ThuKho, d => d.Lenh.LayHangAsync(lenh, new LayHangYeuCau(o, hang, soLuong)));

    private async Task<string> DonDaDuyet(params (string BienThe, decimal SoLuong)[] dong)
    {
        var ma = await Voi("ND007", Vai.BanHang, d => d.Don.TaoAsync(new DonDatForm
        {
            MaKho = "K01", MaNoiNhan = "NN01", Dong = dong.Select(x => new DongDonDatForm { MaBienThe = x.BienThe, SoLuong = x.SoLuong }).ToList(),
        }, true));
        await Voi("ND002", Vai.QuanLyKho, d => d.Giu.DuyetAsync([ma]));
        return ma;
    }

    private async Task<Dictionary<string, (decimal SoLuong, decimal DaGiu)>> Ton()
    {
        await using var db = CsdlFixture.TaoDbContext(_chuoi);
        return await db.TonKho.ToDictionaryAsync(t => t.MaLo, t => (t.SoLuong, t.SoLuongDaGiu));
    }

    private async Task<string> DenChoXacNhan(string lenh)
    {
        await Voi("ND004", Vai.ThuKho, d => d.Lenh.XongLayAsync(lenh));
        await Voi("ND002", Vai.QuanLyKho, d => d.Lenh.KiemTraAsync(lenh, new KiemTraYeuCau(true, null)));
        return lenh;
    }

    [Fact]
    public async Task LuongTron_Hinh26_ChanSaiHang_HetHan_Vuot_NguoiKiemKhacNguoiLay()
    {
        var don = await DonDaDuyet(("AOTHUN-01", 12), ("SUATUOI-01", 4));
        await Assert.ThrowsAsync<NghiepVuException>(() => Voi("ND002", Vai.QuanLyKho, d => d.Lenh.TaoAsync(new LapLenhYeuCau(don, "ND006"))));
        var lenh = await Voi("ND002", Vai.QuanLyKho, d => d.Lenh.TaoAsync(new LapLenhYeuCau(don, "ND004")));
        Assert.Equal("LX000001", lenh);

        await Assert.ThrowsAsync<KhongCoQuyenException>(() => Lay(lenh, Thuong + "01", "L1", 1, "ND005"));
        Assert.Contains("Sai hàng", (await Assert.ThrowsAsync<NghiepVuException>(() => Lay(lenh, Thuong + "03", "LD", 1))).Message);
        Assert.Contains("hết hạn", (await Assert.ThrowsAsync<NghiepVuException>(() => Lay(lenh, Lanh + "01", "LX", 1))).Message);
        Assert.Contains("khu lưu trữ", (await Assert.ThrowsAsync<NghiepVuException>(() => Lay(lenh, CachLy, "LC", 1))).Message);
        await Lay(lenh, Thuong + "01", "L1", 10);
        Assert.Contains("Lấy vượt", (await Assert.ThrowsAsync<NghiepVuException>(() => Lay(lenh, Thuong + "02", "L2", 5))).Message);
        await Lay(lenh, Thuong + "02", "L2", 2);
        string maVach;
        await using (var db = CsdlFixture.TaoDbContext(_chuoi))
            maVach = (await db.BienThe.SingleAsync(b => b.MaBienThe == "SUATUOI-01")).MaVach!;
        await Lay(lenh, Lanh + "02", maVach, 4);

        await Voi("ND004", Vai.ThuKho, d => d.Lenh.XongLayAsync(lenh));
        Assert.Contains("khác người lấy", (await Assert.ThrowsAsync<NghiepVuException>(() => Voi("ND004", Vai.ThuKho, d => d.Lenh.KiemTraAsync(lenh, new KiemTraYeuCau(true, null))))).Message);
        await Assert.ThrowsAsync<NghiepVuException>(() => Voi("ND002", Vai.QuanLyKho, d => d.Lenh.KiemTraAsync(lenh, new KiemTraYeuCau(false, null))));
        await Voi("ND002", Vai.QuanLyKho, d => d.Lenh.KiemTraAsync(lenh, new KiemTraYeuCau(false, "Đếm lại L2")));
        Assert.Equal(TrangThaiLenhXuat.ChoLayHang, (await Voi("ND004", Vai.ThuKho, d => d.Lenh.LayAsync(lenh))).TrangThai);
        await DenChoXacNhan(lenh);

        var (px, kq) = await Voi("ND002", Vai.QuanLyKho, d => d.Lenh.XacNhanAsync(lenh, "xn-1"));
        Assert.Equal(("PX000001", KetQuaGhi.DaGhi), (px, kq));
        Assert.Equal((px, KetQuaGhi.DaGhiTruocDo), await Voi("ND002", Vai.QuanLyKho, d => d.Lenh.XacNhanAsync(lenh, "xn-1")));
        await Assert.ThrowsAsync<NghiepVuException>(() => Voi("ND002", Vai.QuanLyKho, d => d.Lenh.XacNhanAsync(lenh, "xn-2")));

        var ton = await Ton();
        Assert.Equal([(0m, 0m), (6m, 0m), (1m, 0m)], new[] { ton["L1"], ton["L2"], ton["L3"] });
        await using var kt = CsdlFixture.TaoDbContext(_chuoi);
        Assert.Equal(-16, await kt.SoBienDong.Where(s => s.MaChungTu == px).SumAsync(s => s.SoLuongThayDoi));
        Assert.False(await kt.GiuHang.AnyAsync());
        Assert.Equal(TrangThaiDonDat.HoanThanh, (await kt.DonDatHang.SingleAsync(d => d.MaDonDat == don)).TrangThai);
        Assert.Equal([12m, 4m], await kt.ChiTietDonDat.Where(c => c.MaDonDat == don).OrderBy(c => c.MaBienThe).Select(c => c.SoLuongDaXuat).ToListAsync());
        Assert.Equal(TrangThaiLenhXuat.HoanThanh, (await kt.LenhXuat.SingleAsync()).TrangThai);
        Assert.All(await kt.LichSuGoiY.Where(x => x.MaChungTu == don).ToListAsync(), x => Assert.True(x.ChapNhan));
        Assert.True(await kt.NhatKy.AnyAsync(n => n.LoaiThaoTac == "XacNhanXuat" && n.MaDoiTuong == lenh));
    }

    [Fact]
    public async Task HaiThuKhoLayCungHangChuaGiu_KhongTonAm()
    {
        var donA = await DonDaDuyet(("AOTHUN-01", 10));
        var donB = await DonDaDuyet(("AOTHUN-01", 8));
        var a = await Voi("ND002", Vai.QuanLyKho, d => d.Lenh.TaoAsync(new LapLenhYeuCau(donA, "ND004")));
        var b = await Voi("ND002", Vai.QuanLyKho, d => d.Lenh.TaoAsync(new LapLenhYeuCau(donB, "ND005")));

        Assert.Contains("cần 8, còn 0", (await Assert.ThrowsAsync<NghiepVuException>(() => Lay(a, Thuong + "02", "L2", 8))).Message);
        var kq = await Task.WhenAll(new[] { (a, "ND004"), (b, "ND005") }.Select(async x =>
        {
            try { await Lay(x.Item1, Thuong + "04", "L4", 5, x.Item2); return "ok"; }
            catch (NghiepVuException e) { return e.Message; }
        }));
        Assert.Single(kq, "ok");
        Assert.Equal((5m, 5m), (await Ton())["L4"]);
    }

    [Fact]
    public async Task LayNgoaiGoiY_GiuThem_XoaDongBoGiu_GhiKhongChapNhan()
    {
        var don = await DonDaDuyet(("AOTHUN-01", 10));
        var lenh = await Voi("ND002", Vai.QuanLyKho, d => d.Lenh.TaoAsync(new LapLenhYeuCau(don, "ND004")));
        var dong = await Lay(lenh, Thuong + "04", "L4", 5);
        Assert.Equal((5m, 5m), (await Ton())["L4"]);
        await Voi("ND004", Vai.ThuKho, d => d.Lenh.XoaDongLayAsync(lenh, dong));
        Assert.Equal((5m, 0m), (await Ton())["L4"]);

        await Lay(lenh, Thuong + "04", "L4", 5);
        await Lay(lenh, Thuong + "01", "L1", 5);
        await DenChoXacNhan(lenh);
        await Voi("ND002", Vai.QuanLyKho, d => d.Lenh.XacNhanAsync(lenh, "xn"));

        var ton = await Ton();
        Assert.Equal([(5m, 0m), (0m, 0m)], new[] { ton["L1"], ton["L4"] });
        await using var db = CsdlFixture.TaoDbContext(_chuoi);
        Assert.False((await db.LichSuGoiY.SingleAsync(x => x.MaChungTu == don)).ChapNhan);
    }

    [Fact]
    public async Task PhieuXuatKhac_TraNhaCungCap()
    {
        PhieuXuatForm Form(LoaiPhieuXuat loai, decimal soLuong, string khoa, string? ncc = null) => new()
        {
            MaKho = "K01", LoaiPhieu = loai, MaNhaCungCap = ncc, LyDo = "Hàng lỗi", KhoaChongLap = khoa,
            Dong = [new() { LoO = $"LC|{CachLy}", SoLuong = soLuong }],
        };
        var (ma, kq) = await Voi("ND002", Vai.QuanLyKho, d => d.Phieu.TaoKhacAsync(Form(LoaiPhieuXuat.TraNhaCungCap, 30, "tra-1", "NCC01")));
        Assert.Equal(KetQuaGhi.DaGhi, kq);
        Assert.Equal((ma, KetQuaGhi.DaGhiTruocDo), await Voi("ND002", Vai.QuanLyKho, d => d.Phieu.TaoKhacAsync(Form(LoaiPhieuXuat.TraNhaCungCap, 30, "tra-1", "NCC01"))));
        Assert.Equal(20, (await Ton())["LC"].SoLuong);

        await Assert.ThrowsAsync<NghiepVuException>(() => Voi("ND002", Vai.QuanLyKho, d => d.Phieu.TaoKhacAsync(Form(LoaiPhieuXuat.TraNhaCungCap, 1, "tra-2"))));
        Assert.Contains("cần 21, còn 20", (await Assert.ThrowsAsync<NghiepVuException>(() => Voi("ND002", Vai.QuanLyKho, d => d.Phieu.TaoKhacAsync(Form(LoaiPhieuXuat.XuatKhac, 21, "khac-1"))))).Message);
        await Assert.ThrowsAsync<KhongCoQuyenException>(() => Voi("ND004", Vai.ThuKho, d => d.Phieu.TaoKhacAsync(Form(LoaiPhieuXuat.XuatKhac, 1, "khac-2"))));
        await Assert.ThrowsAsync<NghiepVuException>(() => Voi("ND002", Vai.QuanLyKho, d => d.Phieu.TaoKhacAsync(Form(LoaiPhieuXuat.XuatBan, 1, "khac-3"))));

        var p = await Voi("ND004", Vai.ThuKho, d => d.Phieu.LayAsync(ma));
        Assert.Equal((LoaiPhieuXuat.TraNhaCungCap, TrangThaiPhieu.HoanTat, 30m), (p.LoaiPhieu, p.TrangThai, p.Dong.Single().SoLuong));
    }

    [Fact]
    public async Task ManHinh_Api_PhamViThuKho()
    {
        var don = await DonDaDuyet(("AOTHUN-01", 2));
        var quanLy = _app.TaoClient(Vai.QuanLyKho, "ND002");
        Assert.Contains("Lập lệnh xuất", await quanLy.GetStringAsync($"/DonDat/{don}"));
        var r = await NguoiThu.PostFormAsync(quanLy, "/LenhXuat/Them", $"/DonDat/{don}", new() { ["maDonDat"] = don, ["nguoiThucHien"] = "ND004" });
        Assert.Equal(HttpStatusCode.Redirect, r.StatusCode);
        var lenh = r.Headers.Location!.ToString().Split('/').Last();

        var thuKho = _app.TaoClient(Vai.ThuKho, "ND004");
        Assert.Contains("man-hinh-quet", await thuKho.GetStringAsync($"/LenhXuat/{lenh}"));
        Assert.DoesNotContain(lenh, await _app.TaoClient(Vai.ThuKho, "ND005").GetStringAsync("/LenhXuat"));

        var token = await NguoiThu.TokenAsync(thuKho, $"/LenhXuat/{lenh}");
        var lay = new HttpRequestMessage(HttpMethod.Post, $"/api/lenh-xuat/{lenh}/lay") { Content = JsonContent.Create(new { maViTri = Thuong + "01", ma = "L1", soLuong = 2 }) };
        lay.Headers.Add("RequestVerificationToken", token);
        Assert.Equal(HttpStatusCode.Created, (await thuKho.SendAsync(lay)).StatusCode);
        Assert.Equal("ChoLayHang", (await thuKho.GetFromJsonAsync<JsonElement>($"/api/lenh-xuat/{lenh}")).GetProperty("trangThai").GetString());

        await DenChoXacNhan(lenh);
        var qlToken = await NguoiThu.TokenAsync(quanLy, $"/LenhXuat/{lenh}");
        var khongKhoa = new HttpRequestMessage(HttpMethod.Post, $"/api/lenh-xuat/{lenh}/xac-nhan");
        khongKhoa.Headers.Add("RequestVerificationToken", qlToken);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await quanLy.SendAsync(khongKhoa)).StatusCode);
        var xn = new HttpRequestMessage(HttpMethod.Post, $"/api/lenh-xuat/{lenh}/xac-nhan");
        xn.Headers.Add("RequestVerificationToken", qlToken);
        xn.Headers.Add("Idempotency-Key", "api-xn");
        Assert.Equal(HttpStatusCode.OK, (await quanLy.SendAsync(xn)).StatusCode);

        Assert.Contains($"LC|{CachLy}", await quanLy.GetStringAsync("/PhieuXuat/Them?maKho=K01"));
        Assert.Equal(HttpStatusCode.Forbidden, (await thuKho.GetAsync("/PhieuXuat/Them")).StatusCode);
    }
}
