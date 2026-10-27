namespace quanlykho.Domain;

public class NhomHang
{
    public string MaNhom { get; set; } = "";
    public string? MaCha { get; set; }
    public string Ten { get; set; } = "";
}

/// <summary>Thuộc tính cấu hình theo nhóm hàng (size, màu, công suất...).</summary>
public class ThuocTinh
{
    public string MaNhom { get; set; } = "";
    public string Ten { get; set; } = "";
    public int ThuTu { get; set; }
    public string[] GiaTriGoiY { get; set; } = [];
}

public class SanPham
{
    public string MaSanPham { get; set; } = "";
    public string Ten { get; set; } = "";
    public string DonViTinh { get; set; } = "";
    public string MaNhom { get; set; } = "";
    public DieuKienBaoQuan DieuKienBaoQuan { get; set; }
    public bool TheoDoiHanDung { get; set; }
    public string? MoTa { get; set; }
    public TrangThaiSuDung TrangThai { get; set; }
}

public class BienThe
{
    public string MaBienThe { get; set; } = "";
    public string MaSanPham { get; set; } = "";
    public SanPham SanPham { get; set; } = null!;
    /// <summary>Giá trị thuộc tính, ví dụ { "Size": "M", "Màu": "Đỏ" }; lưu jsonb.</summary>
    public Dictionary<string, string> ThuocTinh { get; set; } = [];
    public decimal DaiDongGoi { get; set; }
    public decimal RongDongGoi { get; set; }
    public decimal CaoDongGoi { get; set; }
    public decimal KhoiLuong { get; set; }
    public string? MaVach { get; set; }
    public TrangThaiSuDung TrangThai { get; set; }
}

public class DonViQuyDoi
{
    public string MaBienThe { get; set; } = "";
    public string TenDonVi { get; set; } = "";
    public int HeSo { get; set; }
    public string? MaVach { get; set; }
    public TrangThaiSuDung TrangThai { get; set; }
}

public class NhaCungCap
{
    public string MaNhaCungCap { get; set; } = "";
    public string Ten { get; set; } = "";
    public string? DiaChi { get; set; }
    public string? NguoiLienHe { get; set; }
    public string? SoDienThoai { get; set; }
    public TrangThaiSuDung TrangThai { get; set; }
}

public class NoiNhan
{
    public string MaNoiNhan { get; set; } = "";
    public string Ten { get; set; } = "";
    public string? DiaChi { get; set; }
    public string? NguoiLienHe { get; set; }
    public string? SoDienThoai { get; set; }
    public TrangThaiSuDung TrangThai { get; set; }
}

/// <summary>Tồn tối thiểu, tối đa và thời gian chờ hàng (ngày) của một biến thể tại một kho.</summary>
public class MucTon
{
    public string MaBienThe { get; set; } = "";
    public string MaKho { get; set; } = "";
    public decimal? ToiThieu { get; set; }
    public decimal? ToiDa { get; set; }
    public int ThoiGianChoHang { get; set; } = 7;
}
