using System.ComponentModel.DataAnnotations;
using quanlykho.Common.GiaoDien;
using quanlykho.Domain;

namespace quanlykho.Features.NhapKho;

public record PhieuNhapDong(string Ma, LoaiPhieuNhap LoaiPhieu, string? MaDonMua, string MaKho, DateTimeOffset NgayNhap, int SoDong, decimal? TongTien, TrangThaiPhieu TrangThai);

/// <summary>Đơn giá, thành tiền là null khi người xem không được thấy giá (thủ kho).</summary>
public record DongPhieuNhap(int STT, string MaLo, string MaBienThe, string TenHang, string TenDonVi, decimal SoLuong, DateOnly? HanDung, ChatLuong ChatLuong, decimal? DonGia, decimal? ThanhTien);

/// <summary>Hàng nhận được trên màn hình quét; ConNhan chỉ có với phiếu từ đơn mua.</summary>
public record HangNhan(string MaBienThe, string? MaVach, string TenHang, string TenDonVi, bool TheoDoiHanDung, decimal? ConNhan);

public record PhieuNhapChiTiet(string Ma, LoaiPhieuNhap LoaiPhieu, string? MaDonMua, string? TenNhaCungCap, string MaKho, string TenKho, string NguoiLap, DateTimeOffset NgayNhap,
    TrangThaiPhieu TrangThai, string? GhiChu, bool XemGia, List<DongPhieuNhap> Dong, List<HangNhan> Hang)
{
    public decimal? TongTien => XemGia ? Dong.Sum(d => d.ThanhTien ?? 0) : null;
}

public class PhieuNhapForm
{
    [Display(Name = "Loại phiếu")]
    public LoaiPhieuNhap LoaiPhieu { get; set; } = LoaiPhieuNhap.MuaHang;

    [Display(Name = "Đơn mua")]
    public string? MaDonMua { get; set; }

    [Display(Name = "Kho nhập")]
    public string? MaKho { get; set; }

    [StringLength(500), Display(Name = "Ghi chú")]
    public string? GhiChu { get; set; }

    public List<LuaChon> DanhSachDonMua { get; set; } = [];
    public List<LuaChon> DanhSachKho { get; set; } = [];
}

/// <summary>Ma là mã vạch hoặc mã biến thể. DonGia chỉ dùng cho phiếu nhập khác do quản lý lập; phiếu từ đơn mua lấy giá theo đơn.</summary>
public record DongPhieuNhapForm(string? Ma, decimal SoLuong, DateOnly? HanDung, ChatLuong ChatLuong, decimal? DonGia = null);
