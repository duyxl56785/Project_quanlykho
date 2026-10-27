using quanlykho.Domain;

namespace quanlykho.Common.PhanQuyen;

/// <summary>Tên vai dùng trong [Authorize(Roles = ...)]; giá trị là tên enum VaiTro (claim Role).</summary>
public static class Vai
{
    public const string QuanTri = nameof(VaiTro.QuanTriVien);
    public const string QuanLyKho = nameof(VaiTro.QuanLyKho);
    public const string ThuKho = nameof(VaiTro.ThuKho);
    public const string BanHang = nameof(VaiTro.NhanVienBanHang);

    /// <summary>Quản lý kho và quản trị viên (quản trị viên làm được mọi việc của quản lý kho).</summary>
    public const string QuanLy = QuanTri + "," + QuanLyKho;
    /// <summary>Người làm việc trong kho: thủ kho, quản lý kho, quản trị viên.</summary>
    public const string TrongKho = QuanLy + "," + ThuKho;

    public static string TenHienThi(VaiTro vai) => vai switch
    {
        VaiTro.QuanTriVien => "Quản trị viên",
        VaiTro.QuanLyKho => "Quản lý kho",
        VaiTro.ThuKho => "Thủ kho",
        VaiTro.NhanVienBanHang => "Nhân viên bán hàng",
        _ => vai.ToString()
    };
}
