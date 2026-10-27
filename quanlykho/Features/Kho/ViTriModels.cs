using System.ComponentModel.DataAnnotations;
using quanlykho.Domain;

namespace quanlykho.Features.Kho;

/// <summary>Bảng 2.31: tên cấp và tiền tố mã của từng cấp.</summary>
public static class CapViTri
{
    public const int CapCuoi = 7;
    public const int CapKhaiKhu = 4;

    private static readonly string[] TienTo = ["", "", "TA", "TG", "P", "H", "KE", "O"];
    private static readonly string[] TenCap = ["", "Kho", "Tòa", "Tầng", "Phòng – dãy", "Hàng", "Kệ", "Ô"];

    public static string Ten(int cap) => TenCap[cap];

    public static string MaCon(string maCha, int capCha, int so) => $"{maCha}-{TienTo[capCha + 1]}{so:D2}";

    public static string TenKhu(LoaiKhu? k) => k switch
    {
        LoaiKhu.TiepNhan => "Khu tiếp nhận",
        LoaiKhu.LuuTru => "Khu lưu trữ",
        LoaiKhu.CachLy => "Khu cách ly",
        LoaiKhu.Xuat => "Khu xuất",
        _ => "",
    };

    public static string TenBaoQuan(DieuKienBaoQuan? d) => d switch
    {
        DieuKienBaoQuan.Thuong => "Thường",
        DieuKienBaoQuan.Lanh => "Lạnh",
        _ => "",
    };
}

public record ViTriNut(string Ma, string? MaCha, short Cap, string Ten, LoaiKhu? LoaiKhu, DieuKienBaoQuan? DieuKienBaoQuan,
    decimal? Dai, decimal? Rong, decimal? Cao, decimal? TaiTrongToiDa, bool LaOMacDinh, bool DangKhoa, string? MaPhieuKiemKe, int SoCon, decimal TonHienTai)
{
    public List<ViTriNut> Con { get; } = [];
}

public record HangTaiViTri(string MaViTri, string MaLo, string MaBienThe, string TenHang, string DonViTinh, DateOnly? HanDung, decimal SoLuong, decimal SoLuongDaGiu);

public record ViTriChiTiet(ViTriNut Nut, string TenDayDu, string MaKho, bool DaTungCoHang, List<ViTriNut> Con, List<HangTaiViTri> Hang)
{
    public bool XoaDuoc => Nut.Cap > 1 && Nut.SoCon == 0 && !DaTungCoHang && !Nut.LaOMacDinh;
}

public class ViTriForm
{
    public string? Ma { get; set; }

    [Display(Name = "Vị trí cha")]
    public string? MaCha { get; set; }

    [Range(1, 99, ErrorMessage = "Số thứ tự từ 1 đến 99."), Display(Name = "Số thứ tự")]
    public int? So { get; set; }

    [Required(ErrorMessage = "Nhập tên."), StringLength(200), Display(Name = "Tên")]
    public string Ten { get; set; } = "";

    [Display(Name = "Loại khu")]
    public LoaiKhu? LoaiKhu { get; set; }

    [Display(Name = "Điều kiện bảo quản")]
    public DieuKienBaoQuan? DieuKienBaoQuan { get; set; }

    [Display(Name = "Dài (m)")]
    public decimal? Dai { get; set; }

    [Display(Name = "Rộng (m)")]
    public decimal? Rong { get; set; }

    [Display(Name = "Cao (m)")]
    public decimal? Cao { get; set; }

    [Display(Name = "Tải trọng tối đa (kg)")]
    public decimal? TaiTrongToiDa { get; set; }

    /// <summary>Cấp của vị trí đang thêm hoặc sửa; nạp kèm để view biết hiện ô nào.</summary>
    public int Cap { get; set; }
    public string TenDayDuCha { get; set; } = "";
    /// <summary>Cấp 5–7: loại khu, điều kiện bảo quản lấy từ phòng – dãy chứa nó.</summary>
    public string? KeThua { get; set; }
}
