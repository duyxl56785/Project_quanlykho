using Microsoft.EntityFrameworkCore;
using quanlykho.Common;
using quanlykho.Common.GiaoDien;
using quanlykho.Common.Loi;
using quanlykho.Common.PhanQuyen;
using quanlykho.Data;
using quanlykho.Domain;
using quanlykho.Features.DanhMuc;

namespace quanlykho.Features.NhapKho;

public class DonMuaService(AppDbContext db, NguoiDungHienTai nguoiDung, MaChungTu maChungTu, GhiNhatKy nhatKy, TimeProvider gio)
{
    public const string ChoNhanHang = "Chờ nhận hàng";
    public const string ChoKiemDem = "Chờ kiểm đếm";

    public async Task<TrangDuLieu<DonMuaDong>> DanhSachAsync(string? tuKhoa, string? maNhaCungCap, string? maKho,
        DateOnly? tuNgay, DateOnly? denNgay, TrangThaiDonMua? trangThai, int trang)
    {
        var kho = await nguoiDung.KhoDuocGiaoAsync();
        var q = db.DonMua.Where(d => kho.Contains(d.MaKho));
        if (!string.IsNullOrWhiteSpace(tuKhoa)) q = q.Where(d => EF.Functions.ILike(d.MaDonMua, $"%{tuKhoa.Trim()}%"));
        if (!string.IsNullOrWhiteSpace(maNhaCungCap)) q = q.Where(d => d.MaNhaCungCap == maNhaCungCap);
        if (!string.IsNullOrWhiteSpace(maKho)) q = q.Where(d => d.MaKho == maKho);
        if (tuNgay is not null)
        {
            var tu = DinhDang.BatDauNgay(tuNgay.Value);
            q = q.Where(d => d.NgayTao >= tu);
        }
        if (denNgay is not null)
        {
            var den = DinhDang.BatDauNgay(denNgay.Value.AddDays(1));
            q = q.Where(d => d.NgayTao < den);
        }
        if (trangThai is not null) q = q.Where(d => d.TrangThai == trangThai);

        return await TrangDuLieu<DonMuaDong>.TaoAsync(q.OrderByDescending(d => d.NgayTao).ThenByDescending(d => d.MaDonMua).Select(d => new DonMuaDong(
            d.MaDonMua,
            db.NhaCungCap.Where(n => n.MaNhaCungCap == d.MaNhaCungCap).Select(n => n.Ten).First(),
            d.MaKho, d.NgayTao,
            d.ChiTiet.Sum(c => c.SoLuongDat * c.DonGia),
            d.TrangThai,
            d.TrangThai == TrangThaiDonMua.DangThucHien
                ? (db.PhieuNhap.Any(p => p.MaDonMua == d.MaDonMua && p.TrangThai == TrangThaiPhieu.LuuTam) ? ChoKiemDem : ChoNhanHang)
                : null)), trang);
    }

    public async Task<DonMuaChiTiet> LayAsync(string ma)
    {
        var d = await db.DonMua.AsNoTracking().Include(x => x.ChiTiet).SingleOrDefaultAsync(x => x.MaDonMua == ma)
            ?? throw new NghiepVuException($"Không có đơn mua {ma}.");
        await nguoiDung.DamBaoQuyenKhoAsync(d.MaKho);

        var nhan = await SoNhanAsync(ma);
        var tenHang = await db.BienThe.Where(b => d.ChiTiet.Select(c => c.MaBienThe).Contains(b.MaBienThe))
            .Select(b => new { b.MaBienThe, b.SanPham.Ten, b.ThuocTinh, b.SanPham.DonViTinh }).ToListAsync();
        var dong = d.ChiTiet.OrderBy(c => c.MaBienThe).Select(c =>
        {
            var h = tenHang.Single(x => x.MaBienThe == c.MaBienThe);
            var n = nhan.GetValueOrDefault(c.MaBienThe);
            return new TienDoDong(c.MaBienThe, MoTaHang(h.Ten, h.ThuocTinh), c.TenDonVi, c.HeSo, c.SoLuongDat, c.DonGia, n.DaNhan, n.DangNhan, h.DonViTinh);
        }).ToList();
        var phieu = await db.PhieuNhap.Where(p => p.MaDonMua == ma && p.TrangThai != TrangThaiPhieu.DaHuy)
            .OrderBy(p => p.MaPhieuNhap).Select(p => p.MaPhieuNhap).ToListAsync();

        return new DonMuaChiTiet(d.MaDonMua, d.MaNhaCungCap,
            await db.NhaCungCap.Where(n => n.MaNhaCungCap == d.MaNhaCungCap).Select(n => n.Ten).SingleAsync(),
            d.MaKho, await db.ViTri.Where(v => v.MaViTri == d.MaKho).Select(v => v.Ten).SingleAsync(),
            await db.NguoiDung.Where(n => n.MaNguoiDung == d.NguoiTao).Select(n => n.Ten).SingleAsync(),
            d.NgayTao, d.TrangThai, Buoc(d.TrangThai, dong), d.GhiChu, dong, phieu);
    }

