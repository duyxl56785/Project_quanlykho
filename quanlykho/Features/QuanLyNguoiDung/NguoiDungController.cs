using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using quanlykho.Common.PhanQuyen;
using quanlykho.Domain;

namespace quanlykho.Features.QuanLyNguoiDung;

[Authorize(Roles = Vai.QuanTri), Route("NguoiDung")]
public class NguoiDungController(NguoiDungService service) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(string? tuKhoa, VaiTro? vaiTro, TrangThaiTaiKhoan? trangThai, int trang = 1)
    {
        ViewData["TuKhoa"] = tuKhoa;
        ViewData["VaiTro"] = vaiTro;
        ViewData["TrangThai"] = trangThai;
        return View(await service.DanhSachAsync(tuKhoa, vaiTro, trangThai, trang));
    }

    [HttpGet("Them")]
    public async Task<IActionResult> Them() => View(new NguoiDungForm { DanhSachKho = await service.DanhSachKhoAsync() });

    [HttpPost("Them")]
    public async Task<IActionResult> Them(NguoiDungForm form)
    {
        form.DanhSachKho = await service.DanhSachKhoAsync();
        if (!ModelState.IsValid)
            return View(form);
        var ma = await service.ThemAsync(form);
        TempData["ThongBao"] = $"Đã thêm người dùng {ma}.";
        return RedirectToAction(nameof(Sua), new { ma });
    }

    [HttpGet("Sua/{ma}")]
    public async Task<IActionResult> Sua(string ma)
    {
        var form = await service.LayAsync(ma);
        form.DanhSachKho = await service.DanhSachKhoAsync();
        return View(form);
    }

    [HttpPost("Sua/{ma}")]
    public async Task<IActionResult> Sua(string ma, NguoiDungForm form)
    {
        form.Ma = ma;
        form.DanhSachKho = await service.DanhSachKhoAsync();
        if (!ModelState.IsValid)
            return View(form);
        await service.SuaAsync(ma, form);
        TempData["ThongBao"] = "Đã lưu thay đổi.";
        return RedirectToAction(nameof(Sua), new { ma });
    }

    [HttpPost("Khoa/{ma}")]
    public async Task<IActionResult> Khoa(string ma)
    {
        await service.KhoaAsync(ma);
        TempData["ThongBao"] = $"Đã khóa tài khoản {ma}; phiên đăng nhập của người này hết hiệu lực ngay.";
        return RedirectToAction(nameof(Sua), new { ma });
    }

    [HttpPost("MoKhoa/{ma}")]
    public async Task<IActionResult> MoKhoa(string ma)
    {
        await service.MoKhoaAsync(ma);
        TempData["ThongBao"] = $"Đã mở khóa tài khoản {ma}.";
        return RedirectToAction(nameof(Sua), new { ma });
    }

    [HttpPost("DatLaiMatKhau/{ma}")]
    public async Task<IActionResult> DatLaiMatKhau(string ma, string? matKhauMoi)
    {
        await service.DatLaiMatKhauAsync(ma, matKhauMoi);
        TempData["ThongBao"] = "Đã đặt lại mật khẩu; người dùng phải đăng nhập lại.";
        return RedirectToAction(nameof(Sua), new { ma });
    }
}
