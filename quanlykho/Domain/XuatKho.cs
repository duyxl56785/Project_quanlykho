namespace quanlykho.Domain;

/// <summary>Đơn đặt hàng của nhân viên bán hàng; mỗi đơn xuất từ một kho.</summary>
public class DonDatHang
{
    public string MaDonDat { get; set; } = "";
    public string MaKho { get; set; } = "";
    public string MaNoiNhan { get; set; } = "";
    public string NguoiTao { get; set; } = "";
    public DateTimeOffset NgayTao { get; set; }
    public DateOnly? HanGiao { get; set; }
    public TrangThaiDonDat TrangThai { get; set; }
    public string? GhiChu { get; set; }
    public List<ChiTietDonDat> ChiTiet { get; set; } = [];
}

public class ChiTietDonDat
{
    public string MaDonDat { get; set; } = "";
    public string MaBienThe { get; set; } = "";
    public string TenDonVi { get; set; } = "";
    public int HeSo { get; set; } = 1;
    /// <summary>Theo đơn vị đã chọn trên dòng.</summary>
    public decimal SoLuongDat { get; set; }
    /// <summary>Theo đơn vị tính chính, cập nhật khi xác nhận phiếu xuất.</summary>
    public decimal SoLuongDaXuat { get; set; }
}

/// <summary>Lịch sử gợi ý AI (G1, G3): đầu vào, kết quả và người dùng có chấp nhận không, để thống kê tỷ lệ chấp nhận.</summary>
public class LichSuGoiY
{
    public long MaGoiY { get; set; }
    public LoaiGoiY Loai { get; set; }
    public string MaKho { get; set; } = "";
    /// <summary>G1 gợi ý cho một biến thể; G3 gợi ý cả danh sách nên để trống.</summary>
    public string? MaBienThe { get; set; }
    public DateTimeOffset ThoiDiem { get; set; }
    public string NguoiDung { get; set; } = "";
    /// <summary>JSON.</summary>
    public string DauVao { get; set; } = "";
    /// <summary>JSON.</summary>
    public string KetQua { get; set; } = "";
    /// <summary>Null khi chưa biết người dùng có làm theo không.</summary>
    public bool? ChapNhan { get; set; }
    public DateTimeOffset? ThoiDiemPhanHoi { get; set; }
    /// <summary>Chứng từ dùng gợi ý (đơn đặt khi giữ hàng, lệnh xuất khi lấy hàng, đơn mua ở G3).</summary>
    public string? MaChungTu { get; set; }
}
