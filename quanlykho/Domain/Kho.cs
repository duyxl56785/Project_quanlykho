namespace quanlykho.Domain;

/// <summary>
/// Vị trí cây 7 cấp (Bảng 2.31): 1 kho, 2 tòa, 3 tầng, 4 phòng – dãy, 5 hàng, 6 kệ, 7 ô. Kho là vị trí cấp 1.
/// Hàng chỉ nằm ở cấp ô.
/// </summary>
public class ViTri
{
    public string MaViTri { get; set; } = "";
    public string? MaCha { get; set; }
    /// <summary>Mã vị trí cấp 1 chứa vị trí này; ở cấp 1 bằng chính MaViTri.</summary>
    public string MaKho { get; set; } = "";
    public short Cap { get; set; }
    public string Ten { get; set; } = "";
    public string TenDayDu { get; set; } = "";
    public int ThuTu { get; set; }
    /// <summary>Khai ở cấp phòng – dãy, cấp con kế thừa.</summary>
    public LoaiKhu? LoaiKhu { get; set; }
    public DieuKienBaoQuan? DieuKienBaoQuan { get; set; }
    public decimal? Dai { get; set; }
    public decimal? Rong { get; set; }
    public decimal? Cao { get; set; }
    public decimal? TaiTrongToiDa { get; set; }
    /// <summary>Ô mặc định nhận hàng của khu tiếp nhận hoặc khu cách ly trong kho.</summary>
    public bool LaOMacDinh { get; set; }
    public bool DangKhoa { get; set; }
    /// <summary>Phiếu kiểm kê đang khóa vị trí này (null khi không kiểm kê).</summary>
    public string? MaPhieuKiemKe { get; set; }

    // Chỉ dùng ở cấp kho.
    public string? DiaChi { get; set; }
    public string? LoaiHangLuuTru { get; set; }
    public decimal? HeSoLapDay { get; set; }
}
