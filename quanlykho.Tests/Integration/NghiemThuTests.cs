using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using quanlykho.Common;
using quanlykho.Common.PhanQuyen;
using quanlykho.Data;
using quanlykho.Domain;
using quanlykho.Features.BaoCao;
using quanlykho.Features.DanhMuc;
using quanlykho.Features.NhapKho;
using quanlykho.Features.XepHang;
using quanlykho.Features.XuatKho;
using quanlykho.Tests.HaTang;

namespace quanlykho.Tests.Integration;

/// <summary>Kịch bản nghiệm thu Bảng 1.6 chưa có test riêng ở các mảng.</summary>
[Collection(CsdlCollection.Ten)]
public class NghiemThuTests(CsdlFixture csdl) : IAsyncLifetime
{
    private static readonly FakeTimeProvider Gio = new(new DateTimeOffset(2026, 11, 1, 3, 0, 0, TimeSpan.Zero));
    private const string TiepNhan = "K01-TA01-TG01-P01-H01-KE01-O01";
    private const string O = "K01-TA01-TG01-P02-H01-KE01-O01";
    private string _chuoi = "";

    public async Task InitializeAsync() => _chuoi = await csdl.TaoCsdlCoDuLieuThuAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<T> Voi<T>(string ma, string vai, Func<AppDbContext, NguoiDungHienTai, Task<T>> lam)
    {
        await using var db = CsdlFixture.TaoDbContext(_chuoi);
        return await lam(db, NguoiThu.HienTai(db, ma, vai));
    }

    [Fact]
    public async Task QuanTri_ThemNganhMoiKhongSuaMa_RoiNhapXepXuatDuoc()
    {
        // Quản trị viên khai báo ngành, thuộc tính, sản phẩm, sinh biến thể trên giao diện (qua Service).
        await Voi("ND001", Vai.QuanTri, async (db, h) =>
        {
            var dm = new DanhMucService(db, NguoiThu.NhatKy(db, h), new QuyDoiDonVi(db));
            await dm.ThemNhomAsync(new NhomForm { Ma = "GD", Ten = "Đồ gia dụng" });
            await dm.LuuThuocTinhAsync("GD", "Công suất", "1000W, 1500W");
            await dm.LuuThuocTinhAsync("GD", "Bảo hành", "12 tháng");
            await dm.ThemSanPhamAsync(new SanPhamForm { Ma = "NOICOM", Ten = "Nồi cơm điện", DonViTinh = "chiếc", MaNhom = "GD", DieuKienBaoQuan = DieuKienBaoQuan.Thuong });
            var (moi, _) = await dm.SinhBienTheAsync("NOICOM", new SinhBienTheForm
            {
                GiaTri = new() { ["Công suất"] = "1000W, 1500W", ["Bảo hành"] = "12 tháng" }, Dai = 0.35m, Rong = 0.35m, Cao = 0.3m, KhoiLuong = 3.5m,
            });
            Assert.Equal(2, moi);
            return 0;
        });

        // Quản lý kho nhập 10 chiếc NOICOM-01, thủ kho xếp vào ô, quản lý kho xuất khác 4 chiếc.
        var maLo = await Voi("ND002", Vai.QuanLyKho, async (db, h) =>
        {
            var nk = NguoiThu.NhatKy(db, h);
            var pn = new PhieuNhapService(db, h, new MaChungTu(db), nk, new DonMuaService(db, h, new MaChungTu(db), nk, Gio), new CongGhiTon(db, h, Gio), Gio);
            var ma = await pn.TaoAsync(new PhieuNhapForm { LoaiPhieu = LoaiPhieuNhap.NhapKhac, MaKho = "K01" });
            var (_, lo) = await pn.ThemDongAsync(ma, new DongPhieuNhapForm("NOICOM-01", 10, null, ChatLuong.Dat, 650000));
            await pn.XacNhanAsync(ma, "nt-nhap");
            return lo;
        });
        await Voi("ND004", Vai.ThuKho, (db, h) => new XepHangService(db, h, new MaChungTu(db), new CongGhiTon(db, h, Gio), Gio)
            .XepAsync(new XepHangYeuCau(maLo, TiepNhan, O, 10), "nt-xep"));
        await Voi("ND002", Vai.QuanLyKho, (db, h) => new PhieuXuatService(db, h, new MaChungTu(db), new CongGhiTon(db, h, Gio), NguoiThu.NhatKy(db, h), Gio)
            .TaoKhacAsync(new PhieuXuatForm { MaKho = "K01", LoaiPhieu = LoaiPhieuXuat.XuatKhac, LyDo = "Trưng bày", KhoaChongLap = "nt-xuat",
                Dong = [new DongPhieuXuatForm { LoO = $"{maLo}|{O}", SoLuong = 4 }] }));

        var kd = await Voi("ND007", Vai.BanHang, (db, h) => new TonKhaDung(db, h, Gio).TraCuuAsync("NOICOM", null, false, 1));
        Assert.Equal([("NOICOM-01", 6m), ("NOICOM-02", 0m)], kd.Muc.Select(d => (d.MaBienThe, d.Tong)));
        await using var kt = CsdlFixture.TaoDbContext(_chuoi);
        Assert.Equal(6, await kt.SoBienDong.Where(s => s.MaLo == maLo).SumAsync(s => s.SoLuongThayDoi));
    }

