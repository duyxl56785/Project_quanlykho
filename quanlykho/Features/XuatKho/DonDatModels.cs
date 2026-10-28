using System.ComponentModel.DataAnnotations;
using quanlykho.Common.GiaoDien;
using quanlykho.Domain;

namespace quanlykho.Features.XuatKho;

public record DonDatDong(string Ma, string MaKho, string NoiNhan, string NguoiTao, DateTimeOffset NgayTao, DateOnly? HanGiao, int SoDong, TrangThaiDonDat TrangThai);

public record DongDonDat(string MaBienThe, string TenHang, string TenDonVi, int HeSo, decimal SoLuongDat, decimal SoLuongDaXuat, decimal KhaDung)
{
    public decimal SoDatChinh => SoLuongDat * HeSo;
}

public record DonDatChiTiet(string Ma, string MaKho, string TenKho, string MaNoiNhan, string TenNoiNhan, string? DiaChiNhan, string MaNguoiTao, string NguoiTao,
    DateTimeOffset NgayTao, DateOnly? HanGiao, TrangThaiDonDat TrangThai, string? GhiChu, List<DongDonDat> Dong, List<HangDaGiu> DaGiu, string? MaLenh)
{
    public bool SuaDuoc => TrangThai is TrangThaiDonDat.Nhap or TrangThaiDonDat.ChoDuyet;
}

public record HangDaGiu(int ThuTu, string MaBienThe, string MaLo, string MaViTri, DateOnly? HanDung, decimal SoLuong);

public class DongDonDatForm
{
    public string MaBienThe { get; set; } = "";
    public decimal SoLuong { get; set; }
}

public class DonDatForm
{
    public string? Ma { get; set; }

    [Required(ErrorMessage = "Chọn kho xuất."), Display(Name = "Kho xuất")]
    public string MaKho { get; set; } = "";

    [Required(ErrorMessage = "Chọn nơi nhận."), Display(Name = "Nơi nhận")]
    public string MaNoiNhan { get; set; } = "";

    [Display(Name = "Hạn giao")]
    public DateOnly? HanGiao { get; set; }

    [StringLength(500), Display(Name = "Ghi chú")]
    public string? GhiChu { get; set; }

    public List<DongDonDatForm> Dong { get; set; } = [];

    public List<LuaChon> DanhSachKho { get; set; } = [];
    public List<LuaChon> DanhSachNoiNhan { get; set; } = [];
    public List<LuaChon> DanhSachBienThe { get; set; } = [];
}
