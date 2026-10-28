namespace quanlykho.Domain;

/// <summary>Phiếu kiểm kê một kho. Bắt đầu thì khóa các ô trong phạm vi (ViTri.MaPhieuKiemKe) và chốt số sổ sách.</summary>
public class PhieuKiemKe
{
    public string MaPhieu { get; set; } = "";
    public string MaKho { get; set; } = "";
    public PhamViKiemKe PhamVi { get; set; }
    /// <summary>Mã vị trí (phạm vi theo vị trí) hoặc mã lô (phạm vi theo lô); trống khi kiểm toàn kho.</summary>
    public List<string> DoiTuong { get; set; } = [];
    public string? MucDich { get; set; }
    public DateOnly NgayKiem { get; set; }
    public string NguoiLap { get; set; } = "";
    public DateTimeOffset NgayLap { get; set; }
    public DateTimeOffset? ThoiDiemBatDau { get; set; }
    public DateTimeOffset? ThoiDiemHoanThanh { get; set; }
    public TrangThaiKiemKe TrangThai { get; set; }
    public List<ThanhVienKiemKe> ThanhVien { get; set; } = [];
    public List<ChiTietKiemKe> ChiTiet { get; set; } = [];
}

public class ThanhVienKiemKe
{
    public string MaPhieu { get; set; } = "";
    public string MaNguoiDung { get; set; } = "";
}

/// <summary>Một (lô, ô) trong phạm vi: số sổ sách chốt lúc bắt đầu, số thực tế do thành viên đếm. Chênh lệch = thực tế − sổ sách, không lưu.</summary>
public class ChiTietKiemKe
{
    public string MaPhieu { get; set; } = "";
    public int STT { get; set; }
    public string MaViTri { get; set; } = "";
    /// <summary>Null khi đếm thấy hàng không có nhãn lô (hàng ngoài danh sách chỉ biết biến thể).</summary>
    public string? MaLo { get; set; }
    public string MaBienThe { get; set; } = "";
    /// <summary>Theo đơn vị tính chính.</summary>
    public decimal SoLuongSoSach { get; set; }
    public decimal? SoLuongThucTe { get; set; }
    public bool NgoaiDanhSach { get; set; }
    public string? NguoiDem { get; set; }
    public DateTimeOffset? ThoiDiemDem { get; set; }
}
