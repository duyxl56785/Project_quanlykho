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
public class DonMuaTests(CsdlFixture csdl) : IAsyncLifetime
{
    private string _chuoi = "";
    private UngDungFactory _app = null!;

    public async Task InitializeAsync()
    {
        _chuoi = await csdl.TaoCsdlCoDuLieuThuAsync();
        _app = new UngDungFactory { ChuoiKetNoi = _chuoi };
    }

    public async Task DisposeAsync() => await _app.DisposeAsync();

    private static DonMuaService Service(AppDbContext db, string ma = "ND002", string vai = Vai.QuanLyKho)
    {
        var hienTai = NguoiThu.HienTai(db, ma, vai);
        return new DonMuaService(db, hienTai, new MaChungTu(db), NguoiThu.NhatKy(db, hienTai), TimeProvider.System);
    }

    private static DonMuaForm Form(string kho = "K01", params (string BienThe, decimal SoLuong)[] dong) => new()
    {
        MaNhaCungCap = "NCC01", MaKho = kho,
        Dong = (dong.Length == 0 ? [("AOTHUN-01", 10m), ("AOTHUN-02", 5m)] : dong)
            .Select(d => new DongDonMuaForm { MaBienThe = d.BienThe, SoLuong = d.SoLuong, DonGia = 85000 }).ToList(),
    };

    private async Task<string> TaoDon(params (string, decimal)[] dong)
    {
        await using var db = CsdlFixture.TaoDbContext(_chuoi);
        return await Service(db).TaoAsync(Form("K01", dong));
    }

    private async Task ThemPhieu(string maPhieu, string maDon, TrangThaiPhieu trangThai, params (string MaLo, string BienThe, decimal SoLuong)[] dong)
    {
        await using var db = CsdlFixture.TaoDbContext(_chuoi);
        foreach (var d in dong)
            db.Lo.Add(new Lo { MaLo = d.MaLo, MaBienThe = d.BienThe, NgayNhap = DateTimeOffset.UtcNow });
        db.PhieuNhap.Add(new PhieuNhap
        {
            MaPhieuNhap = maPhieu, LoaiPhieu = LoaiPhieuNhap.MuaHang, MaDonMua = maDon, MaKho = "K01", NguoiLap = "ND004", NgayNhap = DateTimeOffset.UtcNow, TrangThai = trangThai,
            ChiTiet = dong.Select((d, i) => new ChiTietPhieuNhap { STT = i + 1, MaLo = d.MaLo, TenDonVi = "cái", SoLuongGoc = d.SoLuong, SoLuongThucNhan = d.SoLuong, DonGia = 85000 }).ToList(),
        });
        await db.SaveChangesAsync();
        await Service(db).CapNhatTrangThaiAsync(maDon);
        await db.SaveChangesAsync();
    }

    private async Task<DonMuaChiTiet> Lay(string ma)
    {
        await using var db = CsdlFixture.TaoDbContext(_chuoi);
        return await Service(db).LayAsync(ma);
    }

    [Fact]
    public async Task Tao_ChuaThucHien_DonViTinhChinh()
    {
        var ma = await TaoDon();

        var d = await Lay(ma);
        Assert.Equal("DM000001", ma);
        Assert.Equal(TrangThaiDonMua.ChuaThucHien, d.TrangThai);
        Assert.All(d.Dong, x => Assert.Equal(("cái", 1), (x.TenDonVi, x.HeSo)));
        Assert.Equal(15 * 85000m, d.TongTien);
    }

    [Fact]
    public async Task Tao_KhoKhongDuocGiao_BiChan()
    {
        await using var db = CsdlFixture.TaoDbContext(_chuoi);
        await Assert.ThrowsAsync<KhongCoQuyenException>(() => Service(db).TaoAsync(Form("K02")));
    }

