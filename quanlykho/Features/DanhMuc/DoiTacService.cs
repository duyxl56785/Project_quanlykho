using Microsoft.EntityFrameworkCore;
using quanlykho.Common;
using quanlykho.Common.GiaoDien;
using quanlykho.Common.Loi;
using quanlykho.Data;
using quanlykho.Domain;

namespace quanlykho.Features.DanhMuc;

public class DoiTacService(AppDbContext db, GhiNhatKy nhatKy)
{
    public Task<TrangDuLieu<DoiTacDong>> DanhSachAsync(LoaiDoiTac loai, string? tuKhoa, TrangThaiSuDung? trangThai, int trang)
    {
        var tk = string.IsNullOrWhiteSpace(tuKhoa) ? null : $"%{tuKhoa.Trim()}%";
        if (loai == LoaiDoiTac.NhaCungCap)
        {
            var q = db.NhaCungCap.AsQueryable();
            if (tk is not null) q = q.Where(x => EF.Functions.ILike(x.MaNhaCungCap, tk) || EF.Functions.ILike(x.Ten, tk));
            if (trangThai is not null) q = q.Where(x => x.TrangThai == trangThai);
            return TrangDuLieu<DoiTacDong>.TaoAsync(q.OrderBy(x => x.MaNhaCungCap)
                .Select(x => new DoiTacDong(x.MaNhaCungCap, x.Ten, x.DiaChi, x.NguoiLienHe, x.SoDienThoai, x.TrangThai)), trang);
        }
        var n = db.NoiNhan.AsQueryable();
        if (tk is not null) n = n.Where(x => EF.Functions.ILike(x.MaNoiNhan, tk) || EF.Functions.ILike(x.Ten, tk));
        if (trangThai is not null) n = n.Where(x => x.TrangThai == trangThai);
        return TrangDuLieu<DoiTacDong>.TaoAsync(n.OrderBy(x => x.MaNoiNhan)
            .Select(x => new DoiTacDong(x.MaNoiNhan, x.Ten, x.DiaChi, x.NguoiLienHe, x.SoDienThoai, x.TrangThai)), trang);
    }

    public async Task<DoiTacForm> LayAsync(LoaiDoiTac loai, string ma)
    {
        var x = await (loai == LoaiDoiTac.NhaCungCap
                ? db.NhaCungCap.Where(n => n.MaNhaCungCap == ma).Select(n => new DoiTacDong(n.MaNhaCungCap, n.Ten, n.DiaChi, n.NguoiLienHe, n.SoDienThoai, n.TrangThai))
                : db.NoiNhan.Where(n => n.MaNoiNhan == ma).Select(n => new DoiTacDong(n.MaNoiNhan, n.Ten, n.DiaChi, n.NguoiLienHe, n.SoDienThoai, n.TrangThai)))
            .SingleOrDefaultAsync() ?? throw new NghiepVuException($"Không có mã {ma}.");
        return new DoiTacForm { Ma = x.Ma, Ten = x.Ten, DiaChi = x.DiaChi, NguoiLienHe = x.NguoiLienHe, SoDienThoai = x.SoDienThoai, TrangThai = x.TrangThai, Loai = loai, LaSua = true };
    }

    public async Task<string> ThemAsync(DoiTacForm form)
    {
        var ma = form.Ma.Trim().ToUpperInvariant();
        if (form.Loai == LoaiDoiTac.NhaCungCap)
        {
            if (await db.NhaCungCap.AnyAsync(x => x.MaNhaCungCap == ma)) throw new NghiepVuException($"Mã {ma} đã có.");
            db.NhaCungCap.Add(Gan(new NhaCungCap { MaNhaCungCap = ma }, form));
        }
        else
        {
            if (await db.NoiNhan.AnyAsync(x => x.MaNoiNhan == ma)) throw new NghiepVuException($"Mã {ma} đã có.");
            db.NoiNhan.Add(Gan(new NoiNhan { MaNoiNhan = ma }, form));
        }
        await db.SaveChangesAsync();
        return ma;
    }

    public async Task SuaAsync(string ma, DoiTacForm form)
    {
        TrangThaiSuDung cu;
        if (form.Loai == LoaiDoiTac.NhaCungCap)
        {
            var x = await db.NhaCungCap.SingleOrDefaultAsync(x => x.MaNhaCungCap == ma) ?? throw new NghiepVuException($"Không có mã {ma}.");
            cu = x.TrangThai;
            Gan(x, form);
        }
        else
        {
            var x = await db.NoiNhan.SingleOrDefaultAsync(x => x.MaNoiNhan == ma) ?? throw new NghiepVuException($"Không có mã {ma}.");
            cu = x.TrangThai;
            Gan(x, form);
        }
        if (cu != form.TrangThai)
            nhatKy.Ghi(form.TrangThai == TrangThaiSuDung.NgungSuDung ? "NgungSuDung" : "DungLai", form.Loai == LoaiDoiTac.NhaCungCap ? nameof(NhaCungCap) : nameof(NoiNhan), ma);
        await db.SaveChangesAsync();
    }

    private static NhaCungCap Gan(NhaCungCap x, DoiTacForm f)
    {
        (x.Ten, x.DiaChi, x.NguoiLienHe, x.SoDienThoai, x.TrangThai) = (f.Ten.Trim(), f.DiaChi?.Trim(), f.NguoiLienHe?.Trim(), f.SoDienThoai?.Trim(), f.TrangThai);
        return x;
    }

    private static NoiNhan Gan(NoiNhan x, DoiTacForm f)
    {
        (x.Ten, x.DiaChi, x.NguoiLienHe, x.SoDienThoai, x.TrangThai) = (f.Ten.Trim(), f.DiaChi?.Trim(), f.NguoiLienHe?.Trim(), f.SoDienThoai?.Trim(), f.TrangThai);
        return x;
    }
}
