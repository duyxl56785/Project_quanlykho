using Microsoft.EntityFrameworkCore;
using quanlykho.Common;
using quanlykho.Common.GiaoDien;
using quanlykho.Common.Loi;
using quanlykho.Common.PhanQuyen;
using quanlykho.Data;
using quanlykho.Domain;
using quanlykho.Features.NhapKho;

namespace quanlykho.Features.XuatKho;

/// <summary>
/// Đơn đặt hàng (mục 2.2, Bảng 2.14 dòng 2). Nhân viên bán hàng tạo, gửi duyệt, chỉ thấy đơn của mình;
/// quản lý kho thấy đơn của kho được giao; quản trị viên thấy mọi đơn.
/// </summary>
public class DonDatService(AppDbContext db, NguoiDungHienTai nguoiDung, MaChungTu maChungTu, GhiNhatKy nhatKy, TonKhaDung tonKhaDung, GiuHangService giuHang, TimeProvider gio)
{
    public async Task<TrangDuLieu<DonDatDong>> DanhSachAsync(string? tuKhoa, string? maKho, TrangThaiDonDat? trangThai, int trang)
    {
        var q = await PhamViAsync();
        if (!string.IsNullOrWhiteSpace(tuKhoa)) q = q.Where(d => EF.Functions.ILike(d.MaDonDat, $"%{tuKhoa.Trim()}%"));
        if (!string.IsNullOrWhiteSpace(maKho)) q = q.Where(d => d.MaKho == maKho);
        if (trangThai is not null) q = q.Where(d => d.TrangThai == trangThai);
        return await TrangDuLieu<DonDatDong>.TaoAsync(q.OrderByDescending(d => d.NgayTao).ThenByDescending(d => d.MaDonDat).Select(d => new DonDatDong(
            d.MaDonDat, d.MaKho,
            db.NoiNhan.Where(n => n.MaNoiNhan == d.MaNoiNhan).Select(n => n.Ten).First(),
            db.NguoiDung.Where(n => n.MaNguoiDung == d.NguoiTao).Select(n => n.Ten).First(),
            d.NgayTao, d.HanGiao, d.ChiTiet.Count, d.TrangThai)), trang);
    }

    public async Task<DonDatChiTiet> LayAsync(string ma)
    {
        var d = await (await PhamViAsync()).AsNoTracking().Include(x => x.ChiTiet).SingleOrDefaultAsync(x => x.MaDonDat == ma)
            ?? throw new NghiepVuException($"Không có đơn đặt hàng {ma} hoặc bạn không được xem đơn này.");
        var maHang = d.ChiTiet.Select(c => c.MaBienThe).ToList();
        var hang = await db.BienThe.Where(b => maHang.Contains(b.MaBienThe))
            .Select(b => new { b.MaBienThe, b.SanPham.Ten, b.ThuocTinh }).ToDictionaryAsync(b => b.MaBienThe);
        var khaDung = await tonKhaDung.TruyVan().Where(k => k.MaKho == d.MaKho && maHang.Contains(k.MaBienThe))
            .GroupBy(k => k.MaBienThe).Select(g => new { g.Key, Tong = g.Sum(k => k.KhaDung) }).ToDictionaryAsync(x => x.Key, x => x.Tong);
        var noiNhan = await db.NoiNhan.Where(n => n.MaNoiNhan == d.MaNoiNhan).Select(n => new { n.Ten, n.DiaChi }).SingleAsync();
        var daGiu = await db.GiuHang.Where(g => g.MaDonDat == ma).OrderBy(g => g.ThuTu)
            .Join(db.Lo, g => g.MaLo, l => l.MaLo, (g, l) => new HangDaGiu(g.ThuTu, l.MaBienThe, g.MaLo, g.MaViTri, l.HanDung, g.SoLuong))
            .ToListAsync();
        return new DonDatChiTiet(d.MaDonDat, d.MaKho, await db.ViTri.Where(v => v.MaViTri == d.MaKho).Select(v => v.Ten).SingleAsync(),
            d.MaNoiNhan, noiNhan.Ten, noiNhan.DiaChi, d.NguoiTao, await db.NguoiDung.Where(n => n.MaNguoiDung == d.NguoiTao).Select(n => n.Ten).SingleAsync(),
            d.NgayTao, d.HanGiao, d.TrangThai, d.GhiChu,
            d.ChiTiet.OrderBy(c => c.MaBienThe).Select(c => new DongDonDat(c.MaBienThe, DonMuaService.MoTaHang(hang[c.MaBienThe].Ten, hang[c.MaBienThe].ThuocTinh),
                c.TenDonVi, c.HeSo, c.SoLuongDat, c.SoLuongDaXuat, khaDung.GetValueOrDefault(c.MaBienThe))).ToList(), daGiu,
            await db.LenhXuat.Where(l => l.MaDonDat == ma).Select(l => l.MaLenh).FirstOrDefaultAsync());
    }

