using Microsoft.EntityFrameworkCore;
using quanlykho.Common;
using quanlykho.Common.GiaoDien;
using quanlykho.Common.Loi;
using quanlykho.Common.PhanQuyen;
using quanlykho.Data;
using quanlykho.Domain;

namespace quanlykho.Features.Kho;

public class ViTriService(AppDbContext db, NguoiDungHienTai nguoiDung, GhiNhatKy nhatKy)
{
    public async Task<List<LuaChon>> KhoAsync()
    {
        var kho = await nguoiDung.KhoDuocGiaoAsync();
        return await db.ViTri.Where(v => kho.Contains(v.MaViTri)).OrderBy(v => v.MaViTri).Select(v => new LuaChon(v.MaViTri, v.Ten)).ToListAsync();
    }

    /// <summary>Mọi vị trí của kho theo thứ tự cây (mã có tiền tố cha nên sắp theo mã là đúng thứ tự); Con của từng nút đã nối sẵn.</summary>
    public async Task<List<ViTriNut>> CayAsync(string maKho)
    {
        await nguoiDung.DamBaoQuyenKhoAsync(maKho);
        var ds = await db.ViTri.Where(v => v.MaKho == maKho).OrderBy(v => v.MaViTri)
            .Select(v => new
            {
                v, SoCon = db.ViTri.Count(c => c.MaCha == v.MaViTri),
                Ton = db.TonKho.Where(t => t.MaViTri == v.MaViTri).Sum(t => (decimal?)t.SoLuong) ?? 0,
            }).ToListAsync();
        var nut = ds.Select(x => TaoNut(x.v, x.SoCon, ds.Where(y => y.v.MaViTri == x.v.MaViTri || y.v.MaViTri.StartsWith(x.v.MaViTri + "-")).Sum(y => y.Ton))).ToList();
        var theoMa = nut.ToDictionary(n => n.Ma);
        foreach (var n in nut)
            if (n.MaCha is not null && theoMa.TryGetValue(n.MaCha, out var cha))
                cha.Con.Add(n);
        return nut;
    }

    public async Task<ViTriChiTiet> LayAsync(string ma)
    {
        var v = await TimAsync(ma);
        var con = await db.ViTri.Where(c => c.MaCha == ma).OrderBy(c => c.MaViTri)
            .Select(c => new { c, SoCon = db.ViTri.Count(x => x.MaCha == c.MaViTri) }).ToListAsync();
        var hang = (await db.TonKho.Where(t => (t.MaViTri == ma || t.MaViTri.StartsWith(ma + "-")) && t.SoLuong > 0)
                .Join(db.Lo, t => t.MaLo, l => l.MaLo, (t, l) => new { t, l.MaBienThe, l.HanDung, l.BienThe.SanPham.Ten, l.BienThe.ThuocTinh, l.BienThe.SanPham.DonViTinh })
                .OrderBy(x => x.t.MaViTri).ThenBy(x => x.t.MaLo).ToListAsync())
            .Select(x => new HangTaiViTri(x.t.MaViTri, x.t.MaLo, x.MaBienThe, NhapKho.DonMuaService.MoTaHang(x.Ten, x.ThuocTinh), x.DonViTinh, x.HanDung, x.t.SoLuong, x.t.SoLuongDaGiu))
            .ToList();
        return new ViTriChiTiet(TaoNut(v, con.Count, hang.Sum(h => h.SoLuong)), v.TenDayDu, v.MaKho,
            await db.SoBienDong.AnyAsync(s => s.MaViTri == ma),
            con.Select(x => TaoNut(x.c, x.SoCon, 0)).ToList(), hang);
    }

    public async Task<ViTriForm> FormThemAsync(string maCha)
    {
        var cha = await TimAsync(maCha);
        var so = (await db.ViTri.Where(c => c.MaCha == maCha).MaxAsync(c => (int?)c.ThuTu) ?? 0) + 1;
        return NapForm(new ViTriForm { MaCha = maCha, So = Math.Min(so, 99) }, cha.Cap + 1, cha);
    }

    public async Task<ViTriForm> FormSuaAsync(string ma)
    {
        var v = await TimAsync(ma);
        var cha = v.MaCha is null ? null : await db.ViTri.SingleAsync(c => c.MaViTri == v.MaCha);
        return NapForm(new ViTriForm
        {
            Ma = ma, MaCha = v.MaCha, So = v.ThuTu, Ten = v.Ten, LoaiKhu = v.LoaiKhu, DieuKienBaoQuan = v.DieuKienBaoQuan,
            Dai = v.Dai, Rong = v.Rong, Cao = v.Cao, TaiTrongToiDa = v.TaiTrongToiDa,
        }, v.Cap, cha);
    }

