using quanlykho.Common.GiaoDien;
using quanlykho.Common.Loi;
using quanlykho.Features.DanhMuc;

namespace quanlykho.Tests.Unit;

public class QuyDoiDonViTests
{
    private static readonly DonViChon[] Chai = [new("chai", 1), new("lốc", 6), new("thùng", 24)];

    [Fact]
    public void MuoiThung_RaHaiTramBonMuoiChai()
    {
        Assert.Equal(new KetQuaQuyDoi("thùng", 24, 240), QuyDoiDonVi.QuyVeChinh(Chai, "thùng", 10));
        Assert.Equal(new KetQuaQuyDoi("thùng", 24, 36), QuyDoiDonVi.QuyVeChinh(Chai, " THÙNG ", 1.5m));
        Assert.Equal(new KetQuaQuyDoi("chai", 1, 7), QuyDoiDonVi.QuyVeChinh(Chai, null, 7));
        Assert.Throws<NghiepVuException>(() => QuyDoiDonVi.QuyVeChinh(Chai, "két", 1));
    }

    [Theory]
    [InlineData(235, "9 thùng 19 chai")]
    [InlineData(240, "10 thùng")]
    [InlineData(19, "19 chai")]
    [InlineData(0, "0 chai")]
    [InlineData(-30, "−1 thùng 6 chai")]
    [InlineData(24.5, "1 thùng 0,5 chai")]
    public void HienThi_QuyRaDonViLonNhat(decimal soLuong, string mong) => Assert.Equal(mong, QuyDoiDonVi.HienThi(Chai, soLuong));

    [Fact]
    public void KhongCoDonViQuyDoi_ChiHienDonViChinh() => Assert.Equal("1.235 chai", QuyDoiDonVi.HienThi([new("chai", 1)], 1235));
}
