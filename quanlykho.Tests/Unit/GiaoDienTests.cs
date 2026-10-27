using quanlykho.Common.GiaoDien;
using quanlykho.Domain;

namespace quanlykho.Tests.Unit;

public class GiaoDienTests
{
    [Theory]
    [InlineData("1234.5", "1.234,5")]
    [InlineData("240", "240")]
    [InlineData("0.125", "0,125")]
    [InlineData("1000000", "1.000.000")]
    public void So_KieuVietNam(string vao, string ra) => Assert.Equal(ra, DinhDang.So(decimal.Parse(vao, System.Globalization.CultureInfo.InvariantCulture)));

    [Fact]
    public void Tien_KieuVietNam() => Assert.Equal("1.234.500 ₫", DinhDang.Tien(1234500m));

    [Fact]
    public void GiaTriNull_ChuoiRong()
    {
        Assert.Equal("", DinhDang.So(null));
        Assert.Equal("", DinhDang.Tien(null));
        Assert.Equal("", DinhDang.Ngay((DateOnly?)null));
    }

    [Fact]
    public void NgayGio_DoiSangGioVietNam()
    {
        // 18:30 UTC ngày 31/10 là 01:30 ngày 01/11 giờ Việt Nam (UTC+7).
        var utc = new DateTimeOffset(2026, 10, 31, 18, 30, 0, TimeSpan.Zero);
        Assert.Equal("01/11/2026 01:30", DinhDang.NgayGio(utc));
        Assert.Equal("01/11/2026", DinhDang.Ngay(utc));
        Assert.Equal("05/11/2026", DinhDang.Ngay(new DateOnly(2026, 11, 5)));
    }

    [Fact]
    public void TrangThai_CoTenTiengVietVaMau()
    {
        Assert.Equal(("Hoàn thành", "luc"), TrangThaiHienThi.Lay(TrangThaiDonMua.HoanThanh));
        Assert.Equal(("Đã khóa", "do"), TrangThaiHienThi.Lay(TrangThaiTaiKhoan.DaKhoa));
    }

    [Fact]
    public void MoiTrangThaiHienCo_DeuCoTenTiengViet()
    {
        Enum[] tatCa = [.. Enum.GetValues<TrangThaiDonMua>().Cast<Enum>(), .. Enum.GetValues<TrangThaiPhieu>().Cast<Enum>(),
            .. Enum.GetValues<TrangThaiTaiKhoan>().Cast<Enum>(), .. Enum.GetValues<TrangThaiSuDung>().Cast<Enum>(), .. Enum.GetValues<ChatLuong>().Cast<Enum>()];
        foreach (var tt in tatCa)
            Assert.NotEqual(tt.ToString(), TrangThaiHienThi.Lay(tt).Ten);
    }

    [Theory]
    [InlineData(1, 1, 10)]
    [InlineData(6, 6, 7)]   // trang cuối có 7 dòng
    [InlineData(99, 6, 7)]  // vượt quá thì về trang cuối
    [InlineData(0, 1, 10)]
    public void TrangDuLieu_PhanTrangDung(int trangYeuCau, int trangThucTe, int soDong)
    {
        var trang = TrangDuLieu<int>.Tao(Enumerable.Range(1, 57), trangYeuCau, 10);
        Assert.Equal(trangThucTe, trang.Trang);
        Assert.Equal(soDong, trang.Muc.Count);
        Assert.Equal(6, trang.TongTrang);
        Assert.Equal(57, trang.TongSo);
    }

    [Fact]
    public void TrangDuLieu_Rong_MotTrang()
    {
        var trang = TrangDuLieu<int>.Tao([], 3);
        Assert.Equal(1, trang.Trang);
        Assert.Equal(1, trang.TongTrang);
        Assert.Empty(trang.Muc);
    }
}
