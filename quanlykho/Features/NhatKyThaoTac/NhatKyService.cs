using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using quanlykho.Common.GiaoDien;
using quanlykho.Data;

namespace quanlykho.Features.NhatKyThaoTac;

public class LocNhatKy
{
    public string? MaNguoiDung { get; set; }
    public string? MaKho { get; set; }
    public string? LoaiThaoTac { get; set; }
    /// <summary>Mã chứng từ, tài khoản, vị trí... (khớp đầu mã).</summary>
    public string? MaDoiTuong { get; set; }
    public DateOnly? TuNgay { get; set; }
    public DateOnly? DenNgay { get; set; }

    [BindNever]
    public string? Loi { get; set; }
}

public record NhatKyDong(long Id, DateTimeOffset ThoiDiem, string? MaNguoiDung, string? TenNguoiDung, string? MaKho, string LoaiThaoTac, string DoiTuong, string? MaDoiTuong, string? ChiTiet);

public record NhatKyTrang(LocNhatKy Loc, List<LuaChon> NguoiDung, List<LuaChon> Kho, List<LuaChon> LoaiThaoTac, TrangDuLieu<NhatKyDong> Trang);

/// <summary>Tra nhật ký thao tác (chỉ đọc; CSDL chặn sửa, xóa bằng trigger).</summary>
public class NhatKyService(AppDbContext db)
{
    public async Task<NhatKyTrang> TraCuuAsync(LocNhatKy loc, int trang)
    {
        var nk = db.NhatKy.AsQueryable();
        if (!string.IsNullOrWhiteSpace(loc.MaNguoiDung)) nk = nk.Where(n => n.MaNguoiDung == loc.MaNguoiDung);
        if (!string.IsNullOrWhiteSpace(loc.MaKho)) nk = nk.Where(n => n.MaKho == loc.MaKho);
        if (!string.IsNullOrWhiteSpace(loc.LoaiThaoTac)) nk = nk.Where(n => n.LoaiThaoTac == loc.LoaiThaoTac);
        if (!string.IsNullOrWhiteSpace(loc.MaDoiTuong)) nk = nk.Where(n => n.MaDoiTuong != null && n.MaDoiTuong.StartsWith(loc.MaDoiTuong.Trim()));
        if (loc.TuNgay > loc.DenNgay)
        {
            loc.Loi = "Ngày bắt đầu không được sau ngày kết thúc.";
            nk = nk.Where(n => false);
        }
        if (loc.TuNgay is { } tu)
        {
            var tuUtc = DinhDang.BatDauNgay(tu);
            nk = nk.Where(n => n.ThoiDiem >= tuUtc);
        }
        if (loc.DenNgay is { } den)
        {
            var denUtc = DinhDang.BatDauNgay(den.AddDays(1));
            nk = nk.Where(n => n.ThoiDiem < denUtc);
        }
        var q = (from n in nk
                 join u in db.NguoiDung on n.MaNguoiDung equals u.MaNguoiDung into nd
                 from u in nd.DefaultIfEmpty()
                 select new { n, Ten = u == null ? null : u.Ten })
            .OrderByDescending(x => x.n.ThoiDiem).ThenByDescending(x => x.n.Id)
            .Select(x => new NhatKyDong(x.n.Id, x.n.ThoiDiem, x.n.MaNguoiDung, x.Ten, x.n.MaKho, x.n.LoaiThaoTac, x.n.DoiTuong, x.n.MaDoiTuong, x.n.ChiTiet));

        return new NhatKyTrang(loc,
            await db.NguoiDung.OrderBy(n => n.MaNguoiDung).Select(n => new LuaChon(n.MaNguoiDung, n.MaNguoiDung + " – " + n.Ten)).ToListAsync(),
            await db.ViTri.Where(v => v.Cap == 1).OrderBy(v => v.MaViTri).Select(v => new LuaChon(v.MaViTri, v.Ten)).ToListAsync(),
            (await db.NhatKy.Select(n => n.LoaiThaoTac).Distinct().ToListAsync()).Select(l => new LuaChon(l, TenThaoTac(l))).OrderBy(l => l.Ten).ToList(),
            await TrangDuLieu<NhatKyDong>.TaoAsync(q, trang));
    }

    public static string TenThaoTac(string loai) => loai switch
    {
        "ThemNguoiDung" => "Thêm người dùng",
        "DoiQuyen" => "Đổi vai, kho được giao",
        "KhoaTaiKhoan" => "Khóa tài khoản",
        "MoKhoaTaiKhoan" => "Mở khóa tài khoản",
        "DatLaiMatKhau" => "Đặt lại mật khẩu",
        "KhoaTamDangNhap" => "Khóa tạm do đăng nhập sai",
        "ThemKho" => "Thêm kho",
        "SuaKho" => "Sửa kho",
        "NgungKho" => "Ngừng kho",
        "MoLaiKho" => "Mở lại kho",
        "TaoKhuMacDinh" => "Tạo khu mặc định",
        "ThemViTri" => "Thêm vị trí",
        "SuaViTri" => "Sửa vị trí",
        "KhoaViTri" => "Khóa vị trí",
        "MoKhoaViTri" => "Mở khóa vị trí",
        "XoaViTri" => "Xóa vị trí",
        "NgungSuDung" => "Ngừng sử dụng",
        "DungLai" => "Dùng lại",
        "XacNhanDonMua" => "Xác nhận đơn mua",
        "HuyDonMua" => "Hủy đơn mua",
        "LapPhieuNhap" => "Lập hóa đơn nhập",
        "XacNhanPhieuNhap" => "Xác nhận hóa đơn nhập",
        "HuyPhieuNhap" => "Hủy hóa đơn nhập",
        "GuiDuyetDonDat" => "Gửi duyệt đơn đặt hàng",
        "DuyetDonDat" => "Duyệt đơn, giữ hàng",
        "BoGiuDonDat" => "Bỏ giữ hàng",
        "HuyDonDat" => "Hủy đơn đặt hàng",
        "LapLenhXuat" => "Lập lệnh xuất",
        "XongLayHang" => "Xong lấy hàng",
        "KiemTraLenhXuat" => "Kiểm tra hàng đã lấy",
        "XacNhanXuat" => "Xác nhận xuất kho",
        "XuatKhac" => "Xuất khác",
        "TraNhaCungCap" => "Xuất trả nhà cung cấp",
        "XuatDieuChuyen" => "Chuyển kho: xuất đi",
        "NhanDieuChuyen" => "Chuyển kho: nhận",
        "XuLyChenhLechDieuChuyen" => "Xử lý chênh lệch chuyển kho",
        "HuyDieuChuyen" => "Hủy phiếu chuyển kho",
        "LapKiemKe" => "Lập phiếu kiểm kê",
        "BatDauKiemKe" => "Bắt đầu kiểm kê",
        "DuyetDieuChinhKiemKe" => "Duyệt điều chỉnh kiểm kê",
        "HoanThanhKiemKe" => "Hoàn thành kiểm kê",
        "HuyKiemKe" => "Hủy kiểm kê",
        _ => loai,
    };
}