    [Fact]
    public async Task LuatDongHang()
    {
        await using var db = CsdlFixture.TaoDbContext(_chuoi);
        var s = Service(db);

        Assert.Contains("mỗi biến thể chỉ một dòng", (await Assert.ThrowsAsync<NghiepVuException>(() => s.TaoAsync(Form("K01", ("AOTHUN-01", 1), ("AOTHUN-01", 2))))).Message);
        Assert.Contains("lớn hơn 0", (await Assert.ThrowsAsync<NghiepVuException>(() => s.TaoAsync(Form("K01", ("AOTHUN-01", 0))))).Message);
        await Assert.ThrowsAsync<NghiepVuException>(() => s.TaoAsync(Form("K01", ("KHONGCO-01", 1))));

        await db.NhaCungCap.Where(n => n.MaNhaCungCap == "NCC01").ExecuteUpdateAsync(x => x.SetProperty(n => n.TrangThai, TrangThaiSuDung.NgungSuDung));
        Assert.Contains("ngừng sử dụng", (await Assert.ThrowsAsync<NghiepVuException>(() => s.TaoAsync(Form()))).Message);
    }

    [Fact]
    public async Task TrangThaiTinhTuPhieuNhap()
    {
        var ma = await TaoDon();

        await ThemPhieu("PN000001", ma, TrangThaiPhieu.LuuTam, ("L1", "AOTHUN-01", 6));
        var d = await Lay(ma);
        Assert.Equal((TrangThaiDonMua.DangThucHien, DonMuaService.ChoKiemDem), (d.TrangThai, d.Buoc));
        Assert.Equal(6m, d.Dong.Single(x => x.MaBienThe == "AOTHUN-01").DangNhan);

        await using (var db = CsdlFixture.TaoDbContext(_chuoi))
        {
            await db.PhieuNhap.Where(p => p.MaPhieuNhap == "PN000001").ExecuteUpdateAsync(x => x.SetProperty(p => p.TrangThai, TrangThaiPhieu.HoanTat));
            await Service(db).CapNhatTrangThaiAsync(ma);
            await db.SaveChangesAsync();
        }
        d = await Lay(ma);
        Assert.Equal((TrangThaiDonMua.DangThucHien, DonMuaService.ChoNhanHang), (d.TrangThai, d.Buoc));
        Assert.Equal(4m, d.Dong.Single(x => x.MaBienThe == "AOTHUN-01").ChuaNhan);

        await ThemPhieu("PN000002", ma, TrangThaiPhieu.HoanTat, ("L2", "AOTHUN-01", 4), ("L3", "AOTHUN-02", 5));
        Assert.Equal(TrangThaiDonMua.HoanThanh, (await Lay(ma)).TrangThai);
    }

    [Fact]
    public async Task Huy_ChanKhiCoHoaDonNhap_GhiNhatKy()
    {
        var coPhieu = await TaoDon();
        await ThemPhieu("PN000001", coPhieu, TrangThaiPhieu.HoanTat, ("L1", "AOTHUN-01", 1));
        var khongPhieu = await TaoDon();

        await using var db = CsdlFixture.TaoDbContext(_chuoi);
        await Assert.ThrowsAsync<NghiepVuException>(() => Service(db).HuyAsync(coPhieu));
        await Service(db).HuyAsync(khongPhieu);

        Assert.Equal(TrangThaiDonMua.DaHuy, (await Lay(khongPhieu)).TrangThai);
        Assert.True(await db.NhatKy.AnyAsync(n => n.LoaiThaoTac == "HuyDonMua" && n.MaDoiTuong == khongPhieu && n.MaNguoiDung == "ND002"));
    }

    [Fact]
    public async Task DonNhap_SuaRoiXacNhan_DonCoPhieuKhongSuaDuoc()
    {
        string nhap;
        await using (var db = CsdlFixture.TaoDbContext(_chuoi))
        {
            nhap = await Service(db).TaoAsync(Form(), laNhap: true);
            await Service(db).SuaAsync(nhap, Form("K01", ("GIAYA4-01", 20)));
            await Service(db).XacNhanAsync(nhap);
        }
        var d = await Lay(nhap);
        Assert.Equal(TrangThaiDonMua.ChuaThucHien, d.TrangThai);
        Assert.Equal(["GIAYA4-01"], d.Dong.Select(x => x.MaBienThe));

        await ThemPhieu("PN000001", nhap, TrangThaiPhieu.LuuTam, ("L1", "GIAYA4-01", 1));
        await using var db2 = CsdlFixture.TaoDbContext(_chuoi);
        await Assert.ThrowsAsync<NghiepVuException>(() => Service(db2).SuaAsync(nhap, Form()));
    }

