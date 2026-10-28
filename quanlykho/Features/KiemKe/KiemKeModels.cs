using System.ComponentModel.DataAnnotations;
using quanlykho.Common.GiaoDien;
using quanlykho.Domain;

namespace quanlykho.Features.KiemKe;

public record KiemKeDong(string Ma, string MaKho, PhamViKiemKe PhamVi, DateOnly NgayKiem, string? MucDich, int SoDong, TrangThaiKiemKe TrangThai);

public record DongKiemKe(int STT, string MaViTri, string? MaLo, string MaBienThe, string TenHang, string DonViTinh, DateOnly? HanDung,
    decimal SoSach, decimal? ThucTe, bool NgoaiDanhSach, string? NguoiDem)
{
    /// <summary>Thực tế − sổ sách; null khi chưa đếm.</summary>
    public decimal? ChenhLech => ThucTe - SoSach;
}

public record KiemKeChiTiet(string Ma, string MaKho, string TenKho, PhamViKiemKe PhamVi, List<string> DoiTuong, string? MucDich, DateOnly NgayKiem,
    string NguoiLap, DateTimeOffset NgayLap, DateTimeOffset? ThoiDiemBatDau, DateTimeOffset? ThoiDiemHoanThanh, TrangThaiKiemKe TrangThai,
    List<LuaChon> ThanhVien, List<DongKiemKe> Dong, List<string> CacO, bool LaThanhVien)
{
    public int ChuaDem => Dong.Count(d => d.ThucTe is null);
    public int CoChenhLech => Dong.Count(d => d.ChenhLech is not null and not 0);
}

public class KiemKeForm
{
    [Required(ErrorMessage = "Chọn kho kiểm kê."), Display(Name = "Kho")]
    public string MaKho { get; set; } = "";

    [Display(Name = "Phạm vi")]
    public PhamViKiemKe PhamVi { get; set; } = PhamViKiemKe.ToanKho;

    /// <summary>Mã vị trí hoặc mã lô tùy phạm vi.</summary>
    public List<string> DoiTuong { get; set; } = [];

    [StringLength(500), Display(Name = "Mục đích")]
    public string? MucDich { get; set; }

    [Display(Name = "Ngày kiểm")]
    public DateOnly? NgayKiem { get; set; }

    [Display(Name = "Thành viên")]
    public List<string> ThanhVien { get; set; } = [];

    public List<LuaChon> DanhSachKho { get; set; } = [];
    public List<LuaChon> DanhSachViTri { get; set; } = [];
    public List<LuaChon> DanhSachLo { get; set; } = [];
    public List<LuaChon> DanhSachNguoi { get; set; } = [];
}

public class DongDemForm
{
    public int STT { get; set; }
    public decimal? SoLuongThucTe { get; set; }
}

/// <summary>Hàng ngoài danh sách: Ma là mã lô, mã biến thể hoặc mã vạch.</summary>
public record ThemDongKiemKe(string MaViTri, string Ma, decimal SoLuongThucTe);