    public async Task<DonDatForm> NapDanhSachAsync(DonDatForm form)
    {
        var kho = await nguoiDung.KhoDuocGiaoAsync();
        form.DanhSachKho = await db.ViTri.Where(v => kho.Contains(v.MaViTri) && !v.DangKhoa).OrderBy(v => v.MaViTri)
            .Select(v => new LuaChon(v.MaViTri, v.Ten)).ToListAsync();
        form.DanhSachNoiNhan = await db.NoiNhan.Where(n => n.TrangThai == TrangThaiSuDung.DangSuDung).OrderBy(n => n.MaNoiNhan)
            .Select(n => new LuaChon(n.MaNoiNhan, n.Ten)).ToListAsync();
        form.DanhSachBienThe = (await db.BienThe
                .Where(b => b.TrangThai == TrangThaiSuDung.DangSuDung && b.SanPham.TrangThai == TrangThaiSuDung.DangSuDung)
                .OrderBy(b => b.MaBienThe).Select(b => new { b.MaBienThe, b.SanPham.Ten, b.SanPham.DonViTinh, b.ThuocTinh }).ToListAsync())
            .Select(b => new LuaChon(b.MaBienThe, $"{DonMuaService.MoTaHang(b.Ten, b.ThuocTinh)} ({b.DonViTinh})")).ToList();
        return form;
    }

    public async Task<DonDatForm> LayFormAsync(string ma)
    {
        var d = await TimDeSuaAsync(ma);
        return await NapDanhSachAsync(new DonDatForm
        {
            Ma = d.MaDonDat, MaKho = d.MaKho, MaNoiNhan = d.MaNoiNhan, HanGiao = d.HanGiao, GhiChu = d.GhiChu,
            Dong = d.ChiTiet.OrderBy(c => c.MaBienThe).Select(c => new DongDonDatForm { MaBienThe = c.MaBienThe, SoLuong = c.SoLuongDat }).ToList(),
        });
    }

    public async Task<string> TaoAsync(DonDatForm form, bool guiDuyet)
    {
        var dong = await KiemTraAsync(form);
        var d = new DonDatHang
        {
            MaDonDat = await maChungTu.TaoAsync("DD"), MaKho = form.MaKho, MaNoiNhan = form.MaNoiNhan, NguoiTao = nguoiDung.Ma,
            NgayTao = gio.GetUtcNow(), HanGiao = form.HanGiao, GhiChu = form.GhiChu?.Trim(),
            TrangThai = guiDuyet ? TrangThaiDonDat.ChoDuyet : TrangThaiDonDat.Nhap, ChiTiet = dong,
        };
        db.DonDatHang.Add(d);
        if (guiDuyet)
            nhatKy.Ghi("GuiDuyetDonDat", nameof(DonDatHang), d.MaDonDat, maKho: d.MaKho);
        await db.SaveChangesAsync();
        return d.MaDonDat;
    }

    public async Task SuaAsync(string ma, DonDatForm form)
    {
        var d = await TimDeSuaAsync(ma);
        var dong = await KiemTraAsync(form);
        await using var tx = await db.Database.BeginTransactionAsync();
        (d.MaKho, d.MaNoiNhan, d.HanGiao, d.GhiChu) = (form.MaKho, form.MaNoiNhan, form.HanGiao, form.GhiChu?.Trim());
        db.ChiTietDonDat.RemoveRange(d.ChiTiet);
        await db.SaveChangesAsync();
        d.ChiTiet = dong;
        await db.SaveChangesAsync();
        await tx.CommitAsync();
    }

    public async Task GuiDuyetAsync(string ma)
    {
        var d = await TimDeSuaAsync(ma);
        if (d.TrangThai != TrangThaiDonDat.Nhap)
            throw new NghiepVuException("Đơn đã gửi duyệt.");
        d.TrangThai = TrangThaiDonDat.ChoDuyet;
        nhatKy.Ghi("GuiDuyetDonDat", nameof(DonDatHang), ma, maKho: d.MaKho);
        await db.SaveChangesAsync();
    }

