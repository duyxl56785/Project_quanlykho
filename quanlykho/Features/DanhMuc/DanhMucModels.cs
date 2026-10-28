using System.ComponentModel.DataAnnotations;
using quanlykho.Domain;

namespace quanlykho.Features.DanhMuc;

public static class MaHopLe
{
    public const string Mau = "^[A-Za-z0-9_-]{1,20}$";
    public const string Loi = "Mã gồm tối đa 20 ký tự: chữ không dấu, số, gạch ngang, gạch dưới.";
}

public record NhomCay(string Ma, string Ten, string? MaCha, int DoSau, List<ThuocTinh> ThuocTinh, int SoSanPham);

public class NhomForm
{
    [Required(ErrorMessage = "Nhập mã nhóm."), RegularExpression(MaHopLe.Mau, ErrorMessage = MaHopLe.Loi)]
    [Display(Name = "Mã nhóm")]
    public string Ma { get; set; } = "";

    [Required(ErrorMessage = "Nhập tên nhóm."), StringLength(200)]
    [Display(Name = "Tên nhóm")]
    public string Ten { get; set; } = "";

    [Display(Name = "Nhóm cha")]
    public string? MaCha { get; set; }

    public List<NhomCay> DanhSachNhom { get; set; } = [];
}

public record SanPhamDong(string Ma, string Ten, string DonViTinh, string TenNhom, int SoBienThe, decimal TonTong, TrangThaiSuDung TrangThai);

public class SanPhamForm
{
    [Required(ErrorMessage = "Nhập mã hàng."), RegularExpression(MaHopLe.Mau, ErrorMessage = MaHopLe.Loi)]
    [Display(Name = "Mã hàng")]
    public string Ma { get; set; } = "";

    [Required(ErrorMessage = "Nhập tên hàng."), StringLength(200)]
    [Display(Name = "Tên hàng")]
    public string Ten { get; set; } = "";

    [Required(ErrorMessage = "Nhập đơn vị tính chính."), StringLength(20)]
    [Display(Name = "Đơn vị tính chính")]
    public string DonViTinh { get; set; } = "";

    [Required(ErrorMessage = "Chọn nhóm hàng.")]
    [Display(Name = "Nhóm hàng")]
    public string MaNhom { get; set; } = "";

    [Display(Name = "Điều kiện bảo quản")]
    public DieuKienBaoQuan DieuKienBaoQuan { get; set; }

    [Display(Name = "Theo dõi hạn dùng")]
    public bool TheoDoiHanDung { get; set; }

    [Display(Name = "Mô tả")]
    public string? MoTa { get; set; }

    [Display(Name = "Trạng thái")]
    public TrangThaiSuDung TrangThai { get; set; }

    public bool LaSua { get; set; }
    public List<NhomCay> DanhSachNhom { get; set; } = [];
    public List<BienThe> BienThe { get; set; } = [];
    public List<ThuocTinh> ThuocTinhNhom { get; set; } = [];
}

public class SinhBienTheForm
{
    /// <summary>Tên thuộc tính → các giá trị cách nhau bởi dấu phẩy.</summary>
    public Dictionary<string, string> GiaTri { get; set; } = [];

    [Range(0.001, 100, ErrorMessage = "Kích thước từ 0,001 đến 100 m.")]
    public decimal Dai { get; set; }

    [Range(0.001, 100, ErrorMessage = "Kích thước từ 0,001 đến 100 m.")]
    public decimal Rong { get; set; }

    [Range(0.001, 100, ErrorMessage = "Kích thước từ 0,001 đến 100 m.")]
    public decimal Cao { get; set; }

    [Range(0.001, 100000, ErrorMessage = "Khối lượng phải lớn hơn 0.")]
    public decimal KhoiLuong { get; set; }
}

public class MucTonDong
{
    public string MaKho { get; set; } = "";
    public string TenKho { get; set; } = "";

    [Range(0, 1e9, ErrorMessage = "Tồn tối thiểu không âm.")]
    public decimal? ToiThieu { get; set; }

    [Range(0, 1e9, ErrorMessage = "Tồn tối đa không âm.")]
    public decimal? ToiDa { get; set; }

    [Range(0, 365, ErrorMessage = "Thời gian chờ hàng từ 0 đến 365 ngày.")]
    public int ThoiGianChoHang { get; set; } = 7;
}

public class BienTheForm
{
    public string Ma { get; set; } = "";
    public string MaSanPham { get; set; } = "";
    public string TenSanPham { get; set; } = "";
    public string DonViTinh { get; set; } = "";
    public Dictionary<string, string> ThuocTinh { get; set; } = [];

    [Range(0.001, 100, ErrorMessage = "Kích thước từ 0,001 đến 100 m."), Display(Name = "Dài (m)")]
    public decimal DaiDongGoi { get; set; }

    [Range(0.001, 100, ErrorMessage = "Kích thước từ 0,001 đến 100 m."), Display(Name = "Rộng (m)")]
    public decimal RongDongGoi { get; set; }

    [Range(0.001, 100, ErrorMessage = "Kích thước từ 0,001 đến 100 m."), Display(Name = "Cao (m)")]
    public decimal CaoDongGoi { get; set; }

    [Range(0.001, 100000, ErrorMessage = "Khối lượng phải lớn hơn 0."), Display(Name = "Khối lượng (kg)")]
    public decimal KhoiLuong { get; set; }

    [StringLength(64), RegularExpression("^[A-Za-z0-9-]*$", ErrorMessage = "Mã vạch chỉ gồm chữ không dấu, số, gạch ngang."), Display(Name = "Mã vạch")]
    public string? MaVach { get; set; }

    [Display(Name = "Trạng thái")]
    public TrangThaiSuDung TrangThai { get; set; }

    public List<MucTonDong> MucTon { get; set; } = [];

    /// <summary>Đơn vị quy đổi một tầng: 1 TenDonVi = HeSo DonViTinh.</summary>
    public List<DonViQuyDoiDong> DonVi { get; set; } = [];
}

public enum LoaiDoiTac { NhaCungCap, NoiNhan }

public record DoiTacDong(string Ma, string Ten, string? DiaChi, string? NguoiLienHe, string? SoDienThoai, TrangThaiSuDung TrangThai);

public class DoiTacForm
{
    [Required(ErrorMessage = "Nhập mã."), RegularExpression(MaHopLe.Mau, ErrorMessage = MaHopLe.Loi)]
    [Display(Name = "Mã")]
    public string Ma { get; set; } = "";

    [Required(ErrorMessage = "Nhập tên."), StringLength(200)]
    [Display(Name = "Tên")]
    public string Ten { get; set; } = "";

    [StringLength(300), Display(Name = "Địa chỉ")]
    public string? DiaChi { get; set; }

    [StringLength(100), Display(Name = "Người liên hệ")]
    public string? NguoiLienHe { get; set; }

    [Phone(ErrorMessage = "Số điện thoại không hợp lệ."), StringLength(20), Display(Name = "Số điện thoại")]
    public string? SoDienThoai { get; set; }

    [Display(Name = "Trạng thái")]
    public TrangThaiSuDung TrangThai { get; set; }

    public LoaiDoiTac Loai { get; set; }
    public bool LaSua { get; set; }
}
