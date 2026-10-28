using Microsoft.EntityFrameworkCore;
using Npgsql;
using quanlykho.Common;
using quanlykho.Common.Loi;
using quanlykho.Common.PhanQuyen;
using quanlykho.Data;
using quanlykho.Domain;
using quanlykho.Features.Kho;
using quanlykho.Features.NhapKho;
using static quanlykho.Common.GiaoDien.DinhDang;

namespace quanlykho.Features.XepHang;

public record XepHangYeuCau(string MaLo, string MaViTriDi, string MaViTri, decimal SoLuong);

public record KetQuaXep(string MaChuyen, bool DaGhiTruocDo);

/// <summary>Hàng đang chờ xếp: một lô tại một ô khu tiếp nhận.</summary>
public record HangChoXep(string MaLo, string MaBienThe, string? MaVach, string TenHang, string DonViTinh, DateOnly? HanDung, string MaViTri, decimal SoLuong);

/// <summary>Xếp hàng từ khu tiếp nhận vào ô lưu trữ (Hình 2.10, Bảng 2.33); cùng luồng chuyển hàng giữa hai ô lưu trữ trong một kho (FR16).</summary>
public class XepHangService(AppDbContext db, NguoiDungHienTai nguoiDung, MaChungTu maChungTu, CongGhiTon congGhiTon, TimeProvider gio)
{
    public const string LoaiChungTu = "XepHang";
    public const string LoaiChuyen = "ChuyenTrongKho";

    public async Task<List<HangChoXep>> ChoXepAsync(string maKho)
    {
        await nguoiDung.DamBaoQuyenKhoAsync(maKho);
        return (await db.TonKho.Where(t => t.SoLuong > t.SoLuongDaGiu)
                .Join(db.ViTri.Where(v => v.MaKho == maKho && v.LoaiKhu == LoaiKhu.TiepNhan), t => t.MaViTri, v => v.MaViTri, (t, v) => t)
                .Join(db.Lo, t => t.MaLo, l => l.MaLo, (t, l) => new
                {
                    t.MaLo, l.MaBienThe, l.BienThe.MaVach, l.BienThe.SanPham.Ten, l.BienThe.ThuocTinh, l.BienThe.SanPham.DonViTinh, l.HanDung,
                    t.MaViTri, ConLai = t.SoLuong - t.SoLuongDaGiu,
                })
                .OrderBy(x => x.MaLo).ThenBy(x => x.MaViTri).ToListAsync())
            .Select(x => new HangChoXep(x.MaLo, x.MaBienThe, x.MaVach, DonMuaService.MoTaHang(x.Ten, x.ThuocTinh), x.DonViTinh, x.HanDung, x.MaViTri, x.ConLai))
            .ToList();
    }

    /// <summary>Hàng chuyển được ở một ô lưu trữ: phần chưa giữ của từng lô; ô không phải ô lưu trữ của kho thì trả rỗng.</summary>
    public async Task<List<HangChoXep>> HangTaiOAsync(string maKho, string maViTri)
    {
        await nguoiDung.DamBaoQuyenKhoAsync(maKho);
        return (await db.TonKho.Where(t => t.MaViTri == maViTri && t.SoLuong > t.SoLuongDaGiu)
                .Join(db.ViTri.Where(v => v.MaKho == maKho && v.LoaiKhu == LoaiKhu.LuuTru), t => t.MaViTri, v => v.MaViTri, (t, v) => t)
                .Join(db.Lo, t => t.MaLo, l => l.MaLo, (t, l) => new
                {
                    t.MaLo, l.MaBienThe, l.BienThe.MaVach, l.BienThe.SanPham.Ten, l.BienThe.ThuocTinh, l.BienThe.SanPham.DonViTinh, l.HanDung,
                    t.MaViTri, ConLai = t.SoLuong - t.SoLuongDaGiu,
                })
                .OrderBy(x => x.MaLo).ToListAsync())
            .Select(x => new HangChoXep(x.MaLo, x.MaBienThe, x.MaVach, DonMuaService.MoTaHang(x.Ten, x.ThuocTinh), x.DonViTinh, x.HanDung, x.MaViTri, x.ConLai))
            .ToList();
    }

