using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using quanlykho.Common;
using quanlykho.Common.Loi;
using quanlykho.Common.PhanQuyen;
using quanlykho.Data;
using quanlykho.Domain;
using quanlykho.Features.NhapKho;
using quanlykho.Tests.HaTang;

namespace quanlykho.Tests.Integration;

[Collection(CsdlCollection.Ten)]
public class PhieuNhapTests(CsdlFixture csdl) : IAsyncLifetime
{
    private string _chuoi = "";
    private UngDungFactory _app = null!;

    public async Task InitializeAsync()
    {
        _chuoi = await csdl.TaoCsdlCoDuLieuThuAsync();
        _app = new UngDungFactory { ChuoiKetNoi = _chuoi };
    }

    public async Task DisposeAsync() => await _app.DisposeAsync();

    private static PhieuNhapService Service(AppDbContext db, string ma = "ND004", string vai = Vai.ThuKho)
    {
        var hienTai = NguoiThu.HienTai(db, ma, vai);
        var nhatKy = NguoiThu.NhatKy(db, hienTai);
        var donMua = new DonMuaService(db, hienTai, new MaChungTu(db), nhatKy, TimeProvider.System);
        return new PhieuNhapService(db, hienTai, new MaChungTu(db), nhatKy, donMua, TimeProvider.System);
    }

    private async Task<T> Voi<T>(Func<PhieuNhapService, Task<T>> lam, string ma = "ND004", string vai = Vai.ThuKho)
    {
        await using var db = CsdlFixture.TaoDbContext(_chuoi);
        return await lam(Service(db, ma, vai));
    }

    private async Task Voi(Func<PhieuNhapService, Task> lam, string ma = "ND004", string vai = Vai.ThuKho)
    {
        await using var db = CsdlFixture.TaoDbContext(_chuoi);
        await lam(Service(db, ma, vai));
    }

    private async Task<string> TaoDon(params (string BienThe, decimal SoLuong)[] dong)
    {
        await using var db = CsdlFixture.TaoDbContext(_chuoi);
        var hienTai = NguoiThu.HienTai(db, "ND002", Vai.QuanLyKho);
        return await new DonMuaService(db, hienTai, new MaChungTu(db), NguoiThu.NhatKy(db, hienTai), TimeProvider.System).TaoAsync(new DonMuaForm
        {
            MaNhaCungCap = "NCC01", MaKho = "K01",
            Dong = dong.Select(d => new DongDonMuaForm { MaBienThe = d.BienThe, SoLuong = d.SoLuong, DonGia = 85000 }).ToList(),
        });
    }

    private Task<string> TaoPhieu(string maDon) =>
        Voi(s => s.TaoAsync(new PhieuNhapForm { LoaiPhieu = LoaiPhieuNhap.MuaHang, MaDonMua = maDon }));

    private Task<(int, string)> Them(string maPhieu, string ma, decimal soLuong, DateOnly? hanDung = null, ChatLuong chatLuong = ChatLuong.Dat) =>
        Voi(s => s.ThemDongAsync(maPhieu, new DongPhieuNhapForm(ma, soLuong, hanDung, chatLuong)));

    [Fact]
    public async Task LapTuDonMua_MoiDongMotLoMoi_GiaTheoDon()
    {
        var don = await TaoDon(("AOTHUN-01", 10));
        var phieu = await TaoPhieu(don);
        Assert.Equal("PN000001", phieu);

        string maVach;
        await using (var db = CsdlFixture.TaoDbContext(_chuoi))
        {
            Assert.Equal(TrangThaiDonMua.DangThucHien, (await db.DonMua.SingleAsync(d => d.MaDonMua == don)).TrangThai);
            maVach = (await db.BienThe.SingleAsync(b => b.MaBienThe == "AOTHUN-01")).MaVach!;
        }
        Assert.Equal((1, "L000001"), await Them(phieu, maVach, 4));
        Assert.Equal((2, "L000002"), await Them(phieu, "AOTHUN-01", 3, chatLuong: ChatLuong.Loi));

        var p = await Voi(s => s.LayAsync(phieu), "ND002", Vai.QuanLyKho);
        Assert.Equal(TrangThaiPhieu.LuuTam, p.TrangThai);
        Assert.Equal([340000m, 255000m], p.Dong.Select(d => d.ThanhTien!.Value));
        Assert.Equal(595000m, p.TongTien);
        Assert.Equal(3m, p.Hang.Single().ConNhan);

        await using var db2 = CsdlFixture.TaoDbContext(_chuoi);
        var hienTai = NguoiThu.HienTai(db2, "ND002", Vai.QuanLyKho);
        var don2 = await new DonMuaService(db2, hienTai, new MaChungTu(db2), NguoiThu.NhatKy(db2, hienTai), TimeProvider.System).LayAsync(don);
        Assert.Equal(7m, don2.Dong.Single().DangNhan);
        Assert.Equal(DonMuaService.ChoKiemDem, don2.Buoc);
    }