    /// <summary>Hủy trước khi xuất; đơn đã duyệt thì bỏ giữ hàng cùng lúc.</summary>
    public async Task HuyAsync(string ma)
    {
        var d = await (await PhamViAsync()).SingleOrDefaultAsync(x => x.MaDonDat == ma)
            ?? throw new NghiepVuException($"Không có đơn đặt hàng {ma} hoặc bạn không được xem đơn này.");
        if (d.TrangThai == TrangThaiDonDat.DaDuyet)
        {
            await giuHang.HuyDonDaDuyetAsync(ma);
            return;
        }
        if (d.TrangThai is not (TrangThaiDonDat.Nhap or TrangThaiDonDat.ChoDuyet))
            throw new NghiepVuException(d.TrangThai is TrangThaiDonDat.DaHuy ? "Đơn đã hủy." : "Đơn đã xuất kho, không hủy được.");
        d.TrangThai = TrangThaiDonDat.DaHuy;
        nhatKy.Ghi("HuyDonDat", nameof(DonDatHang), ma, maKho: d.MaKho);
        await db.SaveChangesAsync();
    }

    /// <summary>Nhân viên bán hàng chỉ thấy đơn mình tạo; vai khác thấy đơn của kho được giao.</summary>
    private async Task<IQueryable<DonDatHang>> PhamViAsync()
    {
        if (nguoiDung.VaiTro == VaiTro.NhanVienBanHang)
        {
            var toi = nguoiDung.Ma;
            return db.DonDatHang.Where(d => d.NguoiTao == toi);
        }
        var kho = await nguoiDung.KhoDuocGiaoAsync();
        return db.DonDatHang.Where(d => kho.Contains(d.MaKho));
    }

    /// <summary>Chỉ người tạo (hoặc quản trị viên) sửa, gửi duyệt, khi đơn còn Nháp hoặc Chờ duyệt.</summary>
    private async Task<DonDatHang> TimDeSuaAsync(string ma)
    {
        var d = await (await PhamViAsync()).Include(x => x.ChiTiet).SingleOrDefaultAsync(x => x.MaDonDat == ma)
            ?? throw new NghiepVuException($"Không có đơn đặt hàng {ma} hoặc bạn không được xem đơn này.");
        if (d.NguoiTao != nguoiDung.Ma && nguoiDung.VaiTro != VaiTro.QuanTriVien)
            throw new KhongCoQuyenException("Chỉ người tạo đơn được sửa, gửi duyệt.");
        if (d.TrangThai is not (TrangThaiDonDat.Nhap or TrangThaiDonDat.ChoDuyet))
            throw new NghiepVuException("Chỉ sửa được đơn nháp hoặc đơn chờ duyệt.");
        return d;
    }

    private async Task<List<ChiTietDonDat>> KiemTraAsync(DonDatForm form)
    {
        await nguoiDung.DamBaoQuyenKhoAsync(form.MaKho);
        if (!await db.ViTri.AnyAsync(v => v.MaViTri == form.MaKho && v.Cap == 1 && !v.DangKhoa))
            throw new NghiepVuException("Kho xuất không có hoặc đã ngừng sử dụng.");
        if (!await db.NoiNhan.AnyAsync(n => n.MaNoiNhan == form.MaNoiNhan && n.TrangThai == TrangThaiSuDung.DangSuDung))
            throw new NghiepVuException("Nơi nhận không có hoặc đã ngừng sử dụng.");
        if (form.HanGiao is { } han && han < tonKhaDung.HomNay)
            throw new NghiepVuException("Hạn giao không được trước hôm nay.");

        var dong = form.Dong.Where(d => !string.IsNullOrWhiteSpace(d.MaBienThe)).ToList();
        if (dong.Count == 0)
            throw new NghiepVuException("Đơn đặt hàng phải có ít nhất một dòng hàng.");
        var trung = dong.GroupBy(d => d.MaBienThe).FirstOrDefault(g => g.Count() > 1);
        if (trung is not null)
            throw new NghiepVuException($"Biến thể {trung.Key} xuất hiện nhiều dòng; mỗi biến thể chỉ một dòng.");
        if (dong.Any(d => d.SoLuong <= 0))
            throw new NghiepVuException("Số lượng đặt phải lớn hơn 0.");

        var ma = dong.Select(d => d.MaBienThe).ToList();
        var bienThe = await db.BienThe.Where(b => ma.Contains(b.MaBienThe)
                && b.TrangThai == TrangThaiSuDung.DangSuDung && b.SanPham.TrangThai == TrangThaiSuDung.DangSuDung)
            .Select(b => new { b.MaBienThe, b.SanPham.DonViTinh })
            .ToDictionaryAsync(b => b.MaBienThe, b => b.DonViTinh);
        var thieu = ma.FirstOrDefault(m => !bienThe.ContainsKey(m));
        if (thieu is not null)
            throw new NghiepVuException($"Biến thể {thieu} không có hoặc đã ngừng sử dụng.");

        return dong.Select(d => new ChiTietDonDat { MaBienThe = d.MaBienThe, TenDonVi = bienThe[d.MaBienThe], HeSo = 1, SoLuongDat = d.SoLuong }).ToList();
    }
}
