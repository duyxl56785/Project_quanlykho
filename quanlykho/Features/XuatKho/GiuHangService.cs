using Microsoft.EntityFrameworkCore;
using quanlykho.Common;
using quanlykho.Common.Loi;
using quanlykho.Common.PhanQuyen;
using quanlykho.Data;
using quanlykho.Domain;
using static quanlykho.Common.GiaoDien.DinhDang;

namespace quanlykho.Features.XuatKho;

/// <summary>
/// Duyệt đơn đặt hàng và giữ hàng (mục 2.2 dòng 2): G1 chọn lô, ô cho từng dòng, cổng ghi tồn tăng SoLuongDaGiu, ghi GiuHang.
/// Thiếu hàng ở bất kỳ dòng nào thì báo "Cần X, còn Y" và không giữ gì.
/// </summary>
public class GiuHangService(AppDbContext db, NguoiDungHienTai nguoiDung, CongGhiTon congGhiTon, GoiYService goiY, GhiNhatKy nhatKy, TimeProvider gio)
{
    public async Task DuyetAsync(IReadOnlyCollection<string> maDon)
    {
        var ma = maDon.Where(m => !string.IsNullOrWhiteSpace(m)).Distinct().Order(StringComparer.Ordinal).ToArray();
        if (ma.Length == 0)
            throw new NghiepVuException("Chọn ít nhất một đơn để duyệt.");

        await using var tx = await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlAsync($"""SELECT 1 FROM "DonDatHang" WHERE "MaDonDat" = ANY({ma}) ORDER BY "MaDonDat" FOR UPDATE""");
        var don = await db.DonDatHang.Include(d => d.ChiTiet).Where(d => ma.Contains(d.MaDonDat)).OrderBy(d => d.MaDonDat).ToListAsync();
        if (ma.Except(don.Select(d => d.MaDonDat)).FirstOrDefault() is { } khongCo)
            throw new NghiepVuException($"Không có đơn đặt hàng {khongCo}.");
        foreach (var d in don)
        {
            await nguoiDung.DamBaoQuyenKhoAsync(d.MaKho);
            if (d.TrangThai != TrangThaiDonDat.ChoDuyet)
                throw new NghiepVuException($"Đơn {d.MaDonDat} không ở trạng thái Chờ duyệt.");
        }

        // Khóa mọi dòng tồn của các biến thể cần giữ trước khi G1 đọc, để hai lần duyệt song song không cùng chọn một số hàng.
        foreach (var nhom in don.GroupBy(d => d.MaKho).OrderBy(g => g.Key, StringComparer.Ordinal))
        {
            var bienThe = nhom.SelectMany(d => d.ChiTiet.Select(c => c.MaBienThe)).Distinct().ToArray();
            await db.Database.ExecuteSqlAsync($"""
                SELECT 1 FROM "TonKho" t
                JOIN "Lo" l ON l."MaLo" = t."MaLo"
                JOIN "ViTri" v ON v."MaViTri" = t."MaViTri"
                WHERE v."MaKho" = {nhom.Key} AND l."MaBienThe" = ANY({bienThe})
                ORDER BY t."MaLo", t."MaViTri"
                FOR UPDATE OF t
                """);
        }

        var thieu = new List<(string MaDon, string MaBienThe, decimal Can, decimal Con)>();
        var thoiDiem = gio.GetUtcNow();
        foreach (var d in don)
        {
            var giu = new List<GiuHang>();
            foreach (var c in d.ChiTiet.OrderBy(c => c.MaBienThe, StringComparer.Ordinal))
            {
                var can = c.SoLuongDat * c.HeSo - c.SoLuongDaXuat;
                if (can <= 0)
                    continue;
                var kq = await goiY.LayHangAsync(d.MaKho, c.MaBienThe, can, d.MaDonDat);
                if (kq.ConThieu > 0)
                {
                    thieu.Add((d.MaDonDat, c.MaBienThe, can, can - kq.ConThieu));
                    continue;
                }
                giu.AddRange(kq.Dong.Select(x => new GiuHang
                {
                    MaDonDat = d.MaDonDat, MaLo = x.MaLo, MaViTri = x.MaViTri, SoLuong = x.SoLuong, ThuTu = x.ThuTu,
                    MaGoiY = kq.MaGoiY, ThoiDiem = thoiDiem, NguoiGiu = nguoiDung.Ma,
                }));
            }
            if (thieu.Count > 0)
                continue;

            await congGhiTon.GiuAsync(giu.Select(g => new DongGiu(g.MaLo, g.MaViTri, g.SoLuong)).ToList());
            DanhSoThuTu(giu);
            db.GiuHang.AddRange(giu);
            d.TrangThai = TrangThaiDonDat.DaDuyet;
            nhatKy.Ghi("DuyetDonDat", nameof(DonDatHang), d.MaDonDat, maKho: d.MaKho);
            await db.SaveChangesAsync();
        }

        if (thieu.Count > 0)
            throw new NghiepVuException(
                string.Join("; ", thieu.Select(t => $"Đơn {t.MaDon}, {t.MaBienThe}: cần {So(t.Can)}, còn {So(t.Con)}")),
                thieu.Select(t => new { maDonDat = t.MaDon, maBienThe = t.MaBienThe, can = t.Can, con = t.Con }).ToList());
        await tx.CommitAsync();
    }

