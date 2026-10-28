using System.Net;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using quanlykho.Common;
using quanlykho.Common.Loi;
using quanlykho.Common.PhanQuyen;
using quanlykho.Domain;
using quanlykho.Features.NhapExcel;
using quanlykho.Features.NhapKho;
using quanlykho.Tests.HaTang;
using quanlykho.Tests.TepMau;

namespace quanlykho.Tests.Integration;

[Collection(CsdlCollection.Ten)]
public class NhapTonDauKyTests(CsdlFixture csdl) : IAsyncLifetime
{
    private static readonly FakeTimeProvider Gio = new(new DateTimeOffset(2026, 11, 1, 3, 0, 0, TimeSpan.Zero));
    private string _chuoi = "";

    public async Task InitializeAsync() => _chuoi = await csdl.TaoCsdlCoDuLieuThuAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<KetQuaTonDauKy> Nhap(byte[] tep, string maKho = "K01", string ma = "ND002", string vai = Vai.QuanLyKho)
    {
        await using var db = CsdlFixture.TaoDbContext(_chuoi);
        var h = NguoiThu.HienTai(db, ma, vai);
        return await new NhapTonDauKyService(db, h, new MaChungTu(db), NguoiThu.NhatKy(db, h), Gio).NhapAsync(maKho, new MemoryStream(tep), "tondauky.xlsx");
    }

    private async Task<T> PhieuNhap<T>(Func<PhieuNhapService, Task<T>> lam)
    {
        await using var db = CsdlFixture.TaoDbContext(_chuoi);
        var h = NguoiThu.HienTai(db, "ND002", Vai.QuanLyKho);
        var nk = NguoiThu.NhatKy(db, h);
        return await lam(new PhieuNhapService(db, h, new MaChungTu(db), nk, new DonMuaService(db, h, new MaChungTu(db), nk, Gio), new CongGhiTon(db, h, Gio), Gio));
    }

    [Fact]
    public async Task Tep300Dong_SinhPhieuLuuTam_XacNhanGhiDungTonVaoO_NhapLaiBiChan()
    {
        var tep = TepMauTonDauKy.Tao();
        var kq = await Nhap(tep);
        Assert.Empty(kq.Loi);
        Assert.Equal(300, kq.SoDong);

        await using (var db = CsdlFixture.TaoDbContext(_chuoi))
        {
            var p = await db.PhieuNhap.Include(x => x.ChiTiet).SingleAsync(x => x.MaPhieuNhap == kq.MaPhieu);
            Assert.Equal((LoaiPhieuNhap.TonDauKy, TrangThaiPhieu.LuuTam, "K01", 300), (p.LoaiPhieu, p.TrangThai, p.MaKho, p.ChiTiet.Count));
            Assert.Equal(0, await db.TonKho.CountAsync(t => t.MaLo.StartsWith("LTD")));
        }

        await PhieuNhap(s => s.XacNhanAsync(kq.MaPhieu!, "xn"));
        await using (var db = CsdlFixture.TaoDbContext(_chuoi))
        {
            var ton = await db.TonKho.Where(t => t.MaLo.StartsWith("LTD")).ToListAsync();
            Assert.Equal(300, ton.Count);
            Assert.All(ton, t => Assert.Equal(TepMauTonDauKy.O(int.Parse(t.MaLo[3..])), t.MaViTri));
            Assert.Equal(270 * 2 + 30 * 3, ton.Sum(t => t.SoLuong));
            Assert.Equal(new DateOnly(2027, 6, 30), await db.Lo.Where(l => l.MaLo == "LTD000").Select(l => l.HanDung).SingleAsync());
            Assert.Equal(630, await db.SoBienDong.Where(s => s.MaLo.StartsWith("LTD")).SumAsync(s => s.SoLuongThayDoi));
        }

        var lai = await Nhap(tep);
        Assert.Equal((Gio.GetUtcNow(), (string?)null), (lai.DaNhapLuc, lai.MaPhieu));
    }