    /// <summary>Nạp cấp, tên cha, phần kế thừa cho view; gọi lại khi hiện lại form lỗi.</summary>
    public async Task<ViTriForm> NapFormAsync(ViTriForm form)
    {
        var cha = form.MaCha is null ? null : await db.ViTri.AsNoTracking().SingleOrDefaultAsync(c => c.MaViTri == form.MaCha);
        var cap = form.Ma is null ? (cha?.Cap ?? 0) + 1 : await db.ViTri.Where(v => v.MaViTri == form.Ma).Select(v => (int)v.Cap).SingleOrDefaultAsync();
        return NapForm(form, cap, cha);
    }

    public async Task<string> ThemAsync(ViTriForm form)
    {
        var cha = await TimAsync(form.MaCha ?? "");
        if (cha.Cap >= CapViTri.CapCuoi)
            throw new NghiepVuException("Ô là cấp cuối (cấp 7), không thêm vị trí con được.");
        var so = form.So ?? (await db.ViTri.Where(c => c.MaCha == cha.MaViTri).MaxAsync(c => (int?)c.ThuTu) ?? 0) + 1;
        if (so is < 1 or > 99)
            throw new NghiepVuException("Số thứ tự từ 1 đến 99; nhánh này đã đủ 99 vị trí con.");
        var ma = CapViTri.MaCon(cha.MaViTri, cha.Cap, so);
        if (await db.ViTri.AnyAsync(v => v.MaViTri == ma))
            throw new NghiepVuException($"Mã {ma} đã có; chọn số thứ tự khác.");

        var v = new ViTri
        {
            MaViTri = ma, MaCha = cha.MaViTri, MaKho = cha.MaKho, Cap = (short)(cha.Cap + 1), ThuTu = so,
            Ten = form.Ten.Trim(), TenDayDu = $"{cha.TenDayDu} / {form.Ten.Trim()}", DangKhoa = cha.DangKhoa,
        };
        GanKhu(v, cha, form);
        GanKichThuoc(v, form);
        db.ViTri.Add(v);
        nhatKy.Ghi("ThemViTri", nameof(ViTri), ma, maKho: v.MaKho);
        await db.SaveChangesAsync();
        return ma;
    }

    public async Task SuaAsync(string ma, ViTriForm form)
    {
        var v = await TimAsync(ma);
        if (v.Cap == 1)
            throw new NghiepVuException("Thông tin kho sửa ở màn hình Danh mục kho.");
        var cha = await db.ViTri.SingleAsync(c => c.MaViTri == v.MaCha);
        var con = await db.ViTri.Where(c => c.MaViTri.StartsWith(ma + "-")).ToListAsync();

        var tenCu = v.TenDayDu;
        v.Ten = form.Ten.Trim();
        v.TenDayDu = $"{cha.TenDayDu} / {v.Ten}";
        foreach (var c in con)
            c.TenDayDu = v.TenDayDu + c.TenDayDu[tenCu.Length..];

        if (v.Cap == CapViTri.CapKhaiKhu && (form.LoaiKhu != v.LoaiKhu || form.DieuKienBaoQuan != v.DieuKienBaoQuan))
        {
            if (await db.TonKho.AnyAsync(t => (t.MaViTri == ma || t.MaViTri.StartsWith(ma + "-")) && t.SoLuong > 0))
                throw new NghiepVuException("Khu đang có hàng, không đổi loại khu hoặc điều kiện bảo quản được.");
            if (form.LoaiKhu != v.LoaiKhu && con.Any(c => c.LaOMacDinh))
                throw new NghiepVuException("Khu chứa ô mặc định nhận hàng, không đổi loại khu được.");
            GanKhu(v, cha, form);
            foreach (var c in con)
                (c.LoaiKhu, c.DieuKienBaoQuan) = (v.LoaiKhu, v.DieuKienBaoQuan);
        }
        GanKichThuoc(v, form);
        nhatKy.Ghi("SuaViTri", nameof(ViTri), ma, maKho: v.MaKho);
        await db.SaveChangesAsync();
    }