    public async Task<DonMuaForm> NapDanhSachAsync(DonMuaForm form)
    {
        form.DanhSachNhaCungCap = await db.NhaCungCap.Where(n => n.TrangThai == TrangThaiSuDung.DangSuDung)
            .OrderBy(n => n.MaNhaCungCap).Select(n => new LuaChon(n.MaNhaCungCap, n.Ten)).ToListAsync();
        var kho = await nguoiDung.KhoDuocGiaoAsync();
        form.DanhSachKho = await db.ViTri.Where(v => kho.Contains(v.MaViTri)).OrderBy(v => v.MaViTri)
            .Select(v => new LuaChon(v.MaViTri, v.Ten)).ToListAsync();
        form.DanhSachBienThe = (await db.BienThe
                .Where(b => b.TrangThai == TrangThaiSuDung.DangSuDung && b.SanPham.TrangThai == TrangThaiSuDung.DangSuDung)
                .OrderBy(b => b.MaBienThe).Select(b => new { b.MaBienThe, b.SanPham.Ten, b.SanPham.DonViTinh, b.ThuocTinh }).ToListAsync())
            .Select(b => new LuaChon(b.MaBienThe, $"{MoTaHang(b.Ten, b.ThuocTinh)} ({b.DonViTinh})")).ToList();
        form.DonViHang = await new QuyDoiDonVi(db).BangAsync(form.DanhSachBienThe.Select(b => b.Ma));
        return form;
    }

    public async Task<DonMuaForm> LayFormAsync(string ma)
    {
        var d = await db.DonMua.AsNoTracking().Include(x => x.ChiTiet).SingleOrDefaultAsync(x => x.MaDonMua == ma)
            ?? throw new NghiepVuException($"Không có đơn mua {ma}.");
        await nguoiDung.DamBaoQuyenKhoAsync(d.MaKho);
        return await NapDanhSachAsync(new DonMuaForm
        {
            Ma = d.MaDonMua, MaNhaCungCap = d.MaNhaCungCap, MaKho = d.MaKho, GhiChu = d.GhiChu,
            Dong = d.ChiTiet.OrderBy(c => c.MaBienThe).Select(c => new DongDonMuaForm { MaBienThe = c.MaBienThe, TenDonVi = c.TenDonVi, SoLuong = c.SoLuongDat, DonGia = c.DonGia }).ToList(),
        });
    }

    public async Task<string> TaoAsync(DonMuaForm form, bool laNhap = false)
    {
        var dong = await KiemTraAsync(form);
        var d = new DonMua
        {
            MaDonMua = await maChungTu.TaoAsync("DM"), MaNhaCungCap = form.MaNhaCungCap, MaKho = form.MaKho,
            NguoiTao = nguoiDung.Ma, NgayTao = gio.GetUtcNow(), GhiChu = form.GhiChu?.Trim(),
            TrangThai = laNhap ? TrangThaiDonMua.Nhap : TrangThaiDonMua.ChuaThucHien, ChiTiet = dong,
        };
        db.DonMua.Add(d);
        await db.SaveChangesAsync();
        return d.MaDonMua;
    }

