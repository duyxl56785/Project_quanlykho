using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using quanlykho.Common.Loi;
using quanlykho.Common.PhanQuyen;

namespace quanlykho.Features.NhapExcel;

[Authorize(Roles = Vai.QuanTri), Route("NhapExcel")]
public class NhapExcelController(NhapExcelService service) : Controller
{
    public const string KieuXlsx = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    [HttpGet("")]
    public IActionResult Index() => View((KetQuaKiemTra?)null);

    [HttpGet("Mau")]
    public async Task<IActionResult> Mau() => File(await service.TaoMauAsync(), KieuXlsx, "MauNhapDanhMuc.xlsx");

    [HttpPost("KiemTra"), RequestSizeLimit(NhapExcelService.KichThuocToiDa + 64 * 1024)]
    public async Task<IActionResult> KiemTra(IFormFile? tep)
    {
        if (tep is null)
            throw new NghiepVuException("Chọn tệp Excel cần nhập.");
        await using var s = tep.OpenReadStream();
        return View(nameof(Index), await service.KiemTraAsync(s, tep.FileName));
    }

    [HttpPost("Ghi")]
    public async Task<IActionResult> Ghi(string maBam, string tenTep)
    {
        var kq = await service.GhiAsync(maBam, tenTep);
        TempData["ThongBao"] = $"Đã nhập {kq.TenTep}: {kq.SoSanPham} sản phẩm, {kq.SoBienThe} biến thể, {kq.SoNhaCungCap} nhà cung cấp.";
        return RedirectToAction(nameof(Index));
    }
}

[ApiController, Authorize(Roles = Vai.QuanTri), Route("api/nhap-excel")]
public class NhapExcelApiController(NhapExcelService service) : ControllerBase
{
    [HttpGet("mau")]
    public async Task<IActionResult> Mau() => File(await service.TaoMauAsync(), NhapExcelController.KieuXlsx, "MauNhapDanhMuc.xlsx");

    [HttpPost("kiem-tra"), RequestSizeLimit(NhapExcelService.KichThuocToiDa + 64 * 1024)]
    public async Task<IActionResult> KiemTra(IFormFile tep)
    {
        await using var s = tep.OpenReadStream();
        return Ok(await service.KiemTraAsync(s, tep.FileName));
    }

    [HttpPost("{maBam}/ghi")]
    public async Task<IActionResult> Ghi(string maBam, string tenTep) => Ok(await service.GhiAsync(maBam, tenTep));
}
