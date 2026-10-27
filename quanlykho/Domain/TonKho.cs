namespace quanlykho.Domain;

public class Lo
{
    public string MaLo { get; set; } = "";
    public string MaBienThe { get; set; } = "";
    public BienThe BienThe { get; set; } = null!;
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

public enum LoaiChuyenViTri { XepHang, ChuyenTrongKho }

/// <summary>Mỗi lần chuyển một lô giữa hai ô cùng kho (xếp hàng, chuyển vị trí); tồn đổi qua cổng ghi tồn với MaChungTu = MaChuyen.</summary>
public class ChuyenViTri
{
    public string MaChuyen { get; set; } = "";
    public LoaiChuyenViTri Loai { get; set; }
    public string MaKho { get; set; } = "";
    public string MaLo { get; set; } = "";
    public string MaViTriDi { get; set; } = "";
    public string MaViTriDen { get; set; } = "";
    public decimal SoLuong { get; set; }
    public DateTimeOffset ThoiDiem { get; set; }
    public string NguoiThucHien { get; set; } = "";
}
