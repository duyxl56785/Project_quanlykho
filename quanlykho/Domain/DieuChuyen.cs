namespace quanlykho.Domain;

/// <summary>Phiếu điều chuyển giữa hai kho, hai bước: kho đi lưu thì trừ tồn, kho đến nhận vào ô tiếp nhận mặc định.</summary>
public class PhieuDieuChuyen
{
    public string MaPhieu { get; set; } = "";
    public string MaKhoDi { get; set; } = "";
    public string MaKhoDen { get; set; } = "";
    public DateOnly NgayChuyen { get; set; }
    public string? PhuongTien { get; set; }
    public string? NguoiVanChuyen { get; set; }
    public string? LyDo { get; set; }
    public string NguoiLap { get; set; } = "";
    public DateTimeOffset NgayLap { get; set; }
    public DateTimeOffset? ThoiDiemXuat { get; set; }
    public string? NguoiNhan { get; set; }
    public DateTimeOffset? ThoiDiemNhan { get; set; }
    public TrangThaiDieuChuyen TrangThai { get; set; }
    public List<ChiTietDieuChuyen> ChiTiet { get; set; } = [];
}

public class ChiTietDieuChuyen
{
    public string MaPhieu { get; set; } = "";
    public int STT { get; set; }
    public string MaLo { get; set; } = "";
    /// <summary>Ô ở kho đi lấy hàng ra; hủy phiếu thì cộng trả về đúng ô này.</summary>
    public string MaViTriDi { get; set; } = "";
    public string TenDonVi { get; set; } = "";
    public int HeSo { get; set; } = 1;
    public decimal SoLuongGoc { get; set; }
    /// <summary>Theo đơn vị tính chính.</summary>
    public decimal SoLuong { get; set; }
    /// <summary>Null khi chưa nhận; nhỏ hơn SoLuong là chênh lệch chờ xử lý.</summary>
    public decimal? SoLuongNhan { get; set; }
    public XuLyChenhLech? CachXuLy { get; set; }
    public string? LyDoXuLy { get; set; }
    public string? NguoiXuLy { get; set; }
    public DateTimeOffset? ThoiDiemXuLy { get; set; }
}