    public async Task SuaAsync(string ma, DonMuaForm form)
    {
        var d = await db.DonMua.Include(x => x.ChiTiet).SingleOrDefaultAsync(x => x.MaDonMua == ma)
            ?? throw new NghiepVuException($"Không có đơn mua {ma}.");
        await nguoiDung.DamBaoQuyenKhoAsync(d.MaKho);
        if (!(d.TrangThai is TrangThaiDonMua.Nhap || (d.TrangThai is TrangThaiDonMua.ChuaThucHien && !await db.PhieuNhap.AnyAsync(p => p.MaDonMua == ma))))
            throw new NghiepVuException("Chỉ sửa được đơn nháp hoặc đơn chưa có hóa đơn nhập.");
        var dong = await KiemTraAsync(form);
        await using var tx = await db.Database.BeginTransactionAsync();
        d.MaNhaCungCap = form.MaNhaCungCap;
        d.MaKho = form.MaKho;
        d.GhiChu = form.GhiChu?.Trim();
        db.ChiTietDonMua.RemoveRange(d.ChiTiet);
        await db.SaveChangesAsync();
        d.ChiTiet = dong;
        await db.SaveChangesAsync();
        await tx.CommitAsync();
    }

    public async Task XacNhanAsync(string ma)
    {
        var d = await TimDeDoi(ma);
        if (d.TrangThai != TrangThaiDonMua.Nhap)
            throw new NghiepVuException("Chỉ xác nhận được đơn nháp.");
        d.TrangThai = TrangThaiDonMua.ChuaThucHien;
        await PhanHoiGoiYAsync(ma, true);
        nhatKy.Ghi("XacNhanDonMua", nameof(DonMua), ma, maKho: d.MaKho);
        await db.SaveChangesAsync();
    }

    /// <summary>Đơn nháp từ G3: xác nhận là chấp nhận đề xuất, hủy nháp là không chấp nhận.</summary>
    private async Task PhanHoiGoiYAsync(string ma, bool chapNhan)
    {
        foreach (var ls in await db.LichSuGoiY.Where(l => l.Loai == LoaiGoiY.NhapBoSung && l.MaChungTu == ma && l.ChapNhan == null).ToListAsync())
        {
            ls.ChapNhan = chapNhan;
            ls.ThoiDiemPhanHoi = gio.GetUtcNow();
        }
    }

    public async Task HuyAsync(string ma)
    {
        var d = await TimDeDoi(ma);
        if (d.TrangThai is TrangThaiDonMua.DaHuy or TrangThaiDonMua.HoanThanh)
            throw new NghiepVuException("Đơn đã hoàn thành hoặc đã hủy.");
        if (await db.PhieuNhap.AnyAsync(p => p.MaDonMua == ma && p.TrangThai != TrangThaiPhieu.DaHuy))
            throw new NghiepVuException("Đơn đã có hóa đơn nhập; hủy các hóa đơn nhập liên quan trước.");
        d.TrangThai = TrangThaiDonMua.DaHuy;
        await PhanHoiGoiYAsync(ma, false);
        nhatKy.Ghi("HuyDonMua", nameof(DonMua), ma, maKho: d.MaKho);
        await db.SaveChangesAsync();
    }

    /// <summary>Gọi sau mỗi lần phiếu nhập của đơn đổi trạng thái; không lưu, Service gọi lưu cùng transaction.</summary>
    public async Task CapNhatTrangThaiAsync(string ma)
    {
        var d = await db.DonMua.Include(x => x.ChiTiet).SingleAsync(x => x.MaDonMua == ma);
        if (d.TrangThai is TrangThaiDonMua.Nhap or TrangThaiDonMua.DaHuy)
            return;
        var nhan = await SoNhanAsync(ma);
        var coPhieu = await db.PhieuNhap.AnyAsync(p => p.MaDonMua == ma && p.TrangThai != TrangThaiPhieu.DaHuy);
        d.TrangThai = !coPhieu ? TrangThaiDonMua.ChuaThucHien
            : d.ChiTiet.All(c => nhan.GetValueOrDefault(c.MaBienThe).DaNhan >= c.SoLuongDat * c.HeSo) ? TrangThaiDonMua.HoanThanh
            : TrangThaiDonMua.DangThucHien;
    }

