using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using quanlykho.Common.PhanQuyen;
using quanlykho.Domain;

namespace quanlykho.Features.DanhMuc;

[Route("DanhMuc/NhomHang")]
public class NhomHangController(DanhMucService service) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index() => View(await service.CayNhomAsync());

    [HttpGet("Them"), Authorize(Roles = Vai.QuanTri)]
    public async Task<IActionResult> Them(string? maCha) => View(new NhomForm { MaCha = maCha, DanhSachNhom = await service.CayNhomAsync() });

    [HttpPost("Them"), Authorize(Roles = Vai.QuanTri)]
    public async Task<IActionResult> Them(NhomForm form)
    {
        form.DanhSachNhom = await service.CayNhomAsync();
        if (!ModelState.IsValid)
            return View(form);
        await service.ThemNhomAsync(form);
        TempData["ThongBao"] = $"Đã thêm nhóm {form.Ma.ToUpperInvariant()}.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("Sua/{ma}"), Authorize(Roles = Vai.QuanTri)]
    public async Task<IActionResult> Sua(string ma)
    {
        var form = await service.LayNhomAsync(ma);
        form.DanhSachNhom = await service.CayNhomAsync();
        return View(form);
    }

    [HttpPost("Sua/{ma}"), Authorize(Roles = Vai.QuanTri)]
    public async Task<IActionResult> Sua(string ma, NhomForm form)
    {
        form.Ma = ma;
        form.DanhSachNhom = await service.CayNhomAsync();
        ModelState.Remove(nameof(NhomForm.Ma));
        if (!ModelState.IsValid)
            return View(form);
        await service.SuaNhomAsync(ma, form);
        TempData["ThongBao"] = "Đã lưu nhóm hàng.";
        return RedirectToAction(nameof(Sua), new { ma });
    }

    [HttpPost("Xoa/{ma}"), Authorize(Roles = Vai.QuanTri)]
    public async Task<IActionResult> Xoa(string ma)
    {
        await service.XoaNhomAsync(ma);
        TempData["ThongBao"] = $"Đã xóa nhóm {ma}.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("{ma}/ThuocTinh"), Authorize(Roles = Vai.QuanTri)]
    public async Task<IActionResult> LuuThuocTinh(string ma, string ten, string? giaTriGoiY)
    {
        await service.LuuThuocTinhAsync(ma, ten ?? "", giaTriGoiY);
        TempData["ThongBao"] = $"Đã lưu thuộc tính {ten}.";
        return RedirectToAction(nameof(Sua), new { ma });
    }

    [HttpPost("{ma}/ThuocTinh/Xoa"), Authorize(Roles = Vai.QuanTri)]
    public async Task<IActionResult> XoaThuocTinh(string ma, string ten)
    {
        await service.XoaThuocTinhAsync(ma, ten);
        TempData["ThongBao"] = $"Đã xóa thuộc tính {ten}.";
        return RedirectToAction(nameof(Sua), new { ma });
    }
}

[Route("DanhMuc/SanPham")]
public class SanPhamController(DanhMucService service) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(string? tuKhoa, string? maNhom, TrangThaiSuDung? trangThai, int trang = 1)
    {
        ViewData["TuKhoa"] = tuKhoa;
        ViewData["MaNhom"] = maNhom;
        ViewData["TrangThai"] = trangThai;
        ViewData["Cay"] = await service.CayNhomAsync();
        return View(await service.DanhSachSanPhamAsync(tuKhoa, maNhom, trangThai, trang));
    }

    [HttpGet("Them"), Authorize(Roles = Vai.QuanTri)]
    public async Task<IActionResult> Them() => View(await service.NapDanhSach(new SanPhamForm()));

    [HttpPost("Them"), Authorize(Roles = Vai.QuanTri)]
    public async Task<IActionResult> Them(SanPhamForm form)
    {
        await service.NapDanhSach(form);
        if (!ModelState.IsValid)
            return View(form);
        var ma = await service.ThemSanPhamAsync(form);
        TempData["ThongBao"] = $"Đã thêm sản phẩm {ma}. Tiếp theo: sinh biến thể.";
        return RedirectToAction(nameof(Sua), new { ma });
    }

    [HttpGet("{ma}")]
    public async Task<IActionResult> Sua(string ma) => View(await service.LaySanPhamAsync(ma));

    [HttpPost("{ma}"), Authorize(Roles = Vai.QuanTri)]
    public async Task<IActionResult> Sua(string ma, SanPhamForm form)
    {
        form.Ma = ma;
        form.LaSua = true;
        await service.NapDanhSach(form);
        if (!ModelState.IsValid)
            return View(form);
        await service.SuaSanPhamAsync(ma, form);
        TempData["ThongBao"] = "Đã lưu sản phẩm.";
        return RedirectToAction(nameof(Sua), new { ma });
    }

    [HttpPost("{ma}/SinhBienThe"), Authorize(Roles = Vai.QuanTri)]
    public async Task<IActionResult> SinhBienThe(string ma, SinhBienTheForm form)
    {
        if (!ModelState.IsValid)
        {
            TempData["Loi"] = "Nhập kích thước đóng gói và khối lượng lớn hơn 0 cho biến thể mới.";
            return RedirectToAction(nameof(Sua), new { ma });
        }
        var (taoMoi, daCo) = await service.SinhBienTheAsync(ma, form);
        TempData["ThongBao"] = $"Đã tạo {taoMoi} biến thể" + (daCo > 0 ? $", bỏ qua {daCo} tổ hợp đã có." : ".");
        return RedirectToAction(nameof(Sua), new { ma });
    }

    [HttpGet("BienThe/{ma}")]
    public async Task<IActionResult> BienThe(string ma) => View(await service.LayBienTheAsync(ma));

    [HttpPost("BienThe/{ma}"), Authorize(Roles = Vai.QuanTri)]
    public async Task<IActionResult> BienThe(string ma, BienTheForm form)
    {
        form.Ma = ma;
        await service.NapThongTinBienThe(form);
        if (!ModelState.IsValid)
            return View(form);
        await service.SuaBienTheAsync(ma, form);
        TempData["ThongBao"] = "Đã lưu biến thể.";
        return RedirectToAction(nameof(BienThe), new { ma });
    }
}

