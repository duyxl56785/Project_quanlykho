using Microsoft.EntityFrameworkCore;
using quanlykho.Common;
using quanlykho.Common.GiaoDien;
using quanlykho.Common.Loi;
using quanlykho.Common.PhanQuyen;
using quanlykho.Data;
using quanlykho.Domain;

namespace quanlykho.Features.Kho;

/// <summary>Danh mục kho (vị trí cấp 1) và gán người dùng theo kho; chỉ quản trị viên dùng (kiểm ở controller).</summary>
public class KhoService(AppDbContext db, GhiNhatKy nhatKy)
{
    private record KhuMacDinh(string Ten, LoaiKhu Loai, decimal Dai, decimal Rong, decimal Cao, decimal TaiTrong);

    private static readonly KhuMacDinh[] CacKhuMacDinh =
    [
        new("Khu tiếp nhận", LoaiKhu.TiepNhan, 4, 4, 2, 2000),
        new("Khu lưu trữ", LoaiKhu.LuuTru, 1.2m, 1.0m, 1.0m, 300),
        new("Khu cách ly", LoaiKhu.CachLy, 4, 4, 2, 2000),
        new("Khu xuất", LoaiKhu.Xuat, 4, 4, 2, 2000),
    ];

    public async Task<List<KhoDong>> DanhSachAsync() =>
        (await db.ViTri.Where(v => v.Cap == 1).OrderBy(v => v.MaViTri).Select(v => new
        {
            v,
            SoO = db.ViTri.Count(o => o.MaKho == v.MaViTri && o.Cap == CapViTri.CapCuoi),
            CoOMacDinh = db.ViTri.Any(o => o.MaKho == v.MaViTri && o.LaOMacDinh),
            Nguoi = db.NguoiDungKho.Where(k => k.MaKho == v.MaViTri)
                .Join(db.NguoiDung, k => k.MaNguoiDung, n => n.MaNguoiDung, (k, n) => n.Ten).OrderBy(t => t).ToList(),
        }).ToListAsync())
        .Select(x => new KhoDong(x.v.MaViTri, x.v.Ten, x.v.DiaChi, x.v.LoaiHangLuuTru, x.v.HeSoLapDay,
            x.v.DangKhoa ? TrangThaiSuDung.NgungSuDung : TrangThaiSuDung.DangSuDung, x.SoO, x.CoOMacDinh, x.Nguoi))
        .ToList();

    public async Task NapFormAsync(KhoForm form) =>
        form.DanhSachNguoiDung = (await db.NguoiDung.Where(n => n.VaiTro == VaiTro.QuanLyKho || n.VaiTro == VaiTro.ThuKho)
                .OrderBy(n => n.MaNguoiDung).ToListAsync())
            .Select(n => new LuaChon(n.MaNguoiDung, $"{n.Ten} – {Vai.TenHienThi(n.VaiTro)}{(n.TrangThai == TrangThaiTaiKhoan.DaKhoa ? " (đã khóa)" : "")}"))
            .ToList();

    public async Task<KhoForm> FormSuaAsync(string ma)
    {
        var k = await TimAsync(ma);
        var form = new KhoForm
        {
            Ma = k.MaViTri, Ten = k.Ten, DiaChi = k.DiaChi, LoaiHangLuuTru = k.LoaiHangLuuTru, HeSoLapDay = k.HeSoLapDay ?? 0.85m, LaSua = true,
            NguoiDung = await db.NguoiDungKho.Where(x => x.MaKho == ma).Select(x => x.MaNguoiDung).ToListAsync(),
        };
        await NapFormAsync(form);
        return form;
    }

