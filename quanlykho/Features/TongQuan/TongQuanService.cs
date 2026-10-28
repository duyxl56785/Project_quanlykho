using Microsoft.EntityFrameworkCore;
using quanlykho.Common.PhanQuyen;
using quanlykho.Data;
using quanlykho.Features.NhapKho;
using quanlykho.Features.XuatKho;

namespace quanlykho.Features.TongQuan;

public enum LoaiThe { DuoiToiThieu, VuotToiDa, SapHetHan, QuaHan }

/// <param name="Muc">Tồn tối thiểu hoặc tối đa (hai thẻ mức tồn).</param>
public record DongCanhBao(string MaKho, string MaBienThe, string TenHang, string DonViTinh, decimal SoLuong, decimal? Muc = null, string? MaLo = null, DateOnly? HanDung = null);

public record TheCanhBao(LoaiThe Loai, int SoNgay, DateOnly HomNay, DateTimeOffset TinhDen, List<DongCanhBao> Dong);

/// <summary>Bốn thẻ cảnh báo của Tổng quan (Bảng 2.7); chỉ đọc, chỉ kho được giao.</summary>
public class TongQuanService(AppDbContext db, NguoiDungHienTai nguoiDung, TonKhaDung tonKhaDung, TimeProvider gio)
{
    public const int SoNgayMacDinh = 30;

    public async Task<TheCanhBao> TheAsync(LoaiThe loai, int soNgay)
    {
        var kho = await nguoiDung.KhoDuocGiaoAsync();
        var homNay = tonKhaDung.HomNay;
        var dong = loai switch
        {
            LoaiThe.DuoiToiThieu or LoaiThe.VuotToiDa => await MucTonAsync(kho, loai == LoaiThe.DuoiToiThieu),
            _ => await HanDungAsync(kho, loai == LoaiThe.QuaHan ? null : homNay, loai == LoaiThe.QuaHan ? homNay.AddDays(-1) : homNay.AddDays(soNgay)),
        };
        return new TheCanhBao(loai, soNgay, homNay, gio.GetUtcNow(), dong);
    }

    /// <summary>So tồn khả dụng với mức tồn đã khai báo của biến thể ở kho đó.</summary>
    private async Task<List<DongCanhBao>> MucTonAsync(IReadOnlyList<string> kho, bool duoiToiThieu)
    {
        var muc = await db.MucTon.Where(m => kho.Contains(m.MaKho) && (duoiToiThieu ? m.ToiThieu != null : m.ToiDa != null))
            .Select(m => new { m.MaKho, m.MaBienThe, Muc = duoiToiThieu ? m.ToiThieu!.Value : m.ToiDa!.Value }).ToListAsync();
        var ma = muc.Select(m => m.MaBienThe).Distinct().ToList();
        var ton = await tonKhaDung.TruyVan().Where(d => kho.Contains(d.MaKho) && ma.Contains(d.MaBienThe))
            .GroupBy(d => new { d.MaKho, d.MaBienThe }).Select(g => new { g.Key.MaKho, g.Key.MaBienThe, Tong = g.Sum(d => d.KhaDung) })
            .ToDictionaryAsync(x => (x.MaKho, x.MaBienThe), x => x.Tong);
        var hang = await TenHangAsync(ma);
        return muc.Select(m => new DongCanhBao(m.MaKho, m.MaBienThe, hang[m.MaBienThe].Ten, hang[m.MaBienThe].DonViTinh, ton.GetValueOrDefault((m.MaKho, m.MaBienThe)), m.Muc))
            .Where(d => duoiToiThieu ? d.SoLuong < d.Muc : d.SoLuong > d.Muc)
            .OrderBy(d => d.MaKho).ThenBy(d => d.MaBienThe, StringComparer.Ordinal).ToList();
    }

    /// <summary>Lô của hàng theo dõi hạn dùng còn tồn (mọi khu) có hạn trong [tu, den]; tu null là mọi lô đã hết hạn.</summary>
    private async Task<List<DongCanhBao>> HanDungAsync(IReadOnlyList<string> kho, DateOnly? tu, DateOnly den)
    {
        var lo = await (from t in db.TonKho
                        join v in db.ViTri on t.MaViTri equals v.MaViTri
                        join l in db.Lo on t.MaLo equals l.MaLo
                        where kho.Contains(v.MaKho) && t.SoLuong > 0 && l.BienThe.SanPham.TheoDoiHanDung
                            && l.HanDung <= den && (tu == null || l.HanDung >= tu)
                        group t.SoLuong by new { v.MaKho, l.MaLo, l.MaBienThe, l.HanDung } into g
                        select new { g.Key.MaKho, g.Key.MaLo, g.Key.MaBienThe, g.Key.HanDung, Tong = g.Sum() }).ToListAsync();
        var hang = await TenHangAsync(lo.Select(x => x.MaBienThe).Distinct().ToList());
        return lo.Select(x => new DongCanhBao(x.MaKho, x.MaBienThe, hang[x.MaBienThe].Ten, hang[x.MaBienThe].DonViTinh, x.Tong, MaLo: x.MaLo, HanDung: x.HanDung))
            .OrderBy(d => d.HanDung).ThenBy(d => d.MaKho).ThenBy(d => d.MaLo, StringComparer.Ordinal).ToList();
    }

    private async Task<Dictionary<string, (string Ten, string DonViTinh)>> TenHangAsync(List<string> ma) =>
        (await db.BienThe.Where(b => ma.Contains(b.MaBienThe)).Select(b => new { b.MaBienThe, b.SanPham.Ten, b.ThuocTinh, b.SanPham.DonViTinh }).ToListAsync())
        .ToDictionary(b => b.MaBienThe, b => (DonMuaService.MoTaHang(b.Ten, b.ThuocTinh), b.DonViTinh));
}