[Route("DanhMuc/{loai:regex(^(NhaCungCap|NoiNhan)$)}")]
public class DoiTacController(DoiTacService service) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(LoaiDoiTac loai, string? tuKhoa, TrangThaiSuDung? trangThai, int trang = 1)
    {
        ViewData["Loai"] = loai;
        ViewData["TuKhoa"] = tuKhoa;
        ViewData["TrangThai"] = trangThai;
        return View(await service.DanhSachAsync(loai, tuKhoa, trangThai, trang));
    }

    [HttpGet("Them"), Authorize(Roles = Vai.QuanTri)]
    public IActionResult Them(LoaiDoiTac loai) => View(new DoiTacForm { Loai = loai });

    [HttpPost("Them"), Authorize(Roles = Vai.QuanTri)]
    public async Task<IActionResult> Them(LoaiDoiTac loai, DoiTacForm form)
    {
        form.Loai = loai;
        if (!ModelState.IsValid)
            return View(form);
        var ma = await service.ThemAsync(form);
        TempData["ThongBao"] = $"Đã thêm {ma}.";
        return RedirectToAction(nameof(Index), new { loai });
    }

    [HttpGet("{ma}"), Authorize(Roles = Vai.QuanTri)]
    public async Task<IActionResult> Sua(LoaiDoiTac loai, string ma) => View(await service.LayAsync(loai, ma));

    [HttpPost("{ma}"), Authorize(Roles = Vai.QuanTri)]
    public async Task<IActionResult> Sua(LoaiDoiTac loai, string ma, DoiTacForm form)
    {
        (form.Loai, form.Ma, form.LaSua) = (loai, ma, true);
        ModelState.Remove(nameof(DoiTacForm.Ma));
        if (!ModelState.IsValid)
            return View(form);
        await service.SuaAsync(ma, form);
        TempData["ThongBao"] = $"Đã lưu {ma}.";
        return RedirectToAction(nameof(Index), new { loai });
    }
}
