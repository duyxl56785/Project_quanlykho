using System.ComponentModel.DataAnnotations;
using quanlykho.Common.GiaoDien;
using quanlykho.Domain;

namespace quanlykho.Features.DieuChuyen;

public record DieuChuyenDong(string Ma, string MaKhoDi, string MaKhoDen, DateOnly NgayChuyen, string? PhuongTien, int SoDong, TrangThaiDieuChuyen TrangThai);

/// <param name="SoLuong">Theo đơn vị tính chính (DonViChinh), cùng SoLuongNhan; SoLuongGoc theo TenDonVi chọn khi lập.</param>
public record DongDieuChuyen(int STT, string MaLo, string MaBienThe, string TenHang, string MaViTriDi, DateOnly? HanDung, string TenDonVi, decimal SoLuong, decimal? SoLuongNhan,
    XuLyChenhLech? CachXuLy, string? LyDoXuLy, decimal SoLuongGoc = 0, int HeSo = 1, string DonViChinh = "")
{
    public decimal? ChenhLech => SoLuongNhan is null ? null : SoLuong - SoLuongNhan;
    public bool ChoXuLy => ChenhLech > 0 && CachXuLy is null;

    public static string TenCach(XuLyChenhLech c) => c switch
    {
        XuLyChenhLech.NhapKhoDen => "Nhập tại kho đến",
        XuLyChenhLech.NhapLaiKhoDi => "Nhập lại kho đi",
        _ => "Ghi giảm hao hụt",
    };
}

public record XuLyChenhLechYeuCau(int STT, XuLyChenhLech CachXuLy, string? LyDo);

public record DieuChuyenChiTiet(string Ma, string MaKhoDi, string TenKhoDi, string MaKhoDen, string TenKhoDen, DateOnly NgayChuyen, string? PhuongTien,
    string? NguoiVanChuyen, string? LyDo, string NguoiLap, DateTimeOffset NgayLap, DateTimeOffset? ThoiDiemXuat, string? NguoiNhan, DateTimeOffset? ThoiDiemNhan,
    TrangThaiDieuChuyen TrangThai, List<DongDieuChuyen> Dong, bool LaKhoDi, bool LaKhoDen);

public class DongDieuChuyenForm
{
    /// <summary>"MaLo|MaViTri" chọn từ tồn khả dụng của kho đi.</summary>
    public string LoO { get; set; } = "";
    /// <summary>Trống là đơn vị tính chính.</summary>
    public string? TenDonVi { get; set; }
    public decimal SoLuong { get; set; }
}

public class DieuChuyenForm
{
    public string? Ma { get; set; }

    [Required(ErrorMessage = "Chọn kho đi."), Display(Name = "Kho đi")]
    public string MaKhoDi { get; set; } = "";

    [Required(ErrorMessage = "Chọn kho đến."), Display(Name = "Kho đến")]
    public string MaKhoDen { get; set; } = "";

    [Display(Name = "Ngày chuyển")]
    public DateOnly? NgayChuyen { get; set; }

    [StringLength(100), Display(Name = "Phương tiện")]
    public string? PhuongTien { get; set; }

    [StringLength(100), Display(Name = "Người vận chuyển")]
    public string? NguoiVanChuyen { get; set; }

    [StringLength(500), Display(Name = "Lý do")]
    public string? LyDo { get; set; }

    public string KhoaChongLap { get; set; } = Guid.NewGuid().ToString("N");

    public List<DongDieuChuyenForm> Dong { get; set; } = [];

    public List<LuaChon> DanhSachKhoDi { get; set; } = [];
    public List<LuaChon> DanhSachKhoDen { get; set; } = [];
    public List<LuaChon> DanhSachTon { get; set; } = [];
    /// <summary>Đơn vị theo từng lựa chọn "MaLo|MaViTri" của DanhSachTon.</summary>
    public Dictionary<string, List<DonViChon>> DonViHang { get; set; } = [];
}

public class DongNhanForm
{
    public int STT { get; set; }
    public decimal SoLuongNhan { get; set; }
}
