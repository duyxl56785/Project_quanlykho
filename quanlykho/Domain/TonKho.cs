namespace quanlykho.Domain;

public class Lo
{
    public string MaLo { get; set; } = "";
    public string MaBienThe { get; set; } = "";
    public DateOnly? HanDung { get; set; }
    public DateTimeOffset NgayNhap { get; set; }
}

/// <summary>Tồn theo (lô, vị trí), luôn theo đơn vị tính chính. Chỉ cổng ghi tồn được sửa.</summary>
public class TonKho
{
    public string MaLo { get; set; } = "";
    public string MaViTri { get; set; } = "";
    public decimal SoLuong { get; set; }
    public decimal SoLuongDaGiu { get; set; }
}

/// <summary>Sổ biến động: mỗi dòng một lần tồn thay đổi. Chỉ thêm, không sửa, không xóa.</summary>
public class SoBienDong
{
    public long MaBienDong { get; set; }
    public string MaLo { get; set; } = "";
    public string MaViTri { get; set; } = "";
    public decimal SoLuongThayDoi { get; set; }
    public string LoaiChungTu { get; set; } = "";
    public string MaChungTu { get; set; } = "";
    public string MaKhoaChongLap { get; set; } = "";
    public DateTimeOffset ThoiDiem { get; set; }
    public string NguoiThucHien { get; set; } = "";
}
