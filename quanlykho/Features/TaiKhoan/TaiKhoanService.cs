using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using quanlykho.Common;
using quanlykho.Common.PhanQuyen;
using quanlykho.Data;
using quanlykho.Domain;

namespace quanlykho.Features.TaiKhoan;

/// <summary>Đăng nhập, chống dò mật khẩu: sai liên tiếp quá số lần cho phép thì khóa tạm (KE_HOACH, nhiệm vụ đăng nhập).</summary>
public class TaiKhoanService(AppDbContext db, IPasswordHasher<NguoiDung> hasher, TimeProvider gio, IConfiguration config, GhiNhatKy nhatKy)
{
    /// <summary>Một câu cho mọi trường hợp sai (không có email, sai mật khẩu, bị khóa) để không lộ email nào tồn tại.</summary>
    public const string LoiDangNhap = "Email hoặc mật khẩu không đúng, hoặc tài khoản đang bị khóa.";

    private int SoLanSaiToiDa => config.GetValue("DangNhap:SoLanSaiToiDa", 5);
    private int PhutKhoaTam => config.GetValue("DangNhap:PhutKhoaTam", 15);

    /// <summary>Trả principal khi đúng; null khi sai (kèm LoiDangNhap).</summary>
    public async Task<ClaimsPrincipal?> DangNhapAsync(string email, string matKhau)
    {
        email = email.Trim().ToLowerInvariant();
        var nd = await db.NguoiDung.SingleOrDefaultAsync(n => n.Email == email);
        if (nd is null || nd.TrangThai != TrangThaiTaiKhoan.DangHoatDong)
            return null;

        var bayGio = gio.GetUtcNow();
        if (nd.KhoaDenLuc > bayGio)
            return null;

        var ketQua = hasher.VerifyHashedPassword(nd, nd.MatKhauBam, matKhau);
        if (ketQua == PasswordVerificationResult.Failed)
        {
            nd.SoLanDangNhapSai++;
            if (nd.SoLanDangNhapSai >= SoLanSaiToiDa)
            {
                nd.SoLanDangNhapSai = 0;
                nd.KhoaDenLuc = bayGio.AddMinutes(PhutKhoaTam);
                nhatKy.Ghi("KhoaTamDangNhap", nameof(NguoiDung), nd.MaNguoiDung, new { soPhut = PhutKhoaTam }, maNguoiDung: nd.MaNguoiDung);
            }
            await db.SaveChangesAsync();
            return null;
        }

        if (ketQua == PasswordVerificationResult.SuccessRehashNeeded)
            nd.MatKhauBam = hasher.HashPassword(nd, matKhau);
        nd.SoLanDangNhapSai = 0;
        nd.KhoaDenLuc = null;
        await db.SaveChangesAsync();
        return CauHinhPhanQuyen.TaoPrincipal(nd);
    }
}
