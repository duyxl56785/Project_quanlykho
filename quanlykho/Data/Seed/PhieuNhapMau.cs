using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using quanlykho.Domain;
using quanlykho.Features.Kho;
using quanlykho.Features.NhapKho;
using quanlykho.Features.XepHang;

namespace quanlykho.Data.Seed;

/// <summary>
/// Mỗi kho một hóa đơn nhập mẫu đã xác nhận và đã xếp vào ô lưu trữ, ghi tồn qua đúng luồng nghiệp vụ (cổng ghi tồn).
/// Chạy sau DuLieuThu khi --seed, với quyền quản trị viên ND001; kho đã có phiếu mẫu thì bỏ qua.
/// </summary>
public class PhieuNhapMau(AppDbContext db, IHttpContextAccessor http, PhieuNhapService phieuNhap, XepHangService xepHang, OPhuHop oPhuHop, TimeProvider gio)
{
    public const string GhiChu = "Hóa đơn nhập mẫu (dữ liệu thử)";

    public async Task NapAsync()
    {
        http.HttpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "ND001"), new Claim(ClaimTypes.Role, nameof(VaiTro.QuanTriVien))], "seed")),
        };
        var hanDung = DateOnly.FromDateTime(gio.GetUtcNow().UtcDateTime).AddDays(90);
        await NapKhoAsync("K01", [("AOTHUN-01", 30, null), ("SUATUOI-01", 24, hanDung)]);
        await NapKhoAsync("K02", [("MIGOI-01", 100, hanDung), ("GIAYA4-01", 20, null)]);
    }

    private async Task NapKhoAsync(string maKho, (string BienThe, decimal SoLuong, DateOnly? HanDung)[] dong)
    {
        if (await db.PhieuNhap.AnyAsync(p => p.MaKho == maKho && p.GhiChu == GhiChu))
            return;
        var ma = await phieuNhap.TaoAsync(new PhieuNhapForm { LoaiPhieu = LoaiPhieuNhap.NhapKhac, MaKho = maKho, GhiChu = GhiChu });
        var lo = new List<(string MaLo, decimal SoLuong)>();
        foreach (var d in dong)
            lo.Add(((await phieuNhap.ThemDongAsync(ma, new DongPhieuNhapForm(d.BienThe, d.SoLuong, d.HanDung, ChatLuong.Dat))).MaLo, d.SoLuong));
        await phieuNhap.XacNhanAsync(ma, $"seed-{ma}");

        var tiepNhan = await db.ViTri.Where(v => v.MaKho == maKho && v.LaOMacDinh && v.LoaiKhu == LoaiKhu.TiepNhan).Select(v => v.MaViTri).SingleAsync();
        foreach (var (maLo, soLuong) in lo)
        {
            var o = (await oPhuHop.TimAsync(maLo, soLuong, maKho)).First(x => x.DuSoLuong);
            await xepHang.XepAsync(new XepHangYeuCau(maLo, tiepNhan, o.MaViTri, soLuong), $"seed-{maLo}");
        }
    }
}
