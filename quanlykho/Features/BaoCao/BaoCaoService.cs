using Microsoft.EntityFrameworkCore;
using quanlykho.Common.GiaoDien;
using quanlykho.Common.PhanQuyen;
using quanlykho.Data;
using quanlykho.Domain;
using quanlykho.Features.DanhMuc;
using quanlykho.Features.NhapKho;
using quanlykho.Features.XuatKho;

namespace quanlykho.Features.BaoCao;

/// <summary>
/// Báo cáo tồn chỉ đọc (Bảng 2.9), chỉ kho được giao. Số tồn lấy từ TonKho, số theo kỳ lấy từ SoBienDong;
/// hai nguồn khớp nhau vì mọi thay đổi tồn đi qua cổng ghi tồn. Hàng đang vận chuyển không nằm trong TonKho nên tách cột riêng.
/// </summary>
public class BaoCaoService(AppDbContext db, NguoiDungHienTai nguoiDung, TonKhaDung tonKhaDung)
{
    private record HangBaoCao(string Ten, string DonViTinh, bool TheoDoiHanDung, List<DonViChon> DonVi);

    private record DongTon(string MaKho, LoaiKhu? LoaiKhu, string MaViTri, string? MaPhieuKiemKe, string MaLo, string MaBienThe, DateOnly? HanDung, DateTimeOffset NgayNhap,
        decimal SoLuong, decimal DaGiu);

    public async Task<DanhSachLoc> DanhSachLocAsync()
    {
        var kho = await nguoiDung.KhoDuocGiaoAsync();
        return new(await db.ViTri.Where(v => kho.Contains(v.MaViTri)).OrderBy(v => v.MaViTri).Select(v => new LuaChon(v.MaViTri, v.Ten)).ToListAsync(),
            await db.NhomHang.OrderBy(n => n.Ten).Select(n => new LuaChon(n.MaNhom, n.Ten)).ToListAsync());
    }

    public async Task<TongHopTrang> TongHopAsync(LocBaoCao loc, int trang)
    {
        var kho = await KhoAsync(loc);
        var hang = await HangAsync(loc);
        var ma = hang.Keys.ToList();
        var ton = (await TonAsync(kho, ma)).ToLookup(t => t.MaBienThe);
        var khaDung = (await tonKhaDung.TruyVan().Where(d => kho.Contains(d.MaKho) && ma.Contains(d.MaBienThe))
                .GroupBy(d => d.MaBienThe).Select(g => new { g.Key, Tong = g.Sum(d => d.KhaDung) }).ToListAsync())
            .ToDictionary(x => x.Key, x => x.Tong);
        var vanChuyen = await VanChuyenAsync(kho, ma);

        var dong = ma.Select(m => new DongTongHop(m, hang[m].Ten, hang[m].DonViTinh,
                kho.ToDictionary(k => k, k => ton[m].Where(t => t.MaKho == k).Sum(t => t.SoLuong)),
                khaDung.GetValueOrDefault(m), Khu(ton[m], LoaiKhu.TiepNhan), Khu(ton[m], LoaiKhu.CachLy), vanChuyen.GetValueOrDefault(m), hang[m].DonVi))
            .Where(d => d.Tong != 0 || d.VanChuyen != 0)
            .OrderBy(d => d.MaBienThe, StringComparer.Ordinal).ToList();
        var tong = new DongTongHop("", "Tổng cộng", "", kho.ToDictionary(k => k, k => dong.Sum(d => d.TheoKho[k])),
            dong.Sum(d => d.KhaDung), dong.Sum(d => d.TiepNhan), dong.Sum(d => d.CachLy), dong.Sum(d => d.VanChuyen));
        return new TongHopTrang(loc, await DanhSachLocAsync(), kho, TrangDuLieu<DongTongHop>.Tao(dong, trang), tong);
    }