    /// <summary>Theo đơn vị tính chính: đã nhận (phiếu Hoàn tất) và đang nhận (phiếu Lưu tạm) của từng biến thể.</summary>
    public async Task<Dictionary<string, (decimal DaNhan, decimal DangNhan)>> SoNhanAsync(string maDonMua) =>
        (await (from p in db.PhieuNhap
                where p.MaDonMua == maDonMua && p.TrangThai != TrangThaiPhieu.DaHuy
                from c in p.ChiTiet
                join l in db.Lo on c.MaLo equals l.MaLo
                group new { p.TrangThai, c.SoLuongThucNhan } by l.MaBienThe into g
                select new
                {
                    g.Key,
                    DaNhan = g.Where(x => x.TrangThai == TrangThaiPhieu.HoanTat).Sum(x => x.SoLuongThucNhan),
                    DangNhan = g.Where(x => x.TrangThai == TrangThaiPhieu.LuuTam).Sum(x => x.SoLuongThucNhan),
                }).ToListAsync())
        .ToDictionary(x => x.Key, x => (x.DaNhan, x.DangNhan));

    private async Task<DonMua> TimDeDoi(string ma)
    {
        var d = await db.DonMua.SingleOrDefaultAsync(x => x.MaDonMua == ma) ?? throw new NghiepVuException($"Không có đơn mua {ma}.");
        await nguoiDung.DamBaoQuyenKhoAsync(d.MaKho);
        return d;
    }

    private async Task<List<ChiTietDonMua>> KiemTraAsync(DonMuaForm form)
    {
        await nguoiDung.DamBaoQuyenKhoAsync(form.MaKho);
        if (!await db.NhaCungCap.AnyAsync(n => n.MaNhaCungCap == form.MaNhaCungCap && n.TrangThai == TrangThaiSuDung.DangSuDung))
            throw new NghiepVuException("Nhà cung cấp không có hoặc đã ngừng sử dụng.");

        var dong = form.Dong.Where(d => !string.IsNullOrWhiteSpace(d.MaBienThe)).ToList();
        if (dong.Count == 0)
            throw new NghiepVuException("Đơn mua phải có ít nhất một dòng hàng.");
        var trung = dong.GroupBy(d => d.MaBienThe).FirstOrDefault(g => g.Count() > 1);
        if (trung is not null)
            throw new NghiepVuException($"Biến thể {trung.Key} xuất hiện nhiều dòng; mỗi biến thể chỉ một dòng.");
        if (dong.Any(d => d.SoLuong <= 0))
            throw new NghiepVuException("Số lượng đặt phải lớn hơn 0.");
        if (dong.Any(d => d.DonGia < 0))
            throw new NghiepVuException("Đơn giá không được âm.");

        var ma = dong.Select(d => d.MaBienThe).ToList();
        var dangDung = await db.BienThe.Where(b => ma.Contains(b.MaBienThe)
                && b.TrangThai == TrangThaiSuDung.DangSuDung && b.SanPham.TrangThai == TrangThaiSuDung.DangSuDung)
            .Select(b => b.MaBienThe).ToListAsync();
        var thieu = ma.FirstOrDefault(m => !dangDung.Contains(m));
        if (thieu is not null)
            throw new NghiepVuException($"Biến thể {thieu} không có hoặc đã ngừng sử dụng.");

        // Dòng lưu đơn vị, hệ số tại thời điểm lập; số đặt và đơn giá theo đơn vị đã chọn (quy tắc 15).
        var donVi = await new QuyDoiDonVi(db).BangAsync(ma);
        return dong.Select(d =>
        {
            var q = QuyDoiDonVi.QuyVeChinh(donVi[d.MaBienThe], d.TenDonVi, d.SoLuong, d.MaBienThe);
            return new ChiTietDonMua { MaBienThe = d.MaBienThe, TenDonVi = q.TenDonVi, HeSo = q.HeSo, SoLuongDat = d.SoLuong, DonGia = d.DonGia };
        }).ToList();
    }

    private static string? Buoc(TrangThaiDonMua trangThai, List<TienDoDong> dong) =>
        trangThai != TrangThaiDonMua.DangThucHien ? null : dong.Any(d => d.DangNhan > 0) ? ChoKiemDem : ChoNhanHang;

    public static string MoTaHang(string ten, Dictionary<string, string> thuocTinh) =>
        thuocTinh.Count == 0 ? ten : $"{ten} {string.Join(" ", thuocTinh.Values)}";
}