    [Fact]
    public async Task DongVuotSucChua_VaLoiKhac_BaoDungDong_KhongSinhPhieu()
    {
        var kq = await Nhap(TepMauTonDauKy.Tao(vuotSucChua: true));
        var loi = Assert.Single(kq.Loi);
        Assert.Equal(TepMauTonDauKy.VuotSucChua, loi.Dong);
        Assert.Contains("chỉ còn xếp được tối đa", loi.NoiDung);
        Assert.Null(kq.MaPhieu);

        using var wb = new XLWorkbook(new MemoryStream(TepMauTonDauKy.Tao(tienToLo: "LTE")));
        var ws = wb.Worksheet(NhapTonDauKyService.Sheet);
        ws.Cell(2, 3).Value = "";                                // sữa thiếu hạn dùng
        ws.Cell(3, 4).Value = "K02-TA01-TG01-P02-H01-KE01-O01";   // ô kho khác
        ws.Cell(4, 7).Value = "Lỗi";                             // hàng lỗi ở ô lưu trữ
        ws.Cell(5, 1).Value = "SUATUOI-01";                      // sữa (lạnh) vào ô thường
        ws.Cell(5, 3).Value = "2027-06-30";
        ws.Cell(6, 2).Value = "LTE000";                          // trùng số lô với dòng 2 nhưng khác biến thể
        ws.Cell(7, 5).Value = "-1";
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        var nhieu = await Nhap(ms.ToArray());
        Assert.Equal([2, 3, 4, 5, 6, 7], nhieu.Loi.Select(l => l.Dong));
        Assert.Contains("hạn dùng", nhieu.Loi[0].NoiDung);
        Assert.Contains("không phải ô", nhieu.Loi[1].NoiDung);
        Assert.Contains("cách ly", nhieu.Loi[2].NoiDung);
        Assert.Contains("bảo quản", nhieu.Loi[3].NoiDung);
        Assert.Contains("Số lô LTE000", nhieu.Loi[4].NoiDung);

        await using var db = CsdlFixture.TaoDbContext(_chuoi);
        Assert.Equal(0, await db.PhieuNhap.CountAsync(p => p.LoaiPhieu == LoaiPhieuNhap.TonDauKy));
    }

    [Fact]
    public async Task HuyPhieuLuuTam_ChoNhapLaiCungTep_QuyenTheoKho()
    {
        var tep = TepMauTonDauKy.Tao();
        var kq = await Nhap(tep);
        await PhieuNhap(async s => { await s.HuyAsync(kq.MaPhieu!); return 0; });
        var lai = await Nhap(tep);
        Assert.NotNull(lai.MaPhieu);
        Assert.NotEqual(kq.MaPhieu, lai.MaPhieu);

        await Assert.ThrowsAsync<KhongCoQuyenException>(() => Nhap(tep, "K01", "ND003"));
        await Assert.ThrowsAsync<KhongCoQuyenException>(() => Nhap(tep, "K01", "ND004", Vai.ThuKho));

        await using var app = new UngDungFactory { ChuoiKetNoi = _chuoi, Gio = Gio };
        var ql = app.TaoClient(Vai.QuanLyKho, "ND002");
        Assert.Equal(HttpStatusCode.OK, (await ql.GetAsync("/NhapExcel/TonDauKy")).StatusCode);
        Assert.Equal(NhapExcelController.KieuXlsx, (await ql.GetAsync("/NhapExcel/TonDauKy/Mau")).Content.Headers.ContentType?.MediaType);
        Assert.Equal(HttpStatusCode.Forbidden, (await ql.GetAsync("/NhapExcel")).StatusCode);
    }

    [Fact]
    public void TepMau_GhiRaDia()
    {
        var thuMuc = Environment.GetEnvironmentVariable("GHI_TEP_MAU");
        if (string.IsNullOrEmpty(thuMuc))
            return;
        File.WriteAllBytes(Path.Combine(thuMuc, "TonDauKy_K01_300dong.xlsx"), TepMauTonDauKy.Tao());
        File.WriteAllBytes(Path.Combine(thuMuc, "TonDauKy_K01_300dong_vuotsucchua.xlsx"), TepMauTonDauKy.Tao(vuotSucChua: true));
    }
}
