using quanlykho.Features.Kho;

namespace quanlykho.Tests.Unit;

public class SucChuaTests
{
    private static readonly OChua OMetKhoi = new(1, 1, 1, 50);

    private static KetQuaSucChua Tinh(OChua o, DonViHang h, params HangTrongO[] dangCo) => SucChua.Tinh(o, 0.85m, "A", h, dangCo);

    [Fact]
    public void ViDu1_ThungTheoTaiTrongChiDuoc4()
    {
        var kq = Tinh(OMetKhoi, new DonViHang(0.5m, 0.5m, 0.4m, 12));
        Assert.Equal((8m, 4m, 4m), (kq.SoTheoKhongGian, kq.SoTheoTaiTrong, kq.QMax));
    }

    [Fact]
    public void ViDu2_ThanhDaiKhongVuaO()
    {
        var kq = Tinh(OMetKhoi, new DonViHang(1.2m, 0.05m, 0.05m, 1));
        Assert.False(kq.VuaKichThuoc);
        Assert.Equal(0, kq.QMax);
    }

    [Fact]
    public void ViDu3_XepLuoiChi4DuTheTichRa5()
    {
        var h = new DonViHang(0.4m, 1, 0.5m, 1);
        Assert.Equal(4, SucChua.SoTheoLuoi(OMetKhoi, h));
        Assert.Equal(4, Tinh(OMetKhoi, h).QMax);
    }

    [Fact]
    public void XoayHang_VuaKichThuoc()
    {
        Assert.True(SucChua.VuaKichThuoc(new OChua(0.5m, 2, 1, 10), new DonViHang(2, 0.5m, 1, 1)));
        Assert.Equal(1, SucChua.SoTheoLuoi(new OChua(0.5m, 2, 1, 10), new DonViHang(2, 0.5m, 1, 1)));
    }

    [Fact]
    public void ODay_QMaxBang0()
    {
        var h = new DonViHang(0.5m, 0.5m, 0.5m, 1);
        Assert.Equal(0, Tinh(new OChua(1, 1, 1, 100), h, new HangTrongO("A", h, 8)).QMax);
    }

    [Fact]
    public void KhoiLuongBangDungTaiTrong_VanXepDuoc()
    {
        var h = new DonViHang(0.1m, 0.1m, 0.1m, 12.5m);
        Assert.Equal(4, Tinh(new OChua(1, 1, 1, 50), h).QMax);
        Assert.Equal(1, Tinh(new OChua(1, 1, 1, 50), h, new HangTrongO("A", h, 3)).QMax);
        Assert.Equal(0, Tinh(new OChua(1, 1, 1, 50), h, new HangTrongO("A", h, 4)).QMax);
    }

    [Fact]
    public void ONhieuBienThe_TinhTheTichVoiHeSoLapDay()
    {
        var a = new DonViHang(0.1m, 0.1m, 0.1m, 0.1m);
        var b = new DonViHang(0.5m, 0.5m, 0.5m, 0.1m);
        // (1 × 0,85 − 0,125) / 0,001 = 725
        var kq = Tinh(new OChua(1, 1, 1, 1000), a, new HangTrongO("B", b, 1));
        Assert.Equal(725, kq.QMax);
        Assert.Equal(0.875m, kq.TheTichConLai);
        // Cùng biến thể: lưới 1000 trừ 100 đang có
        Assert.Equal(900, Tinh(new OChua(1, 1, 1, 1000), a, new HangTrongO("A", a, 100)).QMax);
    }
}
