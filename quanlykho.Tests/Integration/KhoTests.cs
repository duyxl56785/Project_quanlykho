using System.Net;
using Microsoft.EntityFrameworkCore;
using quanlykho.Common.Loi;
using quanlykho.Common.PhanQuyen;
using quanlykho.Domain;
using quanlykho.Features.Kho;
using quanlykho.Tests.HaTang;

namespace quanlykho.Tests.Integration;

[Collection(CsdlCollection.Ten)]
public class KhoTests(CsdlFixture csdl) : IAsyncLifetime
{
    private string _chuoi = "";
    private UngDungFactory _app = null!;

    public async Task InitializeAsync()
    {
        _chuoi = await csdl.TaoCsdlCoDuLieuThuAsync();
        _app = new UngDungFactory { ChuoiKetNoi = _chuoi };
    }

    public async Task DisposeAsync() => await _app.DisposeAsync();

    private async Task<T> Voi<T>(Func<KhoService, Task<T>> lam)
    {
        await using var db = CsdlFixture.TaoDbContext(_chuoi);
        return await lam(new KhoService(db, NguoiThu.NhatKy(db, NguoiThu.HienTai(db, "ND001", Vai.QuanTri))));
    }

    private Task Voi(Func<KhoService, Task> lam) => Voi(async s => { await lam(s); return 0; });

    private async Task<T> LaNguoi<T>(string ma, string vai, Func<NguoiDungHienTai, ViTriService, Task<T>> lam)
    {
        await using var db = CsdlFixture.TaoDbContext(_chuoi);
        var hienTai = NguoiThu.HienTai(db, ma, vai);
        return await lam(hienTai, new ViTriService(db, hienTai, NguoiThu.NhatKy(db, hienTai)));
    }

    [Fact]
    public async Task ThemKho_GanNguoi_NguoiDoChiThayKhoCuaMinh()
    {
        string temCu;
        await using (var db = CsdlFixture.TaoDbContext(_chuoi))
            temCu = (await db.NguoiDung.SingleAsync(n => n.MaNguoiDung == "ND006")).SecurityStamp;

        Assert.Equal("K03", await Voi(s => s.ThemAsync(new KhoForm { Ma = "k03", Ten = "Kho Đà Nẵng", DiaChi = "Hải Châu", NguoiDung = ["ND006"] })));
        await Assert.ThrowsAsync<NghiepVuException>(() => Voi(s => s.ThemAsync(new KhoForm { Ma = "K03", Ten = "Trùng" })));
        await Assert.ThrowsAsync<NghiepVuException>(() => Voi(s => s.ThemAsync(new KhoForm { Ma = "K04", Ten = "Giao bán hàng", NguoiDung = ["ND007"] })));

        Assert.Equal(["K02", "K03"], await LaNguoi("ND006", Vai.ThuKho, (h, _) => h.KhoDuocGiaoAsync()));
        Assert.Equal(["K01"], (await LaNguoi("ND004", Vai.ThuKho, (_, v) => v.KhoAsync())).Select(k => k.Ma));
        await Assert.ThrowsAsync<KhongCoQuyenException>(() => LaNguoi("ND004", Vai.ThuKho, (_, v) => v.CayAsync("K03")));

        await using (var db = CsdlFixture.TaoDbContext(_chuoi))
        {
            Assert.NotEqual(temCu, (await db.NguoiDung.SingleAsync(n => n.MaNguoiDung == "ND006")).SecurityStamp);
            Assert.True(await db.NhatKy.AnyAsync(n => n.LoaiThaoTac == "DoiQuyen" && n.MaDoiTuong == "ND006"));
            Assert.Equal(0.85m, (await db.ViTri.SingleAsync(v => v.MaViTri == "K03")).HeSoLapDay);
        }

        await Voi(s => s.SuaAsync("K03", new KhoForm { Ten = "Kho Đà Nẵng" }));
        var loi = await Assert.ThrowsAsync<NghiepVuException>(() => Voi(s => s.SuaAsync("K02", new KhoForm { Ten = "Kho Bắc Ninh", NguoiDung = ["ND003"] })));
        Assert.Contains("chỉ được giao kho này", loi.Message);
        Assert.Equal(["K02"], await LaNguoi("ND006", Vai.ThuKho, (h, _) => h.KhoDuocGiaoAsync()));
    }

    [Fact]
    public async Task TaoKhuMacDinh_DoiTen_NgungSuDung()
    {
        await Voi(s => s.ThemAsync(new KhoForm { Ma = "K03", Ten = "Kho Đà Nẵng" }));
        Assert.Equal("K03-TA01", await Voi(s => s.TaoKhuMacDinhAsync("K03")));
        await Assert.ThrowsAsync<NghiepVuException>(() => Voi(s => s.TaoKhuMacDinhAsync("K03")));

        await Voi(s => s.SuaAsync("K03", new KhoForm { Ten = "Kho miền Trung", HeSoLapDay = 0.8m }));
        await using (var db = CsdlFixture.TaoDbContext(_chuoi))
        {
            var o = await db.ViTri.Where(v => v.MaKho == "K03" && v.Cap == 7).OrderBy(v => v.MaViTri).ToListAsync();
            Assert.Equal(4, o.Count);
            Assert.Equal([LoaiKhu.TiepNhan, LoaiKhu.CachLy], o.Where(x => x.LaOMacDinh).Select(x => x.LoaiKhu!.Value));
            Assert.Equal("Kho miền Trung / Tòa 1 / Tầng 1 / Khu tiếp nhận / Hàng 1 / Kệ 1 / Ô 1", o[0].TenDayDu);
        }

        await Voi(s => s.KhoaAsync("K03", true));
        var ds = await Voi(s => s.DanhSachAsync());
        Assert.Equal(TrangThaiSuDung.NgungSuDung, ds.Single(k => k.Ma == "K03").TrangThai);
        await using (var db = CsdlFixture.TaoDbContext(_chuoi))
            Assert.True(await db.ViTri.Where(v => v.MaKho == "K03").AllAsync(v => v.DangKhoa));
        await Voi(s => s.KhoaAsync("K03", false));
    }

    [Fact]
    public async Task ManHinh_ChiQuanTri()
    {
        var quanTri = _app.TaoClient(Vai.QuanTri, "ND001");
        var html = await quanTri.GetStringAsync("/Kho");
        Assert.Contains("Kho Hà Nội", html);
        Assert.Contains("Quản lý kho Hà Nội", html);
        Assert.Equal(HttpStatusCode.Forbidden, (await _app.TaoClient(Vai.QuanLyKho, "ND002").GetAsync("/Kho")).StatusCode);

        var r = await NguoiThu.PostFormAsync(quanTri, "/Kho/Them", "/Kho/Them", new() { ["Ma"] = "K-9", ["Ten"] = "Sai mã", ["HeSoLapDay"] = "0.85" });
        Assert.Contains("Mã kho gồm tối đa 10 ký tự", await r.Content.ReadAsStringAsync());
        r = await NguoiThu.PostFormAsync(quanTri, "/Kho/Them", "/Kho/Them", new() { ["Ma"] = "K05", ["Ten"] = "Kho mới", ["HeSoLapDay"] = "0.9", ["NguoiDung"] = "ND005" });
        Assert.Equal(HttpStatusCode.Redirect, r.StatusCode);
        Assert.Contains("id=\"kho-K05\" checked=\"checked\"", await quanTri.GetStringAsync("/NguoiDung/Sua/ND005"));
    }
}
