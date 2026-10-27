using Microsoft.EntityFrameworkCore;
using quanlykho.Common;
using quanlykho.Common.GiaoDien;
using quanlykho.Common.Loi;
using quanlykho.Common.PhanQuyen;
using quanlykho.Data;
using quanlykho.Domain;

namespace quanlykho.Features.NhapKho;

public class PhieuNhapService(AppDbContext db, NguoiDungHienTai nguoiDung, MaChungTu maChungTu, GhiNhatKy nhatKy, DonMuaService donMua, TimeProvider gio)
{
    private bool XemGia => nguoiDung.VaiTro is VaiTro.QuanTriVien or VaiTro.QuanLyKho;

    public async Task<TrangDuLieu<PhieuNhapDong>> DanhSachAsync(string? tuKhoa, string? maKho, TrangThaiPhieu? trangThai, int trang)
    {
        var kho = await nguoiDung.KhoDuocGiaoAsync();
        var q = db.PhieuNhap.Where(p => kho.Contains(p.MaKho));
        if (!string.IsNullOrWhiteSpace(tuKhoa))
        {
            var k = $"%{tuKhoa.Trim()}%";
            q = q.Where(p => EF.Functions.ILike(p.MaPhieuNhap, k) || EF.Functions.ILike(p.MaDonMua!, k));
        }
        if (!string.IsNullOrWhiteSpace(maKho)) q = q.Where(p => p.MaKho == maKho);
        if (trangThai is not null) q = q.Where(p => p.TrangThai == trangThai);

        var xemGia = XemGia;
        return await TrangDuLieu<PhieuNhapDong>.TaoAsync(q.OrderByDescending(p => p.NgayNhap).ThenByDescending(p => p.MaPhieuNhap)
            .Select(p => new PhieuNhapDong(p.MaPhieuNhap, p.LoaiPhieu, p.MaDonMua, p.MaKho, p.NgayNhap, p.ChiTiet.Count,
                xemGia ? p.ChiTiet.Sum(c => c.ThanhTien) : null, p.TrangThai)), trang);
    }

    public async Task<PhieuNhapChiTiet> LayAsync(string ma)
    {
        var p = await db.PhieuNhap.AsNoTracking().Include(x => x.ChiTiet).SingleOrDefaultAsync(x => x.MaPhieuNhap == ma)
            ?? throw new NghiepVuException($"Không có hóa đơn nhập {ma}.");
        await nguoiDung.DamBaoQuyenKhoAsync(p.MaKho);

        var xemGia = XemGia;
        var maLo = p.ChiTiet.Select(c => c.MaLo).ToList();
        var lo = await db.Lo.Where(l => maLo.Contains(l.MaLo))
            .Select(l => new { l.MaLo, l.MaBienThe, l.HanDung, l.BienThe.SanPham.Ten, l.BienThe.ThuocTinh })
            .ToDictionaryAsync(l => l.MaLo);
        var dong = p.ChiTiet.OrderBy(c => c.STT).Select(c =>
        {
            var l = lo[c.MaLo];
            return new DongPhieuNhap(c.STT, c.MaLo, l.MaBienThe, DonMuaService.MoTaHang(l.Ten, l.ThuocTinh), c.TenDonVi, c.SoLuongGoc, l.HanDung, c.ChatLuong,
                xemGia ? c.DonGia : null, xemGia ? c.ThanhTien : null);
        }).ToList();

        var tenNhaCungCap = p.MaDonMua is null ? null
            : await db.DonMua.Where(d => d.MaDonMua == p.MaDonMua)
                .Select(d => db.NhaCungCap.Where(n => n.MaNhaCungCap == d.MaNhaCungCap).Select(n => n.Ten).First()).SingleAsync();

        return new PhieuNhapChiTiet(p.MaPhieuNhap, p.LoaiPhieu, p.MaDonMua, tenNhaCungCap,
            p.MaKho, await db.ViTri.Where(v => v.MaViTri == p.MaKho).Select(v => v.Ten).SingleAsync(),
            await db.NguoiDung.Where(n => n.MaNguoiDung == p.NguoiLap).Select(n => n.Ten).SingleAsync(),
            p.NgayNhap, p.TrangThai, p.GhiChu, xemGia, dong,
            p.TrangThai == TrangThaiPhieu.LuuTam ? await HangNhanAsync(p.MaDonMua) : []);
    }

