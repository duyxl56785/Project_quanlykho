using quanlykho.Features.NhapKho;

namespace quanlykho.Tests.Unit;

public class DeXuatNhapBoSungTests
{
    private static readonly DateOnly HomNay = new(2026, 11, 30);

    /// <summary>Lần xuất đầu cách hôm nay (soNgay − 1) ngày để N = soNgay.</summary>
    private static HangG3 Hang(decimal a, int soNgay, decimal tongXuat, decimal m = 5, int l = 7, decimal choNhap = 0) =>
        new("H", a, HomNay.AddDays(1 - soNgay), tongXuat, m, l, choNhap);

    [Fact]
    public void TinhTay_DuoiDiemDatHang_DeXuatDungSo()
    {
        // d = 60/30 = 2; điểm đặt = 2 × 7 + 5 = 19; số = 2 × 37 − 10 = 64.
        var kq = DeXuatNhapBoSung.Tinh(Hang(10, 30, 60), HomNay);
        Assert.Equal(KetLuanG3.DeXuat, kq.KetLuan);
        Assert.Equal(30, kq.SoNgay);
        Assert.Equal(2, kq.NhuCau);
        Assert.Equal(19, kq.DiemDatHang);
        Assert.Equal(64, kq.SoDeXuat);
    }

    [Fact]
    public void TruSoDangChoNhap()
    {
        Assert.Equal(44, DeXuatNhapBoSung.Tinh(Hang(10, 30, 60, choNhap: 20), HomNay).SoDeXuat);
        var du = DeXuatNhapBoSung.Tinh(Hang(10, 30, 60, choNhap: 64), HomNay);
        Assert.Equal(KetLuanG3.DuChoNhap, du.KetLuan);
        Assert.Equal(0, du.SoDeXuat);
    }

    [Fact]
    public void BangDiemDatHang_VanDeXuat_VuotThiKhong()
    {
        Assert.Equal(KetLuanG3.DeXuat, DeXuatNhapBoSung.Tinh(Hang(19, 30, 60), HomNay).KetLuan);
        Assert.Equal(KetLuanG3.ChuaCan, DeXuatNhapBoSung.Tinh(Hang(19.001m, 30, 60), HomNay).KetLuan);
    }

    [Fact]
    public void DuoiMuoiBonNgayHoacKhongXuat_KhongDeXuat()
    {
        Assert.Equal(KetLuanG3.ThieuDuLieu, DeXuatNhapBoSung.Tinh(Hang(0, 13, 60), HomNay).KetLuan);
        Assert.Equal(KetLuanG3.DeXuat, DeXuatNhapBoSung.Tinh(Hang(0, 14, 60), HomNay).KetLuan);
        Assert.Equal(KetLuanG3.ThieuDuLieu, DeXuatNhapBoSung.Tinh(Hang(0, 30, 0), HomNay).KetLuan);
        Assert.Equal(KetLuanG3.ThieuDuLieu, DeXuatNhapBoSung.Tinh(new HangG3("H", 0, null, 0, 5, 7, 0), HomNay).KetLuan);
    }

    [Fact]
    public void LamTronLen_SoChiaHetKhongLech()
    {
        // d = 7/21 = 1/3: 37/3 = 12,33 → 13.
        Assert.Equal(13, DeXuatNhapBoSung.Tinh(Hang(0, 21, 7, m: 0), HomNay).SoDeXuat);
        // 7 × (14 + 7)/21 = 7 đúng, không thành 8 do sai số khi chia trước.
        Assert.Equal(7, DeXuatNhapBoSung.Tinh(Hang(0, 21, 7, m: 0, l: 14), HomNay, soNgayDuHang: 7).SoDeXuat);
    }

    [Fact]
    public void LichSuDaiHon90Ngay_ChiaCho90()
    {
        var kq = DeXuatNhapBoSung.Tinh(Hang(0, 200, 180, m: 0), HomNay);
        Assert.Equal(90, kq.SoNgay);
        Assert.Equal(2, kq.NhuCau);
    }

    [Fact]
    public void SoNgayDuHangCauHinhDuoc()
    {
        // d = 2: 2 × (7 + 60) − 10 = 124.
        Assert.Equal(124, DeXuatNhapBoSung.Tinh(Hang(10, 30, 60), HomNay, soNgayDuHang: 60).SoDeXuat);
    }
}