    [Fact]
    public async Task ThuKho_XemDuocNhungKhongThayGia()
    {
        var ma = await TaoDon();
        var thuKho = _app.TaoClient(Vai.ThuKho, "ND004");

        var html = await thuKho.GetStringAsync($"/DonMua/{ma}");
        Assert.DoesNotContain("Đơn giá", html);
        Assert.DoesNotContain("85.000", html);
        var json = await thuKho.GetFromJsonAsync<JsonElement>($"/api/don-mua/{ma}");
        Assert.Equal(JsonValueKind.Null, json.GetProperty("tongTien").ValueKind);
        Assert.Equal(JsonValueKind.Null, json.GetProperty("dong")[0].GetProperty("donGia").ValueKind);

        Assert.Contains("85.000", await _app.TaoClient(Vai.QuanLyKho, "ND002").GetStringAsync($"/DonMua/{ma}"));
    }

    [Fact]
    public async Task PhanQuyen()
    {
        var ma = await TaoDon();

        Assert.Equal(HttpStatusCode.Forbidden, (await _app.TaoClient(Vai.ThuKho, "ND004").GetAsync("/DonMua/Them")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await _app.TaoClient(Vai.BanHang, "ND007").GetAsync("/DonMua")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await _app.TaoClient(Vai.QuanLyKho, "ND003").GetAsync($"/DonMua/{ma}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await _app.TaoClient(Vai.QuanLyKho, "ND003").GetAsync($"/api/don-mua/{ma}")).StatusCode);
        Assert.DoesNotContain(ma, await _app.TaoClient(Vai.QuanLyKho, "ND003").GetStringAsync("/DonMua"));
        Assert.Contains(ma, await _app.TaoClient(Vai.QuanLyKho, "ND002").GetStringAsync("/DonMua"));
    }

    [Fact]
    public async Task GiaoDien_LapDon_VaApiTaoDon()
    {
        var quanLy = _app.TaoClient(Vai.QuanLyKho, "ND002");

        var r = await NguoiThu.PostFormAsync(quanLy, "/DonMua/Them", "/DonMua/Them", new()
        {
            ["MaNhaCungCap"] = "NCC03", ["MaKho"] = "K01",
            ["Dong[0].MaBienThe"] = "SUATUOI-01", ["Dong[0].SoLuong"] = "48", ["Dong[0].DonGia"] = "7500.5",
        });
        Assert.Equal("/DonMua/DM000001", r.Headers.Location?.OriginalString);

        var loi = await NguoiThu.PostFormAsync(quanLy, "/DonMua/Them", "/DonMua/Them", new() { ["MaNhaCungCap"] = "NCC03", ["MaKho"] = "K01" });
        Assert.Contains("ít nhất một dòng hàng", await loi.Content.ReadAsStringAsync());

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/don-mua")
        {
            Content = JsonContent.Create(new { maNhaCungCap = "NCC02", maKho = "K01", dong = new[] { new { maBienThe = "DTA-01", soLuong = 3, donGia = 12000000 } } })
        };
        request.Headers.Add("RequestVerificationToken", await NguoiThu.TokenAsync(quanLy, "/DonMua"));
        var api = await quanLy.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, api.StatusCode);
        Assert.Equal("DM000002", (await api.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("ma").GetString());

        await using var db = CsdlFixture.TaoDbContext(_chuoi);
        Assert.Equal(7500.5m, (await db.ChiTietDonMua.SingleAsync(c => c.MaDonMua == "DM000001")).DonGia);
    }
}
