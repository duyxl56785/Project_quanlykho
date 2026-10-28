using Microsoft.EntityFrameworkCore;
using quanlykho.Common.GiaoDien;
using quanlykho.Common.Loi;
using quanlykho.Data;
using quanlykho.Domain;

namespace quanlykho.Features.DanhMuc;

/// <summary>Đơn vị quy đổi của biến thể khai báo trên biểu mẫu; HeSo là decimal để bắt được số không nguyên người dùng gõ.</summary>
public class DonViQuyDoiDong
{
    public string TenDonVi { get; set; } = "";
    public decimal HeSo { get; set; }
    public string? MaVach { get; set; }
    public TrangThaiSuDung TrangThai { get; set; }
}

public record KetQuaQuyDoi(string TenDonVi, int HeSo, decimal SoLuong);

public record MaVachHang(string MaBienThe, string TenDonVi, int HeSo);

/// <summary>
/// Quy đổi đơn vị một tầng (quy tắc 15): tồn, giữ hàng, sổ biến động, G1, G3 chỉ tính theo đơn vị tính chính;
/// dòng chứng từ quy về đơn vị chính qua đây trước mọi so sánh và trước khi gọi cổng ghi tồn.
/// </summary>
public class QuyDoiDonVi(AppDbContext db)
{
    /// <summary>Đơn vị chính (hệ số 1) và các đơn vị quy đổi đang dùng, xếp theo hệ số.</summary>
    public async Task<List<DonViChon>> DonViAsync(string maBienThe) =>
        (await BangAsync([maBienThe])).GetValueOrDefault(maBienThe) ?? throw new NghiepVuException($"Không có biến thể {maBienThe}.");

    /// <summary>Đơn vị của nhiều biến thể một lần, cho danh sách và báo cáo.</summary>
    public async Task<Dictionary<string, List<DonViChon>>> BangAsync(IEnumerable<string> maBienThe)
    {
        var ma = maBienThe.Distinct().ToList();
        var chinh = await db.BienThe.Where(b => ma.Contains(b.MaBienThe)).Select(b => new { b.MaBienThe, b.SanPham.DonViTinh }).ToListAsync();
        var quyDoi = (await db.DonViQuyDoi.Where(d => ma.Contains(d.MaBienThe) && d.TrangThai == TrangThaiSuDung.DangSuDung).ToListAsync())
            .ToLookup(d => d.MaBienThe);
        return chinh.ToDictionary(b => b.MaBienThe,
            b => quyDoi[b.MaBienThe].OrderBy(d => d.HeSo).Select(d => new DonViChon(d.TenDonVi, d.HeSo)).Prepend(new DonViChon(b.DonViTinh, 1)).ToList());
    }

    /// <summary>Đơn vị theo lô (đơn vị của biến thể của lô), cho dòng chứng từ chọn theo (lô, ô).</summary>
    public async Task<Dictionary<string, List<DonViChon>>> TheoLoAsync(IEnumerable<string> maLo)
    {
        var ma = maLo.Distinct().ToList();
        var lo = await db.Lo.Where(l => ma.Contains(l.MaLo)).ToDictionaryAsync(l => l.MaLo, l => l.MaBienThe);
        var bang = await BangAsync(lo.Values);
        return lo.ToDictionary(x => x.Key, x => bang[x.Value]);
    }

    /// <summary>Đơn vị cho ô chọn đơn vị trên dòng chọn theo "MaLo|MaViTri" (khóa là giá trị lựa chọn).</summary>
    public async Task<Dictionary<string, List<DonViChon>>> TheoLuaChonLoOAsync(IEnumerable<LuaChon> loO)
    {
        var ds = loO.ToList();
        var theoLo = await TheoLoAsync(ds.Select(x => x.Ma.Split('|')[0]));
        return ds.Where(x => theoLo.ContainsKey(x.Ma.Split('|')[0])).ToDictionary(x => x.Ma, x => theoLo[x.Ma.Split('|')[0]]);
    }

    /// <summary>Quy một dòng chọn theo lô; lô không có thì báo lỗi.</summary>
    public static KetQuaQuyDoi QuyVeChinhTheoLo(Dictionary<string, List<DonViChon>> theoLo, string maLo, string? tenDonVi, decimal soLuong) =>
        QuyVeChinh(theoLo.GetValueOrDefault(maLo) ?? throw new NghiepVuException($"Không có lô {maLo}."), tenDonVi, soLuong, maLo);

