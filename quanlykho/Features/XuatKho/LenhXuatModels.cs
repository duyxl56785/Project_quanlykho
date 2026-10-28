using quanlykho.Common.GiaoDien;
using quanlykho.Domain;

namespace quanlykho.Features.XuatKho;

public record LenhXuatDong(string Ma, string MaDonDat, string MaKho, string NguoiThucHien, DateTimeOffset NgayLap, TrangThaiLenhXuat TrangThai);

public record DongYeuCauXuat(string MaBienThe, string TenHang, string DonViTinh, decimal YeuCau, decimal DaLay)
{
    public decimal ConLai => YeuCau - DaLay;
}

public record DongDaLay(long MaDong, string MaBienThe, string MaLo, string MaViTri, decimal SoLuong, string NguoiLay, DateTimeOffset ThoiDiem);

public record LenhXuatChiTiet(string Ma, string MaDonDat, string MaKho, string TenKho, string NoiNhan, string MaNguoiThucHien, string NguoiThucHien,
    string? MaNguoiKiemTra, string? NguoiKiemTra, string? GhiChuKiemTra, DateTimeOffset NgayLap, TrangThaiLenhXuat TrangThai, string? MaPhieuXuat,
    List<DongYeuCauXuat> Dong, List<HangDaGiu> GoiY, List<DongDaLay> DaLay, Dictionary<string, DonViChon>? MaVachDonVi = null)
{
    public bool DangLay => TrangThai is TrangThaiLenhXuat.ChuaThucHien or TrangThaiLenhXuat.ChoLayHang;
}

/// <summary>Quét ô rồi quét hàng: Ma là mã lô, mã biến thể hoặc mã vạch (của biến thể hoặc của thùng); SoLuong theo TenDonVi, trống là đơn vị của mã vạch quét hoặc đơn vị chính.</summary>
public record LayHangYeuCau(string MaViTri, string Ma, decimal SoLuong, string? TenDonVi = null);

public record LapLenhYeuCau(string MaDonDat, string NguoiThucHien);

public record KiemTraYeuCau(bool Dung, string? GhiChu);
