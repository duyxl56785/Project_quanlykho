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