    /// <summary>Tỷ lệ chấp nhận gợi ý AI (quy tắc 13) theo loại, trên kho được giao, lọc theo ngày gợi ý.</summary>
    public async Task<GoiYTrang> GoiYAsync(LocBaoCao loc)
    {
        var danhSach = await DanhSachLocAsync();
        if (loc.TuNgay > loc.DenNgay)
            return new GoiYTrang(LoiNgay(loc), danhSach, []);
        var kho = await KhoAsync(loc);
        var q = db.LichSuGoiY.Where(l => kho.Contains(l.MaKho));
        if (loc.TuNgay is { } tu)
        {
            var tuUtc = DinhDang.BatDauNgay(tu);
            q = q.Where(l => l.ThoiDiem >= tuUtc);
        }
        if (loc.DenNgay is { } den)
        {
            var denUtc = DinhDang.BatDauNgay(den.AddDays(1));
            q = q.Where(l => l.ThoiDiem < denUtc);
        }
        var dem = await q.GroupBy(l => l.Loai)
            .Select(g => new { g.Key, Tong = g.Count(), CoPhanHoi = g.Count(l => l.ChapNhan != null), ChapNhan = g.Count(l => l.ChapNhan == true) })
            .ToDictionaryAsync(x => x.Key);
        return new GoiYTrang(loc, danhSach, Enum.GetValues<LoaiGoiY>()
            .Select(l => dem.TryGetValue(l, out var d) ? new DongGoiY(l, d.Tong, d.CoPhanHoi, d.ChapNhan) : new DongGoiY(l, 0, 0, 0)).ToList());
    }

    /// <summary>Theo lô và hạn dùng: chỉ hàng bật theo dõi hạn dùng (Bảng 2.9 dòng 3).</summary>
    public async Task<BaoCaoTrang<DongTheoLo>> TheoLoAsync(LocBaoCao loc, int trang)
    {
        var kho = await KhoAsync(loc);
        var hang = (await HangAsync(loc)).Where(h => h.Value.TheoDoiHanDung).ToDictionary();
        var ma = hang.Keys.ToList();
        var khaDung = (await tonKhaDung.TruyVan().Where(d => kho.Contains(d.MaKho) && ma.Contains(d.MaBienThe))
                .GroupBy(d => new { d.MaKho, d.MaLo }).Select(g => new { g.Key.MaKho, g.Key.MaLo, Tong = g.Sum(d => d.KhaDung) }).ToListAsync())
            .ToDictionary(x => (x.MaKho, x.MaLo), x => x.Tong);
        var dong = (await TonAsync(kho, ma)).GroupBy(t => (t.MaKho, t.MaLo))
            .Select(g =>
            {
                var t = g.First();
                return new DongTheoLo(t.MaKho, t.MaLo, t.MaBienThe, hang[t.MaBienThe].Ten, hang[t.MaBienThe].DonViTinh, t.HanDung, t.NgayNhap,
                    g.Sum(x => x.SoLuong), g.Sum(x => x.DaGiu), khaDung.GetValueOrDefault(g.Key), Khu(g, LoaiKhu.TiepNhan), Khu(g, LoaiKhu.CachLy), hang[t.MaBienThe].DonVi);
            })
            .OrderBy(d => d.HanDung).ThenBy(d => d.MaKho).ThenBy(d => d.MaLo, StringComparer.Ordinal);
        return new(loc, await DanhSachLocAsync(), TrangDuLieu<DongTheoLo>.Tao(dong, trang));
    }

    /// <summary>Theo vị trí: mỗi dòng một (ô, lô); MaViTri lọc theo tiền tố để xem cả kệ, dãy.</summary>
    public async Task<BaoCaoTrang<DongTheoViTri>> TheoViTriAsync(LocBaoCao loc, int trang)
    {
        var kho = await KhoAsync(loc);
        var hang = await HangAsync(loc);
        var dong = (await TonAsync(kho, hang.Keys.ToList()))
            .Where(t => (loc.LoaiKhu is null || t.LoaiKhu == loc.LoaiKhu) && (string.IsNullOrWhiteSpace(loc.MaViTri) || t.MaViTri.StartsWith(loc.MaViTri.Trim(), StringComparison.OrdinalIgnoreCase)))
            .OrderBy(t => t.MaViTri, StringComparer.Ordinal).ThenBy(t => t.MaLo, StringComparer.Ordinal)
            .Select(t => new DongTheoViTri(t.MaViTri, t.LoaiKhu, t.MaLo, t.MaBienThe, hang[t.MaBienThe].Ten, hang[t.MaBienThe].DonViTinh, t.HanDung, t.SoLuong, t.DaGiu, t.MaPhieuKiemKe, hang[t.MaBienThe].DonVi));
        return new(loc, await DanhSachLocAsync(), TrangDuLieu<DongTheoViTri>.Tao(dong, trang));
    }

