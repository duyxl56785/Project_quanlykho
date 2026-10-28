using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using quanlykho.Common;
using quanlykho.Common.GiaoDien;
using quanlykho.Common.Loi;
using quanlykho.Common.PhanQuyen;
using quanlykho.Data;
using quanlykho.Domain;
using quanlykho.Features.DanhMuc;
using quanlykho.Features.NhapKho;
using static quanlykho.Common.GiaoDien.DinhDang;

namespace quanlykho.Features.XuatKho;

public record PhieuXuatDong(string Ma, LoaiPhieuXuat LoaiPhieu, string MaKho, string? MaLenh, DateTimeOffset NgayXuat, int SoDong, TrangThaiPhieu TrangThai);

/// <param name="SoLuongGoc">Theo TenDonVi; SoLuong theo đơn vị tính chính (số đã trừ tồn).</param>
public record DongPhieuXuat(int STT, string MaLo, string MaBienThe, string TenHang, string MaViTri, DateOnly? HanDung, string TenDonVi, decimal SoLuong,
    decimal SoLuongGoc, int HeSo, string DonViChinh);

public record PhieuXuatChiTiet(string Ma, LoaiPhieuXuat LoaiPhieu, string? MaLenh, string MaKho, string TenKho, string? NhaCungCap, string NguoiLap,
    string? NguoiXacNhan, DateTimeOffset NgayXuat, TrangThaiPhieu TrangThai, string? LyDo, List<DongPhieuXuat> Dong);

public class DongPhieuXuatForm
{
    /// <summary>"MaLo|MaViTri" chọn từ danh sách tồn của kho.</summary>
    public string LoO { get; set; } = "";
    /// <summary>Trống là đơn vị tính chính.</summary>
    public string? TenDonVi { get; set; }
    public decimal SoLuong { get; set; }
}

public class PhieuXuatForm
{
    [Required(ErrorMessage = "Chọn kho xuất."), Display(Name = "Kho xuất")]
    public string MaKho { get; set; } = "";

    [Display(Name = "Loại phiếu")]
    public LoaiPhieuXuat LoaiPhieu { get; set; } = LoaiPhieuXuat.XuatKhac;

    [Display(Name = "Nhà cung cấp")]
    public string? MaNhaCungCap { get; set; }

    [Required(ErrorMessage = "Nhập lý do xuất."), StringLength(500), Display(Name = "Lý do")]
    public string LyDo { get; set; } = "";

    public string KhoaChongLap { get; set; } = Guid.NewGuid().ToString("N");

    public List<DongPhieuXuatForm> Dong { get; set; } = [];

    public List<LuaChon> DanhSachKho { get; set; } = [];
    public List<LuaChon> DanhSachNhaCungCap { get; set; } = [];
    public List<LuaChon> DanhSachTon { get; set; } = [];
    /// <summary>Đơn vị theo từng lựa chọn "MaLo|MaViTri" của DanhSachTon.</summary>
    public Dictionary<string, List<DonViChon>> DonViHang { get; set; } = [];
}

/// <summary>Phiếu xuất ghi sổ. Xuất bán sinh từ lệnh xuất; xuất khác và trả nhà cung cấp do quản lý kho lập trực tiếp, Hoàn tất ngay qua cổng ghi tồn.</summary>
public class PhieuXuatService(AppDbContext db, NguoiDungHienTai nguoiDung, MaChungTu maChungTu, CongGhiTon congGhiTon, GhiNhatKy nhatKy, TimeProvider gio)
{
    public const string LoaiChungTu = LenhXuatService.LoaiChungTu;

    public async Task<TrangDuLieu<PhieuXuatDong>> DanhSachAsync(string? maKho, LoaiPhieuXuat? loai, int trang)
    {
        var kho = await nguoiDung.KhoDuocGiaoAsync();
        var q = db.PhieuXuat.Where(p => kho.Contains(p.MaKho));
        if (!string.IsNullOrWhiteSpace(maKho)) q = q.Where(p => p.MaKho == maKho);
        if (loai is not null) q = q.Where(p => p.LoaiPhieu == loai);
        return await TrangDuLieu<PhieuXuatDong>.TaoAsync(q.OrderByDescending(p => p.NgayXuat).ThenByDescending(p => p.MaPhieuXuat)
            .Select(p => new PhieuXuatDong(p.MaPhieuXuat, p.LoaiPhieu, p.MaKho, p.MaLenh, p.NgayXuat, p.ChiTiet.Count, p.TrangThai)), trang);
    }

