using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using quanlykho.Common.PhanQuyen;
using quanlykho.Features.Kho;

namespace quanlykho.Features.XepHang;

[Authorize(Roles = Vai.TrongKho), Route("XepHang")]
public class XepHangController(XepHangService service, ViTriService viTri) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(string? maKho)
    {
        var kho = await viTri.KhoAsync();
        maKho ??= kho.FirstOrDefault()?.Ma;
        ViewData["Kho"] = kho;
        ViewData["MaKho"] = maKho;
        return View(maKho is null ? new List<HangChoXep>() : await service.ChoXepAsync(maKho));
    }

    /// <summary>Chuyển vị trí trong kho: cùng màn hình quét với xếp hàng, nguồn là một ô lưu trữ.</summary>
    [HttpGet("ChuyenViTri")]
    public async Task<IActionResult> ChuyenViTri(string? maKho, string? maViTri)
    {
        var kho = await viTri.KhoAsync();
        maKho ??= kho.FirstOrDefault()?.Ma;
        maViTri = maViTri?.Trim();
        ViewData["Kho"] = kho;
        ViewData["MaKho"] = maKho;
        ViewData["MaViTriDi"] = maViTri;
        return View(nameof(Index), maKho is null || string.IsNullOrEmpty(maViTri) ? new List<HangChoXep>() : await service.HangTaiOAsync(maKho, maViTri));
    }
}

[ApiController, Authorize(Roles = Vai.TrongKho), Route("api/xep-hang")]
public class XepHangApiController(XepHangService service) : ControllerBase
{
    [HttpGet("cho-xep")]
    public async Task<IActionResult> ChoXep(string maKho) => Ok(await service.ChoXepAsync(maKho));

    [HttpGet("tai-o")]
    public async Task<IActionResult> TaiO(string maKho, string maViTri) => Ok(await service.HangTaiOAsync(maKho, maViTri));

    [HttpPost]
    public async Task<IActionResult> Xep(XepHangYeuCau body, [FromHeader(Name = "Idempotency-Key")] string? khoaChongLap)
    {
        var kq = await service.XepAsync(body, khoaChongLap);
        return Ok(new { maChuyen = kq.MaChuyen, daGhiTruocDo = kq.DaGhiTruocDo });
    }
}