    /// <summary>
    /// Nhập xuất tồn theo kỳ, mỗi dòng một (kho, biến thể): đầu kỳ là tổng biến động trước ngày bắt đầu; trong kỳ, mỗi chứng từ tính số ròng
    /// nên chuyển vị trí trong kho (trừ, cộng cùng chứng từ) không thành nhập, xuất.
    /// </summary>
    public async Task<NhapXuatTonTrang> NhapXuatTonAsync(LocBaoCao loc, int trang)
    {
        var homNay = tonKhaDung.HomNay;
        var tu = loc.TuNgay ??= new DateOnly(homNay.Year, homNay.Month, 1);
        var den = loc.DenNgay ??= homNay;
        if (tu > den)
            return new NhapXuatTonTrang(LoiNgay(loc), await DanhSachLocAsync(), TrangDuLieu<DongNhapXuatTon>.Tao([], 1), new("", "", "Tổng cộng", "", 0, 0, 0));
        var kho = await KhoAsync(loc);
        var hang = await HangAsync(loc);
        var ma = hang.Keys.ToList();
        var tuUtc = DinhDang.BatDauNgay(tu);
        var denUtc = DinhDang.BatDauNgay(den.AddDays(1));
        var bienDong = BienDong(kho, ma);

        var dauKy = (await bienDong.Where(x => x.ThoiDiem < tuUtc).GroupBy(x => new { x.MaKho, x.MaBienThe })
                .Select(g => new { g.Key.MaKho, g.Key.MaBienThe, Tong = g.Sum(x => x.SoLuongThayDoi) }).ToListAsync())
            .ToDictionary(x => (x.MaKho, x.MaBienThe), x => x.Tong);
        var trongKy = (await bienDong.Where(x => x.ThoiDiem >= tuUtc && x.ThoiDiem < denUtc).GroupBy(x => new { x.MaKho, x.MaBienThe, x.MaChungTu })
                .Select(g => new { g.Key.MaKho, g.Key.MaBienThe, Rong = g.Sum(x => x.SoLuongThayDoi) }).ToListAsync())
            .ToLookup(x => (x.MaKho, x.MaBienThe), x => x.Rong);

        var dong = dauKy.Keys.Union(trongKy.Select(g => g.Key))
            .Select(k => new DongNhapXuatTon(k.MaKho, k.MaBienThe, hang[k.MaBienThe].Ten, hang[k.MaBienThe].DonViTinh, dauKy.GetValueOrDefault(k),
                trongKy[k].Where(r => r > 0).Sum(), -trongKy[k].Where(r => r < 0).Sum(), hang[k.MaBienThe].DonVi))
            .Where(d => d.DauKy != 0 || d.Nhap != 0 || d.Xuat != 0)
            .OrderBy(d => d.MaKho).ThenBy(d => d.MaBienThe, StringComparer.Ordinal).ToList();
        var tong = new DongNhapXuatTon("", "", "Tổng cộng", "", dong.Sum(d => d.DauKy), dong.Sum(d => d.Nhap), dong.Sum(d => d.Xuat));
        return new NhapXuatTonTrang(loc, await DanhSachLocAsync(), TrangDuLieu<DongNhapXuatTon>.Tao(dong, trang), tong);
    }

