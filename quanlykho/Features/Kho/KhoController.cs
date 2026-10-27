using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using quanlykho.Common.PhanQuyen;

namespace quanlykho.Features.Kho;

[Authorize(Roles = Vai.QuanTri), Route("Kho")]
public class KhoController(KhoService service) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index() => View(await service.DanhSachAsync());

    [HttpGet("Them")]
    public async Task<IActionResult> Them()
    {
        var form = new KhoForm();
        await service.NapFormAsync(form);
        return View(form);
    }

    [HttpPost("Them")]
    public async Task<IActionResult> Them(KhoForm form)
    {
        await service.NapFormAsync(form);
        if (!ModelState.IsValid)
            return View(form);
        var ma = await service.ThemAsync(form);
        TempData["ThongBao"] = $"Đã thêm kho {ma}. Bấm \"Tạo khu mặc định\" để có khu tiếp nhận, lưu trữ, cách ly, xuất.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("{ma}/Sua")]
    public async Task<IActionResult> Sua(string ma) => View(await service.FormSuaAsync(ma));

    [HttpPost("{ma}/Sua")]
    public async Task<IActionResult> Sua(string ma, KhoForm form)
    {
        form.Ma = ma;
        form.LaSua = true;
        await service.NapFormAsync(form);
        if (!ModelState.IsValid)
            return View(form);
        await service.SuaAsync(ma, form);
        TempData["ThongBao"] = $"Đã lưu kho {ma}.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("{ma}/Khoa")]
    public async Task<IActionResult> Khoa(string ma, bool khoa)
    {
        await service.KhoaAsync(ma, khoa);
        TempData["ThongBao"] = khoa ? $"Kho {ma} đã ngừng sử dụng." : $"Kho {ma} đã dùng lại.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("{ma}/KhuMacDinh")]
    public async Task<IActionResult> KhuMacDinh(string ma)
    {
        var toa = await service.TaoKhuMacDinhAsync(ma);
        TempData["ThongBao"] = $"Đã tạo {toa} với khu tiếp nhận, lưu trữ, cách ly, xuất. Sửa kích thước ô, thêm kệ ở màn hình Kho và vị trí.";
        return RedirectToAction(nameof(Index));
    }
}
