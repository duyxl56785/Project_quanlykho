using Microsoft.EntityFrameworkCore;
using quanlykho.Common;
using quanlykho.Common.Loi;
using quanlykho.Common.PhanQuyen;
using quanlykho.Domain;
using quanlykho.Features.NhapKho;
using quanlykho.Tests.HaTang;

namespace quanlykho.Tests.Integration;

/// <summary>Quy đổi ở hóa đơn nhập: sữa có đơn vị thùng = 24 hộp, mã vạch thùng THUNG-SUA.</summary>
[Collection(CsdlCollection.Ten)]
public class QuyDoiPhieuNhapTests(CsdlFixture csdl) : IAsyncLifetime
{
    private const string Sua = "SUATUOI-01";
    private static readonly DateOnly Han = new(2027, 6, 1);
    private string _chuoi = "";

    public async Task InitializeAsync()
    {
        _chuoi = await csdl.TaoCsdlCoDuLieuThuAsync();
        await using var db = CsdlFixture.TaoDbContext(_chuoi);
        db.DonViQuyDoi.Add(new DonViQuyDoi { MaBienThe = Sua, TenDonVi = "thùng", HeSo = 24, MaVach = "THUNG-SUA", TrangThai = TrangThaiSuDung.DangSuDung });
        await db.SaveChangesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<T> Voi<T>(Func<PhieuNhapService, Task<T>> lam, string ma = "ND004", string vai = Vai.ThuKho)
    {
        await using var db = CsdlFixture.TaoDbContext(_chuoi);
        var h = NguoiThu.HienTai(db, ma, vai);
        var nhatKy = NguoiThu.NhatKy(db, h);
        var donMua = new DonMuaService(db, h, new MaChungTu(db), nhatKy, TimeProvider.System);
        return await lam(new PhieuNhapService(db, h, new MaChungTu(db), nhatKy, donMua, new CongGhiTon(db, h, TimeProvider.System), TimeProvider.System));
    }

    /// <summary>Đơn mua sữa theo thùng, 120.000 đ một thùng; trả mã hóa đơn nhập Lưu tạm lập từ đơn.</summary>
    private async Task<string> PhieuTuDon(decimal soThung)
    {
        await using (var db = CsdlFixture.TaoDbContext(_chuoi))
        {
            var h = NguoiThu.HienTai(db, "ND002", Vai.QuanLyKho);
            var maDon = await new DonMuaService(db, h, new MaChungTu(db), NguoiThu.NhatKy(db, h), TimeProvider.System).TaoAsync(new DonMuaForm
            {
                MaNhaCungCap = "NCC03", MaKho = "K01", Dong = [new DongDonMuaForm { MaBienThe = Sua, TenDonVi = "thùng", SoLuong = soThung, DonGia = 120000 }],
            });
            return await Voi(s => s.TaoAsync(new PhieuNhapForm { LoaiPhieu = LoaiPhieuNhap.MuaHang, MaDonMua = maDon }));
        }
    }

    [Fact]
    public async Task QuetMaThung_MuoiThung_GhiTon240Hop()
    {
        var ma = await PhieuTuDon(10);
        await Voi(s => s.ThemDongAsync(ma, new DongPhieuNhapForm("THUNG-SUA", 10, Han, ChatLuong.Dat)));
        var loi = await Assert.ThrowsAsync<NghiepVuException>(() => Voi(s => s.ThemDongAsync(ma, new DongPhieuNhapForm(Sua, 1, Han, ChatLuong.Dat))));
        Assert.Contains("còn 0 hộp, đang nhập 1 hộp", loi.Message);

        var p = await Voi(s => s.LayAsync(ma), "ND002", Vai.QuanLyKho);
        var d = Assert.Single(p.Dong);
        Assert.Equal(("thùng", 24, 10m, 240m, "hộp", 120000m, 1200000m), (d.TenDonVi, d.HeSo, d.SoLuong, d.SoLuongChinh, d.DonViChinh, d.DonGia!.Value, d.ThanhTien!.Value));

        await Voi(s => s.XacNhanAsync(ma, "xn"), "ND002", Vai.QuanLyKho);
        await using var db = CsdlFixture.TaoDbContext(_chuoi);
        Assert.Equal(240, await db.TonKho.Where(t => t.MaLo == d.MaLo).SumAsync(t => t.SoLuong));
        Assert.Equal(240, await db.SoBienDong.Where(s => s.MaLo == d.MaLo).SumAsync(s => s.SoLuongThayDoi));
    }

    [Fact]
    public async Task NhanLeHop_GiaQuyTheoHop_KhongVuotSoDat()
    {
        var ma = await PhieuTuDon(2);
        await Voi(s => s.ThemDongAsync(ma, new DongPhieuNhapForm(Sua, 24, Han, ChatLuong.Dat, TenDonVi: "hộp")));
        await Voi(s => s.ThemDongAsync(ma, new DongPhieuNhapForm(Sua, 1, Han, ChatLuong.Loi, TenDonVi: "thùng")));
        await Assert.ThrowsAsync<NghiepVuException>(() => Voi(s => s.ThemDongAsync(ma, new DongPhieuNhapForm(Sua, 1, Han, ChatLuong.Dat))));
        await Assert.ThrowsAsync<NghiepVuException>(() => Voi(s => s.ThemDongAsync(ma, new DongPhieuNhapForm(Sua, 1, Han, ChatLuong.Dat, TenDonVi: "két"))));

        var p = await Voi(s => s.LayAsync(ma), "ND002", Vai.QuanLyKho);
        Assert.Equal([("hộp", 24m, 24m, 5000m), ("thùng", 1m, 24m, 120000m)], p.Dong.Select(d => (d.TenDonVi, d.SoLuong, d.SoLuongChinh, d.DonGia!.Value)));
        Assert.Equal(240000, p.TongTien);

        // Màn hình quét có đơn vị và mã vạch thùng để nhận ra đơn vị.
        var hang = Assert.Single(p.Hang);
        Assert.Equal(["hộp", "thùng"], hang.DonVi.Select(x => x.Ten));
        Assert.Equal("thùng", hang.MaVachDonVi["THUNG-SUA"]);
        Assert.Equal(0, hang.ConNhan);
    }
}
