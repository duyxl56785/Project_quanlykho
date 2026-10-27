using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using quanlykho.Common;
using quanlykho.Common.GiaoDien;
using quanlykho.Common.Loi;
using quanlykho.Data;
using quanlykho.Domain;
using NguoiDungEntity = quanlykho.Domain.NguoiDung;

namespace quanlykho.Features.QuanLyNguoiDung;

public record NguoiDungDong(string Ma, string Ten, string Email, string? SoDienThoai, VaiTro VaiTro, TrangThaiTaiKhoan TrangThai, DateTimeOffset? KhoaDenLuc, List<string> Kho);

public class NguoiDungForm
{
    public string? Ma { get; set; }

    [Required(ErrorMessage = "Nhập họ tên.")]
    [StringLength(100)]
    [Display(Name = "Họ tên")]
    public string Ten { get; set; } = "";

    [Required(ErrorMessage = "Nhập email.")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
    [StringLength(200)]
    [Display(Name = "Email")]
    public string Email { get; set; } = "";

    [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
    [StringLength(20)]
    [Display(Name = "Số điện thoại")]
    public string? SoDienThoai { get; set; }

    [Display(Name = "Vai trò")]
    public VaiTro VaiTro { get; set; } = VaiTro.ThuKho;

    [Display(Name = "Kho được giao")]
    public List<string> Kho { get; set; } = [];

    /// <summary>Chỉ dùng khi thêm mới.</summary>
    [DataType(DataType.Password)]
    [Display(Name = "Mật khẩu")]
    public string? MatKhau { get; set; }

    public TrangThaiTaiKhoan TrangThai { get; set; }

    /// <summary>Danh sách kho để chọn, controller nạp trước khi hiện form.</summary>
    public List<LuaChon> DanhSachKho { get; set; } = [];
}

/// <summary>Quản lý tài khoản (mục 2.11): chỉ quản trị viên dùng (kiểm ở controller).</summary>
public class NguoiDungService(AppDbContext db, IPasswordHasher<NguoiDungEntity> hasher, MaChungTu maChungTu, GhiNhatKy nhatKy)
{
    public async Task<TrangDuLieu<NguoiDungDong>> DanhSachAsync(string? tuKhoa, VaiTro? vaiTro, TrangThaiTaiKhoan? trangThai, int trang)
    {
        var q = db.NguoiDung.AsQueryable();
        if (!string.IsNullOrWhiteSpace(tuKhoa))
        {
            var tk = $"%{tuKhoa.Trim()}%";
            q = q.Where(n => EF.Functions.ILike(n.Ten, tk) || EF.Functions.ILike(n.Email, tk) || EF.Functions.ILike(n.MaNguoiDung, tk));
        }
        if (vaiTro is not null) q = q.Where(n => n.VaiTro == vaiTro);
        if (trangThai is not null) q = q.Where(n => n.TrangThai == trangThai);

        return await TrangDuLieu<NguoiDungDong>.TaoAsync(q.OrderBy(n => n.MaNguoiDung).Select(n => new NguoiDungDong(
            n.MaNguoiDung, n.Ten, n.Email, n.SoDienThoai, n.VaiTro, n.TrangThai, n.KhoaDenLuc,
            db.NguoiDungKho.Where(k => k.MaNguoiDung == n.MaNguoiDung).OrderBy(k => k.MaKho).Select(k => k.MaKho).ToList())), trang);
    }

    public Task<List<LuaChon>> DanhSachKhoAsync() =>
        db.ViTri.Where(v => v.Cap == 1).OrderBy(v => v.MaViTri).Select(v => new LuaChon(v.MaViTri, v.Ten)).ToListAsync();

    public async Task<NguoiDungForm> LayAsync(string ma)
    {
        var nd = await Tim(ma);
        return new NguoiDungForm
        {
            Ma = nd.MaNguoiDung, Ten = nd.Ten, Email = nd.Email, SoDienThoai = nd.SoDienThoai, VaiTro = nd.VaiTro, TrangThai = nd.TrangThai,
            Kho = await db.NguoiDungKho.Where(k => k.MaNguoiDung == ma).Select(k => k.MaKho).ToListAsync(),
        };
    }

    public async Task<string> ThemAsync(NguoiDungForm form)
    {
        KiemTraMatKhau(form.MatKhau);
        var kho = await ChuanHoaKho(form);
        var nd = new NguoiDungEntity
        {
            MaNguoiDung = await maChungTu.TaoAsync("ND"),
            Ten = form.Ten.Trim(),
            Email = await EmailHopLe(form.Email, null),
            SoDienThoai = form.SoDienThoai?.Trim(),
            VaiTro = form.VaiTro,
            TrangThai = TrangThaiTaiKhoan.DangHoatDong,
            SecurityStamp = TemMoi(),
        };
        nd.MatKhauBam = hasher.HashPassword(nd, form.MatKhau!);
        db.NguoiDung.Add(nd);
        db.NguoiDungKho.AddRange(kho.Select(k => new NguoiDungKho { MaNguoiDung = nd.MaNguoiDung, MaKho = k }));
        nhatKy.Ghi("ThemNguoiDung", "NguoiDung", nd.MaNguoiDung, new { vaiTro = nd.VaiTro.ToString(), kho });
        await db.SaveChangesAsync();
        return nd.MaNguoiDung;
    }

    public async Task SuaAsync(string ma, NguoiDungForm form)
    {
        var nd = await Tim(ma);
        var kho = await ChuanHoaKho(form);
        var khoCu = await db.NguoiDungKho.Where(k => k.MaNguoiDung == ma).ToListAsync();
        var doiQuyen = nd.VaiTro != form.VaiTro || !khoCu.Select(k => k.MaKho).Order().SequenceEqual(kho.Order());

        if (nd.VaiTro == VaiTro.QuanTriVien && form.VaiTro != VaiTro.QuanTriVien)
            await DamBaoConQuanTriKhac(ma);

        nd.Ten = form.Ten.Trim();
        nd.Email = await EmailHopLe(form.Email, ma);
        nd.SoDienThoai = form.SoDienThoai?.Trim();
        if (doiQuyen)
        {
            nhatKy.Ghi("DoiQuyen", "NguoiDung", ma, new
            {
                vaiTroCu = nd.VaiTro.ToString(), vaiTroMoi = form.VaiTro.ToString(), khoCu = khoCu.Select(k => k.MaKho), khoMoi = kho
            });
            nd.VaiTro = form.VaiTro;
            nd.SecurityStamp = TemMoi();
            db.NguoiDungKho.RemoveRange(khoCu);
            db.NguoiDungKho.AddRange(kho.Select(k => new NguoiDungKho { MaNguoiDung = ma, MaKho = k }));
        }
        await db.SaveChangesAsync();
    }

    public async Task KhoaAsync(string ma)
    {
        var nd = await Tim(ma);
        if (nd.TrangThai == TrangThaiTaiKhoan.DaKhoa) return;
        if (nd.VaiTro == VaiTro.QuanTriVien)
            await DamBaoConQuanTriKhac(ma);
        nd.TrangThai = TrangThaiTaiKhoan.DaKhoa;
        nd.SecurityStamp = TemMoi();
        nhatKy.Ghi("KhoaTaiKhoan", "NguoiDung", ma);
        await db.SaveChangesAsync();
    }

    /// <summary>Mở khóa tài khoản đã khóa, đồng thời gỡ khóa tạm do đăng nhập sai.</summary>
    public async Task MoKhoaAsync(string ma)
    {
        var nd = await Tim(ma);
        nd.TrangThai = TrangThaiTaiKhoan.DangHoatDong;
        nd.SoLanDangNhapSai = 0;
        nd.KhoaDenLuc = null;
        nhatKy.Ghi("MoKhoaTaiKhoan", "NguoiDung", ma);
        await db.SaveChangesAsync();
    }

    public async Task DatLaiMatKhauAsync(string ma, string? matKhauMoi)
    {
        KiemTraMatKhau(matKhauMoi);
        var nd = await Tim(ma);
        nd.MatKhauBam = hasher.HashPassword(nd, matKhauMoi!);
        nd.SecurityStamp = TemMoi();
        nhatKy.Ghi("DatLaiMatKhau", "NguoiDung", ma);
        await db.SaveChangesAsync();
    }

    // ---------- Luật ----------

    private async Task<NguoiDungEntity> Tim(string ma) =>
        await db.NguoiDung.SingleOrDefaultAsync(n => n.MaNguoiDung == ma) ?? throw new NghiepVuException($"Không tìm thấy người dùng {ma}.");

    private async Task<string> EmailHopLe(string email, string? maBoQua)
    {
        email = email.Trim().ToLowerInvariant();
        if (await db.NguoiDung.AnyAsync(n => n.Email == email && n.MaNguoiDung != maBoQua))
            throw new NghiepVuException($"Email {email} đã được dùng cho tài khoản khác.");
        return email;
    }

    /// <summary>Quản lý kho, thủ kho phải có ít nhất một kho; vai khác thấy mọi kho nên không lưu kho được giao.</summary>
    private async Task<List<string>> ChuanHoaKho(NguoiDungForm form)
    {
        if (form.VaiTro is not (VaiTro.QuanLyKho or VaiTro.ThuKho))
            return [];
        var kho = form.Kho.Where(k => !string.IsNullOrWhiteSpace(k)).Distinct().ToList();
        if (kho.Count == 0)
            throw new NghiepVuException("Quản lý kho và thủ kho phải được giao ít nhất một kho.");
        var coThat = await db.ViTri.CountAsync(v => v.Cap == 1 && kho.Contains(v.MaViTri));
        if (coThat != kho.Count)
            throw new NghiepVuException("Có kho được chọn không tồn tại.");
        return kho;
    }

    private async Task DamBaoConQuanTriKhac(string ma)
    {
        if (!await db.NguoiDung.AnyAsync(n => n.MaNguoiDung != ma && n.VaiTro == VaiTro.QuanTriVien && n.TrangThai == TrangThaiTaiKhoan.DangHoatDong))
            throw new NghiepVuException("Phải luôn còn ít nhất một quản trị viên đang hoạt động.");
    }

    private static void KiemTraMatKhau(string? matKhau)
    {
        if (matKhau is null || matKhau.Length < 8 || !matKhau.Any(char.IsLetter) || !matKhau.Any(char.IsDigit))
            throw new NghiepVuException("Mật khẩu phải có ít nhất 8 ký tự, gồm cả chữ và số.");
    }

    private static string TemMoi() => Guid.NewGuid().ToString("N");
}