    /// <summary>Thẻ kho một biến thể: sổ biến động theo thời gian với số dư lũy kế; lọc thêm lô, ô (tiền tố).</summary>
    public async Task<TheKhoTrang> TheKhoAsync(LocBaoCao loc, int trang)
    {
        var danhSach = await DanhSachLocAsync();
        var rong = TrangDuLieu<DongTheKho>.Tao([], 1);
        var maBienThe = loc.MaBienThe?.Trim();
        if (string.IsNullOrEmpty(maBienThe))
            return new TheKhoTrang(loc, danhSach, null, null, 0, 0, rong);
        var hang = await db.BienThe.Where(b => b.MaBienThe == maBienThe).Select(b => new { b.SanPham.Ten, b.ThuocTinh, b.SanPham.DonViTinh }).SingleOrDefaultAsync();
        if (hang is null)
        {
            loc.Loi = $"Không có biến thể {maBienThe}.";
            return new TheKhoTrang(loc, danhSach, null, null, 0, 0, rong);
        }
        if (loc.TuNgay > loc.DenNgay)
            return new TheKhoTrang(LoiNgay(loc), danhSach, null, null, 0, 0, rong);
        var kho = await KhoAsync(loc);

        var q = BienDong(kho, [maBienThe]);
        if (!string.IsNullOrWhiteSpace(loc.MaLo)) q = q.Where(x => x.MaLo == loc.MaLo.Trim());
        if (!string.IsNullOrWhiteSpace(loc.MaViTri)) q = q.Where(x => x.MaViTri.StartsWith(loc.MaViTri.Trim()));
        var dauKy = 0m;
        if (loc.TuNgay is { } tu)
        {
            var tuUtc = DinhDang.BatDauNgay(tu);
            dauKy = await q.Where(x => x.ThoiDiem < tuUtc).SumAsync(x => x.SoLuongThayDoi);
            q = q.Where(x => x.ThoiDiem >= tuUtc);
        }
        if (loc.DenNgay is { } den)
        {
            var denUtc = DinhDang.BatDauNgay(den.AddDays(1));
            q = q.Where(x => x.ThoiDiem < denUtc);
        }
        var bienDong = await q.OrderBy(x => x.ThoiDiem).ThenBy(x => x.MaBienDong).ToListAsync();
        var nguoi = bienDong.Select(b => b.NguoiThucHien).Distinct().ToList();
        var ten = await db.NguoiDung.Where(n => nguoi.Contains(n.MaNguoiDung)).ToDictionaryAsync(n => n.MaNguoiDung, n => n.Ten);

        var ton = dauKy;
        var dong = bienDong.Select(b =>
        {
            ton += b.SoLuongThayDoi;
            return new DongTheKho(b.ThoiDiem, b.LoaiChungTu, b.MaChungTu, b.MaLo, b.MaViTri, Math.Max(b.SoLuongThayDoi, 0), Math.Max(-b.SoLuongThayDoi, 0), ton,
                ten.GetValueOrDefault(b.NguoiThucHien, b.NguoiThucHien));
        }).ToList();
        return new TheKhoTrang(loc, danhSach, DonMuaService.MoTaHang(hang.Ten, hang.ThuocTinh), hang.DonViTinh, dauKy, ton, TrangDuLieu<DongTheKho>.Tao(dong, trang),
            await new QuyDoiDonVi(db).DonViAsync(maBienThe));
    }

    private class DongBienDong
    {
        public long MaBienDong { get; init; }
        public DateTimeOffset ThoiDiem { get; init; }
        public string LoaiChungTu { get; init; } = "";
        public string MaChungTu { get; init; } = "";
        public string MaLo { get; init; } = "";
        public string MaViTri { get; init; } = "";
        public string MaKho { get; init; } = "";
        public string MaBienThe { get; init; } = "";
        public decimal SoLuongThayDoi { get; init; }
        public string NguoiThucHien { get; init; } = "";
    }

    private IQueryable<DongBienDong> BienDong(List<string> kho, List<string> ma) =>
        from s in db.SoBienDong
        join v in db.ViTri on s.MaViTri equals v.MaViTri
        join l in db.Lo on s.MaLo equals l.MaLo
        where kho.Contains(v.MaKho) && ma.Contains(l.MaBienThe)
        select new DongBienDong
        {
            MaBienDong = s.MaBienDong, ThoiDiem = s.ThoiDiem, LoaiChungTu = s.LoaiChungTu, MaChungTu = s.MaChungTu, MaLo = s.MaLo, MaViTri = s.MaViTri,
            MaKho = v.MaKho, MaBienThe = l.MaBienThe, SoLuongThayDoi = s.SoLuongThayDoi, NguoiThucHien = s.NguoiThucHien,
        };