    /// <summary>Số theo đơn vị đã chọn ra số theo đơn vị tính chính; tên trống là đơn vị chính.</summary>
    public async Task<KetQuaQuyDoi> QuyVeChinhAsync(string maBienThe, string? tenDonVi, decimal soLuongGoc) =>
        QuyVeChinh(await DonViAsync(maBienThe), tenDonVi, soLuongGoc, maBienThe);

    public static KetQuaQuyDoi QuyVeChinh(IReadOnlyList<DonViChon> donVi, string? tenDonVi, decimal soLuongGoc, string maBienThe = "")
    {
        var dv = string.IsNullOrWhiteSpace(tenDonVi) ? donVi[0]
            : donVi.FirstOrDefault(d => string.Equals(d.Ten, tenDonVi.Trim(), StringComparison.CurrentCultureIgnoreCase))
              ?? throw new NghiepVuException($"Biến thể {maBienThe} không có đơn vị {tenDonVi} (hoặc đơn vị đã ngừng sử dụng).");
        return new KetQuaQuyDoi(dv.Ten, dv.HeSo, soLuongGoc * dv.HeSo);
    }

    public async Task<string> HienThiAsync(string maBienThe, decimal soLuong) => HienThi(await DonViAsync(maBienThe), soLuong);

    /// <summary>235 chai với 1 thùng = 24 chai ra "9 thùng 19 chai"; quy ra đơn vị lớn nhất, chỉ để hiển thị.</summary>
    public static string HienThi(IReadOnlyList<DonViChon> donVi, decimal soLuong)
    {
        var chinh = donVi[0];
        var lon = donVi.MaxBy(d => d.HeSo)!;
        var am = soLuong < 0 ? "−" : "";
        var so = Math.Abs(soLuong);
        if (lon.HeSo <= 1 || so < lon.HeSo)
            return $"{am}{DinhDang.So(so)} {chinh.Ten}";
        var phanLon = Math.Floor(so / lon.HeSo);
        var le = so - phanLon * lon.HeSo;
        return le == 0 ? $"{am}{DinhDang.So(phanLon)} {lon.Ten}" : $"{am}{DinhDang.So(phanLon)} {lon.Ten} {DinhDang.So(le)} {chinh.Ten}";
    }

    /// <summary>"9 thùng 19 chai" khi hàng có đơn vị quy đổi và số đủ một đơn vị lớn; null thì không cần hiện thêm.</summary>
    public static string? HienThiNeuCo(IReadOnlyList<DonViChon>? donVi, decimal soLuong) =>
        donVi is { Count: > 1 } && Math.Abs(soLuong) >= donVi.Max(d => d.HeSo) ? HienThi(donVi, soLuong) : null;

    /// <summary>Mã vạch của biến thể (đơn vị chính) hoặc của một đơn vị quy đổi đang dùng.</summary>
    public async Task<MaVachHang?> TraMaVachAsync(string maVach)
    {
        maVach = maVach.Trim();
        return await db.BienThe.Where(b => b.MaVach == maVach).Select(b => new MaVachHang(b.MaBienThe, b.SanPham.DonViTinh, 1)).FirstOrDefaultAsync()
            ?? await db.DonViQuyDoi.Where(d => d.MaVach == maVach && d.TrangThai == TrangThaiSuDung.DangSuDung)
                .Select(d => new MaVachHang(d.MaBienThe, d.TenDonVi, d.HeSo)).FirstOrDefaultAsync();
    }

    /// <summary>Đơn vị đã có trên dòng chứng từ nào chưa; đã có thì chỉ được ngừng sử dụng, không sửa, không xóa.</summary>
    public async Task<bool> DaDungAsync(string maBienThe, string tenDonVi) =>
        await db.ChiTietDonMua.AnyAsync(c => c.MaBienThe == maBienThe && c.TenDonVi == tenDonVi)
        || await db.ChiTietDonDat.AnyAsync(c => c.MaBienThe == maBienThe && c.TenDonVi == tenDonVi)
        || await db.ChiTietLenhXuat.AnyAsync(c => c.MaBienThe == maBienThe && c.TenDonVi == tenDonVi)
        || await db.ChiTietPhieuNhap.AnyAsync(c => c.TenDonVi == tenDonVi && db.Lo.Any(l => l.MaLo == c.MaLo && l.MaBienThe == maBienThe))
        || await db.ChiTietPhieuXuat.AnyAsync(c => c.TenDonVi == tenDonVi && db.Lo.Any(l => l.MaLo == c.MaLo && l.MaBienThe == maBienThe))
        || await db.ChiTietDieuChuyen.AnyAsync(c => c.TenDonVi == tenDonVi && db.Lo.Any(l => l.MaLo == c.MaLo && l.MaBienThe == maBienThe));