    [Fact]
    public async Task ChanNhanVuotSoChuaNhan_TinhCaPhieuKhac()
    {
        var don = await TaoDon(("AOTHUN-01", 10));
        var p1 = await TaoPhieu(don);
        await Them(p1, "AOTHUN-01", 6);

        var loi = await Assert.ThrowsAsync<NghiepVuException>(() => Them(p1, "AOTHUN-01", 5));
        Assert.Contains("còn 4", loi.Message);

        var p2 = await TaoPhieu(don);
        await Them(p2, "AOTHUN-01", 4);
        await Assert.ThrowsAsync<NghiepVuException>(() => Them(p2, "AOTHUN-01", 0.5m));
        await Assert.ThrowsAsync<NghiepVuException>(() => Them(p2, "AOTHUN-02", 1));
        await Assert.ThrowsAsync<NghiepVuException>(() => Them(p2, "AOTHUN-01", 0));

        await Voi(s => s.XoaDongAsync(p1, 1));
        await Them(p2, "AOTHUN-01", 6);
        await using var db = CsdlFixture.TaoDbContext(_chuoi);
        Assert.False(await db.Lo.AnyAsync(l => l.MaLo == "L000001"));
    }

    [Fact]
    public async Task HangTheoDoiHanDung_ThieuHanDungBiChan()
    {
        var don = await TaoDon(("SUATUOI-01", 48), ("GIAYA4-01", 5));
        var phieu = await TaoPhieu(don);

        var loi = await Assert.ThrowsAsync<NghiepVuException>(() => Them(phieu, "SUATUOI-01", 48));
        Assert.Contains("hạn dùng", loi.Message);

        await Them(phieu, "SUATUOI-01", 48, new DateOnly(2027, 3, 1));
        await Them(phieu, "GIAYA4-01", 5, new DateOnly(2027, 3, 1));
        await using var db = CsdlFixture.TaoDbContext(_chuoi);
        Assert.Equal([new DateOnly(2027, 3, 1), null], await db.Lo.OrderBy(l => l.MaLo).Select(l => l.HanDung).ToListAsync());
    }

    [Fact]
    public async Task NhapKhac_HuyPhieu_DonQuayVeChuaThucHien()
    {
        var khac = await Voi(s => s.TaoAsync(new PhieuNhapForm { LoaiPhieu = LoaiPhieuNhap.NhapKhac, MaKho = "K01" }));
        await Voi(s => s.ThemDongAsync(khac, new DongPhieuNhapForm("BUTBI-01", 100, null, ChatLuong.Dat, 3000)), "ND002", Vai.QuanLyKho);
        await Voi(s => s.ThemDongAsync(khac, new DongPhieuNhapForm("BUTBI-01", 50, null, ChatLuong.Dat, 9999)));
        Assert.Equal([300000m, 0m], (await Voi(s => s.LayAsync(khac), "ND002", Vai.QuanLyKho)).Dong.Select(d => d.ThanhTien!.Value));

        await Assert.ThrowsAsync<KhongCoQuyenException>(() => Voi(s => s.TaoAsync(new PhieuNhapForm { LoaiPhieu = LoaiPhieuNhap.NhapKhac, MaKho = "K02" })));

        var don = await TaoDon(("AOTHUN-01", 10));
        var phieu = await TaoPhieu(don);
        await Voi(s => s.HuyAsync(phieu));
        await Assert.ThrowsAsync<NghiepVuException>(() => Them(phieu, "AOTHUN-01", 1));
        await using var db = CsdlFixture.TaoDbContext(_chuoi);
        Assert.Equal(TrangThaiDonMua.ChuaThucHien, (await db.DonMua.SingleAsync(d => d.MaDonMua == don)).TrangThai);
    }

