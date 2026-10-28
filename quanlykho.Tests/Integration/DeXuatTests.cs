using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Time.Testing;
using quanlykho.Common;
using quanlykho.Common.Loi;
using quanlykho.Common.PhanQuyen;
using quanlykho.Data;
using quanlykho.Domain;
using quanlykho.Features.NhapKho;
using quanlykho.Features.XuatKho;
using quanlykho.Tests.HaTang;

namespace quanlykho.Tests.Integration;

[Collection(CsdlCollection.Ten)]
public class DeXuatTests(CsdlFixture csdl) : IAsyncLifetime
{
    // 10:00 ngày 30/11/2026 giờ Việt Nam.
    private static readonly FakeTimeProvider Gio = new(new DateTimeOffset(2026, 11, 30, 3, 0, 0, TimeSpan.Zero));
    private const string OLanh = "K01-TA01-TG01-P03-H01-KE01-O01";
    private const string OThuong = "K01-TA01-TG01-P02-H01-KE01-O01";
    private string _chuoi = "";

    /// <summary>
    /// Sữa K01 (m 50, L 3): khả dụng 20, xuất bán 300 trong 30 ngày (lần đầu cách 29 ngày) nên d = 10, điểm đặt 80;
    /// đơn mua NCC03 còn chờ 10; xuất khác 1000 không tính. Tính tay: ceil(10 × 33 − 20 − 10) = 300.
    /// Áo K01: xuất bán từ 9 ngày trước (N = 10) nên thiếu dữ liệu.
    /// </summary>
    public async Task InitializeAsync()
    {
        _chuoi = await csdl.TaoCsdlCoDuLieuThuAsync();
        await using var db = CsdlFixture.TaoDbContext(_chuoi);
        var bayGio = Gio.GetUtcNow();
        db.Lo.AddRange(new Lo { MaLo = "LSUA", MaBienThe = "SUATUOI-01", HanDung = new DateOnly(2027, 1, 1), NgayNhap = bayGio.AddDays(-40) },
            new Lo { MaLo = "LAO", MaBienThe = "AOTHUN-01", NgayNhap = bayGio.AddDays(-40) });
        db.DonDatHang.Add(new DonDatHang { MaDonDat = "DDT", MaKho = "K01", MaNoiNhan = "NN01", NguoiTao = "ND007", NgayTao = bayGio.AddDays(-30), TrangThai = TrangThaiDonDat.HoanThanh });
        await db.SaveChangesAsync();
        db.LenhXuat.Add(new LenhXuat { MaLenh = "LXT", MaDonDat = "DDT", MaKho = "K01", NguoiLap = "ND002", NgayLap = bayGio.AddDays(-30), NguoiThucHien = "ND004", TrangThai = TrangThaiLenhXuat.HoanThanh });
        db.DonMua.Add(new DonMua
        {
            MaDonMua = "DMT", MaNhaCungCap = "NCC03", MaKho = "K01", NguoiTao = "ND002", NgayTao = bayGio.AddDays(-5), TrangThai = TrangThaiDonMua.ChuaThucHien,
            ChiTiet = [new ChiTietDonMua { MaBienThe = "SUATUOI-01", TenDonVi = "hộp", SoLuongDat = 10, DonGia = 32000 }],
        });
        var stt = 0;
        void Xuat(LoaiPhieuXuat loai, string lo, decimal soLuong, int ngayTruoc) => db.PhieuXuat.Add(new PhieuXuat
        {
            MaPhieuXuat = $"PXT{++stt}", LoaiPhieu = loai, MaLenh = loai == LoaiPhieuXuat.XuatBan ? "LXT" : null, MaKho = "K01", NguoiLap = "ND002",
            NgayXuat = bayGio.AddDays(-ngayTruoc), TrangThai = TrangThaiPhieu.HoanTat,
            ChiTiet = [new ChiTietPhieuXuat { STT = 1, MaLo = lo, MaViTri = OLanh, TenDonVi = "hộp", SoLuongGoc = soLuong, SoLuong = soLuong }],
        });
        Xuat(LoaiPhieuXuat.XuatBan, "LSUA", 100, 29);
        Xuat(LoaiPhieuXuat.XuatBan, "LSUA", 150, 10);
        Xuat(LoaiPhieuXuat.XuatBan, "LSUA", 50, 0);
        Xuat(LoaiPhieuXuat.XuatKhac, "LSUA", 1000, 3);
        Xuat(LoaiPhieuXuat.XuatBan, "LAO", 40, 9);
        await db.SaveChangesAsync();

        await using var tx = await db.Database.BeginTransactionAsync();
        await new CongGhiTon(db, NguoiThu.HienTai(db, "ND001", Vai.QuanTri), Gio).GhiAsync("nap", "Thu", "T1",
            [new DongGhiTon("LSUA", OLanh, 20), new DongGhiTon("LAO", OThuong, 100)]);
        await tx.CommitAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private static DeXuatService DeXuat(AppDbContext db, string ma, string vai)
    {
        var h = NguoiThu.HienTai(db, ma, vai);
        return new DeXuatService(db, h, new TonKhaDung(db, h, Gio), DonMua(db, h), new ConfigurationBuilder().Build(), Gio);
    }

    private static DonMuaService DonMua(AppDbContext db, NguoiDungHienTai h) => new(db, h, new MaChungTu(db), NguoiThu.NhatKy(db, h), Gio);

    private async Task<T> Voi<T>(Func<DeXuatService, Task<T>> lam, string ma = "ND002", string vai = Vai.QuanLyKho)
    {
        await using var db = CsdlFixture.TaoDbContext(_chuoi);
        return await lam(DeXuat(db, ma, vai));
    }

    private static DeXuatForm ChonSua(decimal soLuong = 300, string? ncc = "NCC03", string? lyDo = null) => new()
    {
        MaKho = "K01", LyDoBoQua = lyDo, Dong = [new DongChonG3 { Chon = true, MaBienThe = "SUATUOI-01", SoLuong = soLuong, MaNhaCungCap = ncc }],
    };

    [Fact]
    public async Task TinhTheoKho_KhopTinhTay()
    {
        var ds = await Voi(s => s.TinhAsync("K01"));

        var sua = ds.Single(d => d.DauVao.MaBienThe == "SUATUOI-01");
        Assert.Equal(KetLuanG3.DeXuat, sua.KetQua.KetLuan);
        Assert.Equal(new HangG3("SUATUOI-01", 20, new DateOnly(2026, 11, 1), 300, 50, 3, 10), sua.DauVao);
        Assert.Equal(10, sua.KetQua.NhuCau);
        Assert.Equal(80, sua.KetQua.DiemDatHang);
        Assert.Equal(300, sua.KetQua.SoDeXuat);
        Assert.Equal(("NCC03", 32000m), (sua.MaNhaCungCap, sua.DonGia));
        Assert.Same(sua, ds[0]);

        Assert.Equal(KetLuanG3.ThieuDuLieu, ds.Single(d => d.DauVao.MaBienThe == "AOTHUN-01").KetQua.KetLuan);
        // K02 có mức tồn nhưng chưa có lịch sử xuất.
        var k02 = await Voi(s => s.TinhAsync("K02"), "ND003");
        Assert.NotEmpty(k02);
        Assert.All(k02, d => Assert.Equal(KetLuanG3.ThieuDuLieu, d.KetQua.KetLuan));
    }

    [Fact]
    public async Task TaoDonNhap_XacNhanLaChapNhan_BamLapBiChan()
    {
        var ma = Assert.Single(await Voi(s => s.TaoDonNhapAsync(ChonSua(280))));

        await using (var db = CsdlFixture.TaoDbContext(_chuoi))
        {
            var don = await db.DonMua.Include(d => d.ChiTiet).SingleAsync(d => d.MaDonMua == ma);
            Assert.Equal((TrangThaiDonMua.Nhap, "NCC03", "K01", DeXuatService.GhiChuDon), (don.TrangThai, don.MaNhaCungCap, don.MaKho, don.GhiChu));
            Assert.Equal(("SUATUOI-01", 280m, 32000m), (don.ChiTiet[0].MaBienThe, don.ChiTiet[0].SoLuongDat, don.ChiTiet[0].DonGia));
            var ls = await db.LichSuGoiY.SingleAsync(l => l.Loai == LoaiGoiY.NhapBoSung);
            Assert.Equal((ma, (bool?)null, "ND002"), (ls.MaChungTu, ls.ChapNhan, ls.NguoiDung));
        }

        // Nháp đã vào số chờ nhập: còn ceil(330 − 20 − 10 − 280) = 20.
        Assert.Equal(20, (await Voi(s => s.TinhAsync("K01"))).Single(d => d.DauVao.MaBienThe == "SUATUOI-01").KetQua.SoDeXuat);
        await Voi(s => s.TaoDonNhapAsync(ChonSua(20)));
        var loi = await Assert.ThrowsAsync<NghiepVuException>(() => Voi(s => s.TaoDonNhapAsync(ChonSua())));
        Assert.Contains("không còn trong danh sách đề xuất", loi.Message);
        Assert.Equal(KetLuanG3.DuChoNhap, (await Voi(s => s.TinhAsync("K01"))).Single(d => d.DauVao.MaBienThe == "SUATUOI-01").KetQua.KetLuan);

        await using (var db = CsdlFixture.TaoDbContext(_chuoi))
            await DonMua(db, NguoiThu.HienTai(db, "ND002", Vai.QuanLyKho)).XacNhanAsync(ma);
        await using (var db = CsdlFixture.TaoDbContext(_chuoi))
        {
            Assert.Equal(TrangThaiDonMua.ChuaThucHien, (await db.DonMua.SingleAsync(d => d.MaDonMua == ma)).TrangThai);
            Assert.True((await db.LichSuGoiY.SingleAsync(l => l.MaChungTu == ma)).ChapNhan);
        }
    }

    [Fact]
    public async Task HaiLanBamDongThoi_ChiMotDon()
    {
        var kq = await Task.WhenAll(Enumerable.Range(0, 2).Select(_ => Task.Run(async () =>
        {
            try { return (await Voi(s => s.TaoDonNhapAsync(ChonSua()))).Count; }
            catch (NghiepVuException) { return 0; }
        })));
        Assert.Equal(new[] { 0, 1 }, kq.Order());
        await using var db = CsdlFixture.TaoDbContext(_chuoi);
        Assert.Equal(1, await db.DonMua.CountAsync(d => d.TrangThai == TrangThaiDonMua.Nhap));
    }

    [Fact]
    public async Task HuyNhapVaBoQua_LaKhongChapNhan()
    {
        var ma = Assert.Single(await Voi(s => s.TaoDonNhapAsync(ChonSua())));
        await using (var db = CsdlFixture.TaoDbContext(_chuoi))
            await DonMua(db, NguoiThu.HienTai(db, "ND002", Vai.QuanLyKho)).HuyAsync(ma);

        Assert.Contains("lý do", (await Assert.ThrowsAsync<NghiepVuException>(() => Voi(s => s.BoQuaAsync(ChonSua())))).Message);
        Assert.Equal(1, await Voi(s => s.BoQuaAsync(ChonSua(lyDo: "Sắp đổi nhà cung cấp"))));

        await using var kt = CsdlFixture.TaoDbContext(_chuoi);
        var ls = await kt.LichSuGoiY.Where(l => l.Loai == LoaiGoiY.NhapBoSung).OrderBy(l => l.MaGoiY).ToListAsync();
        Assert.Equal(2, ls.Count);
        Assert.All(ls, l => Assert.False(l.ChapNhan));
        Assert.Equal(("SUATUOI-01", (string?)null), (ls[1].MaBienThe, ls[1].MaChungTu));
        Assert.Contains("Sắp đổi nhà cung cấp", ls[1].KetQua);
    }

    [Fact]
    public async Task PhanQuyen_ThuKhoVaKhoKhac()
    {
        await Assert.ThrowsAsync<KhongCoQuyenException>(() => Voi(s => s.TinhAsync("K01"), "ND003"));
        await using var app = new UngDungFactory { ChuoiKetNoi = _chuoi, Gio = Gio };
        Assert.Equal(HttpStatusCode.Forbidden, (await app.TaoClient(Vai.ThuKho).GetAsync("/api/de-xuat-nhap?maKho=K01")).StatusCode);
        var ql = app.TaoClient(Vai.QuanLyKho, "ND002");
        Assert.Equal(HttpStatusCode.OK, (await ql.GetAsync("/DeXuatNhap?maKho=K01")).StatusCode);
        Assert.Contains("\"soDeXuat\":300", await ql.GetStringAsync("/api/de-xuat-nhap?maKho=K01"));
    }
}
