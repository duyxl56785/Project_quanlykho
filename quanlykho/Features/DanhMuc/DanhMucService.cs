using Microsoft.EntityFrameworkCore;
using quanlykho.Common;
using quanlykho.Common.GiaoDien;
using quanlykho.Common.Loi;
using quanlykho.Data;
using quanlykho.Domain;

namespace quanlykho.Features.DanhMuc;

public class DanhMucService(AppDbContext db, GhiNhatKy nhatKy, QuyDoiDonVi quyDoi)
{
    // ---------- Nhóm hàng, thuộc tính ----------

    public async Task<List<NhomCay>> CayNhomAsync()
    {
        var nhom = await db.NhomHang.AsNoTracking().OrderBy(n => n.Ten).ToListAsync();
        var thuocTinh = (await db.ThuocTinh.AsNoTracking().OrderBy(t => t.ThuTu).ToListAsync()).ToLookup(t => t.MaNhom);
        var soSanPham = await db.SanPham.GroupBy(s => s.MaNhom).Select(g => new { g.Key, SoLuong = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.SoLuong);
        var con = nhom.ToLookup(n => n.MaCha);

        var ketQua = new List<NhomCay>();
        void Duyet(string? maCha, int doSau)
        {
            foreach (var n in con[maCha])
            {
                ketQua.Add(new NhomCay(n.MaNhom, n.Ten, n.MaCha, doSau, thuocTinh[n.MaNhom].ToList(), soSanPham.GetValueOrDefault(n.MaNhom)));
                Duyet(n.MaNhom, doSau + 1);
            }
        }
        Duyet(null, 0);
        return ketQua;
    }

    public async Task<NhomForm> LayNhomAsync(string ma)
    {
        var n = await TimNhom(ma);
        return new NhomForm { Ma = n.MaNhom, Ten = n.Ten, MaCha = n.MaCha };
    }

    public async Task ThemNhomAsync(NhomForm form)
    {
        var ma = form.Ma.Trim().ToUpperInvariant();
        if (await db.NhomHang.AnyAsync(n => n.MaNhom == ma))
            throw new NghiepVuException($"Mã nhóm {ma} đã có.");
        await KiemTraNhomCha(ma, form.MaCha);
        db.NhomHang.Add(new NhomHang { MaNhom = ma, Ten = form.Ten.Trim(), MaCha = NullNeuRong(form.MaCha) });
        await db.SaveChangesAsync();
    }

    public async Task SuaNhomAsync(string ma, NhomForm form)
    {
        var n = await TimNhom(ma);
        await KiemTraNhomCha(ma, form.MaCha);
        n.Ten = form.Ten.Trim();
        n.MaCha = NullNeuRong(form.MaCha);
        await db.SaveChangesAsync();
    }

    public async Task XoaNhomAsync(string ma)
    {
        var n = await TimNhom(ma);
        if (await db.SanPham.AnyAsync(s => s.MaNhom == ma))
            throw new NghiepVuException($"Nhóm {ma} đã có sản phẩm, không xóa được.");
        if (await db.NhomHang.AnyAsync(c => c.MaCha == ma))
            throw new NghiepVuException($"Nhóm {ma} còn nhóm con, không xóa được.");
        db.ThuocTinh.RemoveRange(db.ThuocTinh.Where(t => t.MaNhom == ma));
        db.NhomHang.Remove(n);
        await db.SaveChangesAsync();
    }

    public async Task LuuThuocTinhAsync(string maNhom, string ten, string? giaTriGoiY)
    {
        await TimNhom(maNhom);
        ten = ten.Trim();
        if (ten.Length is 0 or > 50)
            throw new NghiepVuException("Tên thuộc tính từ 1 đến 50 ký tự.");
        var goiY = TachGiaTri(giaTriGoiY).ToArray();
        var tt = await db.ThuocTinh.SingleOrDefaultAsync(t => t.MaNhom == maNhom && t.Ten == ten);
        if (tt is null)
        {
            var thuTu = await db.ThuocTinh.Where(t => t.MaNhom == maNhom).MaxAsync(t => (int?)t.ThuTu) ?? 0;
            db.ThuocTinh.Add(new ThuocTinh { MaNhom = maNhom, Ten = ten, ThuTu = thuTu + 1, GiaTriGoiY = goiY });
        }
        else
            tt.GiaTriGoiY = goiY;
        await db.SaveChangesAsync();
    }

    public async Task XoaThuocTinhAsync(string maNhom, string ten)
    {
        var tt = await db.ThuocTinh.SingleOrDefaultAsync(t => t.MaNhom == maNhom && t.Ten == ten)
            ?? throw new NghiepVuException($"Không có thuộc tính {ten}.");
        var bienThe = await db.BienThe.Where(b => b.SanPham.MaNhom == maNhom).Select(b => b.ThuocTinh).ToListAsync();
        if (bienThe.Any(t => t.ContainsKey(ten)))
            throw new NghiepVuException($"Thuộc tính {ten} đã dùng cho biến thể, không xóa được.");
        db.ThuocTinh.Remove(tt);
        await db.SaveChangesAsync();
    }

    private async Task<NhomHang> TimNhom(string ma) =>
        await db.NhomHang.SingleOrDefaultAsync(n => n.MaNhom == ma) ?? throw new NghiepVuException($"Không có nhóm {ma}.");

    private async Task KiemTraNhomCha(string ma, string? maCha)
    {
        maCha = NullNeuRong(maCha);
        var cha = maCha;
        while (cha is not null)
        {
            if (cha == ma)
                throw new NghiepVuException("Nhóm cha không được là chính nhóm này hoặc nhóm con của nó.");
            cha = (await db.NhomHang.AsNoTracking().SingleOrDefaultAsync(n => n.MaNhom == cha)
                ?? throw new NghiepVuException($"Không có nhóm {cha}.")).MaCha;
        }
    }

    // ---------- Sản phẩm, biến thể ----------

    public async Task<TrangDuLieu<SanPhamDong>> DanhSachSanPhamAsync(string? tuKhoa, string? maNhom, TrangThaiSuDung? trangThai, int trang)
    {
        var q = db.SanPham.AsQueryable();
        if (!string.IsNullOrWhiteSpace(tuKhoa))
        {
            var tk = $"%{tuKhoa.Trim()}%";
            q = q.Where(s => EF.Functions.ILike(s.MaSanPham, tk) || EF.Functions.ILike(s.Ten, tk)
                || db.BienThe.Any(b => b.MaSanPham == s.MaSanPham && (EF.Functions.ILike(b.MaBienThe, tk) || b.MaVach == tuKhoa.Trim())));
        }
        if (!string.IsNullOrWhiteSpace(maNhom)) q = q.Where(s => s.MaNhom == maNhom);
        if (trangThai is not null) q = q.Where(s => s.TrangThai == trangThai);

        return await TrangDuLieu<SanPhamDong>.TaoAsync(q.OrderBy(s => s.MaSanPham).Select(s => new SanPhamDong(
            s.MaSanPham, s.Ten, s.DonViTinh,
            db.NhomHang.Where(n => n.MaNhom == s.MaNhom).Select(n => n.Ten).First(),
            db.BienThe.Count(b => b.MaSanPham == s.MaSanPham),
            db.TonKho.Where(t => db.Lo.Any(l => l.MaLo == t.MaLo && l.BienThe.MaSanPham == s.MaSanPham)).Sum(t => (decimal?)t.SoLuong) ?? 0,
            s.TrangThai)), trang);
    }

    public async Task<SanPhamForm> LaySanPhamAsync(string ma)
    {
        var s = await TimSanPham(ma);
        return await NapDanhSach(new SanPhamForm
        {
            Ma = s.MaSanPham, Ten = s.Ten, DonViTinh = s.DonViTinh, MaNhom = s.MaNhom, DieuKienBaoQuan = s.DieuKienBaoQuan,
            TheoDoiHanDung = s.TheoDoiHanDung, MoTa = s.MoTa, TrangThai = s.TrangThai, LaSua = true,
        });
    }

    public async Task<SanPhamForm> NapDanhSach(SanPhamForm form)
    {
        form.DanhSachNhom = await CayNhomAsync();
        if (form.LaSua)
        {
            form.BienThe = await db.BienThe.AsNoTracking().Where(b => b.MaSanPham == form.Ma).OrderBy(b => b.MaBienThe).ToListAsync();
            form.ThuocTinhNhom = await db.ThuocTinh.AsNoTracking()
                .Where(t => t.MaNhom == db.SanPham.Where(s => s.MaSanPham == form.Ma).Select(s => s.MaNhom).First())
                .OrderBy(t => t.ThuTu).ToListAsync();
        }
        return form;
    }

    public async Task<string> ThemSanPhamAsync(SanPhamForm form)
    {
        var ma = form.Ma.Trim().ToUpperInvariant();
        if (await db.SanPham.AnyAsync(s => s.MaSanPham == ma))
            throw new NghiepVuException($"Mã hàng {ma} đã có.");
        await TimNhom(form.MaNhom);
        db.SanPham.Add(new SanPham
        {
            MaSanPham = ma, Ten = form.Ten.Trim(), DonViTinh = form.DonViTinh.Trim(), MaNhom = form.MaNhom,
            DieuKienBaoQuan = form.DieuKienBaoQuan, TheoDoiHanDung = form.TheoDoiHanDung, MoTa = form.MoTa?.Trim(),
            TrangThai = TrangThaiSuDung.DangSuDung,
        });
        await db.SaveChangesAsync();
        return ma;
    }

    public async Task SuaSanPhamAsync(string ma, SanPhamForm form)
    {
        var s = await TimSanPham(ma);
        var coBienThe = await db.BienThe.AnyAsync(b => b.MaSanPham == ma);
        if (s.MaNhom != form.MaNhom)
        {
            if (coBienThe)
                throw new NghiepVuException("Sản phẩm đã có biến thể theo thuộc tính của nhóm cũ, không đổi nhóm được.");
            await TimNhom(form.MaNhom);
        }
        if (s.TheoDoiHanDung != form.TheoDoiHanDung && await CoTonAsync(ma))
            throw new NghiepVuException("Sản phẩm đang có tồn, không đổi thiết lập theo dõi hạn dùng được.");
        if (s.DonViTinh != form.DonViTinh.Trim() && await db.Lo.AnyAsync(l => l.BienThe.MaSanPham == ma))
            throw new NghiepVuException("Sản phẩm đã có lô nhập, không đổi đơn vị tính chính được.");

        s.Ten = form.Ten.Trim();
        s.DonViTinh = form.DonViTinh.Trim();
        s.MaNhom = form.MaNhom;
        s.DieuKienBaoQuan = form.DieuKienBaoQuan;
        s.TheoDoiHanDung = form.TheoDoiHanDung;
        s.MoTa = form.MoTa?.Trim();
        GhiDoiTrangThai(nameof(SanPham), ma, s.TrangThai, form.TrangThai);
        s.TrangThai = form.TrangThai;
        await db.SaveChangesAsync();
    }

    /// <returns>Số biến thể tạo mới và số tổ hợp đã có (bỏ qua).</returns>
    public async Task<(int TaoMoi, int DaCo)> SinhBienTheAsync(string maSanPham, SinhBienTheForm form)
    {
        var s = await TimSanPham(maSanPham);
        var tenThuocTinh = await db.ThuocTinh.Where(t => t.MaNhom == s.MaNhom).OrderBy(t => t.ThuTu).Select(t => t.Ten).ToListAsync();
        var toHop = SinhBienThe.ToHop(tenThuocTinh.Select(ten => (ten, (IEnumerable<string>)TachGiaTri(form.GiaTri.GetValueOrDefault(ten)))));
        var daCo = await db.BienThe.Where(b => b.MaSanPham == maSanPham).Select(b => new { b.MaBienThe, b.ThuocTinh }).ToListAsync();

        var moi = toHop.Where(t => !daCo.Any(d => SinhBienThe.CungToHop(d.ThuocTinh, t))).ToList();
        var stt = daCo.Select(d => int.TryParse(d.MaBienThe[(maSanPham.Length + 1)..], out var n) ? n : 0).DefaultIfEmpty(0).Max();
        foreach (var thuocTinh in moi)
            db.BienThe.Add(new BienThe
            {
                MaBienThe = $"{maSanPham}-{++stt:D2}", MaSanPham = maSanPham, ThuocTinh = thuocTinh,
                DaiDongGoi = form.Dai, RongDongGoi = form.Rong, CaoDongGoi = form.Cao, KhoiLuong = form.KhoiLuong,
                TrangThai = TrangThaiSuDung.DangSuDung,
            });
        await db.SaveChangesAsync();
        return (moi.Count, toHop.Count - moi.Count);
    }

    public async Task<BienTheForm> LayBienTheAsync(string ma)
    {
        var b = await db.BienThe.AsNoTracking().Include(x => x.SanPham).SingleOrDefaultAsync(x => x.MaBienThe == ma)
            ?? throw new NghiepVuException($"Không có biến thể {ma}.");
        var form = new BienTheForm
        {
            Ma = b.MaBienThe, DaiDongGoi = b.DaiDongGoi, RongDongGoi = b.RongDongGoi, CaoDongGoi = b.CaoDongGoi,
            KhoiLuong = b.KhoiLuong, MaVach = b.MaVach, TrangThai = b.TrangThai,
        };
        var mucTon = await db.MucTon.AsNoTracking().Where(m => m.MaBienThe == ma).ToDictionaryAsync(m => m.MaKho);
        form.MucTon = (await DanhSachKho()).Select(k => mucTon.TryGetValue(k.Ma, out var m)
            ? new MucTonDong { MaKho = k.Ma, ToiThieu = m.ToiThieu, ToiDa = m.ToiDa, ThoiGianChoHang = m.ThoiGianChoHang }
            : new MucTonDong { MaKho = k.Ma }).ToList();
        form.DonVi = await db.DonViQuyDoi.AsNoTracking().Where(d => d.MaBienThe == ma).OrderBy(d => d.HeSo)
            .Select(d => new DonViQuyDoiDong { TenDonVi = d.TenDonVi, HeSo = d.HeSo, MaVach = d.MaVach, TrangThai = d.TrangThai }).ToListAsync();
        return await NapThongTinBienThe(form);
    }

    public async Task<BienTheForm> NapThongTinBienThe(BienTheForm form)
    {
        var b = await db.BienThe.AsNoTracking().Include(x => x.SanPham).SingleAsync(x => x.MaBienThe == form.Ma);
        form.MaSanPham = b.MaSanPham;
        form.TenSanPham = b.SanPham.Ten;
        form.DonViTinh = b.SanPham.DonViTinh;
        form.ThuocTinh = b.ThuocTinh;
        var tenKho = (await DanhSachKho()).ToDictionary(k => k.Ma, k => k.Ten);
        foreach (var m in form.MucTon)
            m.TenKho = tenKho.GetValueOrDefault(m.MaKho, m.MaKho);
        return form;
    }

    public async Task SuaBienTheAsync(string ma, BienTheForm form)
    {
        var b = await db.BienThe.SingleOrDefaultAsync(x => x.MaBienThe == ma) ?? throw new NghiepVuException($"Không có biến thể {ma}.");
        var maVach = NullNeuRong(form.MaVach);
        if (maVach is not null && (await db.BienThe.AnyAsync(x => x.MaVach == maVach && x.MaBienThe != ma) || await db.DonViQuyDoi.AnyAsync(d => d.MaVach == maVach)))
            throw new NghiepVuException($"Mã vạch {maVach} đã dùng cho hàng khác.");

        var kho = (await DanhSachKho()).Select(k => k.Ma).ToHashSet();
        foreach (var m in form.MucTon)
        {
            if (!kho.Contains(m.MaKho))
                throw new NghiepVuException($"Không có kho {m.MaKho}.");
            if (m.ToiDa is not null && m.ToiThieu is not null && m.ToiDa <= m.ToiThieu)
                throw new NghiepVuException($"Kho {m.MaKho}: tồn tối đa phải lớn hơn tồn tối thiểu.");
        }

        b.DaiDongGoi = form.DaiDongGoi;
        b.RongDongGoi = form.RongDongGoi;
        b.CaoDongGoi = form.CaoDongGoi;
        b.KhoiLuong = form.KhoiLuong;
        b.MaVach = maVach;
        GhiDoiTrangThai(nameof(BienThe), ma, b.TrangThai, form.TrangThai);
        b.TrangThai = form.TrangThai;

        await quyDoi.LuuAsync(b, await db.SanPham.Where(s => s.MaSanPham == b.MaSanPham).Select(s => s.DonViTinh).SingleAsync(), form.DonVi);

        var cu = await db.MucTon.Where(m => m.MaBienThe == ma).ToListAsync();
        db.MucTon.RemoveRange(cu);
        db.MucTon.AddRange(form.MucTon.Where(m => m.ToiThieu is not null || m.ToiDa is not null).Select(m => new MucTon
        {
            MaBienThe = ma, MaKho = m.MaKho, ToiThieu = m.ToiThieu, ToiDa = m.ToiDa, ThoiGianChoHang = m.ThoiGianChoHang,
        }));
        await db.SaveChangesAsync();
    }

    private async Task<SanPham> TimSanPham(string ma) =>
        await db.SanPham.SingleOrDefaultAsync(s => s.MaSanPham == ma) ?? throw new NghiepVuException($"Không có sản phẩm {ma}.");

    private Task<bool> CoTonAsync(string maSanPham) =>
        db.TonKho.AnyAsync(t => t.SoLuong > 0 && db.Lo.Any(l => l.MaLo == t.MaLo && l.BienThe.MaSanPham == maSanPham));

    private Task<List<LuaChon>> DanhSachKho() =>
        db.ViTri.Where(v => v.Cap == 1).OrderBy(v => v.MaViTri).Select(v => new LuaChon(v.MaViTri, v.Ten)).ToListAsync();

    private static IEnumerable<string> TachGiaTri(string? chuoi) =>
        (chuoi ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Distinct();

    private static string? NullNeuRong(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    /// <summary>Quy tắc 5: hàng đã dùng chỉ ngừng sử dụng chứ không xóa; ghi nhật ký mỗi lần ngừng hoặc dùng lại.</summary>
    private void GhiDoiTrangThai(string doiTuong, string ma, TrangThaiSuDung cu, TrangThaiSuDung moi)
    {
        if (cu != moi)
            nhatKy.Ghi(moi == TrangThaiSuDung.NgungSuDung ? "NgungSuDung" : "DungLai", doiTuong, ma);
    }
}
