using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using quanlykho.Common;
using quanlykho.Common.PhanQuyen;
using quanlykho.Domain;
using quanlykho.Features.Kho;

namespace quanlykho.Features.DieuChuyen;

[Authorize(Roles = Vai.TrongKho), Route("DieuChuyen")]
public class DieuChuyenController(DieuChuyenService service, ViTriService viTri) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(string? maKho, TrangThaiDieuChuyen? trangThai, int trang = 1)
    {
        ViewData["Loc"] = new { maKho, trangThai };
        ViewData["Kho"] = await viTri.KhoAsync();
        return View(await service.DanhSachAsync(maKho, trangThai, trang));
    }

    [HttpGet("{ma}")]
    public async Task<IActionResult> ChiTiet(string ma) => View(await service.LayAsync(ma));

    [HttpGet("Them"), Authorize(Roles = Vai.QuanLy)]
    public async Task<IActionResult> Them(string? maKhoDi) => View(await service.NapFormAsync(new DieuChuyenForm { MaKhoDi = maKhoDi ?? "", Dong = [new()] }));

    [HttpPost("Them"), Authorize(Roles = Vai.QuanLy)]
    public async Task<IActionResult> Them(DieuChuyenForm form, bool luu)
    {
        await service.NapFormAsync(form);
        if (!ModelState.IsValid)
            return View(form);
        var ma = await service.TaoAsync(form, luu);
        TempData["ThongBao"] = luu ? $"Đã lưu phiếu {ma}: hàng đã trừ ở kho đi, chờ kho đến nhận." : $"Đã lưu tạm phiếu {ma}.";
        return RedirectToAction(nameof(ChiTiet), new { ma });
    }

    [HttpGet("{ma}/Sua"), Authorize(Roles = Vai.QuanLy)]
    public async Task<IActionResult> Sua(string ma) => View(await service.LayFormAsync(ma));

    [HttpPost("{ma}/Sua"), Authorize(Roles = Vai.QuanLy)]
    public async Task<IActionResult> Sua(string ma, DieuChuyenForm form, bool luu)
    {
        form.Ma = ma;
        await service.NapFormAsync(form);
        if (!ModelState.IsValid)
            return View(form);
        await service.SuaAsync(ma, form, luu);
        TempData["ThongBao"] = luu ? $"Đã lưu phiếu {ma}: hàng đã trừ ở kho đi, chờ kho đến nhận." : "Đã lưu tạm phiếu.";
        return RedirectToAction(nameof(ChiTiet), new { ma });
    }

    [HttpPost("{ma}/Luu"), Authorize(Roles = Vai.QuanLy)]
    public async Task<IActionResult> Luu(string ma, string? khoaChongLap)
    {
        await service.LuuAsync(ma, khoaChongLap);
        TempData["ThongBao"] = "Đã lưu phiếu: hàng đã trừ ở kho đi, chờ kho đến nhận.";
        return RedirectToAction(nameof(ChiTiet), new { ma });
    }

    [HttpPost("{ma}/Nhan")]
    public async Task<IActionResult> Nhan(string ma, List<DongNhanForm> nhan, string? khoaChongLap)
    {
        await service.NhanAsync(ma, nhan, khoaChongLap);
        TempData["ThongBao"] = "Đã nhận hàng vào khu tiếp nhận; xếp lên kệ ở màn hình Xếp hàng.";
        return RedirectToAction(nameof(ChiTiet), new { ma });
    }

    [HttpPost("{ma}/Huy"), Authorize(Roles = Vai.QuanLy)]
    public async Task<IActionResult> Huy(string ma)
    {
        await service.HuyAsync(ma);
        TempData["ThongBao"] = $"Đã hủy phiếu {ma}.";
        return RedirectToAction(nameof(ChiTiet), new { ma });
    }
}

public record DieuChuyenApiTao(string MaKhoDi, string MaKhoDen, DateOnly? NgayChuyen, string? PhuongTien, string? NguoiVanChuyen, string? LyDo,
    List<DongDieuChuyenForm> Dong, bool Luu = false);

[ApiController, Authorize(Roles = Vai.TrongKho), Route("api/dieu-chuyen")]
public class DieuChuyenApiController(DieuChuyenService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> DanhSach(string? maKho, TrangThaiDieuChuyen? trangThai, int trang = 1)
    {
        var ds = await service.DanhSachAsync(maKho, trangThai, trang);
        return Ok(new { ds.Trang, ds.TongTrang, ds.TongSo, muc = ds.Muc });
    }

    [HttpGet("dem-cho-nhan")]
    public async Task<IActionResult> DemChoNhan() => Ok(new { so = await service.DemChoNhanAsync() });

    [HttpGet("{ma}")]
    public async Task<IActionResult> ChiTiet(string ma) => Ok(await service.LayAsync(ma));

    [HttpPost, Authorize(Roles = Vai.QuanLy)]
    public async Task<IActionResult> Tao(DieuChuyenApiTao body, [FromHeader(Name = "Idempotency-Key")] string? khoaChongLap)
    {
        var ma = await service.TaoAsync(new DieuChuyenForm
        {
            MaKhoDi = body.MaKhoDi, MaKhoDen = body.MaKhoDen, NgayChuyen = body.NgayChuyen, PhuongTien = body.PhuongTien,
            NguoiVanChuyen = body.NguoiVanChuyen, LyDo = body.LyDo, Dong = body.Dong, KhoaChongLap = khoaChongLap ?? "",
        }, body.Luu);
        return CreatedAtAction(nameof(ChiTiet), new { ma }, new { ma });
    }

    [HttpPost("{ma}/luu"), Authorize(Roles = Vai.QuanLy)]
    public async Task<IActionResult> Luu(string ma, [FromHeader(Name = "Idempotency-Key")] string? khoaChongLap) =>
        Ok(new { daGhiTruocDo = await service.LuuAsync(ma, khoaChongLap) == KetQuaGhi.DaGhiTruocDo });

    [HttpPost("{ma}/nhan")]
    public async Task<IActionResult> Nhan(string ma, List<DongNhanForm> body, [FromHeader(Name = "Idempotency-Key")] string? khoaChongLap) =>
        Ok(new { daGhiTruocDo = await service.NhanAsync(ma, body, khoaChongLap) == KetQuaGhi.DaGhiTruocDo });

    [HttpPost("{ma}/huy"), Authorize(Roles = Vai.QuanLy)]
    public async Task<IActionResult> Huy(string ma)
    {
        await service.HuyAsync(ma);
        return NoContent();
    }
}
