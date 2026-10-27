using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using quanlykho.Data;

namespace quanlykho.Common.GiaoDien;

public record LuaChon(string Ma, string Ten);

/// <summary>Mô hình của partial _OQuetMa.</summary>
public record OQuetMa(string Id, string Nhan, string GoiY = "Quét hoặc gõ mã rồi nhấn Enter");

public record OChonKhoModel(string Ten, string? Chon, IReadOnlyList<LuaChon> DanhSach, bool CoTatCa);

/// <summary>
/// Ô chọn kho: @await Component.InvokeAsync("OChonKho", new { ten = "maKho", chon = Model.MaKho }).
/// Lấy danh sách kho từ CSDL (lọc theo kho được giao làm ở nhiệm vụ đăng nhập); truyền danhSach để dùng dữ liệu có sẵn.
/// </summary>
public class OChonKhoViewComponent(AppDbContext db) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync(string ten, string? chon = null, bool coTatCa = false, IReadOnlyList<LuaChon>? danhSach = null)
    {
        danhSach ??= await db.ViTri.Where(v => v.Cap == 1 && !v.DangKhoa).OrderBy(v => v.MaViTri)
            .Select(v => new LuaChon(v.MaViTri, v.Ten)).ToListAsync();
        return View(new OChonKhoModel(ten, chon, danhSach, coTatCa));
    }
}
