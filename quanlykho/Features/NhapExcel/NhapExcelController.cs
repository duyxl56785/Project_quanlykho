using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using quanlykho.Common.Loi;
using quanlykho.Common.GiaoDien;
using quanlykho.Common.PhanQuyen;

namespace quanlykho.Features.NhapExcel;

public record TonDauKyTrang(List<LuaChon> Kho, string? MaKho, KetQuaTonDauKy? KetQua);

/// <summary>Danh mục: chỉ quản trị viên; tồn đầu kỳ: quản lý kho của kho đó.</summary>
[Authorize(Roles = Vai.QuanLy), Route("NhapExcel")]
public class NhapExcelController(NhapExcelService service, NhapTonDauKyService tonDauKy) : Controller
{
    public const string KieuXlsx = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    [HttpGet("TonDauKy")]
    public async Task<IActionResult> TonDauKy(string? maKho) => View(new TonDauKyTrang(await tonDauKy.KhoAsync(), maKho, null));

    [HttpGet("TonDauKy/Mau")]
    public IActionResult MauTonDauKy() => File(NhapTonDauKyService.TaoMau(), KieuXlsx, "MauTonDauKy.xlsx");

    /// <summary>Tên action khác view để lỗi nghiệp vụ quay lại trang trước kèm thông báo.</summary>
    [HttpPost("TonDauKy"), RequestSizeLimit(NhapExcelService.KichThuocToiDa + 64 * 1024)]
    public async Task<IActionResult> NhapTonDauKy(string maKho, IFormFile? tep)
    {
        if (tep is null)
            throw new NghiepVuException("Chọn tệp Excel tồn đầu kỳ.");
        await using var s = tep.OpenReadStream();
        var kq = await tonDauKy.NhapAsync(maKho, s, tep.FileName);
        if (kq.MaPhieu is null)
            return View(nameof(TonDauKy), new TonDauKyTrang(await tonDauKy.KhoAsync(), maKho, kq));
        TempData["ThongBao"] = $"Đã sinh phiếu nhập tồn đầu kỳ {kq.MaPhieu} ({kq.SoDong} dòng, Lưu tạm). Kiểm tra rồi bấm Xác nhận để ghi tồn.";
        return Redirect($"/PhieuNhap/{kq.MaPhieu}");
    }

    [HttpGet(""), Authorize(Roles = Vai.QuanTri)]
    public IActionResult Index() => View((KetQuaKiemTra?)null);

    [HttpGet("Mau"), Authorize(Roles = Vai.QuanTri)]
    public async Task<IActionResult> Mau() => File(await service.TaoMauAsync(), KieuXlsx, "MauNhapDanhMuc.xlsx");

    [HttpPost("KiemTra"), Authorize(Roles = Vai.QuanTri), RequestSizeLimit(NhapExcelService.KichThuocToiDa + 64 * 1024)]
    public async Task<IActionResult> KiemTra(IFormFile? tep)
    {
        if (tep is null)
            throw new NghiepVuException("Chọn tệp Excel cần nhập.");
        await using var s = tep.OpenReadStream();
        return View(nameof(Index), await service.KiemTraAsync(s, tep.FileName));
    }

    [HttpPost("Ghi"), Authorize(Roles = Vai.QuanTri)]
    public async Task<IActionResult> Ghi(string maBam, string tenTep)
    {
        var kq = await service.GhiAsync(maBam, tenTep);
        TempData["ThongBao"] = $"Đã nhập {kq.TenTep}: {kq.SoSanPham} sản phẩm, {kq.SoBienThe} biến thể, {kq.SoNhaCungCap} nhà cung cấp.";
        return RedirectToAction(nameof(Index));
    }
}

[ApiController, Authorize(Roles = Vai.QuanLy), Route("api/nhap-excel")]
public class NhapExcelApiController(NhapExcelService service, NhapTonDauKyService tonDauKy) : ControllerBase
{
    [HttpGet("ton-dau-ky/mau")]
    public IActionResult MauTonDauKy() => File(NhapTonDauKyService.TaoMau(), NhapExcelController.KieuXlsx, "MauTonDauKy.xlsx");

    [HttpPost("ton-dau-ky"), RequestSizeLimit(NhapExcelService.KichThuocToiDa + 64 * 1024)]
    public async Task<IActionResult> NhapTonDauKy(string maKho, IFormFile tep)
    {
        await using var s = tep.OpenReadStream();
        return Ok(await tonDauKy.NhapAsync(maKho, s, tep.FileName));
    }

    [HttpGet("mau"), Authorize(Roles = Vai.QuanTri)]
    public async Task<IActionResult> Mau() => File(await service.TaoMauAsync(), NhapExcelController.KieuXlsx, "MauNhapDanhMuc.xlsx");

    [HttpPost("kiem-tra"), Authorize(Roles = Vai.QuanTri), RequestSizeLimit(NhapExcelService.KichThuocToiDa + 64 * 1024)]
    public async Task<IActionResult> KiemTra(IFormFile tep)
    {
        await using var s = tep.OpenReadStream();
        return Ok(await service.KiemTraAsync(s, tep.FileName));
    }

    [HttpPost("{maBam}/ghi"), Authorize(Roles = Vai.QuanTri)]
    public async Task<IActionResult> Ghi(string maBam, string tenTep) => Ok(await service.GhiAsync(maBam, tenTep));
}