    /// <summary>Lưu danh sách đơn vị quy đổi của biến thể (không SaveChanges): kiểm hệ số nguyên &gt; 1, tên không trùng, mã vạch không trùng, đơn vị đã dùng không sửa, xóa.</summary>
    public async Task LuuAsync(BienThe b, string donViTinh, IReadOnlyList<DonViQuyDoiDong> dong)
    {
        dong = dong.Where(d => !string.IsNullOrWhiteSpace(d.TenDonVi)).ToList();
        foreach (var d in dong)
        {
            d.TenDonVi = d.TenDonVi.Trim();
            d.MaVach = string.IsNullOrWhiteSpace(d.MaVach) ? null : d.MaVach.Trim();
            if (d.TenDonVi.Length > 20)
                throw new NghiepVuException($"Tên đơn vị {d.TenDonVi} dài quá 20 ký tự.");
            if (d.HeSo != decimal.Truncate(d.HeSo) || d.HeSo < 2 || d.HeSo > int.MaxValue)
                throw new NghiepVuException($"Hệ số của {d.TenDonVi} phải là số nguyên lớn hơn 1 (1 {d.TenDonVi} = bao nhiêu {donViTinh}).");
            if (string.Equals(d.TenDonVi, donViTinh, StringComparison.CurrentCultureIgnoreCase))
                throw new NghiepVuException($"Đơn vị {d.TenDonVi} trùng đơn vị tính chính.");
        }
        var trung = dong.GroupBy(d => d.TenDonVi, StringComparer.CurrentCultureIgnoreCase).FirstOrDefault(g => g.Count() > 1);
        if (trung is not null)
            throw new NghiepVuException($"Tên đơn vị {trung.Key} khai báo hai lần.");
        var maVach = dong.Where(d => d.MaVach is not null).Select(d => d.MaVach!).ToList();
        var maVachTrung = maVach.GroupBy(m => m).FirstOrDefault(g => g.Count() > 1)?.Key
            ?? maVach.FirstOrDefault(m => m == b.MaVach)
            ?? await db.BienThe.Where(x => x.MaVach != null && maVach.Contains(x.MaVach) && x.MaBienThe != b.MaBienThe).Select(x => x.MaVach).FirstOrDefaultAsync()
            ?? await db.DonViQuyDoi.Where(x => x.MaVach != null && maVach.Contains(x.MaVach) && x.MaBienThe != b.MaBienThe).Select(x => x.MaVach).FirstOrDefaultAsync();
        if (maVachTrung is not null)
            throw new NghiepVuException($"Mã vạch {maVachTrung} đã dùng cho hàng hoặc đơn vị khác.");

        var cu = await db.DonViQuyDoi.Where(x => x.MaBienThe == b.MaBienThe).ToListAsync();
        foreach (var c in cu)
        {
            var moi = dong.FirstOrDefault(d => d.TenDonVi == c.TenDonVi);
            if (moi is null || moi.HeSo != c.HeSo)
            {
                if (await DaDungAsync(b.MaBienThe, c.TenDonVi))
                    throw new NghiepVuException($"Đơn vị {c.TenDonVi} đã có trên chứng từ: không sửa hệ số, không đổi tên, không xóa; chỉ ngừng sử dụng.");
                if (moi is null)
                {
                    db.DonViQuyDoi.Remove(c);
                    continue;
                }
                c.HeSo = (int)moi.HeSo;
            }
            c.MaVach = moi.MaVach;
            c.TrangThai = moi.TrangThai;
        }
        db.DonViQuyDoi.AddRange(dong.Where(d => cu.All(c => c.TenDonVi != d.TenDonVi)).Select(d => new DonViQuyDoi
        {
            MaBienThe = b.MaBienThe, TenDonVi = d.TenDonVi, HeSo = (int)d.HeSo, MaVach = d.MaVach, TrangThai = d.TrangThai,
        }));
    }
}
