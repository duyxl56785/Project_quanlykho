namespace quanlykho.Common.GiaoDien;

/// <summary>
/// Tên tiếng Việt và màu cố định của từng trạng thái (Bảng 1.12: mỗi trạng thái một màu).
/// Khóa là tên giá trị enum; enum trạng thái mới phải thêm vào đây.
/// Màu: xam (chưa bắt đầu), lam (đang làm), vang (chờ người khác), luc (xong), do (hủy, khóa).
/// </summary>
public static class TrangThaiHienThi
{
    private static readonly Dictionary<string, (string Ten, string Mau)> Bang = new()
    {
        ["Nhap"] = ("Nháp", "xam"),
        ["ChuaThucHien"] = ("Chưa thực hiện", "xam"),
        ["DangThucHien"] = ("Đang thực hiện", "lam"),
        ["HoanThanh"] = ("Hoàn thành", "luc"),
        ["DaHuy"] = ("Đã hủy", "do"),
        ["LuuTam"] = ("Lưu tạm", "xam"),
        ["HoanTat"] = ("Hoàn tất", "luc"),
        ["DangHoatDong"] = ("Đang hoạt động", "luc"),
        ["DaKhoa"] = ("Đã khóa", "do"),
        ["DangSuDung"] = ("Đang sử dụng", "luc"),
        ["NgungSuDung"] = ("Ngừng sử dụng", "xam"),
        ["Dat"] = ("Đạt", "luc"),
        ["Loi"] = ("Lỗi", "do"),
    };

    public static (string Ten, string Mau) Lay(Enum trangThai) => Lay(trangThai.ToString());

    public static (string Ten, string Mau) Lay(string tenGiaTri) =>
        Bang.TryGetValue(tenGiaTri, out var v) ? v : (tenGiaTri, "xam");
}
