using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using quanlykho.Common.Loi;
using quanlykho.Common.PhanQuyen;
using quanlykho.Data;
using quanlykho.Domain;

namespace quanlykho.Features.XuatKho;

public record GoiYLayHangTraVe(long MaGoiY, string MaKho, string MaBienThe, decimal SoLuong, List<DongGoiYLay> Dong, decimal ConThieu);

/// <summary>Gợi ý AI chỉ để tham khảo (quy tắc 13): không ghi tồn, không tạo hay duyệt chứng từ; mọi lần gợi ý được lưu vào LichSuGoiY.</summary>
public class GoiYService(AppDbContext db, NguoiDungHienTai nguoiDung, TonKhaDung tonKhaDung, TimeProvider gio)
{
    public async Task<GoiYLayHangTraVe> LayHangAsync(string maKho, string maBienThe, decimal soLuong, string? maChungTu = null)
    {
        await nguoiDung.DamBaoQuyenKhoAsync(maKho);
        if (soLuong <= 0)
            throw new NghiepVuException("Số lượng cần lấy phải lớn hơn 0.");
        if (!await db.BienThe.AnyAsync(b => b.MaBienThe == maBienThe))
            throw new NghiepVuException($"Không có biến thể {maBienThe}.");

        var ungVien = (await tonKhaDung.TruyVan().Where(d => d.MaKho == maKho && d.MaBienThe == maBienThe).ToListAsync())
            .Select(d => new UngVienLay(d.MaLo, d.MaViTri, d.KhaDung, d.HanDung, d.NgayNhap)).ToList();
        var kq = GoiYLayHang.Tinh(ungVien, soLuong, tonKhaDung.HomNay);

        var ls = new LichSuGoiY
        {
            Loai = LoaiGoiY.LayHang, MaKho = maKho, MaBienThe = maBienThe, ThoiDiem = gio.GetUtcNow(), NguoiDung = nguoiDung.Ma, MaChungTu = maChungTu,
            DauVao = JsonSerializer.Serialize(new { soLuong, ungVien }, JsonSerializerOptions.Web),
            KetQua = JsonSerializer.Serialize(kq, JsonSerializerOptions.Web),
        };
        db.LichSuGoiY.Add(ls);
        await db.SaveChangesAsync();
        return new GoiYLayHangTraVe(ls.MaGoiY, maKho, maBienThe, soLuong, kq.Dong, kq.ConThieu);
    }

    /// <summary>Ghi người dùng có làm theo gợi ý hay không; chỉ ghi lần đầu.</summary>
    public async Task PhanHoiAsync(long maGoiY, bool chapNhan, string? maChungTu = null)
    {
        var ls = await db.LichSuGoiY.SingleOrDefaultAsync(x => x.MaGoiY == maGoiY) ?? throw new NghiepVuException($"Không có gợi ý {maGoiY}.");
        await nguoiDung.DamBaoQuyenKhoAsync(ls.MaKho);
        if (ls.ChapNhan is not null)
            return;
        ls.ChapNhan = chapNhan;
        ls.ThoiDiemPhanHoi = gio.GetUtcNow();
        ls.MaChungTu ??= maChungTu;
        await db.SaveChangesAsync();
    }
}