    public async Task<string> ThemAsync(KhoForm form)
    {
        var ma = form.Ma.Trim().ToUpperInvariant();
        if (await db.ViTri.AnyAsync(v => v.MaViTri == ma))
            throw new NghiepVuException($"Mã kho {ma} đã có.");
        var ten = form.Ten.Trim();
        db.ViTri.Add(new ViTri
        {
            MaViTri = ma, MaKho = ma, Cap = 1, Ten = ten, TenDayDu = ten,
            ThuTu = (await db.ViTri.Where(v => v.Cap == 1).MaxAsync(v => (int?)v.ThuTu) ?? 0) + 1,
            DiaChi = form.DiaChi?.Trim(), LoaiHangLuuTru = form.LoaiHangLuuTru?.Trim(), HeSoLapDay = form.HeSoLapDay,
        });
        await GanNguoiDungAsync(ma, form.NguoiDung);
        nhatKy.Ghi("ThemKho", nameof(ViTri), ma, maKho: ma);
        await db.SaveChangesAsync();
        return ma;
    }

    public async Task SuaAsync(string ma, KhoForm form)
    {
        var k = await TimAsync(ma);
        await using var tx = await db.Database.BeginTransactionAsync();
        var ten = form.Ten.Trim();
        if (ten != k.Ten)
        {
            var dai = k.TenDayDu.Length;
            await db.ViTri.Where(v => v.MaKho == ma && v.Cap > 1)
                .ExecuteUpdateAsync(s => s.SetProperty(v => v.TenDayDu, v => ten + v.TenDayDu.Substring(dai)));
            (k.Ten, k.TenDayDu) = (ten, ten);
        }
        k.DiaChi = form.DiaChi?.Trim();
        k.LoaiHangLuuTru = form.LoaiHangLuuTru?.Trim();
        k.HeSoLapDay = form.HeSoLapDay;
        await GanNguoiDungAsync(ma, form.NguoiDung);
        nhatKy.Ghi("SuaKho", nameof(ViTri), ma, maKho: ma);
        await db.SaveChangesAsync();
        await tx.CommitAsync();
    }

    /// <summary>Ngừng sử dụng: khóa mọi vị trí của kho (không nhận hàng); chỉ khi kho đã hết hàng.</summary>
    public async Task KhoaAsync(string ma, bool khoa)
    {
        await TimAsync(ma);
        if (khoa && await db.TonKho.AnyAsync(t => t.SoLuong > 0 && db.ViTri.Any(v => v.MaViTri == t.MaViTri && v.MaKho == ma)))
            throw new NghiepVuException("Kho còn hàng, chuyển hết hàng đi trước khi ngừng sử dụng.");
        await using var tx = await db.Database.BeginTransactionAsync();
        await db.ViTri.Where(v => v.MaKho == ma).ExecuteUpdateAsync(s => s.SetProperty(v => v.DangKhoa, khoa));
        nhatKy.Ghi(khoa ? "NgungKho" : "MoLaiKho", nameof(ViTri), ma, maKho: ma);
        await db.SaveChangesAsync();
        await tx.CommitAsync();
    }

    /// <summary>Sinh tòa mới, tầng 1 và bốn khu (tiếp nhận, lưu trữ, cách ly, xuất), mỗi khu một hàng, một kệ, một ô; ô tiếp nhận và cách ly là ô mặc định.</summary>
    public async Task<string> TaoKhuMacDinhAsync(string ma)
    {
        var kho = await TimAsync(ma);
        if (await db.ViTri.AnyAsync(v => v.MaKho == ma && v.LaOMacDinh))
            throw new NghiepVuException("Kho đã có ô mặc định của khu tiếp nhận và cách ly.");
        var so = (await db.ViTri.Where(v => v.MaCha == ma).MaxAsync(v => (int?)v.ThuTu) ?? 0) + 1;
        var toa = Con(kho, so, "Tòa " + so);
        if (await db.ViTri.AnyAsync(v => v.MaViTri == toa.MaViTri))
            throw new NghiepVuException($"Vị trí {toa.MaViTri} đã có.");
        var tang = Con(toa, 1, "Tầng 1");
        db.ViTri.AddRange(toa, tang);
        for (var i = 0; i < CacKhuMacDinh.Length; i++)
        {
            var khu = CacKhuMacDinh[i];
            var phong = Con(tang, i + 1, khu.Ten, khu.Loai);
            var hang = Con(phong, 1, "Hàng 1", khu.Loai);
            var ke = Con(hang, 1, "Kệ 1", khu.Loai);
            var o = Con(ke, 1, "Ô 1", khu.Loai);
            (o.Dai, o.Rong, o.Cao, o.TaiTrongToiDa) = (khu.Dai, khu.Rong, khu.Cao, khu.TaiTrong);
            o.LaOMacDinh = khu.Loai is LoaiKhu.TiepNhan or LoaiKhu.CachLy;
            db.ViTri.AddRange(phong, hang, ke, o);
        }
        nhatKy.Ghi("TaoKhuMacDinh", nameof(ViTri), toa.MaViTri, maKho: ma);
        await db.SaveChangesAsync();
        return toa.MaViTri;
    }

