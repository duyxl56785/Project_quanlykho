namespace quanlykho.Domain;

public class NguoiDung
{
    public string MaNguoiDung { get; set; } = "";
    public string Ten { get; set; } = "";
    /// <summary>Lưu chữ thường; dùng để đăng nhập.</summary>
    public string Email { get; set; } = "";
    public string? SoDienThoai { get; set; }
    public string MatKhauBam { get; set; } = "";
    public VaiTro VaiTro { get; set; }
    public TrangThaiTaiKhoan TrangThai { get; set; }
    /// <summary>Đổi mỗi khi đổi vai, kho, mật khẩu hoặc khóa để phiên đăng nhập cũ hết hiệu lực.</summary>
    public string SecurityStamp { get; set; } = "";
    public int SoLanDangNhapSai { get; set; }
    public DateTimeOffset? KhoaDenLuc { get; set; }
}

public class NguoiDungKho
{
    public string MaNguoiDung { get; set; } = "";
    public string MaKho { get; set; } = "";
}
