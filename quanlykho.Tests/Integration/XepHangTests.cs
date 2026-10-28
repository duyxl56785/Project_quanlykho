using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using quanlykho.Common;
using quanlykho.Common.Loi;
using quanlykho.Common.PhanQuyen;
using quanlykho.Data;
using quanlykho.Data.Seed;
using quanlykho.Domain;
using quanlykho.Features.Kho;
using quanlykho.Features.NhapKho;
using quanlykho.Features.XepHang;
using quanlykho.Tests.HaTang;

namespace quanlykho.Tests.Integration;

[Collection(CsdlCollection.Ten)]
public class XepHangTests(CsdlFixture csdl) : IAsyncLifetime
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
        db.Lo.Add(new Lo { MaLo = "LT1", MaBienThe = "AOTHUN-01", NgayNhap = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync();
        await using var tx = await db.Database.BeginTransactionAsync();
        await new CongGhiTon(db, NguoiThu.HienTai(db, "ND002", Vai.QuanLyKho), TimeProvider.System).GhiAsync("thu", "Thu", "T1", [new("LT1", TiepNhan, 600)]);
        await tx.CommitAsync();
    }

    public async Task DisposeAsync() => await _app.DisposeAsync();

    private static XepHangService Service(AppDbContext db, string ma = "ND004", string vai = Vai.ThuKho)
    {
        var hienTai = NguoiThu.HienTai(db, ma, vai);
        return new XepHangService(db, hienTai, new MaChungTu(db), new CongGhiTon(db, hienTai, TimeProvider.System), TimeProvider.System);
    }

    private async Task<KetQuaXep> Xep(string o, decimal soLuong, string khoa, string di = TiepNhan, string ma = "ND004", string vai = Vai.ThuKho)
    {
        await using var db = CsdlFixture.TaoDbContext(_chuoi);
        return await Service(db, ma, vai).XepAsync(new XepHangYeuCau("LT1", di, o, soLuong), khoa);
    }

    private async Task<Dictionary<string, decimal>> Ton()
    {
        await using var db = CsdlFixture.TaoDbContext(_chuoi);
        return await db.TonKho.Where(t => t.MaLo == "LT1" && t.SoLuong > 0).ToDictionaryAsync(t => t.MaViTri, t => t.SoLuong);
    }

    [Fact]
    public async Task XepDung_GhiChuyenViTri_TongTonKhongDoi_BamLapKhongGhiHaiLan()
    {
        var kq = await Xep(Ke + "-O01", 100, "k1");
        Assert.Equal(new KetQuaXep("CV000001", false), kq);
        Assert.Equal(new KetQuaXep("CV000001", true), await Xep(Ke + "-O01", 100, "k1"));

        var ton = await Ton();
        Assert.Equal((500m, 100m, 600m), (ton[TiepNhan], ton[Ke + "-O01"], ton.Values.Sum()));
        await using var db = CsdlFixture.TaoDbContext(_chuoi);
        var cv = await db.ChuyenViTri.SingleAsync();
        Assert.Equal((LoaiChuyenViTri.XepHang, "K01", TiepNhan, Ke + "-O01", 100m, "ND004"), (cv.Loai, cv.MaKho, cv.MaViTriDi, cv.MaViTriDen, cv.SoLuong, cv.NguoiThucHien));
        Assert.Equal(0, await db.SoBienDong.Where(s => s.MaChungTu == "CV000001").SumAsync(s => s.SoLuongThayDoi));

        await using var db2 = CsdlFixture.TaoDbContext(_chuoi);
        var cho = await Service(db2).ChoXepAsync("K01");
        Assert.Equal(("LT1", 500m), (cho.Single().MaLo, cho.Single().SoLuong));
    }

    [Fact]
    public async Task ChanTheoBang233_VaKhuVaKhoa()
    {
        var loi = await Assert.ThrowsAsync<NghiepVuException>(() => Xep(Ke + "-O02", 529, "a"));
        Assert.Contains("tối đa 528", loi.Message);
        Assert.Contains("bảo quản", (await Assert.ThrowsAsync<NghiepVuException>(() => Xep("K01-TA01-TG01-P03-H01-KE01-O01", 1, "b"))).Message);
        Assert.Contains("khu lưu trữ", (await Assert.ThrowsAsync<NghiepVuException>(() => Xep("K01-TA01-TG01-P04-H01-KE01-O01", 1, "c"))).Message);
        Assert.Contains("khu tiếp nhận", (await Assert.ThrowsAsync<NghiepVuException>(() => Xep(Ke + "-O03", 1, "d", di: "K01-TA01-TG01-P04-H01-KE01-O01"))).Message);
        await Assert.ThrowsAsync<NghiepVuException>(() => Xep(Ke + "-O02", 1, ""));
        await Assert.ThrowsAsync<KhongCoQuyenException>(() => Xep(Ke + "-O02", 1, "e", ma: "ND006"));

        await using (var db = CsdlFixture.TaoDbContext(_chuoi))
            await db.ViTri.Where(v => v.MaViTri == Ke + "-O03").ExecuteUpdateAsync(s => s.SetProperty(v => v.DangKhoa, true));
        Assert.Contains("đang khóa", (await Assert.ThrowsAsync<NghiepVuException>(() => Xep(Ke + "-O03", 1, "f"))).Message);
        Assert.Equal(600, (await Ton())[TiepNhan]);
    }

    [Fact]
    public async Task OTrongTayNguoiKhac_Tra409()
    {
        await using var giu = CsdlFixture.TaoDbContext(_chuoi);
        await using var tx = await giu.Database.BeginTransactionAsync();
        await giu.Database.ExecuteSqlAsync($"""SELECT 1 FROM "ViTri" WHERE "MaViTri" = {Ke + "-O04"} FOR UPDATE""");

        var loi = await Assert.ThrowsAsync<XungDotException>(() => Xep(Ke + "-O04", 10, "g"));
        Assert.Equal("Ô vừa được cập nhật, vui lòng thử lại.", loi.Message);
    }

    [Fact]
    public async Task HaiNguoiCungXepVuotSucChua_ChiMotThanhCong()
    {
        var kq = await Task.WhenAll(new[] { "p1", "p2" }.Select(async k =>
        {
            try { await Xep(Ke + "-O05", 300, k); return "ok"; }
            catch (XungDotException) { return "409"; }
            catch (NghiepVuException) { return "422"; }
        }));
        Assert.Single(kq, "ok");
        var ton = await Ton();
        Assert.Equal((300m, 600m), (ton[Ke + "-O05"], ton.Values.Sum()));
    }

    [Fact]
    public async Task Api_VaManHinh()
    {
        var thuKho = _app.TaoClient(Vai.ThuKho, "ND004");
        var html = await thuKho.GetStringAsync("/XepHang?maKho=K01");
        Assert.Contains("data-lo=\"LT1\"", html);

        var token = await NguoiThu.TokenAsync(thuKho, "/XepHang");
        HttpRequestMessage Yc(string? khoa, decimal soLuong)
        {
            var r = new HttpRequestMessage(HttpMethod.Post, "/api/xep-hang")
            {
                Content = JsonContent.Create(new { maLo = "LT1", maViTriDi = TiepNhan, maViTri = Ke + "-O06", soLuong }),
            };
            r.Headers.Add("RequestVerificationToken", token);
            if (khoa is not null) r.Headers.Add("Idempotency-Key", khoa);
            return r;
        }
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await thuKho.SendAsync(Yc(null, 5))).StatusCode);
        var vuot = await thuKho.SendAsync(Yc("api-1", 1000));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, vuot.StatusCode);
        Assert.Contains("\"qMax\":528", await vuot.Content.ReadAsStringAsync());
        var tra = await thuKho.SendAsync(Yc("api-2", 5));
        Assert.Equal(HttpStatusCode.OK, tra.StatusCode);
        Assert.Contains("CV0000", await tra.Content.ReadAsStringAsync());

        var khac = _app.TaoClient(Vai.ThuKho, "ND006");
        Assert.Equal(HttpStatusCode.Forbidden, (await khac.GetAsync("/api/xep-hang/cho-xep?maKho=K01")).StatusCode);
    }

    [Fact]
    public async Task ChuyenTrongKho_TonKhongDoi_CapTruCong_KhongChuyenPhanDangGiu()
    {
        await Xep(Ke + "-O01", 100, "k1");
        await using (var db = CsdlFixture.TaoDbContext(_chuoi))
        {
            await using var tx = await db.Database.BeginTransactionAsync();
            await new CongGhiTon(db, NguoiThu.HienTai(db, "ND002", Vai.QuanLyKho), TimeProvider.System).GiuAsync([new DongGiu("LT1", Ke + "-O01", 30)]);
            await tx.CommitAsync();
        }
        await using (var db = CsdlFixture.TaoDbContext(_chuoi))
            Assert.Equal(("LT1", 70m), (await Service(db).HangTaiOAsync("K01", Ke + "-O01")).Select(h => (h.MaLo, h.SoLuong)).Single());

        Assert.Contains("cần 80, còn 70", (await Assert.ThrowsAsync<NghiepVuException>(() => Xep(Ke + "-O02", 80, "m1", di: Ke + "-O01"))).Message);
        Assert.Contains("bảo quản", (await Assert.ThrowsAsync<NghiepVuException>(() => Xep("K01-TA01-TG01-P03-H01-KE01-O01", 1, "m2", di: Ke + "-O01"))).Message);
        var kq = await Xep(Ke + "-O02", 50, "m3", di: Ke + "-O01");
        Assert.Equal(new KetQuaXep(kq.MaChuyen, true), await Xep(Ke + "-O02", 50, "m3", di: Ke + "-O01"));

        var ton = await Ton();
        Assert.Equal((500m, 50m, 50m, 600m), (ton[TiepNhan], ton[Ke + "-O01"], ton[Ke + "-O02"], ton.Values.Sum()));
        await using var kt = CsdlFixture.TaoDbContext(_chuoi);
        Assert.Equal(30, await kt.TonKho.Where(t => t.MaLo == "LT1" && t.MaViTri == Ke + "-O01").Select(t => t.SoLuongDaGiu).SingleAsync());
        var cv = await kt.ChuyenViTri.SingleAsync(c => c.MaChuyen == kq.MaChuyen);
        Assert.Equal((LoaiChuyenViTri.ChuyenTrongKho, Ke + "-O01", Ke + "-O02"), (cv.Loai, cv.MaViTriDi, cv.MaViTriDen));
        var so = await kt.SoBienDong.Where(s => s.MaChungTu == kq.MaChuyen).OrderBy(s => s.SoLuongThayDoi).Select(s => new { s.LoaiChungTu, s.MaViTri, s.SoLuongThayDoi }).ToListAsync();
        Assert.Equal([(XepHangService.LoaiChuyen, Ke + "-O01", -50m), (XepHangService.LoaiChuyen, Ke + "-O02", 50m)], so.Select(s => (s.LoaiChungTu, s.MaViTri, s.SoLuongThayDoi)));

        var thuKho = _app.TaoClient(Vai.ThuKho, "ND004");
        Assert.Contains("data-lo=\"LT1\"", await thuKho.GetStringAsync($"/XepHang/ChuyenViTri?maKho=K01&maViTri={Ke}-O02"));
        Assert.Contains("không có hàng chuyển được", await thuKho.GetStringAsync($"/XepHang/ChuyenViTri?maKho=K01&maViTri={TiepNhan}"));
    }

    [Fact]
    public async Task PhieuNhapMau_MoiKhoMotPhieuDaXep_ChayLaiKhongNhanDoi()
    {
        var chuoi = await csdl.TaoCsdlCoDuLieuThuAsync();
        for (var lan = 0; lan < 2; lan++)
        {
            await using var db = CsdlFixture.TaoDbContext(chuoi);
            var http = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };
            var hienTai = new NguoiDungHienTai(http, db);
            var nhatKy = NguoiThu.NhatKy(db, hienTai);
            var cong = new CongGhiTon(db, hienTai, TimeProvider.System);
            await new PhieuNhapMau(db, http,
                new PhieuNhapService(db, hienTai, new MaChungTu(db), nhatKy, new DonMuaService(db, hienTai, new MaChungTu(db), nhatKy, TimeProvider.System), cong, TimeProvider.System),
                new XepHangService(db, hienTai, new MaChungTu(db), cong, TimeProvider.System), new OPhuHop(db, hienTai), TimeProvider.System).NapAsync();
        }

        await using var kt = CsdlFixture.TaoDbContext(chuoi);
        Assert.Equal(["K01", "K02"], await kt.PhieuNhap.Where(p => p.GhiChu == PhieuNhapMau.GhiChu && p.TrangThai == TrangThaiPhieu.HoanTat).Select(p => p.MaKho).OrderBy(k => k).ToListAsync());
        Assert.Equal(4, await kt.ChuyenViTri.CountAsync());
        var ton = await kt.TonKho.Where(t => t.SoLuong > 0).Join(kt.ViTri, t => t.MaViTri, v => v.MaViTri, (t, v) => new { v.LoaiKhu, t.SoLuong }).ToListAsync();
        Assert.Equal(4, ton.Count);
        Assert.All(ton, t => Assert.Equal(LoaiKhu.LuuTru, t.LoaiKhu));
    }
}
