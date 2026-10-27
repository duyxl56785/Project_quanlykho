using Microsoft.EntityFrameworkCore;

namespace quanlykho.Common.GiaoDien;

public interface ITrangDuLieu
{
    int Trang { get; }
    int KichThuocTrang { get; }
    int TongSo { get; }
    int TongTrang { get; }
}

/// <summary>Một trang của danh sách; dùng với thẻ &lt;bang-du-lieu trang="..."&gt; để có trạng thái rỗng và phân trang.</summary>
public class TrangDuLieu<T>(IReadOnlyList<T> muc, int trang, int kichThuocTrang, int tongSo) : ITrangDuLieu
{
    public const int KichThuocMacDinh = 20;

    public IReadOnlyList<T> Muc { get; } = muc;
    public int Trang { get; } = trang;
    public int KichThuocTrang { get; } = kichThuocTrang;
    public int TongSo { get; } = tongSo;
    public int TongTrang => Math.Max(1, (int)Math.Ceiling(TongSo / (double)KichThuocTrang));

    /// <summary>Trang ngoài khoảng được kéo về trang hợp lệ gần nhất.</summary>
    public static async Task<TrangDuLieu<T>> TaoAsync(IQueryable<T> truyVan, int trang, int kichThuocTrang = KichThuocMacDinh)
    {
        var tongSo = await truyVan.CountAsync();
        trang = Math.Clamp(trang, 1, Math.Max(1, (int)Math.Ceiling(tongSo / (double)kichThuocTrang)));
        var muc = await truyVan.Skip((trang - 1) * kichThuocTrang).Take(kichThuocTrang).ToListAsync();
        return new(muc, trang, kichThuocTrang, tongSo);
    }

    /// <summary>Cho dữ liệu đã nằm trong bộ nhớ (không qua EF).</summary>
    public static TrangDuLieu<T> Tao(IEnumerable<T> nguon, int trang, int kichThuocTrang = KichThuocMacDinh)
    {
        var tatCa = nguon.ToList();
        trang = Math.Clamp(trang, 1, Math.Max(1, (int)Math.Ceiling(tatCa.Count / (double)kichThuocTrang)));
        return new(tatCa.Skip((trang - 1) * kichThuocTrang).Take(kichThuocTrang).ToList(), trang, kichThuocTrang, tatCa.Count);
    }
}
