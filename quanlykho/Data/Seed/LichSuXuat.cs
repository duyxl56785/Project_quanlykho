using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using quanlykho.Common;
using quanlykho.Common.GiaoDien;
using quanlykho.Common.PhanQuyen;
using quanlykho.Domain;
using quanlykho.Features.NhapKho;
using quanlykho.Features.XuatKho;

namespace quanlykho.Data.Seed;

/// <summary>
/// 3 tháng lịch sử xuất bán cho G3 và báo cáo: mỗi kho một phiếu tồn đầu kỳ cách đây 95 ngày, rồi 90 ngày phiếu xuất bán Hoàn tất,
/// tất cả ghi qua cổng ghi tồn với thời điểm lùi về quá khứ. Số lượng chọn để cuối kỳ sữa, giấy (K01) và mì (K02) dưới điểm đặt hàng.
/// Chạy sau PhieuNhapMau khi --seed; kho đã có phiếu tồn đầu kỳ này thì bỏ qua.
/// </summary>
public class LichSuXuat(AppDbContext db, IHttpContextAccessor http, NguoiDungHienTai nguoiDung, MaChungTu maChungTu, TimeProvider gio)
{
    public const string GhiChu = "Tồn đầu kỳ cho lịch sử xuất (dữ liệu thử)";
    public const int SoNgay = 90;

    private record HangXuat(string MaBienThe, string MaViTri, decimal TonDau, decimal XuatMoiNgay, bool CoHan);

    /// <summary>Giờ ghi sổ thay đổi theo từng ngày được nạp.</summary>
    private sealed class GioNap(DateTimeOffset bayGio) : TimeProvider
    {
        public DateTimeOffset BayGio { get; set; } = bayGio;
        public override DateTimeOffset GetUtcNow() => BayGio;
    }