    [Fact]
    public async Task Api_ThuKhoKhongNhanTruongGia()
    {
        var don = await TaoDon(("AOTHUN-01", 10));
        var thuKho = _app.TaoClient(Vai.ThuKho, "ND004");
        var token = await NguoiThu.TokenAsync(thuKho, "/PhieuNhap");

        async Task<HttpResponseMessage> Post(string url, object body)
        {
            var r = new HttpRequestMessage(HttpMethod.Post, url) { Content = JsonContent.Create(body) };
            r.Headers.Add("RequestVerificationToken", token);
            return await thuKho.SendAsync(r);
        }

        var tao = await Post("/api/phieu-nhap", new { loaiPhieu = "MuaHang", maDonMua = don });
        Assert.Equal(HttpStatusCode.Created, tao.StatusCode);
        var ma = (await tao.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("ma").GetString();

        Assert.Equal(HttpStatusCode.Created, (await Post($"/api/phieu-nhap/{ma}/dong", new { ma = "AOTHUN-01", soLuong = 2, chatLuong = "Dat" })).StatusCode);
        var vuot = await Post($"/api/phieu-nhap/{ma}/dong", new { ma = "AOTHUN-01", soLuong = 9, chatLuong = "Dat" });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, vuot.StatusCode);

        var json = await thuKho.GetStringAsync($"/api/phieu-nhap/{ma}");
        Assert.Contains("\"maLo\":\"L000001\"", json);
        Assert.DoesNotContain("donGia", json);
        Assert.DoesNotContain("thanhTien", json);
        Assert.DoesNotContain("tongTien", json);
        Assert.DoesNotContain("85.000", await thuKho.GetStringAsync($"/PhieuNhap/{ma}"));

        var quanLy = await _app.TaoClient(Vai.QuanLyKho, "ND002").GetFromJsonAsync<JsonElement>($"/api/phieu-nhap/{ma}");
        Assert.Equal(170000m, quanLy.GetProperty("tongTien").GetDecimal());

        var xoa = new HttpRequestMessage(HttpMethod.Delete, $"/api/phieu-nhap/{ma}/dong/1");
        xoa.Headers.Add("RequestVerificationToken", token);
        Assert.Equal(HttpStatusCode.NoContent, (await thuKho.SendAsync(xoa)).StatusCode);
    }

    [Fact]
    public async Task GiaoDien_LapPhieu_VaPhanQuyenKho()
    {
        var don = await TaoDon(("AOTHUN-01", 10));
        var thuKho = _app.TaoClient(Vai.ThuKho, "ND004");

        Assert.Contains(don, await thuKho.GetStringAsync("/PhieuNhap/Them"));
        var r = await NguoiThu.PostFormAsync(thuKho, "/PhieuNhap/Them", "/PhieuNhap/Them", new() { ["LoaiPhieu"] = "MuaHang", ["MaDonMua"] = don });
        Assert.Equal("/PhieuNhap/PN000001", r.Headers.Location?.OriginalString);
        Assert.Contains("Quét mã vạch hàng", await thuKho.GetStringAsync("/PhieuNhap/PN000001"));

        var loi = await NguoiThu.PostFormAsync(thuKho, "/PhieuNhap/Them", "/PhieuNhap/Them", new() { ["LoaiPhieu"] = "MuaHang" });
        Assert.Contains("Chọn đơn mua", await loi.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.Forbidden, (await _app.TaoClient(Vai.ThuKho, "ND006").GetAsync("/PhieuNhap/PN000001")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await _app.TaoClient(Vai.BanHang, "ND007").GetAsync("/PhieuNhap")).StatusCode);
        Assert.Contains("PN000001", await _app.TaoClient(Vai.QuanLyKho, "ND002").GetStringAsync($"/DonMua/{don}"));
    }
}
