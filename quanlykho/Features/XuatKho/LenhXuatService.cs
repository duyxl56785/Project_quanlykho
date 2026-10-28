using Microsoft.EntityFrameworkCore;
using quanlykho.Common;
using quanlykho.Common.GiaoDien;
using quanlykho.Common.Loi;
using quanlykho.Common.PhanQuyen;
using quanlykho.Data;
using quanlykho.Domain;
using quanlykho.Features.Kho;
using quanlykho.Features.NhapKho;
using static quanlykho.Common.GiaoDien.DinhDang;

namespace quanlykho.Features.XuatKho;

/// <summary>
/// Lệnh xuất kho (mục 2.2 dòng 3 đến 7, Hình 2.6): lập từ đơn đã duyệt, thủ kho lấy hàng, quản lý kho khác người lấy kiểm tra,
/// xác nhận sinh phiếu xuất Hoàn tất, bỏ giữ và trừ tồn qua cổng ghi tồn trong một transaction.
/// </summary>
public class LenhXuatService(AppDbContext db, NguoiDungHienTai nguoiDung, MaChungTu maChungTu, CongGhiTon congGhiTon, GoiYService goiY,
    TonKhaDung tonKhaDung, GhiNhatKy nhatKy, TimeProvider gio)
{
    public const string LoaiChungTu = "PhieuXuat";

    public async Task<TrangDuLieu<LenhXuatDong>> DanhSachAsync(string? maKho, TrangThaiLenhXuat? trangThai, int trang)
    {
        var q = await PhamViAsync();
        if (!string.IsNullOrWhiteSpace(maKho)) q = q.Where(l => l.MaKho == maKho);
        if (trangThai is not null) q = q.Where(l => l.TrangThai == trangThai);
        return await TrangDuLieu<LenhXuatDong>.TaoAsync(q.OrderByDescending(l => l.NgayLap).ThenByDescending(l => l.MaLenh).Select(l => new LenhXuatDong(
            l.MaLenh, l.MaDonDat, l.MaKho, db.NguoiDung.Where(n => n.MaNguoiDung == l.NguoiThucHien).Select(n => n.Ten).First(), l.NgayLap, l.TrangThai)), trang);
    }

    public async Task<LenhXuatChiTiet> LayAsync(string ma)
    {
        var l = await (await PhamViAsync()).AsNoTracking().Include(x => x.ChiTiet).SingleOrDefaultAsync(x => x.MaLenh == ma)
            ?? throw new NghiepVuException($"Không có lệnh xuất {ma} hoặc bạn không được xem lệnh này.");
        var maHang = l.ChiTiet.Select(c => c.MaBienThe).ToList();
        var hang = await db.BienThe.Where(b => maHang.Contains(b.MaBienThe))
            .Select(b => new { b.MaBienThe, b.SanPham.Ten, b.ThuocTinh, b.SanPham.DonViTinh }).ToDictionaryAsync(b => b.MaBienThe);
        var daLay = await db.DongLayHang.Where(d => d.MaLenh == ma).OrderBy(d => d.MaDong)
            .Join(db.NguoiDung, d => d.NguoiLay, n => n.MaNguoiDung, (d, n) => new DongDaLay(d.MaDong, d.MaBienThe, d.MaLo, d.MaViTri, d.SoLuong, n.Ten, d.ThoiDiem))
            .ToListAsync();
        var goiYLay = await db.GiuHang.Where(g => g.MaDonDat == l.MaDonDat).OrderBy(g => g.MaViTri).ThenBy(g => g.MaLo)
            .Join(db.Lo, g => g.MaLo, lo => lo.MaLo, (g, lo) => new HangDaGiu(g.ThuTu, lo.MaBienThe, g.MaLo, g.MaViTri, lo.HanDung, g.SoLuong))
            .ToListAsync();
        var ten = await db.NguoiDung.Where(n => n.MaNguoiDung == l.NguoiThucHien || n.MaNguoiDung == l.NguoiKiemTra).ToDictionaryAsync(n => n.MaNguoiDung, n => n.Ten);
        var noiNhan = await db.DonDatHang.Where(d => d.MaDonDat == l.MaDonDat)
            .Join(db.NoiNhan, d => d.MaNoiNhan, n => n.MaNoiNhan, (d, n) => n.Ten).SingleAsync();
        return new LenhXuatChiTiet(l.MaLenh, l.MaDonDat, l.MaKho, await db.ViTri.Where(v => v.MaViTri == l.MaKho).Select(v => v.Ten).SingleAsync(), noiNhan,
            l.NguoiThucHien, ten[l.NguoiThucHien], l.NguoiKiemTra, l.NguoiKiemTra is null ? null : ten[l.NguoiKiemTra], l.GhiChuKiemTra, l.NgayLap, l.TrangThai,
            await db.PhieuXuat.Where(p => p.MaLenh == ma).Select(p => p.MaPhieuXuat).FirstOrDefaultAsync(),
            l.ChiTiet.OrderBy(c => c.MaBienThe).Select(c => new DongYeuCauXuat(c.MaBienThe, DonMuaService.MoTaHang(hang[c.MaBienThe].Ten, hang[c.MaBienThe].ThuocTinh),
                hang[c.MaBienThe].DonViTinh, c.SoLuongYeuCau, daLay.Where(d => d.MaBienThe == c.MaBienThe).Sum(d => d.SoLuong))).ToList(),
            goiYLay, daLay);
    }

    /// <summary>Thủ kho đang hoạt động được giao kho này, để phân công lệnh.</summary>
    public async Task<List<LuaChon>> ThuKhoAsync(string maKho)
    {
        await nguoiDung.DamBaoQuyenKhoAsync(maKho);
        return await db.NguoiDung.Where(n => n.VaiTro == VaiTro.ThuKho && n.TrangThai == TrangThaiTaiKhoan.DangHoatDong
                && db.NguoiDungKho.Any(k => k.MaNguoiDung == n.MaNguoiDung && k.MaKho == maKho))
            .OrderBy(n => n.MaNguoiDung).Select(n => new LuaChon(n.MaNguoiDung, n.Ten)).ToListAsync();
    }

    public async Task<string> TaoAsync(LapLenhYeuCau yc)
    {
        await using var tx = await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlAsync($"""SELECT 1 FROM "DonDatHang" WHERE "MaDonDat" = {yc.MaDonDat} FOR UPDATE""");
        var don = await db.DonDatHang.Include(d => d.ChiTiet).SingleOrDefaultAsync(d => d.MaDonDat == yc.MaDonDat)
            ?? throw new NghiepVuException($"Không có đơn đặt hàng {yc.MaDonDat}.");
        await nguoiDung.DamBaoQuyenKhoAsync(don.MaKho);
        if (don.TrangThai != TrangThaiDonDat.DaDuyet)
            throw new NghiepVuException("Chỉ lập lệnh xuất cho đơn đã duyệt và giữ hàng.");
        if ((await ThuKhoAsync(don.MaKho)).All(t => t.Ma != yc.NguoiThucHien))
            throw new NghiepVuException("Người thực hiện phải là thủ kho đang hoạt động được giao kho này.");

        var l = new LenhXuat
        {
            MaLenh = await maChungTu.TaoAsync("LX"), MaDonDat = don.MaDonDat, MaKho = don.MaKho, NguoiLap = nguoiDung.Ma, NgayLap = gio.GetUtcNow(),
            NguoiThucHien = yc.NguoiThucHien, TrangThai = TrangThaiLenhXuat.ChuaThucHien,
            ChiTiet = don.ChiTiet.Where(c => c.SoLuongDat * c.HeSo > c.SoLuongDaXuat).Select(c => new ChiTietLenhXuat
            {
                MaBienThe = c.MaBienThe, TenDonVi = c.TenDonVi, HeSo = 1,
                SoLuongGoc = c.SoLuongDat * c.HeSo - c.SoLuongDaXuat, SoLuongYeuCau = c.SoLuongDat * c.HeSo - c.SoLuongDaXuat,
            }).ToList(),
        };
        db.LenhXuat.Add(l);
        don.TrangThai = TrangThaiDonDat.DangXuat;
        nhatKy.Ghi("LapLenhXuat", nameof(LenhXuat), l.MaLenh, new { maDonDat = don.MaDonDat, nguoiThucHien = yc.NguoiThucHien }, maKho: l.MaKho);
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return l.MaLenh;
    }

    /// <summary>
    /// Thủ kho quét ô rồi quét hàng. Chặn sai hàng, vượt số yêu cầu, lô hết hạn, ô ngoài khu lưu trữ hay đang kiểm kê.
    /// Lấy ngoài số đã giữ cho đơn thì giữ thêm ngay (khóa dòng tồn), nên hai thủ kho không lấy trùng một số hàng.
    /// </summary>
    public async Task<long> LayHangAsync(string ma, LayHangYeuCau yc)
    {
        if (yc.SoLuong <= 0)
            throw new NghiepVuException("Số lượng lấy phải lớn hơn 0.");
        await using var tx = await db.Database.BeginTransactionAsync();
        var l = await KhoaLenhCuaToiAsync(ma);
        if (l.TrangThai is not (TrangThaiLenhXuat.ChuaThucHien or TrangThaiLenhXuat.ChoLayHang))
            throw new NghiepVuException("Lệnh không ở bước lấy hàng.");

        var o = await db.ViTri.AsNoTracking().SingleOrDefaultAsync(v => v.MaViTri == yc.MaViTri) ?? throw new NghiepVuException($"Mã {yc.MaViTri} không phải ô nào; quét mã ô trước.");
        if (o.MaKho != l.MaKho || o.Cap != CapViTri.CapCuoi)
            throw new NghiepVuException($"{o.MaViTri} không phải ô của kho {l.MaKho}.");
        if (o.LoaiKhu != LoaiKhu.LuuTru)
            throw new NghiepVuException($"Ô {o.MaViTri} không thuộc khu lưu trữ; chỉ lấy hàng ở khu lưu trữ.");
        if (o.MaPhieuKiemKe is not null)
            throw new NghiepVuException($"Ô {o.MaViTri} đang khóa để kiểm kê ({o.MaPhieuKiemKe}).");

        var lo = await TimLoAsync(yc.Ma, o.MaViTri);
        var dong = l.ChiTiet.SingleOrDefault(c => c.MaBienThe == lo.MaBienThe)
            ?? throw new NghiepVuException($"Sai hàng: {lo.MaBienThe} không có trong lệnh {ma}.");
        if (lo.HanDung is { } han && han < tonKhaDung.HomNay)
            throw new NghiepVuException($"Lô {lo.MaLo} đã hết hạn ({Ngay(han)}), không được xuất.");
        var daLay = await db.DongLayHang.Where(d => d.MaLenh == ma && d.MaBienThe == lo.MaBienThe).SumAsync(d => d.SoLuong);
        if (daLay + yc.SoLuong > dong.SoLuongYeuCau)
            throw new NghiepVuException($"Lấy vượt số yêu cầu: {lo.MaBienThe} cần {So(dong.SoLuongYeuCau)}, đã lấy {So(daLay)}.");

        var giu = await db.GiuHang.SingleOrDefaultAsync(g => g.MaDonDat == l.MaDonDat && g.MaLo == lo.MaLo && g.MaViTri == o.MaViTri);
        var daLayTaiDay = await db.DongLayHang.Where(d => d.MaLenh == ma && d.MaLo == lo.MaLo && d.MaViTri == o.MaViTri).SumAsync(d => d.SoLuong);
        var giuThem = Math.Max(0, yc.SoLuong - Math.Max(0, (giu?.SoLuong ?? 0) - daLayTaiDay));
        if (giuThem > 0)
        {
            await congGhiTon.GiuAsync([new DongGiu(lo.MaLo, o.MaViTri, giuThem)]);
            if (giu is null)
                db.GiuHang.Add(new GiuHang
                {
                    MaDonDat = l.MaDonDat, MaLo = lo.MaLo, MaViTri = o.MaViTri, SoLuong = giuThem, ThoiDiem = gio.GetUtcNow(), NguoiGiu = nguoiDung.Ma,
                });
            else
                giu.SoLuong += giuThem;
        }

        var dongLay = new DongLayHang
        {
            MaLenh = ma, MaBienThe = lo.MaBienThe, MaLo = lo.MaLo, MaViTri = o.MaViTri, SoLuong = yc.SoLuong, SoLuongGiuThem = giuThem,
            NguoiLay = nguoiDung.Ma, ThoiDiem = gio.GetUtcNow(),
        };
        db.DongLayHang.Add(dongLay);
        l.TrangThai = TrangThaiLenhXuat.ChoLayHang;
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return dongLay.MaDong;
    }

    public async Task XoaDongLayAsync(string ma, long maDong)
    {
        await using var tx = await db.Database.BeginTransactionAsync();
        var l = await KhoaLenhCuaToiAsync(ma);
        if (l.TrangThai != TrangThaiLenhXuat.ChoLayHang)
            throw new NghiepVuException("Chỉ xóa dòng đã lấy khi lệnh đang ở bước lấy hàng.");
        var d = await db.DongLayHang.SingleOrDefaultAsync(x => x.MaDong == maDong && x.MaLenh == ma) ?? throw new NghiepVuException($"Không có dòng lấy {maDong}.");
        if (d.SoLuongGiuThem > 0)
        {
            await congGhiTon.BoGiuAsync([new DongGiu(d.MaLo, d.MaViTri, d.SoLuongGiuThem)]);
            var giu = await db.GiuHang.SingleAsync(g => g.MaDonDat == l.MaDonDat && g.MaLo == d.MaLo && g.MaViTri == d.MaViTri);
            giu.SoLuong -= d.SoLuongGiuThem;
            if (giu.SoLuong == 0)
                db.GiuHang.Remove(giu);
        }
        db.DongLayHang.Remove(d);
        await db.SaveChangesAsync();
        await tx.CommitAsync();
    }

    public async Task XongLayAsync(string ma)
    {
        await using var tx = await db.Database.BeginTransactionAsync();
        var l = await KhoaLenhCuaToiAsync(ma);
        if (l.TrangThai != TrangThaiLenhXuat.ChoLayHang)
            throw new NghiepVuException("Lệnh không ở bước lấy hàng hoặc chưa lấy dòng nào.");
        var daLay = await db.DongLayHang.Where(d => d.MaLenh == ma).GroupBy(d => d.MaBienThe)
            .Select(g => new { g.Key, Tong = g.Sum(d => d.SoLuong) }).ToDictionaryAsync(x => x.Key, x => x.Tong);
        var thieu = l.ChiTiet.Where(c => daLay.GetValueOrDefault(c.MaBienThe) < c.SoLuongYeuCau).ToList();
        if (thieu.Count > 0)
            throw new NghiepVuException("Chưa lấy đủ: " + string.Join("; ", thieu.Select(c => $"{c.MaBienThe} cần {So(c.SoLuongYeuCau)}, đã lấy {So(daLay.GetValueOrDefault(c.MaBienThe))}")));
        l.TrangThai = TrangThaiLenhXuat.ChoKiemTra;
        l.GhiChuKiemTra = null;
        nhatKy.Ghi("XongLayHang", nameof(LenhXuat), ma, maKho: l.MaKho);
        await db.SaveChangesAsync();
        await tx.CommitAsync();
    }

    /// <summary>Quy tắc 11: người kiểm tra khác người lấy. Sai thì trả lệnh về bước lấy hàng kèm ghi chú.</summary>
    public async Task KiemTraAsync(string ma, KiemTraYeuCau yc)
    {
        await using var tx = await db.Database.BeginTransactionAsync();
        var l = await KhoaLenhAsync(ma);
        if (l.TrangThai != TrangThaiLenhXuat.ChoKiemTra)
            throw new NghiepVuException("Lệnh không ở bước chờ kiểm tra.");
        var toi = nguoiDung.Ma;
        if (l.NguoiThucHien == toi || await db.DongLayHang.AnyAsync(d => d.MaLenh == ma && d.NguoiLay == toi))
            throw new NghiepVuException("Người kiểm tra phải khác người lấy hàng.");
        if (!yc.Dung && string.IsNullOrWhiteSpace(yc.GhiChu))
            throw new NghiepVuException("Ghi rõ dòng sai để thủ kho lấy lại.");

        l.TrangThai = yc.Dung ? TrangThaiLenhXuat.ChoXacNhan : TrangThaiLenhXuat.ChoLayHang;
        l.NguoiKiemTra = toi;
        l.GhiChuKiemTra = yc.GhiChu?.Trim();
        nhatKy.Ghi("KiemTraLenhXuat", nameof(LenhXuat), ma, new { dung = yc.Dung, ghiChu = l.GhiChuKiemTra }, maKho: l.MaKho);
        await db.SaveChangesAsync();
        await tx.CommitAsync();
    }

    /// <summary>
    /// Một transaction: sinh phiếu xuất Hoàn tất, bỏ giữ toàn bộ hàng đã giữ cho đơn, trừ tồn theo dòng lấy qua cổng ghi tồn,
    /// cộng số đã xuất của đơn, ghi chấp nhận gợi ý G1 (thủ kho lấy đúng lô, ô gợi ý hay không). Gửi lặp cùng khóa không trừ hai lần.
    /// </summary>
    public async Task<(string MaPhieuXuat, KetQuaGhi KetQua)> XacNhanAsync(string ma, string? khoaChongLap)
    {
        if (string.IsNullOrWhiteSpace(khoaChongLap) || khoaChongLap.Length > 56)
            throw new NghiepVuException("Thiếu khóa chống ghi lặp (Idempotency-Key, tối đa 56 ký tự).");
        await using var tx = await db.Database.BeginTransactionAsync();
        var l = await KhoaLenhAsync(ma);
        if (l.TrangThai == TrangThaiLenhXuat.HoanThanh)
        {
            var daGhi = await db.SoBienDong.Where(s => s.MaKhoaChongLap == khoaChongLap + "#1" && s.LoaiChungTu == LoaiChungTu).Select(s => s.MaChungTu).FirstOrDefaultAsync();
            return daGhi is not null && await db.PhieuXuat.AnyAsync(p => p.MaPhieuXuat == daGhi && p.MaLenh == ma)
                ? (daGhi, KetQuaGhi.DaGhiTruocDo)
                : throw new NghiepVuException("Lệnh đã xác nhận xuất.");
        }
        if (l.TrangThai != TrangThaiLenhXuat.ChoXacNhan)
            throw new NghiepVuException("Lệnh chưa qua bước kiểm tra.");

        var lay = await db.DongLayHang.Where(d => d.MaLenh == ma).ToListAsync();
        var giu = await db.GiuHang.Where(g => g.MaDonDat == l.MaDonDat).ToListAsync();
        foreach (var nhom in giu.Where(g => g.MaGoiY is not null).GroupBy(g => g.MaGoiY!.Value))
        {
            var bienThe = await db.Lo.Where(x => x.MaLo == nhom.First().MaLo).Select(x => x.MaBienThe).SingleAsync();
            var goiYO = nhom.Select(g => (g.MaLo, g.MaViTri)).ToHashSet();
            await goiY.PhanHoiAsync(nhom.Key, lay.Where(d => d.MaBienThe == bienThe).All(d => d.SoLuongGiuThem == 0 && goiYO.Contains((d.MaLo, d.MaViTri))), ma);
        }

        if (giu.Count > 0)
            await congGhiTon.BoGiuAsync(giu.Select(g => new DongGiu(g.MaLo, g.MaViTri, g.SoLuong)).ToList());
        db.GiuHang.RemoveRange(giu);

        var donVi = l.ChiTiet.ToDictionary(c => c.MaBienThe, c => c.TenDonVi);
        var gop = lay.GroupBy(d => (d.MaBienThe, d.MaLo, d.MaViTri)).OrderBy(g => g.Key.MaViTri, StringComparer.Ordinal).ThenBy(g => g.Key.MaLo, StringComparer.Ordinal).ToList();
        var px = new PhieuXuat
        {
            MaPhieuXuat = await maChungTu.TaoAsync("PX"), LoaiPhieu = LoaiPhieuXuat.XuatBan, MaLenh = ma, MaKho = l.MaKho,
            NguoiLap = l.NguoiThucHien, NguoiXacNhan = nguoiDung.Ma, NgayXuat = gio.GetUtcNow(), TrangThai = TrangThaiPhieu.HoanTat,
            ChiTiet = gop.Select((g, i) => new ChiTietPhieuXuat
            {
                STT = i + 1, MaLo = g.Key.MaLo, MaViTri = g.Key.MaViTri, TenDonVi = donVi[g.Key.MaBienThe], HeSo = 1,
                SoLuongGoc = g.Sum(d => d.SoLuong), SoLuong = g.Sum(d => d.SoLuong),
            }).ToList(),
        };
        db.PhieuXuat.Add(px);
        if (await congGhiTon.GhiAsync(khoaChongLap, LoaiChungTu, px.MaPhieuXuat, px.ChiTiet.Select(c => new DongGhiTon(c.MaLo, c.MaViTri, -c.SoLuong)).ToList())
            == KetQuaGhi.DaGhiTruocDo)
            throw new XungDotException("Thao tác vừa được ghi bởi một yêu cầu khác, vui lòng tải lại trang.");

        var don = await db.DonDatHang.Include(d => d.ChiTiet).SingleAsync(d => d.MaDonDat == l.MaDonDat);
        foreach (var c in don.ChiTiet)
            c.SoLuongDaXuat += lay.Where(d => d.MaBienThe == c.MaBienThe).Sum(d => d.SoLuong);
        if (don.ChiTiet.All(c => c.SoLuongDaXuat >= c.SoLuongDat * c.HeSo))
            don.TrangThai = TrangThaiDonDat.HoanThanh;
        l.TrangThai = TrangThaiLenhXuat.HoanThanh;
        nhatKy.Ghi("XacNhanXuat", nameof(LenhXuat), ma, new { maPhieuXuat = px.MaPhieuXuat }, maKho: l.MaKho);
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return (px.MaPhieuXuat, KetQuaGhi.DaGhi);
    }

    /// <summary>Thủ kho chỉ thấy lệnh giao cho mình; quản lý kho thấy lệnh của kho được giao.</summary>
    private async Task<IQueryable<LenhXuat>> PhamViAsync()
    {
        var kho = await nguoiDung.KhoDuocGiaoAsync();
        var q = db.LenhXuat.Where(l => kho.Contains(l.MaKho));
        if (nguoiDung.VaiTro == VaiTro.ThuKho)
        {
            var toi = nguoiDung.Ma;
            q = q.Where(l => l.NguoiThucHien == toi);
        }
        return q;
    }

    private async Task<LenhXuat> KhoaLenhAsync(string ma)
    {
        await db.Database.ExecuteSqlAsync($"""SELECT 1 FROM "LenhXuat" WHERE "MaLenh" = {ma} FOR UPDATE""");
        var l = await db.LenhXuat.Include(x => x.ChiTiet).SingleOrDefaultAsync(x => x.MaLenh == ma) ?? throw new NghiepVuException($"Không có lệnh xuất {ma}.");
        await nguoiDung.DamBaoQuyenKhoAsync(l.MaKho);
        return l;
    }

    /// <summary>Chỉ thủ kho được phân công thao tác lấy hàng.</summary>
    private async Task<LenhXuat> KhoaLenhCuaToiAsync(string ma)
    {
        var l = await KhoaLenhAsync(ma);
        if (l.NguoiThucHien != nguoiDung.Ma)
            throw new KhongCoQuyenException("Chỉ thủ kho được phân công lấy hàng cho lệnh này.");
        return l;
    }

    /// <summary>Mã lô thì dùng luôn; mã biến thể hoặc mã vạch thì chọn lô của hàng đó đang nằm ở ô, có nhiều lô thì bắt quét mã lô.</summary>
    private async Task<Lo> TimLoAsync(string ma, string maViTri)
    {
        ma = ma.Trim();
        var lo = await db.Lo.AsNoTracking().SingleOrDefaultAsync(x => x.MaLo == ma);
        if (lo is not null)
        {
            if (!await db.TonKho.AnyAsync(t => t.MaLo == lo.MaLo && t.MaViTri == maViTri && t.SoLuong > 0))
                throw new NghiepVuException($"Ô {maViTri} không có lô {lo.MaLo}.");
            return lo;
        }
        var bienThe = await db.BienThe.Where(b => b.MaBienThe == ma || b.MaVach == ma).Select(b => b.MaBienThe).FirstOrDefaultAsync()
            ?? throw new NghiepVuException($"Mã {ma} không phải lô hay hàng nào.");
        var cacLo = await db.TonKho.Where(t => t.MaViTri == maViTri && t.SoLuong > 0)
            .Join(db.Lo, t => t.MaLo, l => l.MaLo, (t, l) => l).Where(l => l.MaBienThe == bienThe).ToListAsync();
        return cacLo.Count switch
        {
            0 => throw new NghiepVuException($"Ô {maViTri} không có hàng {bienThe}."),
            1 => cacLo[0],
            _ => throw new NghiepVuException($"Ô {maViTri} có nhiều lô của {bienThe}; quét mã lô trên nhãn."),
        };
    }
}