    public async Task<PhieuXuatChiTiet> LayAsync(string ma)
    {
        var p = await db.PhieuXuat.AsNoTracking().Include(x => x.ChiTiet).SingleOrDefaultAsync(x => x.MaPhieuXuat == ma)
            ?? throw new NghiepVuException($"Không có phiếu xuất {ma}.");
        await nguoiDung.DamBaoQuyenKhoAsync(p.MaKho);
        var maLo = p.ChiTiet.Select(c => c.MaLo).ToList();
        var lo = await db.Lo.Where(l => maLo.Contains(l.MaLo))
            .Select(l => new { l.MaLo, l.MaBienThe, l.HanDung, l.BienThe.SanPham.Ten, l.BienThe.ThuocTinh, l.BienThe.SanPham.DonViTinh }).ToDictionaryAsync(l => l.MaLo);
        var ten = await db.NguoiDung.Where(n => n.MaNguoiDung == p.NguoiLap || n.MaNguoiDung == p.NguoiXacNhan).ToDictionaryAsync(n => n.MaNguoiDung, n => n.Ten);
        return new PhieuXuatChiTiet(p.MaPhieuXuat, p.LoaiPhieu, p.MaLenh, p.MaKho, await db.ViTri.Where(v => v.MaViTri == p.MaKho).Select(v => v.Ten).SingleAsync(),
            p.MaNhaCungCap is null ? null : await db.NhaCungCap.Where(n => n.MaNhaCungCap == p.MaNhaCungCap).Select(n => n.MaNhaCungCap + " – " + n.Ten).SingleAsync(),
            ten[p.NguoiLap], p.NguoiXacNhan is null ? null : ten[p.NguoiXacNhan], p.NgayXuat, p.TrangThai, p.LyDo,
            p.ChiTiet.OrderBy(c => c.STT).Select(c => new DongPhieuXuat(c.STT, c.MaLo, lo[c.MaLo].MaBienThe, DonMuaService.MoTaHang(lo[c.MaLo].Ten, lo[c.MaLo].ThuocTinh),
                c.MaViTri, lo[c.MaLo].HanDung, c.TenDonVi, c.SoLuong, c.SoLuongGoc, c.HeSo, lo[c.MaLo].DonViTinh)).ToList());
    }

    public async Task<PhieuXuatForm> NapFormAsync(PhieuXuatForm form)
    {
        var kho = await nguoiDung.KhoDuocGiaoAsync();
        form.DanhSachKho = await db.ViTri.Where(v => kho.Contains(v.MaViTri)).OrderBy(v => v.MaViTri).Select(v => new LuaChon(v.MaViTri, v.Ten)).ToListAsync();
        form.DanhSachNhaCungCap = await db.NhaCungCap.Where(n => n.TrangThai == TrangThaiSuDung.DangSuDung).OrderBy(n => n.MaNhaCungCap)
            .Select(n => new LuaChon(n.MaNhaCungCap, n.Ten)).ToListAsync();
        if (string.IsNullOrEmpty(form.MaKho))
            form.MaKho = form.DanhSachKho.FirstOrDefault()?.Ma ?? "";
        if (!kho.Contains(form.MaKho))
            return form;
        form.DanhSachTon = (await db.TonKho.Where(t => t.SoLuong > t.SoLuongDaGiu)
                .Join(db.ViTri.Where(v => v.MaKho == form.MaKho), t => t.MaViTri, v => v.MaViTri, (t, v) => new { t, v.LoaiKhu })
                .Join(db.Lo, x => x.t.MaLo, l => l.MaLo, (x, l) => new { x.t.MaLo, x.t.MaViTri, x.LoaiKhu, ConLai = x.t.SoLuong - x.t.SoLuongDaGiu, l.MaBienThe, l.HanDung, l.BienThe.SanPham.DonViTinh })
                .OrderBy(x => x.MaViTri).ThenBy(x => x.MaLo).ToListAsync())
            .Select(x => new LuaChon($"{x.MaLo}|{x.MaViTri}",
                $"{x.MaViTri} · {x.MaLo} · {x.MaBienThe}{(x.HanDung is null ? "" : $" · HSD {Ngay(x.HanDung)}")} · còn {So(x.ConLai)} {x.DonViTinh} ({Kho.CapViTri.TenKhu(x.LoaiKhu)})"))
            .ToList();
        form.DonViHang = await new QuyDoiDonVi(db).TheoLuaChonLoOAsync(form.DanhSachTon);
        return form;
    }

