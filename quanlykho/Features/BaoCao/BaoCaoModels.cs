using Microsoft.AspNetCore.Mvc.ModelBinding;
using quanlykho.Common.GiaoDien;
using quanlykho.Domain;

namespace quanlykho.Features.BaoCao;

/// <summary>Bộ lọc chung của các báo cáo (query string); mỗi báo cáo dùng phần cần thiết.</summary>
public class LocBaoCao
{
    public string? MaKho { get; set; }
    /// <summary>Mã biến thể, tên sản phẩm hoặc mã vạch.</summary>
    public string? TuKhoa { get; set; }
    /// <summary>Lấy cả nhóm con.</summary>
    public string? MaNhom { get; set; }
    public DateOnly? TuNgay { get; set; }
    public DateOnly? DenNgay { get; set; }
    public LoaiKhu? LoaiKhu { get; set; }
    public string? MaBienThe { get; set; }
    public string? MaLo { get; set; }
    public string? MaViTri { get; set; }

    /// <summary>Lỗi bộ lọc do Service đặt, hiện trên trang.</summary>
    [BindNever]
    public string? Loi { get; set; }
}

/// <summary>Dữ liệu cho thanh lọc: kho được giao và nhóm hàng.</summary>
public record DanhSachLoc(List<LuaChon> Kho, List<LuaChon> Nhom);

public record BaoCaoTrang<T>(LocBaoCao Loc, DanhSachLoc DanhSach, TrangDuLieu<T> Trang);

/// <param name="TheoKho">Tồn (mọi khu) của từng kho đang xem.</param>
/// <param name="VanChuyen">Phần chưa nhận của phiếu điều chuyển đi hoặc đến các kho đang xem; không nằm trong TonKho.</param>
public record DongTongHop(string MaBienThe, string TenHang, string DonViTinh, Dictionary<string, decimal> TheoKho, decimal KhaDung, decimal TiepNhan, decimal CachLy, decimal VanChuyen)
{
    public decimal Tong => TheoKho.Values.Sum();
}

public record TongHopTrang(LocBaoCao Loc, DanhSachLoc DanhSach, List<string> Kho, TrangDuLieu<DongTongHop> Trang, DongTongHop TongCong);

public record DongTheoLo(string MaKho, string MaLo, string MaBienThe, string TenHang, string DonViTinh, DateOnly? HanDung, DateTimeOffset NgayNhap,
    decimal SoLuong, decimal DaGiu, decimal KhaDung, decimal TiepNhan, decimal CachLy);

public record DongTheoViTri(string MaViTri, LoaiKhu? LoaiKhu, string MaLo, string MaBienThe, string TenHang, string DonViTinh, DateOnly? HanDung,
    decimal SoLuong, decimal DaGiu, string? MaPhieuKiemKe);

public record DongNhapXuatTon(string MaKho, string MaBienThe, string TenHang, string DonViTinh, decimal DauKy, decimal Nhap, decimal Xuat)
{
    public decimal CuoiKy => DauKy + Nhap - Xuat;
}

public record NhapXuatTonTrang(LocBaoCao Loc, DanhSachLoc DanhSach, TrangDuLieu<DongNhapXuatTon> Trang, DongNhapXuatTon TongCong);

public record DongTheKho(DateTimeOffset ThoiDiem, string LoaiChungTu, string MaChungTu, string MaLo, string MaViTri, decimal Nhap, decimal Xuat, decimal Ton, string NguoiThucHien);

/// <param name="DauKy">Tổng biến động trước ngày bắt đầu.</param>
public record TheKhoTrang(LocBaoCao Loc, DanhSachLoc DanhSach, string? TenHang, string? DonViTinh, decimal DauKy, decimal CuoiKy, TrangDuLieu<DongTheKho> Trang);

public static class TenChungTu
{
    public static string Loai(string loai) => loai switch
    {
        "PhieuNhap" => "Nhập kho",
        "HuyPhieuNhap" => "Hủy phiếu nhập",
        "XepHang" => "Xếp hàng",
        "ChuyenTrongKho" => "Chuyển vị trí",
        "PhieuXuat" => "Xuất kho",
        "DieuChuyenXuat" => "Chuyển kho: xuất đi",
        "DieuChuyenNhan" => "Chuyển kho: nhận",
        "HuyDieuChuyen" => "Hủy chuyển kho",
        "XuLyChenhLechDieuChuyen" => "Xử lý chênh lệch chuyển kho",
        _ => loai,
    };

    /// <summary>Trang chi tiết của chứng từ theo tiền tố mã; null khi không có trang riêng (chuyển vị trí).</summary>
    public static string? Url(string ma) => ma[..Math.Min(2, ma.Length)] switch
    {
        "PN" => $"/PhieuNhap/{ma}",
        "PX" => $"/PhieuXuat/{ma}",
        "DC" => $"/DieuChuyen/{ma}",
        _ => null,
    };
}

/// <summary>Model của partial _Loc: các ô lọc dùng chung, bật thêm ô theo từng báo cáo.</summary>
public record ThanhLocBaoCao(LocBaoCao Loc, DanhSachLoc DanhSach, bool Ky = false, bool Khu = false, bool TheKho = false);
