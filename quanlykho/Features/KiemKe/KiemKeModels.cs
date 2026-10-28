using System.ComponentModel.DataAnnotations;
using quanlykho.Common.GiaoDien;
using quanlykho.Domain;

namespace quanlykho.Features.KiemKe;

public record KiemKeDong(string Ma, string MaKho, PhamViKiemKe PhamVi, DateOnly NgayKiem, string? MucDich, int SoDong, TrangThaiKiemKe TrangThai);

public record DongKiemKe(int STT, string MaViTri, string? MaLo, string MaBienThe, string TenHang, string DonViTinh, DateOnly? HanDung,
    decimal SoSach, decimal? ThucTe, bool NgoaiDanhSach, string? NguoiDem,
    LoaiChenhLech? Loai, string? KetLuan, string? NguoiDuyet, string? MaChungTuXuLy, List<DonViChon>? DonVi = null)
{
    /// <summary>Đơn vị lớn nhất khi hàng có đơn vị quy đổi, để đếm theo thùng và số lẻ.</summary>
    public DonViChon? DonViLon => DonVi is { Count: > 1 } ? DonVi.MaxBy(d => d.HeSo) : null;

    /// <summary>Thực tế − sổ sách; null khi chưa đếm.</summary>
    public decimal? ChenhLech => ThucTe - SoSach;
    public bool ChoXuLy => ChenhLech is not null and not 0 && NguoiDuyet is null;
}

public record KiemKeChiTiet(string Ma, string MaKho, string TenKho, PhamViKiemKe PhamVi, List<string> DoiTuong, string? MucDich, DateOnly NgayKiem,
    string NguoiLap, DateTimeOffset NgayLap, DateTimeOffset? ThoiDiemBatDau, DateTimeOffset? ThoiDiemHoanThanh, TrangThaiKiemKe TrangThai,
    List<LuaChon> ThanhVien, List<DongKiemKe> Dong, List<string> CacO, bool LaThanhVien, List<string> OCuaKho)
{
    public int ChuaDem => Dong.Count(d => d.ThucTe is null);
    /// <summary>Dòng chênh lệch chưa được duyệt xử lý.</summary>
    public int CoChenhLech => Dong.Count(d => d.ChoXuLy);

    public static string TenLoai(LoaiChenhLech l) => l switch
    {
        LoaiChenhLech.Thieu => "Thiếu",
        LoaiChenhLech.Thua => "Thừa",
        LoaiChenhLech.HuHong => "Hư hỏng",
        LoaiChenhLech.HetHan => "Hết hạn",
        LoaiChenhLech.SaiViTri => "Sai vị trí",
        _ => "Sai biến thể",
    };
}

/// <summary>Quản lý kho phân loại một dòng chênh lệch; HanDung dùng khi hàng thừa không có nhãn lô (tạo lô mới).</summary>
public class DongDuyetForm
{
    public int STT { get; set; }
    public LoaiChenhLech? Loai { get; set; }
    public string? KetLuan { get; set; }
    public string? MaViTriDung { get; set; }
    public string? MaBienTheDung { get; set; }
    public DateOnly? HanDung { get; set; }
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
    /// <summary>Số lẻ theo đơn vị tính chính (hoặc cả số khi không đếm theo thùng).</summary>
    public decimal? SoLuongThucTe { get; set; }
    /// <summary>Số đơn vị lớn nhất (thùng); thực tế = SoLon × hệ số + SoLuongThucTe.</summary>
    public decimal? SoLon { get; set; }
}

/// <summary>Hàng ngoài danh sách: Ma là mã lô, mã biến thể hoặc mã vạch; SoLon là số đơn vị lớn nhất (thùng) cộng thêm vào số lẻ.</summary>
public record ThemDongKiemKe(string MaViTri, string Ma, decimal SoLuongThucTe, decimal SoLon = 0);
