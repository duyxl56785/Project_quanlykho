using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using quanlykho.Common.PhanQuyen;
using quanlykho.Domain;
using quanlykho.Features.Kho;

namespace quanlykho.Features.KiemKe;

[Authorize(Roles = Vai.TrongKho), Route("KiemKe")]
public class KiemKeController(KiemKeService service, ViTriService viTri) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(string? maKho, TrangThaiKiemKe? trangThai, int trang = 1)
    {
        ViewData["Loc"] = new { maKho, trangThai };
        ViewData["Kho"] = await viTri.KhoAsync();
        return View(await service.DanhSachAsync(maKho, trangThai, trang));
    }

    [HttpGet("{ma}")]
    public async Task<IActionResult> ChiTiet(string ma) => View(await service.LayAsync(ma));

    [HttpGet("Them"), Authorize(Roles = Vai.QuanLy)]
    public async Task<IActionResult> Them(string? maKho) => View(await service.NapFormAsync(new KiemKeForm { MaKho = maKho ?? "" }));

    [HttpPost("Them"), Authorize(Roles = Vai.QuanLy)]
    public async Task<IActionResult> Them(KiemKeForm form)
    {
        await service.NapFormAsync(form);
        if (!ModelState.IsValid)
            return View(form);
        var ma = await service.TaoAsync(form);
        TempData["ThongBao"] = $"Đã lập phiếu kiểm kê {ma}. Bấm Bắt đầu kiểm kê để khóa ô và chốt sổ sách.";
        return RedirectToAction(nameof(ChiTiet), new { ma });
    }

    [HttpPost("{ma}/BatDau"), Authorize(Roles = Vai.QuanLy)]
    public async Task<IActionResult> BatDau(string ma, bool xacNhanGiuHang)
    {
        await service.BatDauAsync(ma, xacNhanGiuHang);
        TempData["ThongBao"] = "Đã khóa các ô và chốt sổ sách; thành viên bắt đầu đếm.";
        return RedirectToAction(nameof(ChiTiet), new { ma });
    }

    [HttpPost("{ma}/Dem")]
    public async Task<IActionResult> Dem(string ma, List<DongDemForm> dem)
    {
        await service.NhapSoDemAsync(ma, dem);
        TempData["ThongBao"] = "Đã lưu số đếm.";
        return RedirectToAction(nameof(ChiTiet), new { ma });
    }

    [HttpPost("{ma}/ThemDong")]
    public async Task<IActionResult> ThemDong(string ma, string maViTri, string maHang, decimal soLuongThucTe, decimal soLon = 0)
    {
        await service.ThemDongAsync(ma, new ThemDongKiemKe(maViTri, maHang, soLuongThucTe, soLon));
        TempData["ThongBao"] = "Đã thêm hàng ngoài danh sách.";
        return RedirectToAction(nameof(ChiTiet), new { ma });
    }

    [HttpPost("{ma}/DuyetDieuChinh"), Authorize(Roles = Vai.QuanLy)]
    public async Task<IActionResult> DuyetDieuChinh(string ma, List<DongDuyetForm> duyet, string? khoaChongLap)
    {
        await service.DuyetDieuChinhAsync(ma, duyet, khoaChongLap);
        TempData["ThongBao"] = "Đã duyệt điều chỉnh; chứng từ đã ghi qua cổng ghi tồn.";
        return RedirectToAction(nameof(ChiTiet), new { ma });
    }

    [HttpPost("{ma}/HoanThanh"), Authorize(Roles = Vai.QuanLy)]
    public async Task<IActionResult> HoanThanh(string ma)
    {
        await service.HoanThanhAsync(ma);
        TempData["ThongBao"] = "Đã hoàn thành kiểm kê và mở khóa ô.";
        return RedirectToAction(nameof(ChiTiet), new { ma });
    }

    [HttpPost("{ma}/Huy"), Authorize(Roles = Vai.QuanLy)]
    public async Task<IActionResult> Huy(string ma)
    {
        await service.HuyAsync(ma);
        TempData["ThongBao"] = $"Đã hủy phiếu {ma} và mở khóa ô.";
        return RedirectToAction(nameof(ChiTiet), new { ma });
    }
}

public record BatDauKiemKe(bool XacNhanGiuHang = false);

[ApiController, Authorize(Roles = Vai.TrongKho), Route("api/kiem-ke")]
public class KiemKeApiController(KiemKeService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> DanhSach(string? maKho, TrangThaiKiemKe? trangThai, int trang = 1)
    {
        var ds = await service.DanhSachAsync(maKho, trangThai, trang);
        return Ok(new { ds.Trang, ds.TongTrang, ds.TongSo, muc = ds.Muc });
    }

    [HttpGet("{ma}")]
    public async Task<IActionResult> ChiTiet(string ma) => Ok(await service.LayAsync(ma));

    [HttpPost, Authorize(Roles = Vai.QuanLy)]
    public async Task<IActionResult> Tao(KiemKeForm body)
    {
        var ma = await service.TaoAsync(body);
        return CreatedAtAction(nameof(ChiTiet), new { ma }, new { ma });
    }

    [HttpPost("{ma}/bat-dau"), Authorize(Roles = Vai.QuanLy)]
    public async Task<IActionResult> BatDau(string ma, BatDauKiemKe body)
    {
        await service.BatDauAsync(ma, body.XacNhanGiuHang);
        return NoContent();
    }

    [HttpPost("{ma}/dem")]
    public async Task<IActionResult> Dem(string ma, List<DongDemForm> body)
    {
        await service.NhapSoDemAsync(ma, body);
        return NoContent();
    }

    [HttpPost("{ma}/them-dong")]
    public async Task<IActionResult> ThemDong(string ma, ThemDongKiemKe body) => StatusCode(201, new { stt = await service.ThemDongAsync(ma, body) });

    [HttpPost("{ma}/duyet-dieu-chinh"), Authorize(Roles = Vai.QuanLy)]
    public async Task<IActionResult> DuyetDieuChinh(string ma, List<DongDuyetForm> body, [FromHeader(Name = "Idempotency-Key")] string? khoaChongLap) =>
        Ok(new { daGhiTruocDo = await service.DuyetDieuChinhAsync(ma, body, khoaChongLap) == Common.KetQuaGhi.DaGhiTruocDo });

    [HttpPost("{ma}/hoan-thanh"), Authorize(Roles = Vai.QuanLy)]
    public async Task<IActionResult> HoanThanh(string ma)
    {
        await service.HoanThanhAsync(ma);
        return NoContent();
    }

    [HttpPost("{ma}/huy"), Authorize(Roles = Vai.QuanLy)]
    public async Task<IActionResult> Huy(string ma)
    {
        await service.HuyAsync(ma);
        return NoContent();
    }
}
