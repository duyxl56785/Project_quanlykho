using System.ComponentModel.DataAnnotations;
using quanlykho.Common.GiaoDien;
using quanlykho.Domain;

namespace quanlykho.Features.Kho;

public record KhoDong(string Ma, string Ten, string? DiaChi, string? LoaiHangLuuTru, decimal? HeSoLapDay, TrangThaiSuDung TrangThai,
    int SoO, bool CoOMacDinh, List<string> NguoiDuocGiao);

public class KhoForm
{
    /// <summary>Không có gạch ngang vì mã vị trí con nối bằng gạch ngang; tối đa 10 ký tự để mã ô không quá 40.</summary>
    [Required(ErrorMessage = "Nhập mã kho."), RegularExpression("^[A-Za-z0-9_]{1,10}$", ErrorMessage = "Mã kho gồm tối đa 10 ký tự: chữ không dấu, số, gạch dưới.")]
    [Display(Name = "Mã kho")]
    public string Ma { get; set; } = "";

    [Required(ErrorMessage = "Nhập tên kho."), StringLength(200)]
    [Display(Name = "Tên kho")]
    public string Ten { get; set; } = "";

    [StringLength(300)]
    [Display(Name = "Địa chỉ")]
    public string? DiaChi { get; set; }

    [StringLength(200)]
    [Display(Name = "Loại hàng lưu trữ")]
    public string? LoaiHangLuuTru { get; set; }

    [Range(0.01, 1, ErrorMessage = "Hệ số lấp đầy lớn hơn 0 và không quá 1.")]
    [Display(Name = "Hệ số lấp đầy")]
    public decimal HeSoLapDay { get; set; } = 0.85m;

    [Display(Name = "Người được giao")]
    public List<string> NguoiDung { get; set; } = [];

    public bool LaSua { get; set; }
    public List<LuaChon> DanhSachNguoiDung { get; set; } = [];
}
