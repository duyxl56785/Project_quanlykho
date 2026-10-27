using Microsoft.EntityFrameworkCore;
using Npgsql;
using quanlykho.Common.Loi;
using quanlykho.Common.PhanQuyen;
using quanlykho.Data;
using quanlykho.Domain;
using static quanlykho.Common.GiaoDien.DinhDang;

namespace quanlykho.Common;

public record DongGhiTon(string MaLo, string MaViTri, decimal SoLuongThayDoi);

public record DongGiu(string MaLo, string MaViTri, decimal SoLuong);

public enum KetQuaGhi { DaGhi, DaGhiTruocDo }

// Nơi duy nhất được sửa TonKho (KE_HOACH A7); chạy trong transaction do Service gọi mở.
public class CongGhiTon(AppDbContext db, NguoiDungHienTai nguoiDung, TimeProvider gio)
{
    public async Task<KetQuaGhi> GhiAsync(string maKhoaChongLap, string loaiChungTu, string maChungTu,
        IReadOnlyList<DongGhiTon> dong, string? maPhieuKiemKe = null, string? nguoiThucHien = null)
    {
        if (string.IsNullOrWhiteSpace(maKhoaChongLap) || maKhoaChongLap.Length > 56)
            throw new ArgumentException("Khóa chống ghi lặp phải có, tối đa 56 ký tự.", nameof(maKhoaChongLap));
        if (dong.Count == 0 || dong.Any(d => d.SoLuongThayDoi == 0))
            throw new ArgumentException("Phải có dòng và mọi số thay đổi khác 0.", nameof(dong));
        DamBaoCoTransaction();

        var thayDoi = dong.GroupBy(d => (d.MaLo, d.MaViTri)).ToDictionary(g => g.Key, g => g.Sum(d => d.SoLuongThayDoi));
        await KiemTraViTriAsync(thayDoi.Keys.Select(k => k.MaViTri), maPhieuKiemKe);
        await TaoDongThieuAsync(thayDoi.Keys);
        var ton = await KhoaDongAsync(thayDoi.Keys);

        // Kiểm sau khi giữ khóa dòng để request cùng khóa vừa commit cũng được thấy.
        if (await db.SoBienDong.AnyAsync(s => s.MaKhoaChongLap == maKhoaChongLap + "#1"))
            return KetQuaGhi.DaGhiTruocDo;

        foreach (var ((lo, viTri), delta) in thayDoi)
        {
            var conDuoc = ton[(lo, viTri)].SoLuong - ton[(lo, viTri)].SoLuongDaGiu;
            if (delta < 0 && -delta > conDuoc)
                throw new NghiepVuException($"Lô {lo} tại {viTri}: cần {So(-delta)}, còn {So(conDuoc)}.",
                    new { maLo = lo, maViTri = viTri, can = -delta, con = conDuoc });
        }
        await CongDonAsync(thayDoi, "SoLuong");

        var thoiDiem = gio.GetUtcNow();
        var nguoi = nguoiThucHien ?? nguoiDung.Ma;
        db.SoBienDong.AddRange(dong.Select((d, i) => new SoBienDong
        {
            MaLo = d.MaLo, MaViTri = d.MaViTri, SoLuongThayDoi = d.SoLuongThayDoi,
            LoaiChungTu = loaiChungTu, MaChungTu = maChungTu, MaKhoaChongLap = $"{maKhoaChongLap}#{i + 1}",
            ThoiDiem = thoiDiem, NguoiThucHien = nguoi,
        }));
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException e) when (e.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw new XungDotException("Thao tác vừa được ghi bởi một yêu cầu khác, vui lòng tải lại trang.");
        }
        return KetQuaGhi.DaGhi;
    }

    public async Task GiuAsync(IReadOnlyList<DongGiu> dong)
    {
        var giu = GopDong(dong);
        await KiemTraViTriAsync(giu.Keys.Select(k => k.MaViTri), null);
        var ton = await KhoaDongAsync(giu.Keys);
        foreach (var ((lo, viTri), soLuong) in giu)
        {
            var conDuoc = ton.TryGetValue((lo, viTri), out var t) ? t.SoLuong - t.SoLuongDaGiu : 0;
            if (soLuong > conDuoc)
                throw new NghiepVuException($"Lô {lo} tại {viTri}: cần {So(soLuong)}, còn {So(conDuoc)}.",
                    new { maLo = lo, maViTri = viTri, can = soLuong, con = conDuoc });
        }
        await CongDonAsync(giu, "SoLuongDaGiu");
    }

    public async Task BoGiuAsync(IReadOnlyList<DongGiu> dong)
    {
        var boGiu = GopDong(dong);
        var ton = await KhoaDongAsync(boGiu.Keys);
        foreach (var ((lo, viTri), soLuong) in boGiu)
            if (!ton.TryGetValue((lo, viTri), out var t) || t.SoLuongDaGiu < soLuong)
                throw new NghiepVuException($"Lô {lo} tại {viTri} không giữ đủ {So(soLuong)} để bỏ giữ.");
        await CongDonAsync(boGiu.ToDictionary(x => x.Key, x => -x.Value), "SoLuongDaGiu");
    }

    private Dictionary<(string MaLo, string MaViTri), decimal> GopDong(IReadOnlyList<DongGiu> dong)
    {
        if (dong.Count == 0 || dong.Any(d => d.SoLuong <= 0))
            throw new ArgumentException("Phải có dòng và mọi số lượng lớn hơn 0.", nameof(dong));
        DamBaoCoTransaction();
        return dong.GroupBy(d => (d.MaLo, d.MaViTri)).ToDictionary(g => g.Key, g => g.Sum(d => d.SoLuong));
    }

    private void DamBaoCoTransaction()
    {
        if (db.Database.CurrentTransaction is null)
            throw new InvalidOperationException("CongGhiTon phải chạy trong transaction của Service gọi.");
    }

    private async Task KiemTraViTriAsync(IEnumerable<string> maViTri, string? maPhieuKiemKe)
    {
        var ma = maViTri.Distinct().ToArray();
        var viTri = await db.ViTri
            .FromSqlRaw("""SELECT * FROM "ViTri" WHERE "MaViTri" = ANY(@ma) FOR SHARE""", new NpgsqlParameter("ma", ma))
            .AsNoTracking().ToListAsync();
        foreach (var m in ma.Except(viTri.Select(v => v.MaViTri)))
            throw new NghiepVuException($"Không có vị trí {m}.");
        foreach (var v in viTri)
        {
            if (v.Cap != 7)
                throw new NghiepVuException($"Vị trí {v.MaViTri} không phải ô; hàng chỉ nằm ở ô.");
            if (v.MaPhieuKiemKe is not null && v.MaPhieuKiemKe != maPhieuKiemKe)
                throw new NghiepVuException($"Ô {v.MaViTri} đang khóa để kiểm kê ({v.MaPhieuKiemKe}).");
        }
    }

    private Task TaoDongThieuAsync(IEnumerable<(string MaLo, string MaViTri)> khoa) =>
        db.Database.ExecuteSqlRawAsync("""
            INSERT INTO "TonKho" ("MaLo", "MaViTri", "SoLuong", "SoLuongDaGiu")
            SELECT k.lo, k.vt, 0, 0 FROM unnest(@lo, @vt) AS k(lo, vt)
            ON CONFLICT DO NOTHING
            """, ThamSo(khoa));

    // Khóa theo thứ tự cố định (MaLo, MaViTri) để hai giao dịch không chờ nhau vòng tròn.
    private async Task<Dictionary<(string, string), TonKho>> KhoaDongAsync(IEnumerable<(string MaLo, string MaViTri)> khoa) =>
        (await db.TonKho.FromSqlRaw("""
            SELECT t.* FROM "TonKho" t JOIN unnest(@lo, @vt) AS k(lo, vt) ON t."MaLo" = k.lo AND t."MaViTri" = k.vt
            ORDER BY t."MaLo", t."MaViTri"
            FOR UPDATE OF t
            """, ThamSo(khoa)).AsNoTracking().ToListAsync())
        .ToDictionary(t => (t.MaLo, t.MaViTri));

    private Task CongDonAsync(Dictionary<(string MaLo, string MaViTri), decimal> thayDoi, string cot) =>
        db.Database.ExecuteSqlRawAsync($"""
            UPDATE "TonKho" t SET "{cot}" = t."{cot}" + k.d
            FROM unnest(@lo, @vt, @d) AS k(lo, vt, d)
            WHERE t."MaLo" = k.lo AND t."MaViTri" = k.vt
            """, [.. ThamSo(thayDoi.Keys), new NpgsqlParameter("d", thayDoi.Values.ToArray())]);

    private static NpgsqlParameter[] ThamSo(IEnumerable<(string MaLo, string MaViTri)> khoa)
    {
        var ds = khoa.ToList();
        return [new("lo", ds.Select(k => k.MaLo).ToArray()), new("vt", ds.Select(k => k.MaViTri).ToArray())];
    }
}