    public async Task NapAsync()
    {
        http.HttpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "ND001"), new Claim(ClaimTypes.Role, nameof(VaiTro.QuanTriVien))], "seed")),
        };
        await NapKhoAsync("K01", "ND002", "ND004",
        [
            new("SUATUOI-01", "K01-TA01-TG01-P03-H01-KE01-O04", 800, 8.5m, true),
            new("AOTHUN-01", "K01-TA01-TG01-P02-H01-KE02-O06", 600, 5, false),
            new("GIAYA4-01", "K01-TA01-TG01-P02-H01-KE02-O05", 120, 1.2m, false),
        ]);
        await NapKhoAsync("K02", "ND003", "ND006",
        [
            new("MIGOI-01", "K02-TA01-TG01-P02-H01-KE02-O06", 1000, 10, true),
            new("GIAYA4-01", "K02-TA01-TG01-P02-H01-KE02-O05", 100, 0.6m, false),
        ]);
    }

    private async Task NapKhoAsync(string maKho, string quanLy, string thuKho, HangXuat[] hang)
    {
        if (await db.PhieuNhap.AnyAsync(p => p.MaKho == maKho && p.GhiChu == GhiChu))
            return;
        var homNay = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(gio.GetUtcNow(), DinhDang.GioVietNam).DateTime);
        var gioNap = new GioNap(LucGio(homNay.AddDays(-SoNgay - 5), 8));
        var cong = new CongGhiTon(db, nguoiDung, gioNap);
        // Hạt giống cố định theo mã kho: string.GetHashCode đổi theo từng lần chạy, seed sẽ ra số khác nhau mỗi máy.
        var rnd = new Random(maKho.Sum(c => c));

        var maHang = hang.Select(h => h.MaBienThe).ToList();
        var dvt = await db.BienThe.Where(b => maHang.Contains(b.MaBienThe)).Select(b => new { b.MaBienThe, b.SanPham.DonViTinh }).ToDictionaryAsync(b => b.MaBienThe, b => b.DonViTinh);
        await using var tx = await db.Database.BeginTransactionAsync();
        var lo = new Dictionary<string, string>();
        foreach (var h in hang)
        {
            lo[h.MaBienThe] = await maChungTu.TaoAsync("L");
            db.Lo.Add(new Lo { MaLo = lo[h.MaBienThe], MaBienThe = h.MaBienThe, HanDung = h.CoHan ? homNay.AddDays(180) : null, NgayNhap = gioNap.BayGio });
        }
        var maNhap = await maChungTu.TaoAsync("PN");
        db.PhieuNhap.Add(new PhieuNhap
        {
            MaPhieuNhap = maNhap, LoaiPhieu = LoaiPhieuNhap.TonDauKy, MaKho = maKho, NguoiLap = quanLy, NguoiXacNhan = quanLy, NgayNhap = gioNap.BayGio,
            TrangThai = TrangThaiPhieu.HoanTat, GhiChu = GhiChu,
            ChiTiet = hang.Select((h, i) => new ChiTietPhieuNhap
            {
                STT = i + 1, MaLo = lo[h.MaBienThe], TenDonVi = dvt[h.MaBienThe], HeSo = 1, SoLuongGoc = h.TonDau, SoLuongThucNhan = h.TonDau, ChatLuong = ChatLuong.Dat, MaViTri = h.MaViTri,
            }).ToList(),
        });

        var maDon = await maChungTu.TaoAsync("DD");
        var maLenh = await maChungTu.TaoAsync("LX");
        db.DonDatHang.Add(new DonDatHang { MaDonDat = maDon, MaKho = maKho, MaNoiNhan = "NN01", NguoiTao = "ND007", NgayTao = gioNap.BayGio, TrangThai = TrangThaiDonDat.HoanThanh, GhiChu = "Lịch sử xuất (dữ liệu thử)" });
        db.LenhXuat.Add(new LenhXuat { MaLenh = maLenh, MaDonDat = maDon, MaKho = maKho, NguoiLap = quanLy, NgayLap = gioNap.BayGio, NguoiThucHien = thuKho, TrangThai = TrangThaiLenhXuat.HoanThanh });
        await db.SaveChangesAsync();
        await cong.GhiAsync($"seed-ls-{maNhap}", PhieuNhapService.LoaiChungTu, maNhap, hang.Select(h => new DongGhiTon(lo[h.MaBienThe], h.MaViTri, h.TonDau)).ToList());

        var con = hang.ToDictionary(h => h.MaBienThe, h => h.TonDau);
        for (var ngay = SoNgay; ngay >= 1; ngay--)
        {
            gioNap.BayGio = LucGio(homNay.AddDays(-ngay), 10);
            // Dao động ±50% quanh mức trung bình, làm tròn; không xuất quá số còn.
            var dong = hang.Select(h => (h, SoLuong: Math.Min(con[h.MaBienThe], Math.Round(h.XuatMoiNgay * (decimal)(0.5 + rnd.NextDouble())))))
                .Where(x => x.SoLuong > 0).ToList();
            if (dong.Count == 0)
                continue;
            var maXuat = await maChungTu.TaoAsync("PX");
            db.PhieuXuat.Add(new PhieuXuat
            {
                MaPhieuXuat = maXuat, LoaiPhieu = LoaiPhieuXuat.XuatBan, MaLenh = maLenh, MaKho = maKho, NguoiLap = thuKho, NguoiXacNhan = quanLy,
                NgayXuat = gioNap.BayGio, TrangThai = TrangThaiPhieu.HoanTat,
                ChiTiet = dong.Select((x, i) => new ChiTietPhieuXuat
                {
                    STT = i + 1, MaLo = lo[x.h.MaBienThe], MaViTri = x.h.MaViTri, TenDonVi = dvt[x.h.MaBienThe], HeSo = 1, SoLuongGoc = x.SoLuong, SoLuong = x.SoLuong,
                }).ToList(),
            });
            foreach (var x in dong)
                con[x.h.MaBienThe] -= x.SoLuong;
            await cong.GhiAsync($"seed-ls-{maXuat}", LenhXuatService.LoaiChungTu, maXuat, dong.Select(x => new DongGhiTon(lo[x.h.MaBienThe], x.h.MaViTri, -x.SoLuong)).ToList());
        }
        await tx.CommitAsync();
    }

    private static DateTimeOffset LucGio(DateOnly ngay, int gioTrongNgay) =>
        DinhDang.BatDauNgay(ngay).AddHours(gioTrongNgay);
}