    public async Task<PhieuNhapForm> NapDanhSachAsync(PhieuNhapForm form)
    {
        var kho = await nguoiDung.KhoDuocGiaoAsync();
        form.DanhSachKho = await db.ViTri.Where(v => kho.Contains(v.MaViTri)).OrderBy(v => v.MaViTri)
            .Select(v => new LuaChon(v.MaViTri, v.Ten)).ToListAsync();
        form.DanhSachDonMua = (await db.DonMua
                .Where(d => kho.Contains(d.MaKho) && (d.TrangThai == TrangThaiDonMua.ChuaThucHien || d.TrangThai == TrangThaiDonMua.DangThucHien))
                .OrderBy(d => d.MaDonMua)
                .Select(d => new { d.MaDonMua, d.MaKho, Ncc = db.NhaCungCap.Where(n => n.MaNhaCungCap == d.MaNhaCungCap).Select(n => n.Ten).First() })
                .ToListAsync())
            .Select(d => new LuaChon(d.MaDonMua, $"{d.MaDonMua} – {d.Ncc} – {d.MaKho}")).ToList();
        return form;
    }

    public async Task<string> TaoAsync(PhieuNhapForm form)
    {
        await using var tx = await db.Database.BeginTransactionAsync();
        string maKho;
        if (form.LoaiPhieu == LoaiPhieuNhap.MuaHang)
        {
            if (string.IsNullOrWhiteSpace(form.MaDonMua))
                throw new NghiepVuException("Chọn đơn mua cần nhận hàng.");
            await KhoaDonMuaAsync(form.MaDonMua);
            var d = await db.DonMua.SingleOrDefaultAsync(x => x.MaDonMua == form.MaDonMua)
                ?? throw new NghiepVuException($"Không có đơn mua {form.MaDonMua}.");
            if (d.TrangThai is not (TrangThaiDonMua.ChuaThucHien or TrangThaiDonMua.DangThucHien))
                throw new NghiepVuException("Chỉ nhận hàng cho đơn đã xác nhận, chưa hoàn thành và chưa hủy.");
            maKho = d.MaKho;
        }
        else if (form.LoaiPhieu == LoaiPhieuNhap.NhapKhac)
        {
            maKho = string.IsNullOrWhiteSpace(form.MaKho) ? throw new NghiepVuException("Chọn kho nhập.") : form.MaKho;
            form.MaDonMua = null;
        }
        else
            throw new NghiepVuException("Phiếu tồn đầu kỳ chỉ sinh từ tệp nhập Excel.");
        await nguoiDung.DamBaoQuyenKhoAsync(maKho);

        var p = new PhieuNhap
        {
            MaPhieuNhap = await maChungTu.TaoAsync("PN"), LoaiPhieu = form.LoaiPhieu, MaDonMua = form.MaDonMua, MaKho = maKho,
            NguoiLap = nguoiDung.Ma, NgayNhap = gio.GetUtcNow(), TrangThai = TrangThaiPhieu.LuuTam, GhiChu = form.GhiChu?.Trim(),
        };
        db.PhieuNhap.Add(p);
        await db.SaveChangesAsync();
        if (p.MaDonMua is not null)
            await donMua.CapNhatTrangThaiAsync(p.MaDonMua);
        nhatKy.Ghi("LapPhieuNhap", nameof(PhieuNhap), p.MaPhieuNhap, p.MaDonMua is null ? null : $"Đơn mua {p.MaDonMua}", maKho);
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return p.MaPhieuNhap;
    }

