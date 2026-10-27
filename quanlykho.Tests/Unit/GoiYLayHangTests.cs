using quanlykho.Features.XuatKho;

namespace quanlykho.Tests.Unit;

public class GoiYLayHangTests
{
    private static readonly DateOnly HomNay = new(2026, 11, 1);
    private static readonly DateTimeOffset Ngay1 = new(2026, 9, 1, 0, 0, 0, TimeSpan.Zero);

    private static UngVienLay U(string lo, string o, decimal khaDung, DateOnly? han = null, int nhapSau = 0) =>
        new(lo, o, khaDung, han, Ngay1.AddDays(nhapSau));

    [Fact]
    public void KhongBaoGioChonLoHetHan()
    {
        var kq = GoiYLayHang.Tinh([U("HET", "K01-KE01-O01", 100, HomNay.AddDays(-1)), U("CON", "K01-KE01-O02", 3, HomNay.AddDays(30))], 10, HomNay);
        Assert.Equal(["CON"], kq.Dong.Select(d => d.MaLo));
        Assert.Equal(7, kq.ConThieu);
    }

    [Fact]
    public void LoHetHanHomNay_VanDuocChon()
    {
        Assert.Equal(0, GoiYLayHang.Tinh([U("A", "O1", 5, HomNay)], 5, HomNay).ConThieu);
    }

    [Fact]
    public void FefoTruoc_LoKhongHanXepSau_RoiFifo()
    {
        var kq = GoiYLayHang.Tinh(
        [
            U("KHONGHAN", "O1", 10),
            U("HAN_XA", "O2", 10, HomNay.AddDays(90)),
            U("HAN_GAN", "O3", 4, HomNay.AddDays(10)),
        ], 12, HomNay);
        Assert.Equal(["HAN_GAN", "HAN_XA"], kq.Dong.OrderBy(d => d.HanDung).Select(d => d.MaLo));
        Assert.Equal((4m, 8m), (kq.Dong.Single(d => d.MaLo == "HAN_GAN").SoLuong, kq.Dong.Single(d => d.MaLo == "HAN_XA").SoLuong));
        Assert.StartsWith("hết hạn sớm nhất 11/11/2026", kq.Dong.Single(d => d.MaLo == "HAN_GAN").LyDo);

        var fifo = GoiYLayHang.Tinh([U("MOI", "O1", 10, nhapSau: 5), U("CU", "O2", 10, nhapSau: 1)], 10, HomNay);
        Assert.Equal("CU", fifo.Dong.Single().MaLo);
        Assert.StartsWith("nhập sớm nhất", fifo.Dong.Single().LyDo);
    }

    [Fact]
    public void TongDungQ_KhongVuotKhaDungTungO()
    {
        var ungVien = new[] { U("A", "O3", 2.5m), U("A", "O1", 4), U("B", "O2", 10, nhapSau: 1) };
        var kq = GoiYLayHang.Tinh(ungVien, 9, HomNay);
        Assert.Equal(9, kq.TongLay);
        Assert.Equal(0, kq.ConThieu);
        Assert.All(kq.Dong, d => Assert.True(d.SoLuong <= ungVien.Single(u => u.MaLo == d.MaLo && u.MaViTri == d.MaViTri).KhaDung));
        Assert.Equal(["O1", "O2", "O3"], kq.Dong.Select(d => d.MaViTri));
        Assert.Equal([1, 2, 3], kq.Dong.Select(d => d.ThuTu));
    }

    [Fact]
    public void TrongLo_UuTienMotODuThayViNhieuO()
    {
        var kq = GoiYLayHang.Tinh([U("A", "O1", 3), U("A", "O2", 3), U("A", "O3", 8)], 6, HomNay);
        Assert.Equal(["O3"], kq.Dong.Select(d => d.MaViTri));
        Assert.EndsWith("lấy đủ ở một ô", kq.Dong.Single().LyDo);

        var khongODu = GoiYLayHang.Tinh([U("A", "O1", 3), U("A", "O2", 5), U("A", "O3", 4)], 9, HomNay);
        Assert.Equal(["O2", "O3"], khongODu.Dong.Select(d => d.MaViTri));
    }

    [Fact]
    public void CungHanDung_ChonLoItKeHon()
    {
        var han = HomNay.AddDays(20);
        var kq = GoiYLayHang.Tinh([U("RAI", "KE01-O1", 5, han), U("RAI", "KE02-O1", 5, han), U("GON", "KE03-O1", 10, han)], 10, HomNay);
        Assert.Equal([("GON", "KE03-O1")], kq.Dong.Select(d => (d.MaLo, d.MaViTri)));
    }

    [Fact]
    public void ThieuHang_BaoDungSoConThieu()
    {
        var kq = GoiYLayHang.Tinh([U("A", "O1", 3), U("B", "O2", 2)], 10, HomNay);
        Assert.Equal((5m, 5m), (kq.TongLay, kq.ConThieu));
        Assert.Equal(10, GoiYLayHang.Tinh([], 10, HomNay).ConThieu);
    }
}
