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
