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

/// <summary>Hàng giữ cho đơn đặt khi duyệt: đơn nào giữ bao nhiêu ở lô, ô nào; số giữ đã cộng vào TonKho.SoLuongDaGiu qua cổng ghi tồn.</summary>
public class GiuHang
{
    public string MaDonDat { get; set; } = "";
    public string MaLo { get; set; } = "";
    public string MaViTri { get; set; } = "";
    /// <summary>Theo đơn vị tính chính.</summary>
    public decimal SoLuong { get; set; }
    /// <summary>Thứ tự ghé do G1 sắp theo mã ô.</summary>
    public int ThuTu { get; set; }
    public long? MaGoiY { get; set; }
    public DateTimeOffset ThoiDiem { get; set; }
    public string NguoiGiu { get; set; } = "";
}

/// <summary>Lệnh xuất kho: một đơn đặt một lệnh, giao cho một thủ kho của kho xuất.</summary>
public class LenhXuat
{
    public string MaLenh { get; set; } = "";
    public string MaDonDat { get; set; } = "";
    public string MaKho { get; set; } = "";
    public string NguoiLap { get; set; } = "";
    public DateTimeOffset NgayLap { get; set; }
    public string NguoiThucHien { get; set; } = "";
    public string? NguoiKiemTra { get; set; }
    public string? GhiChuKiemTra { get; set; }
    public TrangThaiLenhXuat TrangThai { get; set; }
    public List<ChiTietLenhXuat> ChiTiet { get; set; } = [];
}

public class ChiTietLenhXuat
{
    public string MaLenh { get; set; } = "";
    public string MaBienThe { get; set; } = "";
    public string TenDonVi { get; set; } = "";
    public int HeSo { get; set; } = 1;
    /// <summary>Theo đơn vị đã chọn.</summary>
    public decimal SoLuongGoc { get; set; }
    /// <summary>Theo đơn vị tính chính = SoLuongGoc × HeSo.</summary>
    public decimal SoLuongYeuCau { get; set; }
}

/// <summary>Một lần thủ kho lấy hàng: lô nào, ở ô nào, bao nhiêu (đơn vị tính chính).</summary>
public class DongLayHang
{
    public long MaDong { get; set; }
    public string MaLenh { get; set; } = "";
    public string MaBienThe { get; set; } = "";
    public string MaLo { get; set; } = "";
    public string MaViTri { get; set; } = "";
    public decimal SoLuong { get; set; }
    /// <summary>Phần lấy ngoài số đã giữ, được giữ thêm lúc lấy; xóa dòng thì bỏ giữ phần này.</summary>
    public decimal SoLuongGiuThem { get; set; }
    public string NguoiLay { get; set; } = "";
    public DateTimeOffset ThoiDiem { get; set; }
}

/// <summary>Phiếu xuất ghi sổ: xuất bán (sinh khi xác nhận lệnh), xuất khác, trả nhà cung cấp; chỉ Hoàn tất mới ghi tồn.</summary>
public class PhieuXuat
{
    public string MaPhieuXuat { get; set; } = "";
    public LoaiPhieuXuat LoaiPhieu { get; set; }
    public string? MaLenh { get; set; }
    public string MaKho { get; set; } = "";
    public string? MaNhaCungCap { get; set; }
    public string NguoiLap { get; set; } = "";
    public string? NguoiXacNhan { get; set; }
    public DateTimeOffset NgayXuat { get; set; }
    public TrangThaiPhieu TrangThai { get; set; }
    public string? LyDo { get; set; }
    public List<ChiTietPhieuXuat> ChiTiet { get; set; } = [];
}

public class ChiTietPhieuXuat
{
    public string MaPhieuXuat { get; set; } = "";
    public int STT { get; set; }
    public string MaLo { get; set; } = "";
    public string MaViTri { get; set; } = "";
    public string TenDonVi { get; set; } = "";
    public int HeSo { get; set; } = 1;
    public decimal SoLuongGoc { get; set; }
    /// <summary>Theo đơn vị tính chính.</summary>
    public decimal SoLuong { get; set; }
}
