using System.Net;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using quanlykho.Common;
using quanlykho.Common.PhanQuyen;
using quanlykho.Domain;
using quanlykho.Features.NhapKho;
using quanlykho.Tests.HaTang;
using quanlykho.Tests.Unit;

namespace quanlykho.Tests.Integration;

[Collection(CsdlCollection.Ten)]
public class InNhanTests(CsdlFixture csdl) : IAsyncLifetime
{
    private string _chuoi = "";
    private UngDungFactory _app = null!;

    public async Task InitializeAsync()
    {
        _chuoi = await csdl.TaoCsdlCoDuLieuThuAsync();
        _app = new UngDungFactory { ChuoiKetNoi = _chuoi };
    }

    public async Task DisposeAsync() => await _app.DisposeAsync();

    private static List<string> MaTrenNhan(string html) =>
        Regex.Matches(html, @"<svg[^>]*>.*?</svg>").Select(m => NhanMaVachTests.GiaiMa(m.Value)!).ToList();

    [Fact]
    public async Task NhanO_ChonKe_InMoiOBenDuoi_DungPhamViKho()
    {
        var ke = "K01-TA01-TG01-P02-H01-KE01";
        List<string> o;
        await using (var db = CsdlFixture.TaoDbContext(_chuoi))
            o = await db.ViTri.Where(v => v.MaViTri.StartsWith(ke + "-") && v.Cap == 7).OrderBy(v => v.MaViTri).Select(v => v.MaViTri).ToListAsync();
        Assert.NotEmpty(o);

        var thuKho = _app.TaoClient(Vai.ThuKho, "ND004");
        var tra = await thuKho.GetAsync($"/InNhan/O?ma={ke}&kho=70x40");
        Assert.Equal(HttpStatusCode.OK, tra.StatusCode);
        var html = await tra.Content.ReadAsStringAsync();
        Assert.Equal(o, MaTrenNhan(html));
        Assert.Contains("kho-70x40", html);
        Assert.Contains("Kho Hà Nội / Tòa 1", html);

        Assert.Equal(HttpStatusCode.Forbidden, (await _app.TaoClient(Vai.QuanLyKho, "ND003").GetAsync($"/InNhan/O?ma={ke}")).StatusCode);
    }

    [Fact]
    public async Task NhanLo_ChiKhiDaXacNhan_MoiDongSoBan()
    {
        string phieu;
        await using (var db = CsdlFixture.TaoDbContext(_chuoi))
        {
            var hienTai = NguoiThu.HienTai(db, "ND002", Vai.QuanLyKho);
            var nhatKy = NguoiThu.NhatKy(db, hienTai);
            var s = new PhieuNhapService(db, hienTai, new MaChungTu(db), nhatKy,
                new DonMuaService(db, hienTai, new MaChungTu(db), nhatKy, TimeProvider.System), new CongGhiTon(db, hienTai, TimeProvider.System), TimeProvider.System);
            phieu = await s.TaoAsync(new PhieuNhapForm { LoaiPhieu = LoaiPhieuNhap.NhapKhac, MaKho = "K01" });
            await s.ThemDongAsync(phieu, new DongPhieuNhapForm("AOTHUN-01", 4, null, ChatLuong.Dat));
            await s.ThemDongAsync(phieu, new DongPhieuNhapForm("SUATUOI-01", 12, new DateOnly(2027, 3, 31), ChatLuong.Dat));
        }

        var thuKho = _app.TaoClient(Vai.ThuKho, "ND004");
        var chuaXacNhan = await thuKho.GetAsync($"/InNhan/Lo?maPhieu={phieu}");
        Assert.NotEqual(HttpStatusCode.OK, chuaXacNhan.StatusCode);

        await using (var db = CsdlFixture.TaoDbContext(_chuoi))
        {
            var hienTai = NguoiThu.HienTai(db, "ND002", Vai.QuanLyKho);
            var nhatKy = NguoiThu.NhatKy(db, hienTai);
            await new PhieuNhapService(db, hienTai, new MaChungTu(db), nhatKy,
                new DonMuaService(db, hienTai, new MaChungTu(db), nhatKy, TimeProvider.System), new CongGhiTon(db, hienTai, TimeProvider.System), TimeProvider.System)
                .XacNhanAsync(phieu, "in-nhan");
        }

        var html = await thuKho.GetStringAsync($"/InNhan/Lo?maPhieu={phieu}&soBan=2");
        Assert.Equal(["L000001", "L000001", "L000002", "L000002"], MaTrenNhan(html));
        Assert.Contains("HSD 31/03/2027", html);
        Assert.Contains("12 hộp", html);
        Assert.Contains("In nhãn lô", await thuKho.GetStringAsync($"/PhieuNhap/{phieu}"));
    }
}
