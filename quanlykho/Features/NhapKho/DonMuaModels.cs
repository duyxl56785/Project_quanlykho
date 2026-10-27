using System.ComponentModel.DataAnnotations;
using quanlykho.Common.GiaoDien;
using quanlykho.Domain;

namespace quanlykho.Features.NhapKho;

public record DonMuaDong(string Ma, string NhaCungCap, string MaKho, DateTimeOffset NgayTao, decimal TongTien, TrangThaiDonMua TrangThai, string? Buoc);

public record TienDoDong(string MaBienThe, string TenHang, string TenDonVi, int HeSo, decimal SoLuongDat, decimal DonGia, decimal DaNhan, decimal DangNhan)
{
    public decimal SoDatChinh => SoLuongDat * HeSo;
    public decimal ChuaNhan => Math.Max(0, SoDatChinh - DaNhan);
    public decimal ThanhTien => SoLuongDat * DonGia;
}

public record DonMuaChiTiet(string Ma, string MaNhaCungCap, string TenNhaCungCap, string MaKho, string TenKho, string NguoiTao, DateTimeOffset NgayTao,
    TrangThaiDonMua TrangThai, string? Buoc, string? GhiChu, List<TienDoDong> Dong, List<string> PhieuNhap)
{
    public decimal TongTien => Dong.Sum(d => d.ThanhTien);
    public bool SuaDuoc => TrangThai is TrangThaiDonMua.Nhap || (TrangThai is TrangThaiDonMua.ChuaThucHien && PhieuNhap.Count == 0);
}

public class DongDonMuaForm
{
    public string MaBienThe { get; set; } = "";
    public decimal SoLuong { get; set; }
    public decimal DonGia { get; set; }
}

public class DonMuaForm
{
    public string? Ma { get; set; }

    [Required(ErrorMessage = "Chọn nhà cung cấp."), Display(Name = "Nhà cung cấp")]
    public string MaNhaCungCap { get; set; } = "";

    [Required(ErrorMessage = "Chọn kho nhập."), Display(Name = "Kho nhập")]
    public string MaKho { get; set; } = "";

    [StringLength(500), Display(Name = "Ghi chú")]
    public string? GhiChu { get; set; }

    public List<DongDonMuaForm> Dong { get; set; } = [];

    public List<LuaChon> DanhSachNhaCungCap { get; set; } = [];
    public List<LuaChon> DanhSachKho { get; set; } = [];
    public List<LuaChon> DanhSachBienThe { get; set; } = [];
}