    [Fact]
    public async Task ThongKeTyLeChapNhanGoiYAi()
    {
        await using (var db = CsdlFixture.TaoDbContext(_chuoi))
        {
            LichSuGoiY Ls(LoaiGoiY loai, string kho, bool? chapNhan) => new()
            {
                Loai = loai, MaKho = kho, ThoiDiem = Gio.GetUtcNow(), NguoiDung = "ND002", DauVao = "{}", KetQua = "{}", ChapNhan = chapNhan,
            };
            db.LichSuGoiY.AddRange(Ls(LoaiGoiY.LayHang, "K01", true), Ls(LoaiGoiY.LayHang, "K01", true), Ls(LoaiGoiY.LayHang, "K01", false), Ls(LoaiGoiY.LayHang, "K01", null),
                Ls(LoaiGoiY.NhapBoSung, "K01", true), Ls(LoaiGoiY.NhapBoSung, "K02", false));
            await db.SaveChangesAsync();
        }

        var k01 = await Voi("ND002", Vai.QuanLyKho, (db, h) => new BaoCaoService(db, h, new TonKhaDung(db, h, Gio)).GoiYAsync(new LocBaoCao()));
        Assert.Equal([(LoaiGoiY.LayHang, 4, 3, 2, (decimal?)66.7m), (LoaiGoiY.NhapBoSung, 1, 1, 1, 100m)], k01.Dong.Select(d => (d.Loai, d.Tong, d.CoPhanHoi, d.ChapNhan, d.TyLe)));
        var tatCa = await Voi("ND001", Vai.QuanTri, (db, h) => new BaoCaoService(db, h, new TonKhaDung(db, h, Gio)).GoiYAsync(new LocBaoCao()));
        Assert.Equal(50m, tatCa.Dong.Single(d => d.Loai == LoaiGoiY.NhapBoSung).TyLe);
        var truoc = await Voi("ND001", Vai.QuanTri, (db, h) => new BaoCaoService(db, h, new TonKhaDung(db, h, Gio)).GoiYAsync(new LocBaoCao { DenNgay = new(2026, 10, 31) }));
        Assert.All(truoc.Dong, d => Assert.Null(d.TyLe));

        await using var app = new UngDungFactory { ChuoiKetNoi = _chuoi, Gio = Gio };
        var ql = app.TaoClient(Vai.QuanLyKho, "ND002");
        Assert.Contains("66,7 %", await ql.GetStringAsync("/BaoCao/GoiY"));
        Assert.Equal(HttpStatusCode.OK, (await ql.GetAsync("/api/bao-cao/goi-y")).StatusCode);
    }
}
