using System.Net;
using Microsoft.EntityFrameworkCore;
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

/// <summary>Quy đổi trên đơn mua, đơn đặt hàng và tồn khả dụng: sữa có đơn vị thùng = 24 hộp, tồn 235 hộp.</summary>
[Collection(CsdlCollection.Ten)]
public class QuyDoiDonHangTests(CsdlFixture csdl) : IAsyncLifetime
{
    private const string Sua = "SUATUOI-01";
    private static readonly FakeTimeProvider Gio = new(new DateTimeOffset(2026, 11, 1, 3, 0, 0, TimeSpan.Zero));
    private string _chuoi = "";

    public async Task InitializeAsync()
    {
        _chuoi = await csdl.TaoCsdlCoDuLieuThuAsync();
        await using var db = CsdlFixture.TaoDbContext(_chuoi);
        db.DonViQuyDoi.Add(new DonViQuyDoi { MaBienThe = Sua, TenDonVi = "thùng", HeSo = 24, MaVach = "THUNG-SUA", TrangThai = TrangThaiSuDung.DangSuDung });
        db.Lo.Add(new Lo { MaLo = "LS", MaBienThe = Sua, HanDung = new DateOnly(2027, 1, 1), NgayNhap = Gio.GetUtcNow() });
        await db.SaveChangesAsync();
        await using var tx = await db.Database.BeginTransactionAsync();
        await new CongGhiTon(db, NguoiThu.HienTai(db, "ND001", Vai.QuanTri), Gio).GhiAsync("nap", "Thu", "T1", [new("LS", "K01-TA01-TG01-P03-H01-KE01-O01", 235)]);
        await tx.CommitAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private static (DonDatService Don, GiuHangService Giu, LenhXuatService Lenh, TonKhaDung Ton) DichVu(AppDbContext db, string ma, string vai)
    {
        var h = NguoiThu.HienTai(db, ma, vai);
        var nhatKy = NguoiThu.NhatKy(db, h);
        var ton = new TonKhaDung(db, h, Gio);
        var cong = new CongGhiTon(db, h, Gio);
        var goiY = new GoiYService(db, h, ton, Gio);
        var giu = new GiuHangService(db, h, cong, goiY, nhatKy, Gio);
        return (new DonDatService(db, h, new MaChungTu(db), nhatKy, ton, giu, Gio), giu, new LenhXuatService(db, h, new MaChungTu(db), cong, goiY, ton, nhatKy, Gio), ton);
    }

    private async Task<T> Voi<T>(Func<(DonDatService Don, GiuHangService Giu, LenhXuatService Lenh, TonKhaDung Ton), Task<T>> lam, string ma = "ND002", string vai = Vai.QuanLyKho)
    {
        await using var db = CsdlFixture.TaoDbContext(_chuoi);
        return await lam(DichVu(db, ma, vai));
    }

    [Fact]
    public async Task DatBaThung_GiuDung72Hop_KhaDungHienThungLe()
    {
        var mot = await TaoDon(1);
        Assert.Equal("9 thùng 19 hộp", (await Voi(s => s.Don.LayAsync(mot))).Dong[0].KhaDungQuyDoi);

        var ma = await TaoDon(3);
        var don = await Voi(s => s.Don.LayAsync(ma));
        Assert.Equal(("thùng", 24, 3m, 72m), (don.Dong[0].TenDonVi, don.Dong[0].HeSo, don.Dong[0].SoLuongDat, don.Dong[0].SoDatChinh));

        await Voi(async s => { await s.Giu.DuyetAsync([ma]); return 0; });
        await using (var db = CsdlFixture.TaoDbContext(_chuoi))
        {
            Assert.Equal(72, await db.GiuHang.Where(g => g.MaDonDat == ma).SumAsync(g => g.SoLuong));
            Assert.Equal(72, await db.TonKho.Where(t => t.MaLo == "LS").SumAsync(t => t.SoLuongDaGiu));
        }
        // Còn 235 − 72 = 163 hộp = 6 thùng 19 hộp.
        Assert.Equal("6 thùng 19 hộp", (await Voi(s => s.Don.LayAsync(ma))).Dong[0].KhaDungQuyDoi);

        // Lệnh xuất làm việc theo đơn vị chính.
        var maLenh = await Voi(s => s.Lenh.TaoAsync(new LapLenhYeuCau(ma, "ND004")));
        await using var kt = CsdlFixture.TaoDbContext(_chuoi);
        var dong = await kt.ChiTietLenhXuat.SingleAsync(c => c.MaLenh == maLenh);
        Assert.Equal(("hộp", 1, 72m, 72m), (dong.TenDonVi, dong.HeSo, dong.SoLuongGoc, dong.SoLuongYeuCau));

        await Assert.ThrowsAsync<NghiepVuException>(() => TaoDon(1, "két"));
    }

    private Task<string> TaoDon(decimal soLuong, string donVi = "thùng") => Voi(s => s.Don.TaoAsync(new DonDatForm
    {
        MaKho = "K01", MaNoiNhan = "NN01", Dong = [new DongDonDatForm { MaBienThe = Sua, TenDonVi = donVi, SoLuong = soLuong }],
    }, guiDuyet: true), "ND007", Vai.BanHang);

    [Fact]
    public async Task DonMua_MuoiThung_ThanhTienTheoThung_ChuaNhan240Hop()
    {
        await using (var db = CsdlFixture.TaoDbContext(_chuoi))
        {
            var h = NguoiThu.HienTai(db, "ND002", Vai.QuanLyKho);
            var s = new DonMuaService(db, h, new MaChungTu(db), NguoiThu.NhatKy(db, h), Gio);
            var ma = await s.TaoAsync(new DonMuaForm
            {
                MaNhaCungCap = "NCC03", MaKho = "K01",
                Dong = [new DongDonMuaForm { MaBienThe = Sua, TenDonVi = "Thùng", SoLuong = 10, DonGia = 150000 }, new DongDonMuaForm { MaBienThe = "MIGOI-01", SoLuong = 5, DonGia = 3000 }],
            });
            var ct = await s.LayAsync(ma);
            var sua = ct.Dong.Single(d => d.MaBienThe == Sua);
            Assert.Equal(("thùng", 24, 240m, 240m, 1500000m, "hộp"), (sua.TenDonVi, sua.HeSo, sua.SoDatChinh, sua.ChuaNhan, sua.ThanhTien, sua.DonViChinh));
            var mi = ct.Dong.Single(d => d.MaBienThe == "MIGOI-01");
            Assert.Equal(("gói", 1, 5m), (mi.TenDonVi, mi.HeSo, mi.SoDatChinh));
            Assert.Equal(1515000, ct.TongTien);
            Assert.Equal("thùng", (await s.LayFormAsync(ma)).Dong.Single(d => d.MaBienThe == Sua).TenDonVi);
        }
    }

    [Fact]
    public async Task TonKhaDung_HienThungLe_TimTheoMaVachThung()
    {
        var trang = await Voi(s => s.Ton.TraCuuAsync("THUNG-SUA", null, false, 1), "ND007", Vai.BanHang);
        var d = Assert.Single(trang.Muc);
        Assert.Equal((Sua, 235m), (d.MaBienThe, d.Tong));
        Assert.Equal("9 thùng 19 hộp", Features.DanhMuc.QuyDoiDonVi.HienThi(d.DonVi, d.Tong));

        await using var app = new UngDungFactory { ChuoiKetNoi = _chuoi, Gio = Gio };
        var client = app.TaoClient(Vai.BanHang, "ND007");
        Assert.Contains("9 thùng 19 hộp", await client.GetStringAsync("/TonKhaDung?tuKhoa=SUATUOI"));
        var form = await client.GetStringAsync("/DonDat/Them");
        Assert.Contains("data-don-vi-hang", form);
        Assert.Contains("\"heSo\":24", form);
        Assert.Equal(HttpStatusCode.OK, (await app.TaoClient(Vai.QuanLyKho, "ND002").GetAsync("/DonMua/Them")).StatusCode);
    }
}