    public async Task<(int STT, string MaLo)> ThemDongAsync(string ma, DongPhieuNhapForm f)
    {
        await using var tx = await db.Database.BeginTransactionAsync();
        var p = await KhoaPhieuLuuTamAsync(ma);
        if (f.SoLuong <= 0)
            throw new NghiepVuException("Số thực nhận phải lớn hơn 0.");
        var maQuet = f.Ma?.Trim() ?? "";
        var b = await db.BienThe
            .Where(x => (x.MaBienThe == maQuet || x.MaVach == maQuet)
                && x.TrangThai == TrangThaiSuDung.DangSuDung && x.SanPham.TrangThai == TrangThaiSuDung.DangSuDung)
            .Select(x => new { x.MaBienThe, x.SanPham.DonViTinh, x.SanPham.TheoDoiHanDung })
            .FirstOrDefaultAsync() ?? throw new NghiepVuException($"Mã {maQuet} không khớp hàng nào đang sử dụng.");
        if (b.TheoDoiHanDung && f.HanDung is null)
            throw new NghiepVuException($"Hàng {b.MaBienThe} theo dõi hạn dùng: nhập hạn dùng.");

        decimal donGia = 0;
        if (p.MaDonMua is not null)
        {
            // Khóa đơn để hai thủ kho nhận cùng đơn không cùng lọt qua kiểm tra số chưa nhận.
            await KhoaDonMuaAsync(p.MaDonMua);
            var dongDon = await db.ChiTietDonMua.SingleOrDefaultAsync(c => c.MaDonMua == p.MaDonMua && c.MaBienThe == b.MaBienThe)
                ?? throw new NghiepVuException($"Hàng {b.MaBienThe} không có trong đơn mua {p.MaDonMua}.");
            var n = (await donMua.SoNhanAsync(p.MaDonMua)).GetValueOrDefault(b.MaBienThe);
            var con = dongDon.SoLuongDat * dongDon.HeSo - n.DaNhan - n.DangNhan;
            if (f.SoLuong > con)
                throw new NghiepVuException($"Nhận vượt số chưa nhận của {b.MaBienThe}: còn {DinhDang.So(Math.Max(0, con))} {b.DonViTinh}, đang nhập {DinhDang.So(f.SoLuong)}.");
            donGia = dongDon.DonGia;
        }
        else if (XemGia && f.DonGia is not null)
            donGia = f.DonGia >= 0 ? f.DonGia.Value : throw new NghiepVuException("Đơn giá không được âm.");

        var stt = (await db.ChiTietPhieuNhap.Where(c => c.MaPhieuNhap == ma).MaxAsync(c => (int?)c.STT) ?? 0) + 1;
        var lo = new Lo
        {
            MaLo = await maChungTu.TaoAsync("L"), MaBienThe = b.MaBienThe,
            HanDung = b.TheoDoiHanDung ? f.HanDung : null, NgayNhap = gio.GetUtcNow(),
        };
        db.Lo.Add(lo);
        db.ChiTietPhieuNhap.Add(new ChiTietPhieuNhap
        {
            MaPhieuNhap = ma, STT = stt, MaLo = lo.MaLo, TenDonVi = b.DonViTinh, HeSo = 1,
            SoLuongGoc = f.SoLuong, SoLuongThucNhan = f.SoLuong, DonGia = donGia, ChatLuong = f.ChatLuong,
        });
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return (stt, lo.MaLo);
    }

    public async Task XoaDongAsync(string ma, int stt)
    {
        await using var tx = await db.Database.BeginTransactionAsync();
        await KhoaPhieuLuuTamAsync(ma);
        var c = await db.ChiTietPhieuNhap.SingleOrDefaultAsync(x => x.MaPhieuNhap == ma && x.STT == stt)
            ?? throw new NghiepVuException($"Hóa đơn {ma} không có dòng {stt}.");
        db.ChiTietPhieuNhap.Remove(c);
        await db.SaveChangesAsync();
        // Lô sinh riêng cho dòng này và chưa có tồn vì phiếu chưa xác nhận.
        await db.Lo.Where(l => l.MaLo == c.MaLo).ExecuteDeleteAsync();
        await tx.CommitAsync();
    }

