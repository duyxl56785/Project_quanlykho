using System.Text.Json;
using quanlykho.Common.PhanQuyen;
using quanlykho.Data;

namespace quanlykho.Common;

/// <summary>
/// Ghi nhật ký thao tác quan trọng (duyệt, hủy, hoàn thành, điều chỉnh tồn, phân quyền, khóa tài khoản).
/// Chỉ thêm vào DbContext; dòng nhật ký được lưu cùng SaveChanges (cùng transaction) với thao tác.
/// </summary>
public class GhiNhatKy(AppDbContext db, NguoiDungHienTai nguoiDung, TimeProvider gio)
{
    public void Ghi(string loaiThaoTac, string doiTuong, string? maDoiTuong, object? chiTiet = null, string? maKho = null, string? maNguoiDung = null) =>
        db.NhatKy.Add(new Domain.NhatKy
        {
            ThoiDiem = gio.GetUtcNow(),
            MaNguoiDung = maNguoiDung ?? (nguoiDung.DaDangNhap ? nguoiDung.Ma : null),
            MaKho = maKho,
            LoaiThaoTac = loaiThaoTac,
            DoiTuong = doiTuong,
            MaDoiTuong = maDoiTuong,
            ChiTiet = chiTiet is null ? null : JsonSerializer.Serialize(chiTiet),
        });
}
