using quanlykho.Domain;

namespace quanlykho.Common.GiaoDien;

/// <param name="Url">null khi màn hình chưa làm: hiện mờ trên menu.</param>
/// <param name="VaiTro">null là mọi vai; _Layout chỉ hiện mục người dùng được phép mở.</param>
public record MucMenu(string Ten, string? Url, VaiTro[]? VaiTro = null);

public record NhomMenu(string Ten, MucMenu[] Muc);

/// <summary>Menu bên trái, một nơi khai báo cho cả hệ thống. Làm xong màn hình nào thì điền Url ở đây.</summary>
public static class Menu
{
    private static readonly VaiTro[] QuanTri = [VaiTro.QuanTriVien];

    public static readonly NhomMenu[] TatCa =
    [
        new("Chung", [new("Tổng quan", "/")]),
        new("Nhập kho", [new("Đơn mua", "/DonMua", [VaiTro.QuanTriVien, VaiTro.QuanLyKho, VaiTro.ThuKho]), new("Hóa đơn nhập", "/PhieuNhap", [VaiTro.QuanTriVien, VaiTro.QuanLyKho, VaiTro.ThuKho]), new("Xếp hàng", "/XepHang", [VaiTro.QuanTriVien, VaiTro.QuanLyKho, VaiTro.ThuKho])]),
        new("Xuất kho", [new("Tồn khả dụng", "/TonKhaDung"), new("Đơn đặt hàng", "/DonDat", [VaiTro.QuanTriVien, VaiTro.QuanLyKho, VaiTro.NhanVienBanHang]), new("Lệnh xuất", null)]),
        new("Kho", [new("Chuyển kho", null), new("Kiểm kê", null), new("Báo cáo tồn", null)]),
        new("Danh mục", [new("Hàng hóa", "/DanhMuc/SanPham"), new("Nhóm hàng", "/DanhMuc/NhomHang"), new("Danh mục kho", "/Kho", QuanTri), new("Kho và vị trí", "/ViTri", [VaiTro.QuanTriVien, VaiTro.QuanLyKho, VaiTro.ThuKho]), new("Nhà cung cấp, nơi nhận", "/DanhMuc/NhaCungCap")]),
        new("Hệ thống", [new("Người dùng", "/NguoiDung", QuanTri), new("Nhật ký", null, QuanTri), new("Styleguide", "/Styleguide", QuanTri)]),
    ];
}