    /// <summary>Xuất khác (trưng bày, hủy hàng hết hạn, dùng thử) hoặc trả nhà cung cấp; lấy ở bất kỳ ô nào của kho, kể cả khu cách ly.</summary>
    public async Task<(string Ma, KetQuaGhi KetQua)> TaoKhacAsync(PhieuXuatForm form)
    {
        if (nguoiDung.VaiTro is not (VaiTro.QuanTriVien or VaiTro.QuanLyKho))
            throw new KhongCoQuyenException("Chỉ quản lý kho lập phiếu xuất khác.");
        await nguoiDung.DamBaoQuyenKhoAsync(form.MaKho);
        var khoa = form.KhoaChongLap?.Trim();
        if (string.IsNullOrEmpty(khoa) || khoa.Length > 56)
            throw new NghiepVuException("Thiếu khóa chống ghi lặp (tối đa 56 ký tự).");
        if (form.LoaiPhieu == LoaiPhieuXuat.XuatBan)
            throw new NghiepVuException("Phiếu xuất bán sinh từ lệnh xuất, không lập tay.");
        if (string.IsNullOrWhiteSpace(form.LyDo))
            throw new NghiepVuException("Nhập lý do xuất.");
        string? ncc = null;
        if (form.LoaiPhieu == LoaiPhieuXuat.TraNhaCungCap)
        {
            ncc = form.MaNhaCungCap;
            if (!await db.NhaCungCap.AnyAsync(n => n.MaNhaCungCap == ncc))
                throw new NghiepVuException("Chọn nhà cung cấp nhận hàng trả.");
        }

        var chon = form.Dong.Where(d => !string.IsNullOrWhiteSpace(d.LoO)).ToList();
        var theoLo = await new QuyDoiDonVi(db).TheoLoAsync(chon.Select(d => d.LoO.Split('|')[0]));
        var dong = new List<(string MaLo, string MaViTri, KetQuaQuyDoi Q, decimal SoLuongGoc)>();
        foreach (var d in chon)
        {
            var tach = d.LoO.Split('|');
            if (tach.Length != 2)
                throw new NghiepVuException("Dòng hàng không hợp lệ, chọn lại lô và ô.");
            if (d.SoLuong <= 0)
                throw new NghiepVuException("Số lượng xuất phải lớn hơn 0.");
            dong.Add((tach[0], tach[1], QuyDoiDonVi.QuyVeChinhTheoLo(theoLo, tach[0], d.TenDonVi, d.SoLuong), d.SoLuong));
        }
        if (dong.Count == 0)
            throw new NghiepVuException("Phiếu xuất phải có ít nhất một dòng hàng.");
        if (dong.GroupBy(d => (d.MaLo, d.MaViTri)).FirstOrDefault(g => g.Count() > 1) is { } trung)
            throw new NghiepVuException($"Lô {trung.Key.MaLo} tại {trung.Key.MaViTri} xuất hiện nhiều dòng.");
        var maO = dong.Select(d => d.MaViTri).Distinct().ToList();
        if (await db.ViTri.CountAsync(v => maO.Contains(v.MaViTri) && v.MaKho == form.MaKho) != maO.Count)
            throw new NghiepVuException($"Có ô không thuộc kho {form.MaKho}.");

        await using var tx = await db.Database.BeginTransactionAsync();
        var daGhi = await db.SoBienDong.Where(s => s.MaKhoaChongLap == khoa + "#1" && s.LoaiChungTu == LoaiChungTu).Select(s => s.MaChungTu).FirstOrDefaultAsync();
        if (daGhi is not null)
            return (daGhi, KetQuaGhi.DaGhiTruocDo);

        var p = new PhieuXuat
        {
            MaPhieuXuat = await maChungTu.TaoAsync("PX"), LoaiPhieu = form.LoaiPhieu, MaKho = form.MaKho, MaNhaCungCap = ncc,
            NguoiLap = nguoiDung.Ma, NguoiXacNhan = nguoiDung.Ma, NgayXuat = gio.GetUtcNow(), TrangThai = TrangThaiPhieu.HoanTat, LyDo = form.LyDo.Trim(),
            ChiTiet = dong.Select((d, i) => new ChiTietPhieuXuat
            {
                STT = i + 1, MaLo = d.MaLo, MaViTri = d.MaViTri, TenDonVi = d.Q.TenDonVi, HeSo = d.Q.HeSo, SoLuongGoc = d.SoLuongGoc, SoLuong = d.Q.SoLuong,
            }).ToList(),
        };
        db.PhieuXuat.Add(p);
        nhatKy.Ghi(p.LoaiPhieu == LoaiPhieuXuat.TraNhaCungCap ? "TraNhaCungCap" : "XuatKhac", nameof(PhieuXuat), p.MaPhieuXuat, new { lyDo = p.LyDo }, maKho: p.MaKho);
        if (await congGhiTon.GhiAsync(khoa, LoaiChungTu, p.MaPhieuXuat, dong.Select(d => new DongGhiTon(d.MaLo, d.MaViTri, -d.Q.SoLuong)).ToList()) == KetQuaGhi.DaGhiTruocDo)
            throw new XungDotException("Thao tác vừa được ghi bởi một yêu cầu khác, vui lòng tải lại trang.");
        await tx.CommitAsync();
        return (p.MaPhieuXuat, KetQuaGhi.DaGhi);
    }
}
