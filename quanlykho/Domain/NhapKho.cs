namespace quanlykho.Domain;

/// <summary>Đơn mua đã gắn một kho, đóng vai trò lệnh nhập kho.</summary>
public class DonMua
{
    public string MaDonMua { get; set; } = "";
    public string MaNhaCungCap { get; set; } = "";
    public string MaKho { get; set; } = "";
    public string NguoiTao { get; set; } = "";
    public DateTimeOffset NgayTao { get; set; }
    public TrangThaiDonMua TrangThai { get; set; }
    public string? GhiChu { get; set; }
    public List<ChiTietDonMua> ChiTiet { get; set; } = [];
}

public class ChiTietDonMua
{
    public string MaDonMua { get; set; } = "";
    public string MaBienThe { get; set; } = "";
    public string TenDonVi { get; set; } = "";
    public int HeSo { get; set; } = 1;
    /// <summary>Theo đơn vị đã chọn trên dòng.</summary>
    public decimal SoLuongDat { get; set; }
    /// <summary>Giá một đơn vị đã chọn trên dòng.</summary>
    public decimal DonGia { get; set; }
}

/// <summary>Phiếu nhập kho (hóa đơn nhập khi LoaiPhieu = MuaHang).</summary>
public class PhieuNhap
{
    public string MaPhieuNhap { get; set; } = "";
    public LoaiPhieuNhap LoaiPhieu { get; set; }
    public string? MaDonMua { get; set; }
    public string MaKho { get; set; } = "";
    public string NguoiLap { get; set; } = "";
    public string? NguoiXacNhan { get; set; }
    public DateTimeOffset NgayNhap { get; set; }
    public TrangThaiPhieu TrangThai { get; set; }
    public string? GhiChu { get; set; }
    public List<ChiTietPhieuNhap> ChiTiet { get; set; } = [];
}

public class ChiTietPhieuNhap
{
    public string MaPhieuNhap { get; set; } = "";
    public int STT { get; set; }
    /// <summary>Biến thể suy ra từ lô.</summary>
    public string MaLo { get; set; } = "";
    public string TenDonVi { get; set; } = "";
    public int HeSo { get; set; } = 1;
    /// <summary>Theo đơn vị đã chọn.</summary>
    public decimal SoLuongGoc { get; set; }
    /// <summary>Theo đơn vị tính chính = SoLuongGoc × HeSo.</summary>
    public decimal SoLuongThucNhan { get; set; }
    public decimal DonGia { get; set; }
    /// <summary>Cột tính lưu sẵn = SoLuongGoc × DonGia.</summary>
    public decimal ThanhTien { get; private set; }
    public ChatLuong ChatLuong { get; set; }
    /// <summary>Ô ghi tồn khi xác nhận (phiếu tồn đầu kỳ); null là ô mặc định khu tiếp nhận hoặc cách ly theo chất lượng.</summary>
    public string? MaViTri { get; set; }
}
