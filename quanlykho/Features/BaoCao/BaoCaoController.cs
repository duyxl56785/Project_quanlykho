using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using quanlykho.Common.PhanQuyen;

namespace quanlykho.Features.BaoCao;

[Authorize(Roles = Vai.TrongKho), Route("BaoCao")]
public class BaoCaoController(BaoCaoService service) : Controller
{
    [HttpGet("")]
    public IActionResult Index() => View();

    [HttpGet("TongHop")]
    public async Task<IActionResult> TongHop([FromQuery] LocBaoCao loc, int trang = 1) => View(await service.TongHopAsync(loc, trang));

    [HttpGet("TheoLo")]
    public async Task<IActionResult> TheoLo([FromQuery] LocBaoCao loc, int trang = 1) => View(await service.TheoLoAsync(loc, trang));

    [HttpGet("TheoViTri")]
    public async Task<IActionResult> TheoViTri([FromQuery] LocBaoCao loc, int trang = 1) => View(await service.TheoViTriAsync(loc, trang));

    [HttpGet("NhapXuatTon")]
    public async Task<IActionResult> NhapXuatTon([FromQuery] LocBaoCao loc, int trang = 1) => View(await service.NhapXuatTonAsync(loc, trang));

    [HttpGet("TheKho")]
    public async Task<IActionResult> TheKho([FromQuery] LocBaoCao loc, int trang = 1) => View(await service.TheKhoAsync(loc, trang));

    [HttpGet("GoiY")]
    public async Task<IActionResult> GoiY([FromQuery] LocBaoCao loc) => View(await service.GoiYAsync(loc));
}

[ApiController, Authorize(Roles = Vai.TrongKho), Route("api/bao-cao")]
public class BaoCaoApiController(BaoCaoService service) : ControllerBase
{
    [HttpGet("tong-hop")]
    public async Task<IActionResult> TongHop([FromQuery] LocBaoCao loc, int trang = 1)
    {
        var kq = await service.TongHopAsync(loc, trang);
        return Ok(new { kq.Kho, kq.Trang.Trang, kq.Trang.TongTrang, kq.Trang.TongSo, muc = kq.Trang.Muc, tongCong = kq.TongCong });
    }

    [HttpGet("theo-lo")]
    public async Task<IActionResult> TheoLo([FromQuery] LocBaoCao loc, int trang = 1) => Ok(Trang((await service.TheoLoAsync(loc, trang)).Trang));

    [HttpGet("theo-vi-tri")]
    public async Task<IActionResult> TheoViTri([FromQuery] LocBaoCao loc, int trang = 1) => Ok(Trang((await service.TheoViTriAsync(loc, trang)).Trang));

    [HttpGet("nhap-xuat-ton")]
    public async Task<IActionResult> NhapXuatTon([FromQuery] LocBaoCao loc, int trang = 1)
    {
        var kq = await service.NhapXuatTonAsync(loc, trang);
        return kq.Loc.Loi is { } loi ? UnprocessableEntity(new { loi })
            : Ok(new { kq.Loc.TuNgay, kq.Loc.DenNgay, kq.Trang.Trang, kq.Trang.TongTrang, kq.Trang.TongSo, muc = kq.Trang.Muc, tongCong = kq.TongCong });
    }

    [HttpGet("the-kho")]
    public async Task<IActionResult> TheKho([FromQuery] LocBaoCao loc, int trang = 1)
    {
        var kq = await service.TheKhoAsync(loc, trang);
        return kq.Loc.Loi is { } loi ? UnprocessableEntity(new { loi })
            : Ok(new { kq.TenHang, kq.DonViTinh, kq.DauKy, kq.CuoiKy, kq.Trang.Trang, kq.Trang.TongTrang, kq.Trang.TongSo, muc = kq.Trang.Muc });
    }

    [HttpGet("goi-y")]
    public async Task<IActionResult> GoiY([FromQuery] LocBaoCao loc)
    {
        var kq = await service.GoiYAsync(loc);
        return kq.Loc.Loi is { } loi ? UnprocessableEntity(new { loi })
            : Ok(kq.Dong.Select(d => new { loai = d.Loai.ToString(), d.Tong, d.CoPhanHoi, d.ChapNhan, tyLe = d.TyLe }));
    }

    private static object Trang<T>(Common.GiaoDien.TrangDuLieu<T> t) => new { t.Trang, t.TongTrang, t.TongSo, muc = t.Muc };
}
