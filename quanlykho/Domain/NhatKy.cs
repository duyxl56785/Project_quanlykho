namespace quanlykho.Domain;

/// <summary>Nhật ký thao tác quan trọng. Chỉ thêm, không sửa, không xóa.</summary>
public class NhatKy
{
    public long Id { get; set; }
    public DateTimeOffset ThoiDiem { get; set; }
    public string? MaNguoiDung { get; set; }
    public string? MaKho { get; set; }
    public string LoaiThaoTac { get; set; } = "";
    public string DoiTuong { get; set; } = "";
    public string? MaDoiTuong { get; set; }
    /// <summary>JSON tùy thao tác (giá trị trước, sau, lý do...).</summary>
    public string? ChiTiet { get; set; }
}

/// <summary>Tệp Excel đã nhập thành công; mã băm SHA-256 của nội dung chặn nhập lặp cùng một tệp.</summary>
public class TepDaNhap
{
    public string MaBam { get; set; } = "";
    public string TenTep { get; set; } = "";
    /// <summary>"DanhMuc" hoặc "TonDauKy".</summary>
    public string Loai { get; set; } = "";
    public int SoDong { get; set; }
    public DateTimeOffset ThoiDiem { get; set; }
    public string NguoiNhap { get; set; } = "";
    /// <summary>Phiếu sinh từ tệp (tồn đầu kỳ); hủy phiếu Lưu tạm thì bỏ dòng này để nhập lại được.</summary>
    public string? MaChungTu { get; set; }
}
