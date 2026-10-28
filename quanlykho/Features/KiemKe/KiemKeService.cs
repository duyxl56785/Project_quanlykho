using Microsoft.EntityFrameworkCore;
using quanlykho.Common;
using quanlykho.Common.GiaoDien;
using quanlykho.Common.Loi;
using quanlykho.Common.PhanQuyen;
using quanlykho.Data;
using quanlykho.Domain;
using quanlykho.Features.DanhMuc;
using quanlykho.Features.Kho;
using quanlykho.Features.NhapKho;
using quanlykho.Features.XuatKho;
using static quanlykho.Common.GiaoDien.DinhDang;

namespace quanlykho.Features.KiemKe;

/// <summary>
/// Kiểm kê (mục 2.4, Hình 2.8, quy tắc 10): bắt đầu thì khóa ô trong phạm vi và chốt sổ sách; thành viên đếm, hệ thống tính chênh lệch.
/// Không sửa thẳng tồn: chênh lệch được phân loại và xử lý bằng chứng từ ở bước duyệt điều chỉnh.
/// </summary>
public class KiemKeService(AppDbContext db, NguoiDungHienTai nguoiDung, MaChungTu maChungTu, TonKhaDung tonKhaDung, CongGhiTon congGhiTon,
    GhiNhatKy nhatKy, TimeProvider gio)
{
    public const string LoaiChuyen = "ChuyenTrongKho";

    public async Task<TrangDuLieu<KiemKeDong>> DanhSachAsync(string? maKho, TrangThaiKiemKe? trangThai, int trang)
    {
        var kho = await nguoiDung.KhoDuocGiaoAsync();
        var q = db.PhieuKiemKe.Where(p => kho.Contains(p.MaKho));
        if (!string.IsNullOrWhiteSpace(maKho)) q = q.Where(p => p.MaKho == maKho);
        if (trangThai is not null) q = q.Where(p => p.TrangThai == trangThai);
        return await TrangDuLieu<KiemKeDong>.TaoAsync(q.OrderByDescending(p => p.NgayLap).ThenByDescending(p => p.MaPhieu)
            .Select(p => new KiemKeDong(p.MaPhieu, p.MaKho, p.PhamVi, p.NgayKiem, p.MucDich, p.ChiTiet.Count, p.TrangThai)), trang);
    }

    public async Task<KiemKeChiTiet> LayAsync(string ma)
    {
        var p = await db.PhieuKiemKe.AsNoTracking().Include(x => x.ChiTiet).Include(x => x.ThanhVien).SingleOrDefaultAsync(x => x.MaPhieu == ma)
            ?? throw new NghiepVuException($"Không có phiếu kiểm kê {ma}.");
        await nguoiDung.DamBaoQuyenKhoAsync(p.MaKho);
        var maHang = p.ChiTiet.Select(c => c.MaBienThe).Distinct().ToList();
        var hang = await db.BienThe.Where(b => maHang.Contains(b.MaBienThe))
            .Select(b => new { b.MaBienThe, b.SanPham.Ten, b.ThuocTinh, b.SanPham.DonViTinh }).ToDictionaryAsync(b => b.MaBienThe);
        var donVi = await new QuyDoiDonVi(db).BangAsync(maHang);
        var maLo = p.ChiTiet.Where(c => c.MaLo != null).Select(c => c.MaLo!).Distinct().ToList();
        var han = await db.Lo.Where(l => maLo.Contains(l.MaLo)).ToDictionaryAsync(l => l.MaLo, l => l.HanDung);
        var maNguoi = p.ThanhVien.Select(t => t.MaNguoiDung).Append(p.NguoiLap)
            .Concat(p.ChiTiet.SelectMany(c => new[] { c.NguoiDem, c.NguoiDuyet }).OfType<string>()).Distinct().ToList();
        var ten = await db.NguoiDung.Where(n => maNguoi.Contains(n.MaNguoiDung)).ToDictionaryAsync(n => n.MaNguoiDung, n => n.Ten);
        return new KiemKeChiTiet(p.MaPhieu, p.MaKho, await db.ViTri.Where(v => v.MaViTri == p.MaKho).Select(v => v.Ten).SingleAsync(), p.PhamVi, p.DoiTuong,
            p.MucDich, p.NgayKiem, ten[p.NguoiLap], p.NgayLap, p.ThoiDiemBatDau, p.ThoiDiemHoanThanh, p.TrangThai,
            p.ThanhVien.Select(t => new LuaChon(t.MaNguoiDung, ten[t.MaNguoiDung])).OrderBy(t => t.Ma).ToList(),
            p.ChiTiet.OrderBy(c => c.MaViTri, StringComparer.Ordinal).ThenBy(c => c.STT).Select(c => new DongKiemKe(c.STT, c.MaViTri, c.MaLo, c.MaBienThe,
                DonMuaService.MoTaHang(hang[c.MaBienThe].Ten, hang[c.MaBienThe].ThuocTinh), hang[c.MaBienThe].DonViTinh,
                c.MaLo is null ? null : han[c.MaLo], c.SoLuongSoSach, c.SoLuongThucTe, c.NgoaiDanhSach, c.NguoiDem is null ? null : ten[c.NguoiDem],
                c.LoaiChenhLech, c.KetLuan, c.NguoiDuyet is null ? null : ten[c.NguoiDuyet], c.MaChungTuXuLy, donVi[c.MaBienThe])).ToList(),
            await db.ViTri.Where(v => v.MaPhieuKiemKe == ma).OrderBy(v => v.MaViTri).Select(v => v.MaViTri).ToListAsync(),
            p.ThanhVien.Any(t => t.MaNguoiDung == nguoiDung.Ma),
            await db.ViTri.Where(v => v.MaKho == p.MaKho && v.Cap == CapViTri.CapCuoi).OrderBy(v => v.MaViTri).Select(v => v.MaViTri).ToListAsync());
    }

    public async Task<KiemKeForm> NapFormAsync(KiemKeForm form)
    {
        var kho = await nguoiDung.KhoDuocGiaoAsync();
        form.DanhSachKho = await db.ViTri.Where(v => kho.Contains(v.MaViTri) && !v.DangKhoa).OrderBy(v => v.MaViTri).Select(v => new LuaChon(v.MaViTri, v.Ten)).ToListAsync();
        if (string.IsNullOrEmpty(form.MaKho))
            form.MaKho = form.DanhSachKho.FirstOrDefault()?.Ma ?? "";
        form.NgayKiem ??= tonKhaDung.HomNay;
        if (!kho.Contains(form.MaKho))
            return form;
        form.DanhSachViTri = (await db.ViTri.Where(v => v.MaKho == form.MaKho && v.Cap > 1).OrderBy(v => v.MaViTri)
                .Select(v => new { v.MaViTri, v.Cap, v.Ten }).ToListAsync())
            .Select(v => new LuaChon(v.MaViTri, $"{v.MaViTri} · {CapViTri.Ten(v.Cap)} {v.Ten}")).ToList();
        form.DanhSachLo = (await db.TonKho.Where(t => t.SoLuong > 0)
                .Join(db.ViTri.Where(v => v.MaKho == form.MaKho), t => t.MaViTri, v => v.MaViTri, (t, v) => t.MaLo)
                .Join(db.Lo, m => m, l => l.MaLo, (m, l) => new { l.MaLo, l.MaBienThe, l.HanDung }).Distinct().OrderBy(l => l.MaLo).ToListAsync())
            .Select(l => new LuaChon(l.MaLo, $"{l.MaLo} · {l.MaBienThe}{(l.HanDung is null ? "" : $" · HSD {Ngay(l.HanDung)}")}")).ToList();
        form.DanhSachNguoi = await NguoiKiemAsync(form.MaKho);
        return form;
    }

    public async Task<string> TaoAsync(KiemKeForm form)
    {
        await nguoiDung.DamBaoQuyenKhoAsync(form.MaKho);
        if (!await db.ViTri.AnyAsync(v => v.MaViTri == form.MaKho && v.Cap == 1 && !v.DangKhoa))
            throw new NghiepVuException("Kho không có hoặc đã ngừng sử dụng.");
        var doiTuong = form.PhamVi == PhamViKiemKe.ToanKho ? [] : form.DoiTuong.Where(d => !string.IsNullOrWhiteSpace(d)).Select(d => d.Trim()).Distinct().ToList();
        if (form.PhamVi == PhamViKiemKe.ViTri)
        {
            if (doiTuong.Count == 0)
                throw new NghiepVuException("Chọn ít nhất một vị trí cần kiểm.");
            if (await db.ViTri.CountAsync(v => doiTuong.Contains(v.MaViTri) && v.MaKho == form.MaKho && v.Cap > 1) != doiTuong.Count)
                throw new NghiepVuException($"Có vị trí không thuộc kho {form.MaKho}.");
        }
        if (form.PhamVi == PhamViKiemKe.Lo)
        {
            if (doiTuong.Count == 0)
                throw new NghiepVuException("Chọn ít nhất một lô cần kiểm.");
            if (await db.Lo.CountAsync(l => doiTuong.Contains(l.MaLo)) != doiTuong.Count)
                throw new NghiepVuException("Có mã lô không tồn tại.");
        }
        var thanhVien = form.ThanhVien.Where(t => !string.IsNullOrWhiteSpace(t)).Distinct().ToList();
        if (thanhVien.Count == 0)
            throw new NghiepVuException("Chọn ít nhất một thành viên kiểm kê.");
        var hopLe = (await NguoiKiemAsync(form.MaKho)).Select(n => n.Ma).ToHashSet();
        if (thanhVien.FirstOrDefault(t => !hopLe.Contains(t)) is { } sai)
            throw new NghiepVuException($"{sai} không phải quản lý kho hay thủ kho đang hoạt động của kho {form.MaKho}.");

        var p = new PhieuKiemKe
        {
            MaPhieu = await maChungTu.TaoAsync("KK"), MaKho = form.MaKho, PhamVi = form.PhamVi, DoiTuong = doiTuong, MucDich = form.MucDich?.Trim(),
            NgayKiem = form.NgayKiem ?? tonKhaDung.HomNay, NguoiLap = nguoiDung.Ma, NgayLap = gio.GetUtcNow(), TrangThai = TrangThaiKiemKe.ChuaThucHien,
            ThanhVien = thanhVien.Select(t => new ThanhVienKiemKe { MaNguoiDung = t }).ToList(),
        };
        db.PhieuKiemKe.Add(p);
        nhatKy.Ghi("LapKiemKe", nameof(PhieuKiemKe), p.MaPhieu, new { phamVi = p.PhamVi.ToString(), doiTuong }, maKho: p.MaKho);
        await db.SaveChangesAsync();
        return p.MaPhieu;
    }

    /// <summary>
    /// Khóa mọi ô trong phạm vi (ô đã thuộc phiếu khác thì chặn; ô có hàng đang giữ thì phải xác nhận) và chốt số sổ sách.
    /// Từ đây cổng ghi tồn, xếp hàng, lấy hàng, chuyển kho đều từ chối ô này.
    /// </summary>
    public async Task BatDauAsync(string ma, bool xacNhanGiuHang)
    {
        await using var tx = await db.Database.BeginTransactionAsync();
        var p = await KhoaPhieuAsync(ma);
        if (p.TrangThai != TrangThaiKiemKe.ChuaThucHien)
            throw new NghiepVuException("Phiếu đã bắt đầu hoặc đã kết thúc.");

        var cacO = await CacOAsync(p);
        if (cacO.Count == 0)
            throw new NghiepVuException("Phạm vi kiểm không có ô nào.");
        if (await db.ViTri.Where(v => cacO.Contains(v.MaViTri) && v.MaPhieuKiemKe != null).Select(v => new { v.MaViTri, v.MaPhieuKiemKe }).FirstOrDefaultAsync() is { } ban)
            throw new NghiepVuException($"Ô {ban.MaViTri} đang thuộc phiếu kiểm kê {ban.MaPhieuKiemKe}.");
        var oGiu = await db.TonKho.Where(t => cacO.Contains(t.MaViTri) && t.SoLuongDaGiu > 0).Select(t => t.MaViTri).Distinct().OrderBy(o => o).ToListAsync();
        if (oGiu.Count > 0 && !xacNhanGiuHang)
            throw new NghiepVuException($"Có {oGiu.Count} ô đang giữ hàng cho đơn chưa xuất ({string.Join(", ", oGiu.Take(3))}{(oGiu.Count > 3 ? ", …" : "")}); "
                + "khóa thì lệnh xuất không lấy được ở các ô này cho tới khi kiểm xong. Xác nhận để tiếp tục.", new { canXacNhan = true, oGiu });

        var daKhoa = await db.ViTri.Where(v => cacO.Contains(v.MaViTri) && v.MaPhieuKiemKe == null).ExecuteUpdateAsync(s => s.SetProperty(v => v.MaPhieuKiemKe, ma));
        if (daKhoa != cacO.Count)
            throw new XungDotException("Có ô vừa bị phiếu kiểm kê khác khóa, vui lòng thử lại.");

        var ton = db.TonKho.Where(t => cacO.Contains(t.MaViTri) && t.SoLuong > 0);
        if (p.PhamVi == PhamViKiemKe.Lo)
            ton = ton.Where(t => p.DoiTuong.Contains(t.MaLo));
        var soSach = await ton.Join(db.Lo, t => t.MaLo, l => l.MaLo, (t, l) => new { t.MaViTri, t.MaLo, l.MaBienThe, t.SoLuong })
            .OrderBy(x => x.MaViTri).ThenBy(x => x.MaLo).ToListAsync();
        p.ChiTiet = soSach.Select((x, i) => new ChiTietKiemKe
        {
            STT = i + 1, MaViTri = x.MaViTri, MaLo = x.MaLo, MaBienThe = x.MaBienThe, SoLuongSoSach = x.SoLuong,
        }).ToList();
        p.TrangThai = TrangThaiKiemKe.DangKiemKe;
        p.ThoiDiemBatDau = gio.GetUtcNow();
        nhatKy.Ghi("BatDauKiemKe", nameof(PhieuKiemKe), ma, new { soO = cacO.Count, soDong = p.ChiTiet.Count, xacNhanGiuHang = oGiu.Count > 0 }, maKho: p.MaKho);
        await db.SaveChangesAsync();
        await tx.CommitAsync();
    }

    /// <summary>Thành viên nhập số đếm (đơn vị tính chính, không âm); bỏ trống thì giữ nguyên.</summary>
    public async Task NhapSoDemAsync(string ma, IReadOnlyCollection<DongDemForm> dem)
    {
        await using var tx = await db.Database.BeginTransactionAsync();
        var p = await KhoaPhieuThanhVienAsync(ma);
        var theoStt = p.ChiTiet.ToDictionary(c => c.STT);
        var donVi = await new QuyDoiDonVi(db).BangAsync(p.ChiTiet.Select(c => c.MaBienThe));
        foreach (var d in dem.Where(d => d.SoLuongThucTe is not null || d.SoLon is not null))
        {
            var c = theoStt.GetValueOrDefault(d.STT) ?? throw new NghiepVuException($"Không có dòng {d.STT}.");
            if (d.SoLuongThucTe < 0)
                throw new NghiepVuException($"Dòng {d.STT}: số đếm không được âm.");
            var thucTe = QuyDoiDonVi.CongDonViLon(donVi[c.MaBienThe], d.SoLon ?? 0, d.SoLuongThucTe ?? 0, c.MaBienThe);
            if (c.SoLuongThucTe == thucTe)
                continue;
            if (c.NguoiDuyet is not null)
                throw new NghiepVuException($"Dòng {d.STT} đã duyệt điều chỉnh, không sửa số đếm.");
            (c.SoLuongThucTe, c.NguoiDem, c.ThoiDiemDem) = (thucTe, nguoiDung.Ma, gio.GetUtcNow());
        }
        await db.SaveChangesAsync();
        await tx.CommitAsync();
    }

    /// <summary>Đếm thấy hàng không có trong sổ sách của ô: thêm dòng với sổ sách 0.</summary>
    public async Task<int> ThemDongAsync(string ma, ThemDongKiemKe yc)
    {
        if (yc.SoLuongThucTe < 0)
            throw new NghiepVuException("Số đếm không được âm.");
        await using var tx = await db.Database.BeginTransactionAsync();
        var p = await KhoaPhieuThanhVienAsync(ma);
        if (!await db.ViTri.AnyAsync(v => v.MaViTri == yc.MaViTri && v.MaPhieuKiemKe == ma))
            throw new NghiepVuException($"Ô {yc.MaViTri} không thuộc phạm vi đang kiểm của phiếu {ma}.");

        var maQuet = yc.Ma.Trim();
        var lo = await db.Lo.Where(l => l.MaLo == maQuet).Select(l => new { l.MaLo, l.MaBienThe }).SingleOrDefaultAsync();
        var bienThe = lo?.MaBienThe ?? await db.BienThe.Where(b => b.MaBienThe == maQuet || b.MaVach == maQuet).Select(b => b.MaBienThe).FirstOrDefaultAsync()
            ?? throw new NghiepVuException($"Mã {maQuet} không phải lô hay hàng nào.");
        if (p.ChiTiet.Any(c => c.MaViTri == yc.MaViTri && c.MaLo == lo?.MaLo && c.MaBienThe == bienThe))
            throw new NghiepVuException("Hàng này đã có dòng ở ô này; nhập số đếm vào dòng đó.");

        var stt = p.ChiTiet.Select(c => c.STT).DefaultIfEmpty().Max() + 1;
        p.ChiTiet.Add(new ChiTietKiemKe
        {
            STT = stt, MaViTri = yc.MaViTri, MaLo = lo?.MaLo, MaBienThe = bienThe, SoLuongSoSach = 0,
            SoLuongThucTe = QuyDoiDonVi.CongDonViLon(await new QuyDoiDonVi(db).DonViAsync(bienThe), yc.SoLon, yc.SoLuongThucTe, bienThe),
            NgoaiDanhSach = true, NguoiDem = nguoiDung.Ma, ThoiDiemDem = gio.GetUtcNow(),
        });
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return stt;
    }

    /// <summary>Hoàn thành khi mọi dòng đã đếm và không còn chênh lệch; mở khóa ô.</summary>
    public async Task HoanThanhAsync(string ma)
    {
        await using var tx = await db.Database.BeginTransactionAsync();
        var p = await KhoaPhieuAsync(ma);
        if (p.TrangThai != TrangThaiKiemKe.DangKiemKe)
            throw new NghiepVuException("Phiếu không ở trạng thái Đang kiểm kê.");
        var chuaDem = p.ChiTiet.Count(c => c.SoLuongThucTe is null);
        if (chuaDem > 0)
            throw new NghiepVuException($"Còn {chuaDem} dòng chưa đếm.");
        var lech = p.ChiTiet.Count(ChoXuLy);
        if (lech > 0)
            throw new NghiepVuException($"Còn {lech} dòng chênh lệch; phân loại và duyệt điều chỉnh trước khi hoàn thành.");
        await KetThucAsync(p);
        await db.SaveChangesAsync();
        await tx.CommitAsync();
    }

    /// <summary>
    /// Quản lý kho phân loại, ghi kết luận và duyệt các dòng chênh lệch; một transaction sinh chứng từ qua cổng ghi tồn
    /// (ô đang khóa chỉ cho chính phiếu này ghi): thiếu thành phiếu xuất khác, thừa thành phiếu nhập khác, sai vị trí chuyển ô,
    /// hư hỏng và hết hạn chuyển sang ô cách ly, sai biến thể xuất lô sai rồi nhập lô mới cho biến thể đúng (cùng hạn dùng).
    /// Dòng thừa đối ứng của sai vị trí, sai biến thể không sinh chứng từ riêng nhưng phải khớp số với dòng thiếu tương ứng.
    /// Xử lý hết thì hoàn thành và mở khóa ô.
    /// </summary>
    public async Task<KetQuaGhi> DuyetDieuChinhAsync(string ma, IReadOnlyCollection<DongDuyetForm> duyet, string? khoaChongLap)
    {
        if (nguoiDung.VaiTro is not (VaiTro.QuanTriVien or VaiTro.QuanLyKho))
            throw new KhongCoQuyenException("Chỉ quản lý kho duyệt điều chỉnh tồn.");
        if (string.IsNullOrWhiteSpace(khoaChongLap) || khoaChongLap.Length > 48)
            throw new NghiepVuException("Thiếu khóa chống ghi lặp (tối đa 48 ký tự).");
        await using var tx = await db.Database.BeginTransactionAsync();
        var p = await KhoaPhieuAsync(ma);
        if (await db.SoBienDong.AnyAsync(s => s.MaKhoaChongLap.StartsWith(khoaChongLap + "-")))
            return KetQuaGhi.DaGhiTruocDo;
        if (p.TrangThai != TrangThaiKiemKe.DangKiemKe)
            throw new NghiepVuException("Phiếu không ở trạng thái Đang kiểm kê.");

        var dong = p.ChiTiet.ToDictionary(c => c.STT);
        var chon = new List<(ChiTietKiemKe C, DongDuyetForm D, decimal Lech)>();
        foreach (var d in duyet.Where(d => d.Loai is not null))
        {
            var c = dong.GetValueOrDefault(d.STT) ?? throw new NghiepVuException($"Không có dòng {d.STT}.");
            if (!ChoXuLy(c))
                throw new NghiepVuException($"Dòng {d.STT} không có chênh lệch chờ xử lý.");
            if (string.IsNullOrWhiteSpace(d.KetLuan))
                throw new NghiepVuException($"Dòng {d.STT}: ghi kết luận (lý do) chênh lệch.");
            var lech = c.SoLuongThucTe!.Value - c.SoLuongSoSach;
            var hopLe = d.Loai switch
            {
                LoaiChenhLech.Thieu or LoaiChenhLech.HuHong or LoaiChenhLech.HetHan => lech < 0,
                LoaiChenhLech.Thua => lech > 0,
                _ => true,
            };
            if (!hopLe)
                throw new NghiepVuException($"Dòng {d.STT}: loại {KiemKeChiTiet.TenLoai(d.Loai!.Value)} không hợp với chênh lệch {So(lech)}.");
            chon.Add((c, d, lech));
        }
        if (chon.Count == 0)
            throw new NghiepVuException("Chọn loại chênh lệch cho ít nhất một dòng.");

        var oKho = await db.ViTri.Where(v => v.MaKho == p.MaKho && v.Cap == CapViTri.CapCuoi)
            .Select(v => new { v.MaViTri, v.LoaiKhu, v.LaOMacDinh }).ToDictionaryAsync(v => v.MaViTri);
        foreach (var (c, d, lech) in chon.Where(x => x.Lech < 0 && x.D.Loai == LoaiChenhLech.SaiViTri))
        {
            d.MaViTriDung = d.MaViTriDung?.Trim();
            if (string.IsNullOrEmpty(d.MaViTriDung) || !oKho.ContainsKey(d.MaViTriDung) || d.MaViTriDung == c.MaViTri)
                throw new NghiepVuException($"Dòng {c.STT}: chọn ô (cấp 7) của kho {p.MaKho} nơi hàng thực sự nằm.");
        }
        foreach (var (c, d, lech) in chon.Where(x => x.Lech < 0 && x.D.Loai == LoaiChenhLech.SaiBienThe))
        {
            d.MaBienTheDung = d.MaBienTheDung?.Trim();
            if (string.IsNullOrEmpty(d.MaBienTheDung) || d.MaBienTheDung == c.MaBienThe || !await db.BienThe.AnyAsync(b => b.MaBienThe == d.MaBienTheDung))
                throw new NghiepVuException($"Dòng {c.STT}: nhập mã biến thể đúng (khác {c.MaBienThe}).");
        }
        // Dòng thừa đối ứng: hàng xuất hiện ở ô khác hoặc là biến thể khác, đã được dòng thiếu tương ứng chuyển, nhập đủ.
        foreach (var (c, d, lech) in chon.Where(x => x.Lech > 0 && x.D.Loai is LoaiChenhLech.SaiViTri or LoaiChenhLech.SaiBienThe))
        {
            var doiUng = d.Loai == LoaiChenhLech.SaiViTri
                ? chon.Where(x => x.Lech < 0 && x.D.Loai == LoaiChenhLech.SaiViTri && c.MaLo != null && x.C.MaLo == c.MaLo && x.D.MaViTriDung == c.MaViTri)
                : chon.Where(x => x.Lech < 0 && x.D.Loai == LoaiChenhLech.SaiBienThe && x.D.MaBienTheDung == c.MaBienThe && x.C.MaViTri == c.MaViTri);
            if (-doiUng.Sum(x => x.Lech) != lech)
                throw new NghiepVuException($"Dòng {c.STT}: hàng thừa {So(lech)} phải khớp với dòng thiếu {(d.Loai == LoaiChenhLech.SaiViTri ? "chuyển về ô này" : "khai sai biến thể ở ô này")} được duyệt cùng lúc.");
        }

        var thoiDiem = gio.GetUtcNow();
        var luuY = $"Điều chỉnh kiểm kê {ma}";
        var lo = await db.Lo.Where(l => p.ChiTiet.Select(c => c.MaLo).Contains(l.MaLo)).ToDictionaryAsync(l => l.MaLo);
        var maHang = chon.Select(x => x.C.MaBienThe).Concat(chon.Select(x => x.D.MaBienTheDung).OfType<string>()).Distinct().ToList();
        var hang = await db.BienThe.Where(b => maHang.Contains(b.MaBienThe))
            .Select(b => new { b.MaBienThe, b.SanPham.DonViTinh, b.SanPham.TheoDoiHanDung }).ToDictionaryAsync(b => b.MaBienThe);
        var chungTu = chon.ToDictionary(x => x.C.STT, _ => new List<string>());

        var xuat = chon.Where(x => x.D.Loai is LoaiChenhLech.Thieu || (x.D.Loai == LoaiChenhLech.SaiBienThe && x.Lech < 0)).ToList();
        if (xuat.Count > 0)
        {
            var px = new PhieuXuat
            {
                MaPhieuXuat = await maChungTu.TaoAsync("PX"), LoaiPhieu = LoaiPhieuXuat.XuatKhac, MaKho = p.MaKho, NguoiLap = nguoiDung.Ma,
                NguoiXacNhan = nguoiDung.Ma, NgayXuat = thoiDiem, TrangThai = TrangThaiPhieu.HoanTat, LyDo = luuY,
                ChiTiet = xuat.Select((x, i) => new ChiTietPhieuXuat
                {
                    STT = i + 1, MaLo = x.C.MaLo!, MaViTri = x.C.MaViTri, TenDonVi = hang[x.C.MaBienThe].DonViTinh, HeSo = 1,
                    SoLuongGoc = -x.Lech, SoLuong = -x.Lech,
                }).ToList(),
            };
            db.PhieuXuat.Add(px);
            await congGhiTon.GhiAsync(khoaChongLap + "-X", PhieuXuatService.LoaiChungTu, px.MaPhieuXuat,
                px.ChiTiet.Select(c => new DongGhiTon(c.MaLo, c.MaViTri, -c.SoLuong)).ToList(), maPhieuKiemKe: ma);
            xuat.ForEach(x => chungTu[x.C.STT].Add(px.MaPhieuXuat));
        }

        var nhap = chon.Where(x => x.D.Loai is LoaiChenhLech.Thua || (x.D.Loai == LoaiChenhLech.SaiBienThe && x.Lech < 0)).ToList();
        if (nhap.Count > 0)
        {
            var dongNhap = new List<(int Stt, string MaLo, string MaViTri, string BienThe, decimal SoLuong)>();
            foreach (var (c, d, lech) in nhap)
            {
                if (d.Loai == LoaiChenhLech.Thua && c.MaLo is not null)
                {
                    dongNhap.Add((c.STT, c.MaLo, c.MaViTri, c.MaBienThe, lech));
                    continue;
                }
                var bienThe = d.Loai == LoaiChenhLech.Thua ? c.MaBienThe : d.MaBienTheDung!;
                var hanDung = d.Loai == LoaiChenhLech.Thua ? d.HanDung : lo[c.MaLo!].HanDung;
                if (hang[bienThe].TheoDoiHanDung && hanDung is null)
                    throw new NghiepVuException($"Dòng {c.STT}: {bienThe} theo dõi hạn dùng, nhập hạn dùng cho lô mới.");
                var moi = new Lo { MaLo = await maChungTu.TaoAsync("L"), MaBienThe = bienThe, HanDung = hanDung, NgayNhap = thoiDiem };
                db.Lo.Add(moi);
                dongNhap.Add((c.STT, moi.MaLo, c.MaViTri, bienThe, Math.Abs(lech)));
            }
            var pn = new PhieuNhap
            {
                MaPhieuNhap = await maChungTu.TaoAsync("PN"), LoaiPhieu = LoaiPhieuNhap.NhapKhac, MaKho = p.MaKho, NguoiLap = nguoiDung.Ma,
                NguoiXacNhan = nguoiDung.Ma, NgayNhap = thoiDiem, TrangThai = TrangThaiPhieu.HoanTat, GhiChu = luuY,
                ChiTiet = dongNhap.Select((x, i) => new ChiTietPhieuNhap
                {
                    STT = i + 1, MaLo = x.MaLo, TenDonVi = hang[x.BienThe].DonViTinh, HeSo = 1, SoLuongGoc = x.SoLuong, SoLuongThucNhan = x.SoLuong,
                    DonGia = 0, ChatLuong = ChatLuong.Dat,
                }).ToList(),
            };
            db.PhieuNhap.Add(pn);
            // Cổng chèn dòng TonKho bằng SQL nên lô mới phải có trong CSDL trước.
            await db.SaveChangesAsync();
            await congGhiTon.GhiAsync(khoaChongLap + "-N", PhieuNhapService.LoaiChungTu, pn.MaPhieuNhap,
                dongNhap.Select(x => new DongGhiTon(x.MaLo, x.MaViTri, x.SoLuong)).ToList(), maPhieuKiemKe: ma);
            dongNhap.ForEach(x => chungTu[x.Stt].Add(pn.MaPhieuNhap));
        }

        var cachLy = oKho.Values.Where(v => v.LaOMacDinh && v.LoaiKhu == LoaiKhu.CachLy).Select(v => v.MaViTri).SingleOrDefault();
        foreach (var (c, d, lech) in chon.Where(x => x.Lech < 0 && x.D.Loai is LoaiChenhLech.SaiViTri or LoaiChenhLech.HuHong or LoaiChenhLech.HetHan))
        {
            var den = d.Loai == LoaiChenhLech.SaiViTri ? d.MaViTriDung!
                : cachLy ?? throw new NghiepVuException($"Kho {p.MaKho} chưa có ô mặc định khu cách ly.");
            var cv = new ChuyenViTri
            {
                MaChuyen = await maChungTu.TaoAsync("CV"), Loai = LoaiChuyenViTri.ChuyenTrongKho, MaKho = p.MaKho, MaLo = c.MaLo!,
                MaViTriDi = c.MaViTri, MaViTriDen = den, SoLuong = -lech, ThoiDiem = thoiDiem, NguoiThucHien = nguoiDung.Ma,
            };
            db.ChuyenViTri.Add(cv);
            await congGhiTon.GhiAsync($"{khoaChongLap}-C{c.STT}", LoaiChuyen, cv.MaChuyen,
                [new DongGhiTon(c.MaLo!, c.MaViTri, lech), new DongGhiTon(c.MaLo!, den, -lech)], maPhieuKiemKe: ma);
            chungTu[c.STT].Add(cv.MaChuyen);
        }

        foreach (var (c, d, _) in chon)
        {
            c.LoaiChenhLech = d.Loai;
            c.KetLuan = d.KetLuan!.Trim();
            c.MaViTriDung = d.Loai == LoaiChenhLech.SaiViTri ? d.MaViTriDung : null;
            c.MaBienTheDung = d.Loai == LoaiChenhLech.SaiBienThe ? d.MaBienTheDung : null;
            c.NguoiDuyet = nguoiDung.Ma;
            c.ThoiDiemDuyet = thoiDiem;
            c.MaChungTuXuLy = chungTu[c.STT].Count == 0 ? "Đối ứng" : string.Join(", ", chungTu[c.STT].Distinct());
        }
        nhatKy.Ghi("DuyetDieuChinhKiemKe", nameof(PhieuKiemKe), ma,
            chon.Select(x => new { x.C.STT, loai = x.D.Loai.ToString(), chenhLech = x.Lech, ketLuan = x.D.KetLuan, chungTu = x.C.MaChungTuXuLy }), maKho: p.MaKho);
        if (p.ChiTiet.All(c => c.SoLuongThucTe is not null && !ChoXuLy(c)))
            await KetThucAsync(p);
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return KetQuaGhi.DaGhi;
    }

    public async Task HuyAsync(string ma)
    {
        await using var tx = await db.Database.BeginTransactionAsync();
        var p = await KhoaPhieuAsync(ma);
        if (p.TrangThai is not (TrangThaiKiemKe.ChuaThucHien or TrangThaiKiemKe.DangKiemKe))
            throw new NghiepVuException("Phiếu đã kết thúc.");
        if (p.ChiTiet.Any(c => c.NguoiDuyet is not null))
            throw new NghiepVuException("Phiếu đã có dòng được duyệt điều chỉnh tồn; xử lý nốt các dòng còn lại để hoàn thành, không hủy.");
        await MoKhoaAsync(ma);
        p.TrangThai = TrangThaiKiemKe.DaHuy;
        nhatKy.Ghi("HuyKiemKe", nameof(PhieuKiemKe), ma, maKho: p.MaKho);
        await db.SaveChangesAsync();
        await tx.CommitAsync();
    }

    /// <summary>Quản lý kho, thủ kho đang hoạt động được giao kho này.</summary>
    public async Task<List<LuaChon>> NguoiKiemAsync(string maKho) =>
        (await db.NguoiDung.Where(n => (n.VaiTro == VaiTro.ThuKho || n.VaiTro == VaiTro.QuanLyKho) && n.TrangThai == TrangThaiTaiKhoan.DangHoatDong
                && db.NguoiDungKho.Any(k => k.MaNguoiDung == n.MaNguoiDung && k.MaKho == maKho))
            .OrderBy(n => n.MaNguoiDung).Select(n => new { n.MaNguoiDung, n.Ten, n.VaiTro }).ToListAsync())
        .Select(n => new LuaChon(n.MaNguoiDung, $"{n.Ten} ({Vai.TenHienThi(n.VaiTro)})")).ToList();

    private async Task<List<string>> CacOAsync(PhieuKiemKe p)
    {
        var o = db.ViTri.Where(v => v.MaKho == p.MaKho && v.Cap == CapViTri.CapCuoi);
        return p.PhamVi switch
        {
            PhamViKiemKe.ViTri => (await o.Select(v => v.MaViTri).ToListAsync())
                .Where(m => p.DoiTuong.Any(d => m == d || m.StartsWith(d + "-", StringComparison.Ordinal))).ToList(),
            PhamViKiemKe.Lo => await o.Where(v => db.TonKho.Any(t => t.MaViTri == v.MaViTri && t.SoLuong > 0 && p.DoiTuong.Contains(t.MaLo)))
                .Select(v => v.MaViTri).ToListAsync(),
            _ => await o.Select(v => v.MaViTri).ToListAsync(),
        };
    }

    private static bool ChoXuLy(ChiTietKiemKe c) => c.SoLuongThucTe is not null && c.SoLuongThucTe != c.SoLuongSoSach && c.NguoiDuyet is null;

    private async Task KetThucAsync(PhieuKiemKe p)
    {
        await MoKhoaAsync(p.MaPhieu);
        p.TrangThai = TrangThaiKiemKe.HoanThanh;
        p.ThoiDiemHoanThanh = gio.GetUtcNow();
        nhatKy.Ghi("HoanThanhKiemKe", nameof(PhieuKiemKe), p.MaPhieu, maKho: p.MaKho);
    }

    private Task<int> MoKhoaAsync(string ma) =>
        db.ViTri.Where(v => v.MaPhieuKiemKe == ma).ExecuteUpdateAsync(s => s.SetProperty(v => v.MaPhieuKiemKe, (string?)null));

    private async Task<PhieuKiemKe> KhoaPhieuAsync(string ma)
    {
        await db.Database.ExecuteSqlAsync($"""SELECT 1 FROM "PhieuKiemKe" WHERE "MaPhieu" = {ma} FOR UPDATE""");
        var p = await db.PhieuKiemKe.Include(x => x.ChiTiet).Include(x => x.ThanhVien).SingleOrDefaultAsync(x => x.MaPhieu == ma)
            ?? throw new NghiepVuException($"Không có phiếu kiểm kê {ma}.");
        await nguoiDung.DamBaoQuyenKhoAsync(p.MaKho);
        return p;
    }

    /// <summary>Chỉ thành viên đã khai báo nhập kết quả, khi phiếu đang kiểm kê.</summary>
    private async Task<PhieuKiemKe> KhoaPhieuThanhVienAsync(string ma)
    {
        var p = await KhoaPhieuAsync(ma);
        if (p.ThanhVien.All(t => t.MaNguoiDung != nguoiDung.Ma))
            throw new KhongCoQuyenException("Chỉ thành viên của phiếu kiểm kê mới nhập kết quả.");
        if (p.TrangThai != TrangThaiKiemKe.DangKiemKe)
            throw new NghiepVuException("Phiếu không ở trạng thái Đang kiểm kê.");
        return p;
    }
}
