using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using quanlykho.Common.Loi;
using quanlykho.Data;
using quanlykho.Domain;

namespace quanlykho.Common.PhanQuyen;

/// <summary>
/// Người đang đăng nhập và phạm vi kho của họ (KE_HOACH A5). Mọi Service nhận mã kho phải gọi DamBaoQuyenKhoAsync,
/// mọi truy vấn danh sách phải lọc theo KhoDuocGiaoAsync. Quản trị viên và nhân viên bán hàng thấy mọi kho.
/// </summary>
public class NguoiDungHienTai(IHttpContextAccessor http, AppDbContext db)
{
    private IReadOnlyList<string>? _kho;

    private ClaimsPrincipal? User => http.HttpContext?.User;

    public bool DaDangNhap => User?.Identity?.IsAuthenticated == true;

    public string Ma => User?.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new KhongCoQuyenException("Bạn chưa đăng nhập.");

    public VaiTro VaiTro => Enum.Parse<VaiTro>(User?.FindFirstValue(ClaimTypes.Role)
        ?? throw new KhongCoQuyenException("Bạn chưa đăng nhập."));

    public bool ThayMoiKho => VaiTro is VaiTro.QuanTriVien or VaiTro.NhanVienBanHang;

    public async Task<IReadOnlyList<string>> KhoDuocGiaoAsync() => _kho ??= ThayMoiKho
        ? await db.ViTri.Where(v => v.Cap == 1).OrderBy(v => v.MaViTri).Select(v => v.MaViTri).ToListAsync()
        : await db.NguoiDungKho.Where(k => k.MaNguoiDung == Ma).OrderBy(k => k.MaKho).Select(k => k.MaKho).ToListAsync();

    public async Task DamBaoQuyenKhoAsync(string maKho)
    {
        if (!(await KhoDuocGiaoAsync()).Contains(maKho))
            throw new KhongCoQuyenException($"Bạn không được giao kho {maKho}.");
    }
}
