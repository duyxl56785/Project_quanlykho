using Microsoft.EntityFrameworkCore;
using quanlykho.Common.Loi;
using quanlykho.Common.PhanQuyen;
using quanlykho.Data;
using quanlykho.Domain;

namespace quanlykho.Features.Kho;

public record OPhuHopDong(string MaViTri, string TenDayDu, decimal QMax, decimal TheTichConLai, decimal TaiTrongConLai, bool DuSoLuong);

/// <summary>Ô xếp được lô: cấp 7, khu lưu trữ, đúng điều kiện bảo quản, không khóa, cùng kho với lô, còn chỗ (Bảng 2.33).</summary>
public class OPhuHop(AppDbContext db, NguoiDungHienTai nguoiDung)
{
    public const decimal HeSoLapDayMacDinh = 0.85m;

    public async Task<List<OPhuHopDong>> TimAsync(string maLo, decimal soLuong, string? maKho = null)
    {
        if (soLuong <= 0)
            throw new NghiepVuException("Số lượng xếp phải lớn hơn 0.");
        var lo = await db.Lo.Where(l => l.MaLo == maLo)
            .Select(l => new { l.MaBienThe, l.BienThe.DaiDongGoi, l.BienThe.RongDongGoi, l.BienThe.CaoDongGoi, l.BienThe.KhoiLuong, l.BienThe.SanPham.DieuKienBaoQuan })
            .SingleOrDefaultAsync() ?? throw new NghiepVuException($"Không có lô {maLo}.");

        if (maKho is null)
        {
            var khoCoLo = await db.TonKho.Where(t => t.MaLo == maLo && t.SoLuong > 0)
                .Join(db.ViTri, t => t.MaViTri, v => v.MaViTri, (t, v) => v.MaKho).Distinct().ToListAsync();
            maKho = khoCoLo.Count switch
            {
                0 => throw new NghiepVuException($"Lô {maLo} không còn tồn ở kho nào."),
                1 => khoCoLo[0],
                _ => throw new NghiepVuException($"Lô {maLo} có ở nhiều kho ({string.Join(", ", khoCoLo)}), chọn kho."),
            };
        }
        await nguoiDung.DamBaoQuyenKhoAsync(maKho);

        var heSo = await db.ViTri.Where(v => v.MaViTri == maKho).Select(v => v.HeSoLapDay).SingleOrDefaultAsync() ?? HeSoLapDayMacDinh;
        var o = await db.ViTri.Where(v => v.MaKho == maKho && v.Cap == CapViTri.CapCuoi && v.LoaiKhu == LoaiKhu.LuuTru
                && v.DieuKienBaoQuan == lo.DieuKienBaoQuan && !v.DangKhoa && v.MaPhieuKiemKe == null)
            .OrderBy(v => v.MaViTri)
            .Select(v => new { v.MaViTri, v.TenDayDu, v.Dai, v.Rong, v.Cao, v.TaiTrongToiDa })
            .ToListAsync();
        var maO = o.Select(x => x.MaViTri).ToList();
        var hang = (await db.TonKho.Where(t => maO.Contains(t.MaViTri) && t.SoLuong > 0)
                .Join(db.Lo, t => t.MaLo, l => l.MaLo, (t, l) => new { t.MaViTri, t.SoLuong, l.MaBienThe, l.BienThe.DaiDongGoi, l.BienThe.RongDongGoi, l.BienThe.CaoDongGoi, l.BienThe.KhoiLuong })
                .ToListAsync())
            .ToLookup(t => t.MaViTri, t => new HangTrongO(t.MaBienThe, new DonViHang(t.DaiDongGoi, t.RongDongGoi, t.CaoDongGoi, t.KhoiLuong), t.SoLuong));

        var donVi = new DonViHang(lo.DaiDongGoi, lo.RongDongGoi, lo.CaoDongGoi, lo.KhoiLuong);
        return o.Select(x => (x, kq: SucChua.Tinh(new OChua(x.Dai!.Value, x.Rong!.Value, x.Cao!.Value, x.TaiTrongToiDa!.Value), heSo, lo.MaBienThe, donVi, hang[x.MaViTri].ToList())))
            .Where(x => x.kq.QMax > 0)
            .Select(x => new OPhuHopDong(x.x.MaViTri, x.x.TenDayDu, x.kq.QMax, x.kq.TheTichConLai, x.kq.TaiTrongConLai, x.kq.QMax >= soLuong))
            .ToList();
    }
}
