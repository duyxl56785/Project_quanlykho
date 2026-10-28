using System.Net;
using System.Net.Http.Headers;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using quanlykho.Common.Loi;
using quanlykho.Common.PhanQuyen;
using quanlykho.Data;
using quanlykho.Features.NhapExcel;
using quanlykho.Tests.HaTang;
using quanlykho.Tests.TepMau;

namespace quanlykho.Tests.Integration;

[Collection(CsdlCollection.Ten)]
public class NhapExcelTests(CsdlFixture csdl) : IAsyncLifetime
{
    private static readonly FakeTimeProvider Gio = new(new DateTimeOffset(2026, 11, 30, 3, 0, 0, TimeSpan.Zero));
    private string _chuoi = "";

    public async Task InitializeAsync() => _chuoi = await csdl.TaoCsdlCoDuLieuThuAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<T> Voi<T>(Func<NhapExcelService, Task<T>> lam)
    {
        await using var db = CsdlFixture.TaoDbContext(_chuoi);
        var h = NguoiThu.HienTai(db, "ND001", Vai.QuanTri);
        return await lam(new NhapExcelService(db, h, NguoiThu.NhatKy(db, h), Gio));
    }

    private Task<KetQuaKiemTra> KiemTra(byte[] tep, string ten = "danhmuc.xlsx") => Voi(s => s.KiemTraAsync(new MemoryStream(tep), ten));

    private async Task<(int SanPham, int BienThe, int NhaCungCap)> SoDaNhap()
    {
        await using var db = CsdlFixture.TaoDbContext(_chuoi);
        return (await db.SanPham.CountAsync(s => s.MaSanPham.StartsWith("SPX")), await db.BienThe.CountAsync(b => b.MaBienThe.StartsWith("SPX")),
            await db.NhaCungCap.CountAsync(n => n.MaNhaCungCap.StartsWith("NCCX")));
    }

    [Fact]
    public async Task TepMau300Dong_NamDongLoi_BaoDungDong_KhongGhiGi()
    {
        var kq = await KiemTra(TepMauDanhMuc.Tao(coLoi: true));

        Assert.Equal(TepMauDanhMuc.DongLoi, kq.Loi.Select(l => (l.Sheet, l.Dong)));
        Assert.Contains("Mã nhóm KHONGCO không có", kq.Loi[0].NoiDung);
        Assert.Contains("Thiếu Đơn vị tính", kq.Loi[1].NoiDung);
        Assert.Contains("không phải số", kq.Loi[2].NoiDung);
        Assert.Contains("trùng một dòng khác", kq.Loi[3].NoiDung);
        Assert.Contains("đã có trong hệ thống", kq.Loi[4].NoiDung);
        Assert.False(kq.GhiDuoc);
        await Assert.ThrowsAsync<NghiepVuException>(() => Voi(s => s.GhiAsync(kq.MaBam, "danhmuc.xlsx")));
        Assert.Equal((0, 0, 0), await SoDaNhap());
    }

    [Fact]
    public async Task SuaXong_GhiDu_NhapLaiCungTepBiChan()
    {
        var tep = TepMauDanhMuc.Tao(coLoi: false);
        var kq = await KiemTra(tep);
        Assert.Empty(kq.Loi);
        Assert.Equal((40, 240, 20, 300), (kq.SoSanPham, kq.SoBienThe, kq.SoNhaCungCap, kq.SoDong));
        Assert.Equal("SPX001-01 – SPX001 S Trắng", kq.XemTruoc[NhapExcelService.SheetBienThe][0]);
        Assert.True(kq.GhiDuoc);

        await Voi(s => s.GhiAsync(kq.MaBam, "danhmuc.xlsx"));
        Assert.Equal((40, 240, 20), await SoDaNhap());
        await using (var db = CsdlFixture.TaoDbContext(_chuoi))
        {
            var bt = await db.BienThe.Include(b => b.SanPham).SingleAsync(b => b.MaBienThe == "SPX002-04");
            Assert.Equal(("M", "Đen", 0.2m, "TT"), (bt.ThuocTinh["Size"], bt.ThuocTinh["Màu"], bt.KhoiLuong, bt.SanPham.MaNhom));
            Assert.Equal(("danhmuc.xlsx", 300, "ND001"), (await db.TepDaNhap.SingleAsync() is var t ? (t.TenTep, t.SoDong, t.NguoiNhap) : default));
            Assert.Equal(1, await db.NhatKy.CountAsync(n => n.LoaiThaoTac == "NhapExcelDanhMuc"));
        }

        // Cùng tệp (cùng mã băm), kể cả đổi tên, bị chặn.
        var lai = await KiemTra(tep, "doiten.xlsx");
        Assert.Equal(Gio.GetUtcNow(), lai.DaNhapLuc);
        Assert.False(lai.GhiDuoc);
        await Assert.ThrowsAsync<NghiepVuException>(() => Voi(s => s.GhiAsync(lai.MaBam, "doiten.xlsx")));

        // Tệp khác mà trùng mã đã nhập thì báo lỗi từng dòng.
        var khac = await KiemTra(TepMauDanhMuc.Tao(coLoi: true));
        Assert.Equal(300, khac.Loi.Select(l => (l.Sheet, l.Dong)).Distinct().Count());
        Assert.Equal((40, 240, 20), await SoDaNhap());
    }