    /// <summary>Hủy phiếu Lưu tạm; phiếu Hoàn tất hủy bằng bút toán đảo ở bước xác nhận.</summary>
    public async Task HuyAsync(string ma)
    {
        await using var tx = await db.Database.BeginTransactionAsync();
        var p = await KhoaPhieuLuuTamAsync(ma);
        p.TrangThai = TrangThaiPhieu.DaHuy;
        await db.SaveChangesAsync();
        if (p.MaDonMua is not null)
            await donMua.CapNhatTrangThaiAsync(p.MaDonMua);
        nhatKy.Ghi("HuyPhieuNhap", nameof(PhieuNhap), ma, maKho: p.MaKho);
        await db.SaveChangesAsync();
        await tx.CommitAsync();
    }

    private async Task<List<HangNhan>> HangNhanAsync(string? maDonMua)
    {
        var q = db.BienThe.Where(b => b.TrangThai == TrangThaiSuDung.DangSuDung && b.SanPham.TrangThai == TrangThaiSuDung.DangSuDung);
        if (maDonMua is not null)
            q = q.Where(b => db.ChiTietDonMua.Any(c => c.MaDonMua == maDonMua && c.MaBienThe == b.MaBienThe));
        var hang = await q.OrderBy(b => b.MaBienThe)
            .Select(b => new { b.MaBienThe, b.MaVach, b.SanPham.Ten, b.ThuocTinh, b.SanPham.DonViTinh, b.SanPham.TheoDoiHanDung })
            .ToListAsync();
        if (maDonMua is null)
            return hang.Select(b => new HangNhan(b.MaBienThe, b.MaVach, DonMuaService.MoTaHang(b.Ten, b.ThuocTinh), b.DonViTinh, b.TheoDoiHanDung, null)).ToList();

        var dat = await db.ChiTietDonMua.Where(c => c.MaDonMua == maDonMua).ToDictionaryAsync(c => c.MaBienThe, c => c.SoLuongDat * c.HeSo);
        var nhan = await donMua.SoNhanAsync(maDonMua);
        return hang.Select(b =>
        {
            var n = nhan.GetValueOrDefault(b.MaBienThe);
            return new HangNhan(b.MaBienThe, b.MaVach, DonMuaService.MoTaHang(b.Ten, b.ThuocTinh), b.DonViTinh, b.TheoDoiHanDung,
                Math.Max(0, dat[b.MaBienThe] - n.DaNhan - n.DangNhan));
        }).ToList();
    }

    private async Task<PhieuNhap> KhoaPhieuLuuTamAsync(string ma)
    {
        await db.Database.ExecuteSqlAsync($"""SELECT 1 FROM "PhieuNhap" WHERE "MaPhieuNhap" = {ma} FOR UPDATE""");
        var p = await db.PhieuNhap.SingleOrDefaultAsync(x => x.MaPhieuNhap == ma)
            ?? throw new NghiepVuException($"Không có hóa đơn nhập {ma}.");
        await nguoiDung.DamBaoQuyenKhoAsync(p.MaKho);
        if (p.TrangThai != TrangThaiPhieu.LuuTam)
            throw new NghiepVuException($"Hóa đơn {ma} đã {TrangThaiHienThi.Lay(p.TrangThai).Ten.ToLowerInvariant()}, không sửa được.");
        return p;
    }

    private Task KhoaDonMuaAsync(string maDonMua) =>
        db.Database.ExecuteSqlAsync($"""SELECT 1 FROM "DonMua" WHERE "MaDonMua" = {maDonMua} FOR UPDATE""");
}