    /// <summary>Khóa, mở khóa cả nhánh; vị trí khóa không nhận hàng xếp vào.</summary>
    public async Task KhoaAsync(string ma, bool khoa)
    {
        var v = await TimAsync(ma);
        if (v.Cap == 1)
            throw new NghiepVuException("Khóa kho ở màn hình Danh mục kho.");
        if (!khoa && await db.ViTri.AnyAsync(c => c.MaViTri == v.MaCha && c.DangKhoa))
            throw new NghiepVuException("Vị trí cha đang khóa; mở khóa vị trí cha trước.");
        await using var tx = await db.Database.BeginTransactionAsync();
        await db.ViTri.Where(c => c.MaViTri == ma || c.MaViTri.StartsWith(ma + "-")).ExecuteUpdateAsync(s => s.SetProperty(c => c.DangKhoa, khoa));
        nhatKy.Ghi(khoa ? "KhoaViTri" : "MoKhoaViTri", nameof(ViTri), ma, maKho: v.MaKho);
        await db.SaveChangesAsync();
        await tx.CommitAsync();
    }

    public async Task XoaAsync(string ma)
    {
        var v = await TimAsync(ma);
        if (v.Cap == 1)
            throw new NghiepVuException("Không xóa kho ở đây.");
        if (await db.ViTri.AnyAsync(c => c.MaCha == ma))
            throw new NghiepVuException("Vị trí còn vị trí con, xóa các vị trí con trước.");
        if (v.LaOMacDinh)
            throw new NghiepVuException("Ô mặc định nhận hàng của khu không xóa được.");
        if (await db.SoBienDong.AnyAsync(s => s.MaViTri == ma))
            throw new NghiepVuException("Vị trí đã từng có hàng, chỉ khóa được, không xóa.");
        db.ViTri.Remove(v);
        nhatKy.Ghi("XoaViTri", nameof(ViTri), ma, maKho: v.MaKho);
        await db.SaveChangesAsync();
    }

    private async Task<ViTri> TimAsync(string ma)
    {
        var v = await db.ViTri.SingleOrDefaultAsync(x => x.MaViTri == ma) ?? throw new NghiepVuException($"Không có vị trí {ma}.");
        await nguoiDung.DamBaoQuyenKhoAsync(v.MaKho);
        return v;
    }

    private static void GanKhu(ViTri v, ViTri cha, ViTriForm form)
    {
        if (v.Cap < CapViTri.CapKhaiKhu)
            (v.LoaiKhu, v.DieuKienBaoQuan) = (null, null);
        else if (v.Cap == CapViTri.CapKhaiKhu)
            (v.LoaiKhu, v.DieuKienBaoQuan) = (form.LoaiKhu ?? throw new NghiepVuException("Chọn loại khu cho phòng – dãy."),
                form.DieuKienBaoQuan ?? throw new NghiepVuException("Chọn điều kiện bảo quản cho phòng – dãy."));
        else
            (v.LoaiKhu, v.DieuKienBaoQuan) = (cha.LoaiKhu, cha.DieuKienBaoQuan);
    }

    private static void GanKichThuoc(ViTri v, ViTriForm form)
    {
        if (v.Cap != CapViTri.CapCuoi)
            return;
        if (form.Dai is not > 0 || form.Rong is not > 0 || form.Cao is not > 0 || form.TaiTrongToiDa is not > 0)
            throw new NghiepVuException("Ô bắt buộc nhập dài, rộng, cao và tải trọng tối đa, đều lớn hơn 0.");
        (v.Dai, v.Rong, v.Cao, v.TaiTrongToiDa) = (form.Dai, form.Rong, form.Cao, form.TaiTrongToiDa);
    }

    private static ViTriForm NapForm(ViTriForm form, int cap, ViTri? cha)
    {
        form.Cap = cap;
        form.TenDayDuCha = cha?.TenDayDu ?? "";
        form.KeThua = cap > CapViTri.CapKhaiKhu && cha is not null
            ? $"{CapViTri.TenKhu(cha.LoaiKhu)}, bảo quản {CapViTri.TenBaoQuan(cha.DieuKienBaoQuan).ToLowerInvariant()}"
            : null;
        return form;
    }

    private static ViTriNut TaoNut(ViTri v, int soCon, decimal ton) => new(v.MaViTri, v.MaCha, v.Cap, v.Ten, v.LoaiKhu, v.DieuKienBaoQuan,
        v.Dai, v.Rong, v.Cao, v.TaiTrongToiDa, v.LaOMacDinh, v.DangKhoa, v.MaPhieuKiemKe, soCon, ton);
}
