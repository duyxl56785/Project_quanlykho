using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using quanlykho.Common.GiaoDien;
using quanlykho.Domain;
using quanlykho.Features.DieuChuyen;
using quanlykho.Features.Kho;
using quanlykho.Features.NhapKho;
using quanlykho.Features.XepHang;
using quanlykho.Features.XuatKho;

namespace quanlykho.Data.Seed;

/// <summary>
/// Dữ liệu cho buổi demo (docs/HUONG_DAN_DEMO.md), đi qua service như người dùng thao tác: đơn vị thùng có mã vạch; ở K01 lô sữa sắp hết hạn,
/// lô sữa đã hết hạn, hàng lỗi ở khu cách ly, hàng còn chờ xếp ở khu tiếp nhận, điện thoại dưới tồn tối thiểu; đơn mua chờ nhận hàng;
/// đơn đặt hàng chờ duyệt ở hai kho; phiếu chuyển kho K01 sang K02 chờ nhập kho. Chạy sau LichSuXuat khi --seed; đã nạp thì bỏ qua.
/// </summary>
public class DuLieuDemo(AppDbContext db, IServiceScopeFactory scopes, IHttpContextAccessor http, TimeProvider gio)
{
    public const string GhiChu = "Dữ liệu demo";

    private static readonly DonViQuyDoi[] DonVi =
    [
        new() { MaBienThe = "SUATUOI-01", TenDonVi = "thùng", HeSo = 48, MaVach = "8930099000011" },
        new() { MaBienThe = "NUOCSUOI-01", TenDonVi = "thùng", HeSo = 24, MaVach = "8930099000028" },
        new() { MaBienThe = "MIGOI-01", TenDonVi = "thùng", HeSo = 30, MaVach = "8930099000035" },
    ];

    public async Task NapAsync()
    {
        if (await db.PhieuNhap.AnyAsync(p => p.GhiChu == GhiChu))
            return;
        var daCo = await db.DonViQuyDoi.Select(d => d.MaBienThe + "|" + d.TenDonVi).ToListAsync();
        db.DonViQuyDoi.AddRange(DonVi.Where(d => !daCo.Contains(d.MaBienThe + "|" + d.TenDonVi)));
        await db.SaveChangesAsync();

        var homNay = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(gio.GetUtcNow(), DinhDang.GioVietNam).DateTime);
        await VoiAsync("ND002", VaiTro.QuanLyKho, async sp =>
        {
            var phieuNhap = sp.GetRequiredService<PhieuNhapService>();
            var ma = await phieuNhap.TaoAsync(new PhieuNhapForm { LoaiPhieu = LoaiPhieuNhap.NhapKhac, MaKho = "K01", GhiChu = GhiChu });
            async Task<string> Dong(string hang, decimal soLuong, DateOnly? hanDung, ChatLuong chatLuong = ChatLuong.Dat, string? donVi = null) =>
                (await phieuNhap.ThemDongAsync(ma, new DongPhieuNhapForm(hang, soLuong, hanDung, chatLuong, TenDonVi: donVi))).MaLo;
            var xep = new List<(string MaLo, decimal SoLuong)>
            {
                (await Dong("SUATUOI-01", 2, homNay.AddDays(10), donVi: "thùng"), 96),
                (await Dong("SUATUOI-01", 12, homNay.AddDays(-3)), 12),
                (await Dong("NUOCSUOI-01", 5, homNay.AddDays(200), donVi: "thùng"), 120),
                (await Dong("DTA-01", 3, null), 3),
            };
            await Dong("AOTHUN-03", 8, null);
            await Dong("AOTHUN-02", 10, null, ChatLuong.Loi);
            await phieuNhap.XacNhanAsync(ma, $"demo-{ma}");

            var oPhuHop = sp.GetRequiredService<OPhuHop>();
            var xepHang = sp.GetRequiredService<XepHangService>();
            var tiepNhan = await db.ViTri.Where(v => v.MaKho == "K01" && v.LaOMacDinh && v.LoaiKhu == LoaiKhu.TiepNhan).Select(v => v.MaViTri).SingleAsync();
            foreach (var (maLo, soLuong) in xep)
            {
                var o = (await oPhuHop.TimAsync(maLo, soLuong, "K01")).First(x => x.DuSoLuong);
                await xepHang.XepAsync(new XepHangYeuCau(maLo, tiepNhan, o.MaViTri, soLuong), $"demo-{maLo}");
            }

            await sp.GetRequiredService<DonMuaService>().TaoAsync(new DonMuaForm
            {
                MaNhaCungCap = "NCC03", MaKho = "K01", GhiChu = GhiChu,
                Dong = [new() { MaBienThe = "NUOCSUOI-01", TenDonVi = "thùng", SoLuong = 4, DonGia = 96000 }, new() { MaBienThe = "BUTBI-01", SoLuong = 200, DonGia = 3000 }],
            });

            var giay = await (from t in db.TonKho
                              join v in db.ViTri on t.MaViTri equals v.MaViTri
                              join l in db.Lo on t.MaLo equals l.MaLo
                              where v.MaKho == "K01" && v.LoaiKhu == LoaiKhu.LuuTru && l.MaBienThe == "GIAYA4-01" && t.SoLuong - t.SoLuongDaGiu >= 2
                              select t.MaLo + "|" + t.MaViTri).FirstAsync();
            await sp.GetRequiredService<DieuChuyenService>().TaoAsync(new DieuChuyenForm
            {
                MaKhoDi = "K01", MaKhoDen = "K02", PhuongTien = "Xe tải 29C-123.45", NguoiVanChuyen = "Nguyễn Văn Tài", LyDo = GhiChu,
                KhoaChongLap = "demo-dieu-chuyen", Dong = [new() { LoO = giay, SoLuong = 2 }],
            }, luu: true);
        });

        await VoiAsync("ND007", VaiTro.NhanVienBanHang, async sp =>
        {
            var donDat = sp.GetRequiredService<DonDatService>();
            await donDat.TaoAsync(new DonDatForm
            {
                MaKho = "K01", MaNoiNhan = "NN01", HanGiao = homNay.AddDays(2), GhiChu = GhiChu,
                Dong = [new() { MaBienThe = "SUATUOI-01", TenDonVi = "thùng", SoLuong = 1 }, new() { MaBienThe = "AOTHUN-01", SoLuong = 5 }],
            }, guiDuyet: true);
            await donDat.TaoAsync(new DonDatForm
            {
                MaKho = "K02", MaNoiNhan = "NN02", HanGiao = homNay.AddDays(3), GhiChu = GhiChu,
                Dong = [new() { MaBienThe = "MIGOI-01", TenDonVi = "thùng", SoLuong = 1 }],
            }, guiDuyet: true);
        });
    }

    /// <summary>Mỗi người một scope để NguoiDungHienTai không giữ kho của người trước.</summary>
    private async Task VoiAsync(string ma, VaiTro vai, Func<IServiceProvider, Task> lam)
    {
        http.HttpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, ma), new Claim(ClaimTypes.Role, vai.ToString())], "seed")),
        };
        await using var scope = scopes.CreateAsyncScope();
        await lam(scope.ServiceProvider);
    }
}
