using Microsoft.EntityFrameworkCore;
using quanlykho.Common;
using quanlykho.Common.GiaoDien;
using quanlykho.Common.Loi;
using quanlykho.Common.PhanQuyen;
using quanlykho.Data;
using quanlykho.Domain;
using quanlykho.Features.NhapKho;
using quanlykho.Features.XuatKho;
using static quanlykho.Common.GiaoDien.DinhDang;
using PhieuDieuChuyenEntity = quanlykho.Domain.PhieuDieuChuyen;

namespace quanlykho.Features.DieuChuyen;

/// <summary>
/// Chuyển kho hai bước (mục 2.5, Hình 2.7, quy tắc 6): kho đi lưu phiếu thì cổng ghi tồn trừ ở kho đi, phiếu Chờ nhập kho
/// (hàng đang vận chuyển, không nằm trong TonKho); kho đến nhận thì cổng cộng vào ô tiếp nhận mặc định, giữ nguyên mã lô.
/// </summary>
public class DieuChuyenService(AppDbContext db, NguoiDungHienTai nguoiDung, MaChungTu maChungTu, CongGhiTon congGhiTon, TonKhaDung tonKhaDung,
    GhiNhatKy nhatKy, TimeProvider gio)
{
    public const string LoaiXuat = "DieuChuyenXuat";
    public const string LoaiNhan = "DieuChuyenNhan";
    public const string LoaiHuy = "HuyDieuChuyen";
    public const string LoaiXuLy = "XuLyChenhLechDieuChuyen";

    public async Task<TrangDuLieu<DieuChuyenDong>> DanhSachAsync(string? maKho, TrangThaiDieuChuyen? trangThai, int trang)
    {
        var q = await PhamViAsync();
        if (!string.IsNullOrWhiteSpace(maKho)) q = q.Where(p => p.MaKhoDi == maKho || p.MaKhoDen == maKho);
        if (trangThai is not null) q = q.Where(p => p.TrangThai == trangThai);
        return await TrangDuLieu<DieuChuyenDong>.TaoAsync(q.OrderByDescending(p => p.NgayLap).ThenByDescending(p => p.MaPhieu)
            .Select(p => new DieuChuyenDong(p.MaPhieu, p.MaKhoDi, p.MaKhoDen, p.NgayChuyen, p.PhuongTien, p.ChiTiet.Count, p.TrangThai)), trang);
    }

    /// <summary>Số phiếu đang chờ kho của người dùng nhận (hiện trên menu).</summary>
    public async Task<int> DemChoNhanAsync()
    {
        var kho = await nguoiDung.KhoDuocGiaoAsync();
        return await db.PhieuDieuChuyen.CountAsync(p => p.TrangThai == TrangThaiDieuChuyen.ChoNhapKho && kho.Contains(p.MaKhoDen));
    }

    public async Task<DieuChuyenChiTiet> LayAsync(string ma)
    {
        var p = await (await PhamViAsync()).AsNoTracking().Include(x => x.ChiTiet).SingleOrDefaultAsync(x => x.MaPhieu == ma)
            ?? throw new NghiepVuException($"Không có phiếu điều chuyển {ma} hoặc bạn không được xem phiếu này.");
        var maLo = p.ChiTiet.Select(c => c.MaLo).ToList();
        var lo = await db.Lo.Where(l => maLo.Contains(l.MaLo))
            .Select(l => new { l.MaLo, l.MaBienThe, l.HanDung, l.BienThe.SanPham.Ten, l.BienThe.ThuocTinh }).ToDictionaryAsync(l => l.MaLo);
        var tenKho = await db.ViTri.Where(v => v.MaViTri == p.MaKhoDi || v.MaViTri == p.MaKhoDen).ToDictionaryAsync(v => v.MaViTri, v => v.Ten);
        var tenNguoi = await db.NguoiDung.Where(n => n.MaNguoiDung == p.NguoiLap || n.MaNguoiDung == p.NguoiNhan).ToDictionaryAsync(n => n.MaNguoiDung, n => n.Ten);
        var kho = await nguoiDung.KhoDuocGiaoAsync();
        return new DieuChuyenChiTiet(p.MaPhieu, p.MaKhoDi, tenKho[p.MaKhoDi], p.MaKhoDen, tenKho[p.MaKhoDen], p.NgayChuyen, p.PhuongTien, p.NguoiVanChuyen,
            p.LyDo, tenNguoi[p.NguoiLap], p.NgayLap, p.ThoiDiemXuat, p.NguoiNhan is null ? null : tenNguoi[p.NguoiNhan], p.ThoiDiemNhan, p.TrangThai,
            p.ChiTiet.OrderBy(c => c.STT).Select(c => new DongDieuChuyen(c.STT, c.MaLo, lo[c.MaLo].MaBienThe, DonMuaService.MoTaHang(lo[c.MaLo].Ten, lo[c.MaLo].ThuocTinh),
                c.MaViTriDi, lo[c.MaLo].HanDung, c.TenDonVi, c.SoLuong, c.SoLuongNhan, c.CachXuLy, c.LyDoXuLy)).ToList(),
            kho.Contains(p.MaKhoDi), kho.Contains(p.MaKhoDen));
    }

    public async Task<DieuChuyenForm> NapFormAsync(DieuChuyenForm form)
    {
        var kho = await nguoiDung.KhoDuocGiaoAsync();
        var khoDung = await db.ViTri.Where(v => v.Cap == 1 && !v.DangKhoa).OrderBy(v => v.MaViTri).Select(v => new LuaChon(v.MaViTri, v.Ten)).ToListAsync();
        form.DanhSachKhoDi = khoDung.Where(k => kho.Contains(k.Ma)).ToList();
        if (string.IsNullOrEmpty(form.MaKhoDi))
            form.MaKhoDi = form.DanhSachKhoDi.FirstOrDefault()?.Ma ?? "";
        form.DanhSachKhoDen = khoDung.Where(k => k.Ma != form.MaKhoDi).ToList();
        form.NgayChuyen ??= tonKhaDung.HomNay;
        if (!kho.Contains(form.MaKhoDi))
            return form;
        form.DanhSachTon = (await tonKhaDung.TruyVan().Where(d => d.MaKho == form.MaKhoDi)
                .Join(db.BienThe, d => d.MaBienThe, b => b.MaBienThe, (d, b) => new { d.MaLo, d.MaViTri, d.MaBienThe, d.HanDung, d.KhaDung, b.SanPham.DonViTinh })
                .OrderBy(x => x.MaBienThe).ThenBy(x => x.MaViTri).ToListAsync())
            .Select(x => new LuaChon($"{x.MaLo}|{x.MaViTri}",
                $"{x.MaBienThe} · {x.MaLo}{(x.HanDung is null ? "" : $" · HSD {Ngay(x.HanDung)}")} · {x.MaViTri} · khả dụng {So(x.KhaDung)} {x.DonViTinh}"))
            .ToList();
        return form;
    }

    public async Task<DieuChuyenForm> LayFormAsync(string ma)
    {
        var p = await db.PhieuDieuChuyen.AsNoTracking().Include(x => x.ChiTiet).SingleOrDefaultAsync(x => x.MaPhieu == ma)
            ?? throw new NghiepVuException($"Không có phiếu điều chuyển {ma}.");
        await nguoiDung.DamBaoQuyenKhoAsync(p.MaKhoDi);
        if (p.TrangThai != TrangThaiDieuChuyen.LuuTam)
            throw new NghiepVuException("Chỉ sửa được phiếu lưu tạm.");
        return await NapFormAsync(new DieuChuyenForm
        {
            Ma = p.MaPhieu, MaKhoDi = p.MaKhoDi, MaKhoDen = p.MaKhoDen, NgayChuyen = p.NgayChuyen, PhuongTien = p.PhuongTien,
            NguoiVanChuyen = p.NguoiVanChuyen, LyDo = p.LyDo,
            Dong = p.ChiTiet.OrderBy(c => c.STT).Select(c => new DongDieuChuyenForm { LoO = $"{c.MaLo}|{c.MaViTriDi}", SoLuong = c.SoLuong }).ToList(),
        });
    }

    /// <summary>Lưu tạm, hoặc lưu luôn (xuất khỏi kho đi) khi luu = true.</summary>
    public async Task<string> TaoAsync(DieuChuyenForm form, bool luu)
    {
        var dong = await KiemTraAsync(form);
        await using var tx = await db.Database.BeginTransactionAsync();
        var p = new PhieuDieuChuyenEntity
        {
            MaPhieu = await maChungTu.TaoAsync("DC"), MaKhoDi = form.MaKhoDi, MaKhoDen = form.MaKhoDen, NgayChuyen = form.NgayChuyen ?? tonKhaDung.HomNay,
            PhuongTien = form.PhuongTien?.Trim(), NguoiVanChuyen = form.NguoiVanChuyen?.Trim(), LyDo = form.LyDo?.Trim(),
            NguoiLap = nguoiDung.Ma, NgayLap = gio.GetUtcNow(), TrangThai = TrangThaiDieuChuyen.LuuTam, ChiTiet = dong,
        };
        db.PhieuDieuChuyen.Add(p);
        await db.SaveChangesAsync();
        if (luu)
            await XuatAsync(p, form.KhoaChongLap);
        await tx.CommitAsync();
        return p.MaPhieu;
    }

    public async Task SuaAsync(string ma, DieuChuyenForm form, bool luu)
    {
        var dong = await KiemTraAsync(form);
        await using var tx = await db.Database.BeginTransactionAsync();
        var p = await KhoaPhieuAsync(ma, laKhoDi: true);
        if (p.TrangThai != TrangThaiDieuChuyen.LuuTam)
            throw new NghiepVuException("Chỉ sửa được phiếu lưu tạm.");
        (p.MaKhoDi, p.MaKhoDen, p.NgayChuyen, p.PhuongTien, p.NguoiVanChuyen, p.LyDo) =
            (form.MaKhoDi, form.MaKhoDen, form.NgayChuyen ?? p.NgayChuyen, form.PhuongTien?.Trim(), form.NguoiVanChuyen?.Trim(), form.LyDo?.Trim());
        db.ChiTietDieuChuyen.RemoveRange(p.ChiTiet);
        await db.SaveChangesAsync();
        p.ChiTiet = dong;
        await db.SaveChangesAsync();
        if (luu)
            await XuatAsync(p, form.KhoaChongLap);
        await tx.CommitAsync();
    }

    /// <summary>Lưu phiếu lưu tạm: trừ tồn kho đi. Gửi lặp cùng khóa không trừ hai lần.</summary>
    public async Task<KetQuaGhi> LuuAsync(string ma, string? khoaChongLap)
    {
        await using var tx = await db.Database.BeginTransactionAsync();
        var p = await KhoaPhieuAsync(ma, laKhoDi: true);
        if (p.TrangThai != TrangThaiDieuChuyen.LuuTam)
            return await DaGhiAsync(khoaChongLap, LoaiXuat, ma) ? KetQuaGhi.DaGhiTruocDo : throw new NghiepVuException("Phiếu đã lưu hoặc đã hủy.");
        await XuatAsync(p, khoaChongLap);
        await tx.CommitAsync();
        return KetQuaGhi.DaGhi;
    }

    /// <summary>
    /// Thủ kho kho đến nhập số thực nhận từng dòng; hàng vào ô tiếp nhận mặc định, rồi xếp bằng màn hình xếp hàng.
    /// Nhận thiếu thì phiếu Nhận một phần, phần thiếu là chênh lệch chờ xử lý.
    /// </summary>
    public async Task<KetQuaGhi> NhanAsync(string ma, IReadOnlyCollection<DongNhanForm> nhan, string? khoaChongLap)
    {
        KiemTraKhoa(khoaChongLap);
        await using var tx = await db.Database.BeginTransactionAsync();
        var p = await KhoaPhieuAsync(ma, laKhoDi: false);
        if (p.TrangThai != TrangThaiDieuChuyen.ChoNhapKho)
            return await DaGhiAsync(khoaChongLap, LoaiNhan, ma) ? KetQuaGhi.DaGhiTruocDo : throw new NghiepVuException("Phiếu không ở trạng thái Chờ nhập kho.");

        var so = nhan.GroupBy(n => n.STT).ToDictionary(g => g.Key, g => g.Last().SoLuongNhan);
        foreach (var c in p.ChiTiet)
        {
            if (!so.TryGetValue(c.STT, out var sl))
                throw new NghiepVuException($"Nhập số thực nhận cho dòng {c.STT}.");
            if (sl < 0 || sl > c.SoLuong)
                throw new NghiepVuException($"Dòng {c.STT}: số thực nhận từ 0 đến {So(c.SoLuong)}.");
            c.SoLuongNhan = sl;
        }
        var tiepNhan = await db.ViTri.Where(v => v.MaKho == p.MaKhoDen && v.LaOMacDinh && v.LoaiKhu == LoaiKhu.TiepNhan).Select(v => v.MaViTri).SingleOrDefaultAsync()
            ?? throw new NghiepVuException($"Kho {p.MaKhoDen} chưa có ô mặc định khu tiếp nhận.");

        p.TrangThai = p.ChiTiet.All(c => c.SoLuongNhan == c.SoLuong) ? TrangThaiDieuChuyen.HoanThanh : TrangThaiDieuChuyen.NhanMotPhan;
        p.NguoiNhan = nguoiDung.Ma;
        p.ThoiDiemNhan = gio.GetUtcNow();
        nhatKy.Ghi("NhanDieuChuyen", nameof(PhieuDieuChuyenEntity), ma,
            new { chenhLech = p.ChiTiet.Where(c => c.SoLuongNhan < c.SoLuong).Select(c => new { c.STT, c.MaLo, thieu = c.SoLuong - c.SoLuongNhan }) }, maKho: p.MaKhoDen);
        var cong = p.ChiTiet.Where(c => c.SoLuongNhan > 0).Select(c => new DongGhiTon(c.MaLo, tiepNhan, c.SoLuongNhan!.Value)).ToList();
        if (cong.Count == 0)
            await db.SaveChangesAsync();
        else if (await congGhiTon.GhiAsync(khoaChongLap!, LoaiNhan, ma, cong) == KetQuaGhi.DaGhiTruocDo)
            throw new XungDotException("Thao tác vừa được ghi bởi một yêu cầu khác, vui lòng tải lại trang.");
        await tx.CommitAsync();
        return KetQuaGhi.DaGhi;
    }

    /// <summary>
    /// Xử lý phần nhận thiếu của một dòng (quản lý kho, có lý do): nhập tại kho đến (tìm thấy hàng, cộng vào ô tiếp nhận kho đến),
    /// nhập lại kho đi (hàng chưa đi, cộng trả đúng ô cũ) hoặc ghi giảm hao hụt (hàng mất, chỉ đóng dòng; tồn kho đi đã trừ lúc lưu).
    /// Mọi dòng đủ hoặc đã xử lý thì phiếu Hoàn thành.
    /// </summary>
    public async Task<KetQuaGhi> XuLyChenhLechAsync(string ma, XuLyChenhLechYeuCau yc, string? khoaChongLap)
    {
        if (nguoiDung.VaiTro is not (VaiTro.QuanTriVien or VaiTro.QuanLyKho))
            throw new KhongCoQuyenException("Chỉ quản lý kho xử lý chênh lệch điều chuyển.");
        KiemTraKhoa(khoaChongLap);
        if (string.IsNullOrWhiteSpace(yc.LyDo))
            throw new NghiepVuException("Nhập lý do (nguyên nhân) chênh lệch.");
        await using var tx = await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlAsync($"""SELECT 1 FROM "PhieuDieuChuyen" WHERE "MaPhieu" = {ma} FOR UPDATE""");
        var p = await db.PhieuDieuChuyen.Include(x => x.ChiTiet).SingleOrDefaultAsync(x => x.MaPhieu == ma) ?? throw new NghiepVuException($"Không có phiếu điều chuyển {ma}.");
        var c = p.ChiTiet.SingleOrDefault(x => x.STT == yc.STT) ?? throw new NghiepVuException($"Không có dòng {yc.STT}.");
        if (c.CachXuLy is not null)
            return await DaGhiAsync(khoaChongLap, LoaiXuLy, ma) || c.CachXuLy == XuLyChenhLech.GhiGiamHaoHut ? KetQuaGhi.DaGhiTruocDo
                : throw new NghiepVuException($"Dòng {yc.STT} đã xử lý.");
        if (p.TrangThai != TrangThaiDieuChuyen.NhanMotPhan || c.SoLuongNhan is null || c.SoLuongNhan >= c.SoLuong)
            throw new NghiepVuException($"Dòng {yc.STT} không có chênh lệch chờ xử lý.");
        var thieu = c.SoLuong - c.SoLuongNhan.Value;

        await nguoiDung.DamBaoQuyenKhoAsync(yc.CachXuLy == XuLyChenhLech.NhapLaiKhoDi ? p.MaKhoDi : yc.CachXuLy == XuLyChenhLech.NhapKhoDen ? p.MaKhoDen
            : (await nguoiDung.KhoDuocGiaoAsync()).Contains(p.MaKhoDi) ? p.MaKhoDi : p.MaKhoDen);
        var o = yc.CachXuLy switch
        {
            XuLyChenhLech.NhapKhoDen => await db.ViTri.Where(v => v.MaKho == p.MaKhoDen && v.LaOMacDinh && v.LoaiKhu == LoaiKhu.TiepNhan).Select(v => v.MaViTri).SingleOrDefaultAsync()
                ?? throw new NghiepVuException($"Kho {p.MaKhoDen} chưa có ô mặc định khu tiếp nhận."),
            XuLyChenhLech.NhapLaiKhoDi => c.MaViTriDi,
            _ => null,
        };
        (c.CachXuLy, c.LyDoXuLy, c.NguoiXuLy, c.ThoiDiemXuLy) = (yc.CachXuLy, yc.LyDo.Trim(), nguoiDung.Ma, gio.GetUtcNow());
        if (p.ChiTiet.All(x => x.SoLuongNhan == x.SoLuong || x.CachXuLy is not null))
            p.TrangThai = TrangThaiDieuChuyen.HoanThanh;
        nhatKy.Ghi("XuLyChenhLechDieuChuyen", nameof(PhieuDieuChuyenEntity), ma,
            new { c.STT, c.MaLo, thieu, cach = yc.CachXuLy.ToString(), lyDo = c.LyDoXuLy }, maKho: o is null ? p.MaKhoDi : yc.CachXuLy == XuLyChenhLech.NhapKhoDen ? p.MaKhoDen : p.MaKhoDi);
        if (o is null)
            await db.SaveChangesAsync();
        else if (await congGhiTon.GhiAsync(khoaChongLap!, LoaiXuLy, ma, [new DongGhiTon(c.MaLo, o, thieu)]) == KetQuaGhi.DaGhiTruocDo)
            throw new XungDotException("Thao tác vừa được ghi bởi một yêu cầu khác, vui lòng tải lại trang.");
        await tx.CommitAsync();
        return KetQuaGhi.DaGhi;
    }

    /// <summary>Hủy khi chưa nhận; phiếu đã lưu thì cộng trả hàng về đúng ô ở kho đi.</summary>
    public async Task HuyAsync(string ma)
    {
        await using var tx = await db.Database.BeginTransactionAsync();
        var p = await KhoaPhieuAsync(ma, laKhoDi: true);
        if (p.TrangThai is not (TrangThaiDieuChuyen.LuuTam or TrangThaiDieuChuyen.ChoNhapKho))
            throw new NghiepVuException("Chỉ hủy được phiếu chưa nhận.");
        if (p.TrangThai == TrangThaiDieuChuyen.ChoNhapKho)
            await congGhiTon.GhiAsync($"{ma}-HUY", LoaiHuy, ma, p.ChiTiet.Select(c => new DongGhiTon(c.MaLo, c.MaViTriDi, c.SoLuong)).ToList());
        p.TrangThai = TrangThaiDieuChuyen.DaHuy;
        nhatKy.Ghi("HuyDieuChuyen", nameof(PhieuDieuChuyenEntity), ma, maKho: p.MaKhoDi);
        await db.SaveChangesAsync();
        await tx.CommitAsync();
    }

    /// <summary>Trừ tồn kho đi trong transaction của người gọi. Chỉ chuyển hàng khả dụng (khu lưu trữ, chưa hết hạn, chưa giữ, không kiểm kê).</summary>
    private async Task XuatAsync(PhieuDieuChuyenEntity p, string? khoaChongLap)
    {
        if (nguoiDung.VaiTro is not (VaiTro.QuanTriVien or VaiTro.QuanLyKho))
            throw new KhongCoQuyenException("Chỉ quản lý kho của kho đi lưu phiếu điều chuyển.");
        KiemTraKhoa(khoaChongLap);
        var maLo = p.ChiTiet.Select(c => c.MaLo).ToList();
        var khaDung = await tonKhaDung.TruyVan().Where(d => d.MaKho == p.MaKhoDi && maLo.Contains(d.MaLo))
            .ToDictionaryAsync(d => (d.MaLo, d.MaViTri), d => d.KhaDung);
        foreach (var c in p.ChiTiet)
        {
            var con = khaDung.GetValueOrDefault((c.MaLo, c.MaViTriDi));
            if (c.SoLuong > con)
                throw new NghiepVuException($"Lô {c.MaLo} tại {c.MaViTriDi}: cần {So(c.SoLuong)}, còn khả dụng {So(con)}.");
        }
        p.TrangThai = TrangThaiDieuChuyen.ChoNhapKho;
        p.ThoiDiemXuat = gio.GetUtcNow();
        nhatKy.Ghi("XuatDieuChuyen", nameof(PhieuDieuChuyenEntity), p.MaPhieu, new { khoDen = p.MaKhoDen }, maKho: p.MaKhoDi);
        if (await congGhiTon.GhiAsync(khoaChongLap!, LoaiXuat, p.MaPhieu, p.ChiTiet.Select(c => new DongGhiTon(c.MaLo, c.MaViTriDi, -c.SoLuong)).ToList())
            == KetQuaGhi.DaGhiTruocDo)
            throw new XungDotException("Thao tác vừa được ghi bởi một yêu cầu khác, vui lòng tải lại trang.");
    }

    private async Task<List<ChiTietDieuChuyen>> KiemTraAsync(DieuChuyenForm form)
    {
        await nguoiDung.DamBaoQuyenKhoAsync(form.MaKhoDi);
        if (form.MaKhoDi == form.MaKhoDen)
            throw new NghiepVuException("Kho đến phải khác kho đi.");
        if (!await db.ViTri.AnyAsync(v => v.MaViTri == form.MaKhoDen && v.Cap == 1 && !v.DangKhoa))
            throw new NghiepVuException("Kho đến không có hoặc đã ngừng sử dụng.");
        if (!await db.ViTri.AnyAsync(v => v.MaKho == form.MaKhoDen && v.LaOMacDinh && v.LoaiKhu == LoaiKhu.TiepNhan))
            throw new NghiepVuException($"Kho {form.MaKhoDen} chưa có ô mặc định khu tiếp nhận để nhận hàng.");

        var dong = new List<(string MaLo, string MaViTri, decimal SoLuong)>();
        foreach (var d in form.Dong.Where(d => !string.IsNullOrWhiteSpace(d.LoO)))
        {
            var tach = d.LoO.Split('|');
            if (tach.Length != 2)
                throw new NghiepVuException("Dòng hàng không hợp lệ, chọn lại lô.");
            if (d.SoLuong <= 0)
                throw new NghiepVuException("Số lượng chuyển phải lớn hơn 0.");
            dong.Add((tach[0], tach[1], d.SoLuong));
        }
        if (dong.Count == 0)
            throw new NghiepVuException("Phiếu điều chuyển phải có ít nhất một dòng hàng.");
        if (dong.GroupBy(d => (d.MaLo, d.MaViTri)).FirstOrDefault(g => g.Count() > 1) is { } trung)
            throw new NghiepVuException($"Lô {trung.Key.MaLo} tại {trung.Key.MaViTri} xuất hiện nhiều dòng.");
        var maO = dong.Select(d => d.MaViTri).Distinct().ToList();
        if (await db.ViTri.CountAsync(v => maO.Contains(v.MaViTri) && v.MaKho == form.MaKhoDi) != maO.Count)
            throw new NghiepVuException($"Có ô không thuộc kho {form.MaKhoDi}.");
        var maLo = dong.Select(d => d.MaLo).Distinct().ToList();
        var donVi = await db.Lo.Where(l => maLo.Contains(l.MaLo)).Select(l => new { l.MaLo, l.BienThe.SanPham.DonViTinh }).ToDictionaryAsync(l => l.MaLo, l => l.DonViTinh);
        return dong.Select((d, i) => new ChiTietDieuChuyen
        {
            STT = i + 1, MaLo = d.MaLo, MaViTriDi = d.MaViTri, TenDonVi = donVi.GetValueOrDefault(d.MaLo) ?? throw new NghiepVuException($"Không có lô {d.MaLo}."),
            HeSo = 1, SoLuongGoc = d.SoLuong, SoLuong = d.SoLuong,
        }).ToList();
    }

    private async Task<IQueryable<PhieuDieuChuyenEntity>> PhamViAsync()
    {
        var kho = await nguoiDung.KhoDuocGiaoAsync();
        return db.PhieuDieuChuyen.Where(p => kho.Contains(p.MaKhoDi) || kho.Contains(p.MaKhoDen));
    }

    private async Task<PhieuDieuChuyenEntity> KhoaPhieuAsync(string ma, bool laKhoDi)
    {
        await db.Database.ExecuteSqlAsync($"""SELECT 1 FROM "PhieuDieuChuyen" WHERE "MaPhieu" = {ma} FOR UPDATE""");
        var p = await db.PhieuDieuChuyen.Include(x => x.ChiTiet).SingleOrDefaultAsync(x => x.MaPhieu == ma) ?? throw new NghiepVuException($"Không có phiếu điều chuyển {ma}.");
        await nguoiDung.DamBaoQuyenKhoAsync(laKhoDi ? p.MaKhoDi : p.MaKhoDen);
        return p;
    }

    private Task<bool> DaGhiAsync(string? khoa, string loai, string ma) =>
        db.SoBienDong.AnyAsync(s => s.MaKhoaChongLap == khoa + "#1" && s.LoaiChungTu == loai && s.MaChungTu == ma);

    private static void KiemTraKhoa(string? khoa)
    {
        if (string.IsNullOrWhiteSpace(khoa) || khoa.Length > 56)
            throw new NghiepVuException("Thiếu khóa chống ghi lặp (Idempotency-Key, tối đa 56 ký tự).");
    }
}
