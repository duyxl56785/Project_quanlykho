using quanlykho.Common.GiaoDien;

namespace quanlykho.Features.XuatKho;

/// <summary>Một (lô, ô) còn khả dụng của biến thể cần lấy.</summary>
public record UngVienLay(string MaLo, string MaViTri, decimal KhaDung, DateOnly? HanDung, DateTimeOffset NgayNhap);

public record DongGoiYLay(int ThuTu, string MaLo, string MaViTri, decimal SoLuong, DateOnly? HanDung, string LyDo);

public record KetQuaGoiYLay(List<DongGoiYLay> Dong, decimal ConThieu)
{
    public decimal TongLay => Dong.Sum(d => d.SoLuong);
}

/// <summary>
/// Bài toán G1 (mục 1.2.3): lấy đủ q, không vượt khả dụng từng (lô, ô), bỏ lô hết hạn; ưu tiên FEFO (lô không hạn xếp sau),
/// rồi FIFO, rồi ít kệ phải ghé; thứ tự ghé theo mã ô. Lớp thuần, toàn bộ decimal.
/// </summary>
public static class GoiYLayHang
{
    public static KetQuaGoiYLay Tinh(IEnumerable<UngVienLay> ungVien, decimal q, DateOnly homNay)
    {
        var cacLo = ungVien.Where(u => u.KhaDung > 0 && (u.HanDung is null || u.HanDung >= homNay))
            .GroupBy(u => u.MaLo)
            .Select(g => (MaLo: g.Key, g.First().HanDung, NgayNhap: g.Min(u => u.NgayNhap), O: g.ToList()))
            .OrderBy(l => l.HanDung is null).ThenBy(l => l.HanDung).ThenBy(l => l.NgayNhap)
            .ThenByDescending(l => l.O.Max(o => o.KhaDung)).ThenBy(l => l.MaLo)
            .ToList();

        var con = q;
        var chon = new List<(UngVienLay O, decimal SoLuong, string LyDo)>();
        for (var i = 0; i < cacLo.Count && con > 0; i++)
        {
            var lo = cacLo[i];
            var uuTien = lo.HanDung is { } han
                ? $"{(i == 0 ? "hết hạn sớm nhất" : "hết hạn kế tiếp")} {DinhDang.Ngay(han)}"
                : $"{(i == 0 ? "nhập sớm nhất" : "nhập kế tiếp")} {DinhDang.Ngay(lo.NgayNhap)}";

            var motO = lo.O.Where(o => o.KhaDung >= con).OrderBy(o => o.KhaDung).ThenBy(o => o.MaViTri, StringComparer.Ordinal).FirstOrDefault();
            if (motO is not null)
            {
                chon.Add((motO, con, $"{uuTien}; lấy đủ ở một ô"));
                con = 0;
                break;
            }
            foreach (var o in lo.O.OrderByDescending(o => o.KhaDung).ThenBy(o => o.MaViTri, StringComparer.Ordinal))
            {
                var lay = Math.Min(con, o.KhaDung);
                chon.Add((o, lay, $"{uuTien}; ô nhiều hàng nhất còn lại của lô"));
                con -= lay;
                if (con == 0)
                    break;
            }
        }

        var dong = chon.OrderBy(c => c.O.MaViTri, StringComparer.Ordinal)
            .Select((c, i) => new DongGoiYLay(i + 1, c.O.MaLo, c.O.MaViTri, c.SoLuong, c.O.HanDung, c.LyDo))
            .ToList();
        return new KetQuaGoiYLay(dong, con);
    }
}