    [Fact]
    public async Task TepSaiMau_TepMauRong_TepHong()
    {
        Assert.Contains("Không đọc được tệp", (await Assert.ThrowsAsync<NghiepVuException>(() => KiemTra([1, 2, 3]))).Message);

        var mau = await Voi(s => s.TaoMauAsync());
        Assert.Equal("Tệp không có dòng dữ liệu nào.", Assert.Single((await KiemTra(mau)).Loi).NoiDung);

        using var wb = new XLWorkbook(new MemoryStream(mau));
        wb.Worksheet(NhapExcelService.SheetNhaCungCap).Delete();
        wb.Worksheet(NhapExcelService.SheetBienThe).Cell(1, 4).Value = "Chiều dài";
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        var loi = (await KiemTra(ms.ToArray())).Loi.Select(l => l.NoiDung).ToList();
        Assert.Contains("Thiếu cột Dài (m).", loi);
        Assert.Contains("Thiếu sheet NhaCungCap; hãy dùng tệp mẫu.", loi);
    }

    [Fact]
    public async Task ManHinh_TaiMauTaiLen_ChiQuanTri()
    {
        await using var app = new UngDungFactory { ChuoiKetNoi = _chuoi, Gio = Gio };
        var admin = app.TaoClient(Vai.QuanTri, "ND001");
        var mau = await admin.GetAsync("/NhapExcel/Mau");
        Assert.Equal(NhapExcelController.KieuXlsx, mau.Content.Headers.ContentType?.MediaType);

        var noiDung = new MultipartFormDataContent
        {
            { new StringContent(await NguoiThu.TokenAsync(admin, "/NhapExcel")), "__RequestVerificationToken" },
        };
        var tep = new ByteArrayContent(TepMauDanhMuc.Tao(coLoi: true));
        tep.Headers.ContentType = new MediaTypeHeaderValue(NhapExcelController.KieuXlsx);
        noiDung.Add(tep, "tep", "loi.xlsx");
        var html = await (await admin.PostAsync("/NhapExcel/KiemTra", noiDung)).Content.ReadAsStringAsync();
        Assert.Contains("5 lỗi", html);
        Assert.DoesNotContain("Ghi vào hệ thống", html);

        Assert.Equal(HttpStatusCode.Forbidden, (await app.TaoClient(Vai.QuanLyKho, "ND002").GetAsync("/NhapExcel")).StatusCode);
    }

    [Fact]
    public void TepMau_GhiRaDia()
    {
        var thuMuc = Environment.GetEnvironmentVariable("GHI_TEP_MAU");
        if (string.IsNullOrEmpty(thuMuc))
            return;
        File.WriteAllBytes(Path.Combine(thuMuc, "DanhMuc_300dong_5loi.xlsx"), TepMauDanhMuc.Tao(coLoi: true));
        File.WriteAllBytes(Path.Combine(thuMuc, "DanhMuc_300dong.xlsx"), TepMauDanhMuc.Tao(coLoi: false));
    }
}