    private async Task<List<DongTon>> TonAsync(List<string> kho, List<string> ma) =>
        await (from t in db.TonKho
               join v in db.ViTri on t.MaViTri equals v.MaViTri
               join l in db.Lo on t.MaLo equals l.MaLo
               where kho.Contains(v.MaKho) && ma.Contains(l.MaBienThe) && t.SoLuong != 0
               select new DongTon(v.MaKho, v.LoaiKhu, t.MaViTri, v.MaPhieuKiemKe, t.MaLo, l.MaBienThe, l.HanDung, l.NgayNhap, t.SoLuong, t.SoLuongDaGiu))
            .ToListAsync();

    /// <summary>Phần chưa nhận, chưa xử lý của phiếu điều chuyển đang chờ nhập hoặc nhận một phần.</summary>
    private async Task<Dictionary<string, decimal>> VanChuyenAsync(List<string> kho, List<string> ma) =>
        (await (from c in db.ChiTietDieuChuyen
                join p in db.PhieuDieuChuyen on c.MaPhieu equals p.MaPhieu
                join l in db.Lo on c.MaLo equals l.MaLo
                where (p.TrangThai == TrangThaiDieuChuyen.ChoNhapKho || p.TrangThai == TrangThaiDieuChuyen.NhanMotPhan) && c.CachXuLy == null
                    && (kho.Contains(p.MaKhoDi) || kho.Contains(p.MaKhoDen)) && ma.Contains(l.MaBienThe)
                select new { l.MaBienThe, Con = c.SoLuong - (c.SoLuongNhan ?? 0) }).ToListAsync())
        .GroupBy(x => x.MaBienThe).ToDictionary(g => g.Key, g => g.Sum(x => x.Con));

    private static decimal Khu(IEnumerable<DongTon> ton, LoaiKhu khu) => ton.Where(t => t.LoaiKhu == khu).Sum(t => t.SoLuong);

    private async Task<List<string>> KhoAsync(LocBaoCao loc)
    {
        if (string.IsNullOrWhiteSpace(loc.MaKho))
            return [.. await nguoiDung.KhoDuocGiaoAsync()];
        await nguoiDung.DamBaoQuyenKhoAsync(loc.MaKho);
        return [loc.MaKho];
    }

    /// <summary>Biến thể khớp từ khóa (mã, tên, mã vạch) và nhóm (gồm nhóm con).</summary>
    private async Task<Dictionary<string, HangBaoCao>> HangAsync(LocBaoCao loc)
    {
        var q = db.BienThe.AsQueryable();
        if (!string.IsNullOrWhiteSpace(loc.TuKhoa))
        {
            var tk = loc.TuKhoa.Trim();
            var mau = $"%{tk}%";
            q = q.Where(b => EF.Functions.ILike(b.MaBienThe, mau) || EF.Functions.ILike(b.SanPham.Ten, mau) || b.MaVach == tk);
        }
        if (!string.IsNullOrWhiteSpace(loc.MaNhom))
        {
            var con = (await db.NhomHang.Select(n => new { n.MaNhom, n.MaCha }).ToListAsync()).ToLookup(n => n.MaCha, n => n.MaNhom);
            var nhom = new List<string> { loc.MaNhom };
            for (var i = 0; i < nhom.Count; i++)
                nhom.AddRange(con[nhom[i]]);
            q = q.Where(b => nhom.Contains(b.SanPham.MaNhom));
        }
        var hang = await q.Select(b => new { b.MaBienThe, b.SanPham.Ten, b.ThuocTinh, b.SanPham.DonViTinh, b.SanPham.TheoDoiHanDung }).ToListAsync();
        var donVi = await new QuyDoiDonVi(db).BangAsync(hang.Select(b => b.MaBienThe));
        return hang.ToDictionary(b => b.MaBienThe, b => new HangBaoCao(DonMuaService.MoTaHang(b.Ten, b.ThuocTinh), b.DonViTinh, b.TheoDoiHanDung, donVi[b.MaBienThe]));
    }

    /// <summary>Báo cáo là trang GET chỉ đọc: lỗi bộ lọc hiện ngay trên trang thay vì ném lỗi nghiệp vụ.</summary>
    private static LocBaoCao LoiNgay(LocBaoCao loc)
    {
        loc.Loi = "Ngày bắt đầu không được sau ngày kết thúc.";
        return loc;
    }
}
