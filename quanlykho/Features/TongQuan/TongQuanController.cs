using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using quanlykho.Common.PhanQuyen;

namespace quanlykho.Features.TongQuan;

/// <summary>Trang chủ. Trang không truy vấn CSDL; từng thẻ tự nạp phần HTML của mình (data-tai) để có trạng thái đang tải, lỗi và làm mới riêng.</summary>
public class TongQuanController(TongQuanService service) : Controller
{
    private const string CookieSoNgay = "TongQuan.SoNgay";
    private const int SoNgayToiDa = 365;

    [HttpGet("")]
    public IActionResult Index(int? soNgay)
    {
        if (soNgay is >= 1 and <= SoNgayToiDa)
            Response.Cookies.Append(CookieSoNgay, soNgay.Value.ToString(), new CookieOptions { MaxAge = TimeSpan.FromDays(365), HttpOnly = true, IsEssential = true });
        else if (int.TryParse(Request.Cookies[CookieSoNgay], out var daLuu) && daLuu is >= 1 and <= SoNgayToiDa)
            soNgay = daLuu;
        else
            soNgay = TongQuanService.SoNgayMacDinh;
        return View(soNgay.Value);
    }

    [HttpGet("TongQuan/The/{loai}"), Authorize(Roles = Vai.QuanLy)]
    public async Task<IActionResult> The(LoaiThe loai, int soNgay = TongQuanService.SoNgayMacDinh) =>
        PartialView("_The", await service.TheAsync(loai, Math.Clamp(soNgay, 1, SoNgayToiDa)));
}

[ApiController, Authorize(Roles = Vai.QuanLy), Route("api/tong-quan")]
public class TongQuanApiController(TongQuanService service) : ControllerBase
{
    [HttpGet("{loai}")]
    public async Task<IActionResult> The(LoaiThe loai, int soNgay = TongQuanService.SoNgayMacDinh) => Ok(await service.TheAsync(loai, Math.Clamp(soNgay, 1, 365)));
}