    public async Task<KetQuaXep> XepAsync(XepHangYeuCau yc, string? khoaChongLap)
    {
        if (string.IsNullOrWhiteSpace(khoaChongLap) || khoaChongLap.Length > 56)
            throw new NghiepVuException("Thiếu khóa chống ghi lặp (Idempotency-Key, tối đa 56 ký tự).");
        if (yc.SoLuong <= 0)
            throw new NghiepVuException("Số lượng xếp phải lớn hơn 0.");
        if (yc.MaViTri == yc.MaViTriDi)
            throw new NghiepVuException("Ô đích trùng ô đang chứa hàng.");

        var di = await db.ViTri.AsNoTracking().SingleOrDefaultAsync(v => v.MaViTri == yc.MaViTriDi) ?? throw new NghiepVuException($"Không có vị trí {yc.MaViTriDi}.");
        await nguoiDung.DamBaoQuyenKhoAsync(di.MaKho);
        if (di.LoaiKhu is not (LoaiKhu.TiepNhan or LoaiKhu.LuuTru) || di.Cap != CapViTri.CapCuoi)
            throw new NghiepVuException($"Chỉ xếp hàng ở ô khu tiếp nhận hoặc chuyển hàng ở ô khu lưu trữ; {di.MaViTri} không phải.");
        var (loaiChuyen, loaiChungTu) = di.LoaiKhu == LoaiKhu.TiepNhan ? (LoaiChuyenViTri.XepHang, LoaiChungTu) : (LoaiChuyenViTri.ChuyenTrongKho, LoaiChuyen);

        await using var tx = await db.Database.BeginTransactionAsync();
        try
        {
            await db.Database.ExecuteSqlAsync($"""SELECT 1 FROM "ViTri" WHERE "MaViTri" = {yc.MaViTri} FOR UPDATE NOWAIT""");
        }
        catch (PostgresException e) when (e.SqlState == PostgresErrorCodes.LockNotAvailable)
        {
            throw new XungDotException("Ô vừa được cập nhật, vui lòng thử lại.");
        }

        var daGhi = await db.SoBienDong.Where(s => s.MaKhoaChongLap == khoaChongLap + "#1").Select(s => new { s.LoaiChungTu, s.MaChungTu }).SingleOrDefaultAsync();
        if (daGhi is not null)
            return daGhi.LoaiChungTu == loaiChungTu ? new KetQuaXep(daGhi.MaChungTu, true)
                : throw new NghiepVuException("Khóa chống ghi lặp đã dùng cho thao tác khác.");

        var den = await db.ViTri.AsNoTracking().SingleOrDefaultAsync(v => v.MaViTri == yc.MaViTri) ?? throw new NghiepVuException($"Không có vị trí {yc.MaViTri}.");
        var lo = await db.Lo.Where(l => l.MaLo == yc.MaLo)
            .Select(l => new { l.MaBienThe, l.BienThe.DaiDongGoi, l.BienThe.RongDongGoi, l.BienThe.CaoDongGoi, l.BienThe.KhoiLuong, l.BienThe.SanPham.DieuKienBaoQuan, l.BienThe.SanPham.DonViTinh })
            .SingleOrDefaultAsync() ?? throw new NghiepVuException($"Không có lô {yc.MaLo}.");

        if (den.MaKho != di.MaKho)
            throw new NghiepVuException($"Ô {den.MaViTri} không cùng kho với lô đang xếp.");
        if (den.Cap != CapViTri.CapCuoi)
            throw new NghiepVuException($"{den.MaViTri} không phải ô; hàng chỉ xếp vào ô (cấp 7).");
        if (den.LoaiKhu != LoaiKhu.LuuTru)
            throw new NghiepVuException($"Ô {den.MaViTri} không thuộc khu lưu trữ.");
        if (den.DieuKienBaoQuan != lo.DieuKienBaoQuan)
            throw new NghiepVuException($"Ô {den.MaViTri} không đúng điều kiện bảo quản của hàng ({CapViTri.TenBaoQuan(lo.DieuKienBaoQuan)}).");
        if (den.DangKhoa)
            throw new NghiepVuException($"Ô {den.MaViTri} đang khóa.");
        if (den.MaPhieuKiemKe is not null)
            throw new NghiepVuException($"Ô {den.MaViTri} đang khóa để kiểm kê ({den.MaPhieuKiemKe}).");

        var heSo = await db.ViTri.Where(v => v.MaViTri == den.MaKho).Select(v => v.HeSoLapDay).SingleAsync() ?? OPhuHop.HeSoLapDayMacDinh;
        var dangCo = (await db.TonKho.Where(t => t.MaViTri == den.MaViTri && t.SoLuong > 0)
                .Join(db.Lo, t => t.MaLo, l => l.MaLo, (t, l) => new { t.SoLuong, l.MaBienThe, l.BienThe.DaiDongGoi, l.BienThe.RongDongGoi, l.BienThe.CaoDongGoi, l.BienThe.KhoiLuong })
                .ToListAsync())
            .Select(x => new HangTrongO(x.MaBienThe, new DonViHang(x.DaiDongGoi, x.RongDongGoi, x.CaoDongGoi, x.KhoiLuong), x.SoLuong)).ToList();
        var kq = SucChua.Tinh(new OChua(den.Dai!.Value, den.Rong!.Value, den.Cao!.Value, den.TaiTrongToiDa!.Value), heSo, lo.MaBienThe,
            new DonViHang(lo.DaiDongGoi, lo.RongDongGoi, lo.CaoDongGoi, lo.KhoiLuong), dangCo);
        if (!kq.VuaKichThuoc)
            throw new NghiepVuException($"Hàng không vừa kích thước ô {den.MaViTri}, chọn ô khác.", new { qMax = 0m });
        if (yc.SoLuong > kq.QMax)
            throw new NghiepVuException($"Ô {den.MaViTri} chỉ còn xếp được tối đa {So(kq.QMax)} {lo.DonViTinh}.", new { qMax = kq.QMax });

        var ma = await maChungTu.TaoAsync("CV");
        db.ChuyenViTri.Add(new ChuyenViTri
        {
            MaChuyen = ma, Loai = loaiChuyen, MaKho = den.MaKho, MaLo = yc.MaLo, MaViTriDi = di.MaViTri, MaViTriDen = den.MaViTri,
            SoLuong = yc.SoLuong, ThoiDiem = gio.GetUtcNow(), NguoiThucHien = nguoiDung.Ma,
        });
        // Cổng chỉ cho trừ phần chưa giữ ở ô đi nên hàng đang giữ cho đơn không chuyển được.
        if (await congGhiTon.GhiAsync(khoaChongLap, loaiChungTu, ma, [new(yc.MaLo, di.MaViTri, -yc.SoLuong), new(yc.MaLo, den.MaViTri, yc.SoLuong)]) == KetQuaGhi.DaGhiTruocDo)
            throw new XungDotException("Thao tác vừa được ghi bởi một yêu cầu khác, vui lòng tải lại trang.");
        await tx.CommitAsync();
        return new KetQuaXep(ma, false);
    }
}
