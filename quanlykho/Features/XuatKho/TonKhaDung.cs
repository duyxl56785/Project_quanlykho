using Microsoft.EntityFrameworkCore;
using quanlykho.Common.GiaoDien;
using quanlykho.Common.PhanQuyen;
using quanlykho.Data;
using quanlykho.Domain;
using quanlykho.Features.DanhMuc;
using quanlykho.Features.NhapKho;

namespace quanlykho.Features.XuatKho;

/// <summary>Một dòng TonKho còn khả dụng; lớp có init để EF ghép tiếp Where, GroupBy sau phép chiếu.</summary>
public class DongKhaDung
{
    public string MaLo { get; init; } = "";
    public string MaViTri { get; init; } = "";
    public string MaKho { get; init; } = "";
    public string MaBienThe { get; init; } = "";
    public DateOnly? HanDung { get; init; }
    public DateTimeOffset NgayNhap { get; init; }
    public decimal KhaDung { get; init; }
}

/// <param name="DonVi">Đơn vị chính và đơn vị quy đổi, để hiện kèm "9 thùng 19 chai".</param>
public record TonKhaDungDong(string MaBienThe, string TenHang, string? MaVach, string DonViTinh, Dictionary<string, decimal> TheoKho, List<DonViChon> DonVi)
{
    public decimal Tong => TheoKho.Values.Sum();
}

/// <summary>
/// Tồn khả dụng (quy tắc 7): chỉ hàng ở ô khu lưu trữ, không khóa kiểm kê, lô chưa hết hạn, trừ phần đã giữ.
/// Hàng ở khu tiếp nhận, cách ly và hàng đang vận chuyển (không nằm trong TonKho) tự bị loại.
/// </summary>
public class TonKhaDung(AppDbContext db, NguoiDungHienTai nguoiDung, TimeProvider gio)
{
    public DateOnly HomNay => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(gio.GetUtcNow(), DinhDang.GioVietNam).DateTime);

    /// <summary>Truy vấn dùng chung cho đơn đặt, G1, giữ hàng, báo cáo; chưa lọc theo quyền kho.</summary>
    public IQueryable<DongKhaDung> TruyVan()
    {
        var homNay = HomNay;
        return from t in db.TonKho
               join v in db.ViTri on t.MaViTri equals v.MaViTri
               join l in db.Lo on t.MaLo equals l.MaLo
               where v.LoaiKhu == LoaiKhu.LuuTru && v.MaPhieuKiemKe == null && (l.HanDung == null || l.HanDung >= homNay)
                   && t.SoLuong > t.SoLuongDaGiu
               select new DongKhaDung
               {
                   MaLo = t.MaLo, MaViTri = t.MaViTri, MaKho = v.MaKho, MaBienThe = l.MaBienThe, HanDung = l.HanDung, NgayNhap = l.NgayNhap,
                   KhaDung = t.SoLuong - t.SoLuongDaGiu,
               };
    }

    public async Task<Dictionary<string, decimal>> TheoBienTheAsync(string maKho, IEnumerable<string> maBienThe)
    {
        await nguoiDung.DamBaoQuyenKhoAsync(maKho);
        var ma = maBienThe.Distinct().ToList();
        return await TruyVan().Where(d => d.MaKho == maKho && ma.Contains(d.MaBienThe))
            .GroupBy(d => d.MaBienThe).Select(g => new { g.Key, Tong = g.Sum(d => d.KhaDung) })
            .ToDictionaryAsync(x => x.Key, x => x.Tong);
    }

    /// <summary>Tra cứu theo mã, tên, mã vạch; mỗi dòng một biến thể, số theo từng kho người dùng được xem.</summary>
    public async Task<TrangDuLieu<TonKhaDungDong>> TraCuuAsync(string? tuKhoa, string? maKho, bool chiConHang, int trang)
    {
        IReadOnlyList<string> kho = await nguoiDung.KhoDuocGiaoAsync();
        if (!string.IsNullOrWhiteSpace(maKho))
        {
            await nguoiDung.DamBaoQuyenKhoAsync(maKho);
            kho = [maKho];
        }
        var bienThe = db.BienThe.Where(b => b.TrangThai == TrangThaiSuDung.DangSuDung && b.SanPham.TrangThai == TrangThaiSuDung.DangSuDung);
        if (!string.IsNullOrWhiteSpace(tuKhoa))
        {
            var tk = $"%{tuKhoa.Trim()}%";
            var maVach = tuKhoa.Trim();
            bienThe = bienThe.Where(b => EF.Functions.ILike(b.MaBienThe, tk) || EF.Functions.ILike(b.SanPham.Ten, tk) || b.MaVach == maVach
                || db.DonViQuyDoi.Any(d => d.MaBienThe == b.MaBienThe && d.MaVach == maVach));
        }
        var hang = await bienThe.OrderBy(b => b.MaBienThe)
            .Select(b => new { b.MaBienThe, b.SanPham.Ten, b.ThuocTinh, b.MaVach, b.SanPham.DonViTinh }).ToListAsync();
        var ma = hang.Select(h => h.MaBienThe).ToList();
        var so = (await TruyVan().Where(d => kho.Contains(d.MaKho) && ma.Contains(d.MaBienThe))
                .GroupBy(d => new { d.MaBienThe, d.MaKho }).Select(g => new { g.Key.MaBienThe, g.Key.MaKho, Tong = g.Sum(d => d.KhaDung) })
                .ToListAsync())
            .ToLookup(x => x.MaBienThe);
        var donVi = await new QuyDoiDonVi(db).BangAsync(ma);
        var dong = hang.Select(h => new TonKhaDungDong(h.MaBienThe, DonMuaService.MoTaHang(h.Ten, h.ThuocTinh), h.MaVach, h.DonViTinh,
            kho.ToDictionary(k => k, k => so[h.MaBienThe].Where(x => x.MaKho == k).Sum(x => x.Tong)), donVi[h.MaBienThe]));
        return TrangDuLieu<TonKhaDungDong>.Tao(chiConHang ? dong.Where(d => d.Tong > 0) : dong, trang);
    }
}