    /// <summary>Trả hàng đã giữ về tồn khả dụng; đơn quay lại Chờ duyệt.</summary>
    public async Task BoGiuAsync(string maDon)
    {
        await using var tx = await db.Database.BeginTransactionAsync();
        var d = await KhoaDonAsync(maDon);
        if (d.TrangThai != TrangThaiDonDat.DaDuyet)
            throw new NghiepVuException("Chỉ bỏ giữ được đơn đã duyệt, chưa xuất.");
        await BoGiuTrongGiaoDichAsync(d);
        d.TrangThai = TrangThaiDonDat.ChoDuyet;
        nhatKy.Ghi("BoGiuDonDat", nameof(DonDatHang), maDon, maKho: d.MaKho);
        await db.SaveChangesAsync();
        await tx.CommitAsync();
    }

    /// <summary>Hủy đơn đã duyệt: bỏ giữ và hủy trong một transaction; chỉ quản lý kho, quản trị viên.</summary>
    public async Task HuyDonDaDuyetAsync(string maDon)
    {
        if (nguoiDung.VaiTro is not (VaiTro.QuanTriVien or VaiTro.QuanLyKho))
            throw new KhongCoQuyenException("Đơn đã duyệt và giữ hàng chỉ quản lý kho hủy được.");
        await using var tx = await db.Database.BeginTransactionAsync();
        var d = await KhoaDonAsync(maDon);
        if (d.TrangThai != TrangThaiDonDat.DaDuyet)
            throw new NghiepVuException("Đơn không còn ở trạng thái Đã duyệt.");
        await BoGiuTrongGiaoDichAsync(d);
        d.TrangThai = TrangThaiDonDat.DaHuy;
        nhatKy.Ghi("HuyDonDat", nameof(DonDatHang), maDon, new { boGiu = true }, maKho: d.MaKho);
        await db.SaveChangesAsync();
        await tx.CommitAsync();
    }

    private async Task<DonDatHang> KhoaDonAsync(string maDon)
    {
        await db.Database.ExecuteSqlAsync($"""SELECT 1 FROM "DonDatHang" WHERE "MaDonDat" = {maDon} FOR UPDATE""");
        var d = await db.DonDatHang.SingleOrDefaultAsync(x => x.MaDonDat == maDon) ?? throw new NghiepVuException($"Không có đơn đặt hàng {maDon}.");
        await nguoiDung.DamBaoQuyenKhoAsync(d.MaKho);
        return d;
    }

    private async Task BoGiuTrongGiaoDichAsync(DonDatHang d)
    {
        var giu = await db.GiuHang.Where(g => g.MaDonDat == d.MaDonDat).ToListAsync();
        if (giu.Count > 0)
            await congGhiTon.BoGiuAsync(giu.Select(g => new DongGiu(g.MaLo, g.MaViTri, g.SoLuong)).ToList());
        db.GiuHang.RemoveRange(giu);
    }

    /// <summary>G1 đánh thứ tự riêng từng dòng hàng; gộp cả đơn thì đánh lại theo mã ô cho một lượt ghé.</summary>
    private static void DanhSoThuTu(List<GiuHang> giu)
    {
        var i = 0;
        foreach (var g in giu.OrderBy(g => g.MaViTri, StringComparer.Ordinal).ThenBy(g => g.MaLo, StringComparer.Ordinal))
            g.ThuTu = ++i;
    }
}