    /// <summary>Ghi NguoiDungKho (chung với màn hình tài khoản); người bị đổi kho phải đăng nhập lại.</summary>
    private async Task GanNguoiDungAsync(string maKho, List<string> chon)
    {
        chon = chon.Where(m => !string.IsNullOrWhiteSpace(m)).Distinct().ToList();
        var hopLe = await db.NguoiDung.CountAsync(n => chon.Contains(n.MaNguoiDung) && (n.VaiTro == VaiTro.QuanLyKho || n.VaiTro == VaiTro.ThuKho));
        if (hopLe != chon.Count)
            throw new NghiepVuException("Chỉ giao kho cho quản lý kho và thủ kho; quản trị viên và nhân viên bán hàng đã thấy mọi kho.");

        var cu = await db.NguoiDungKho.Where(k => k.MaKho == maKho).ToListAsync();
        var bo = cu.Where(k => !chon.Contains(k.MaNguoiDung)).ToList();
        var them = chon.Except(cu.Select(k => k.MaNguoiDung)).ToList();
        var boMa = bo.Select(b => b.MaNguoiDung).ToList();
        if (await db.NguoiDung.Where(n => boMa.Contains(n.MaNguoiDung) && !db.NguoiDungKho.Any(k => k.MaNguoiDung == n.MaNguoiDung && k.MaKho != maKho))
                .Select(n => n.Ten).FirstOrDefaultAsync() is { } conMotKho)
            throw new NghiepVuException($"{conMotKho} chỉ được giao kho này; giao kho khác cho người đó ở màn hình Người dùng trước khi bỏ.");

        db.NguoiDungKho.RemoveRange(bo);
        db.NguoiDungKho.AddRange(them.Select(m => new NguoiDungKho { MaNguoiDung = m, MaKho = maKho }));
        var doi = boMa.Concat(them).ToList();
        foreach (var nd in await db.NguoiDung.Where(n => doi.Contains(n.MaNguoiDung)).ToListAsync())
        {
            nd.SecurityStamp = Guid.NewGuid().ToString("N");
            nhatKy.Ghi("DoiQuyen", nameof(NguoiDung), nd.MaNguoiDung, new { kho = maKho, thaoTac = them.Contains(nd.MaNguoiDung) ? "Giao" : "Bo" });
        }
    }

    private async Task<ViTri> TimAsync(string ma) =>
        await db.ViTri.SingleOrDefaultAsync(v => v.MaViTri == ma && v.Cap == 1) ?? throw new NghiepVuException($"Không có kho {ma}.");

    private static ViTri Con(ViTri cha, int so, string ten, LoaiKhu? khu = null) => new()
    {
        MaViTri = CapViTri.MaCon(cha.MaViTri, cha.Cap, so),
        MaCha = cha.MaViTri,
        MaKho = cha.MaKho,
        Cap = (short)(cha.Cap + 1),
        Ten = ten,
        TenDayDu = $"{cha.TenDayDu} / {ten}",
        ThuTu = so,
        LoaiKhu = khu,
        DieuKienBaoQuan = khu is null ? null : DieuKienBaoQuan.Thuong,
        DangKhoa = cha.DangKhoa,
    };
}
