using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using quanlykho.Common.PhanQuyen;

namespace quanlykho.Features.NhatKyThaoTac;

[Authorize(Roles = Vai.QuanTri), Route("NhatKy")]
public class NhatKyController(NhatKyService service) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index([FromQuery] LocNhatKy loc, int trang = 1) => View(await service.TraCuuAsync(loc, trang));
}

[ApiController, Authorize(Roles = Vai.QuanTri), Route("api/nhat-ky")]
public class NhatKyApiController(NhatKyService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> TraCuu([FromQuery] LocNhatKy loc, int trang = 1)
    {
        var kq = await service.TraCuuAsync(loc, trang);
        return kq.Loc.Loi is { } loi ? UnprocessableEntity(new { loi })
            : Ok(new { kq.Trang.Trang, kq.Trang.TongTrang, kq.Trang.TongSo, muc = kq.Trang.Muc });
    }
}
