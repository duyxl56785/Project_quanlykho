using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using quanlykho.Common.PhanQuyen;
using quanlykho.Domain;
using quanlykho.Features.Kho;

namespace quanlykho.Features.XuatKho;

[Authorize(Roles = Vai.XemDonDat), Route("DonDat")]
public class DonDatController(DonDatService service, GiuHangService giuHang) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(string? tuKhoa, string? maKho, TrangThaiDonDat? trangThai, int trang = 1)
    {
        ViewData["Loc"] = new { tuKhoa, maKho, trangThai };
        ViewData["DanhSach"] = await service.NapDanhSachAsync(new DonDatForm());
        return View(await service.DanhSachAsync(tuKhoa, maKho, trangThai, trang));
    }

    [HttpGet("{ma}")]
    public async Task<IActionResult> ChiTiet(string ma) => View(await service.LayAsync(ma));

    [HttpGet("Them"), Authorize(Roles = Vai.DatHang)]
    public async Task<IActionResult> Them() => View(await service.NapDanhSachAsync(new DonDatForm { Dong = [new() { SoLuong = 1 }] }));

    [HttpPost("Them"), Authorize(Roles = Vai.DatHang)]
    public async Task<IActionResult> Them(DonDatForm form, bool guiDuyet)
    {
        await service.NapDanhSachAsync(form);
        if (!ModelState.IsValid)
            return View(form);
        var ma = await service.TaoAsync(form, guiDuyet);
        TempData["ThongBao"] = guiDuyet ? $"Đã tạo đơn {ma} và gửi quản lý kho duyệt." : $"Đã lưu nháp đơn {ma}.";
        return RedirectToAction(nameof(ChiTiet), new { ma });
    }

    [HttpGet("{ma}/Sua"), Authorize(Roles = Vai.DatHang)]
    public async Task<IActionResult> Sua(string ma) => View(await service.LayFormAsync(ma));

    [HttpPost("{ma}/Sua"), Authorize(Roles = Vai.DatHang)]
    public async Task<IActionResult> Sua(string ma, DonDatForm form)
    {
        form.Ma = ma;
        await service.NapDanhSachAsync(form);
        if (!ModelState.IsValid)
            return View(form);
        await service.SuaAsync(ma, form);
        TempData["ThongBao"] = "Đã lưu đơn đặt hàng.";
        return RedirectToAction(nameof(ChiTiet), new { ma });
    }

    [HttpPost("{ma}/GuiDuyet"), Authorize(Roles = Vai.DatHang)]
    public async Task<IActionResult> GuiDuyet(string ma)
    {
        await service.GuiDuyetAsync(ma);
        TempData["ThongBao"] = $"Đã gửi đơn {ma} cho quản lý kho duyệt.";
        return RedirectToAction(nameof(ChiTiet), new { ma });
    }

    [HttpPost("{ma}/Huy")]
    public async Task<IActionResult> Huy(string ma)
    {
        await service.HuyAsync(ma);
        TempData["ThongBao"] = $"Đã hủy đơn {ma}.";
        return RedirectToAction(nameof(ChiTiet), new { ma });
    }
    [HttpPost("Duyet"), Authorize(Roles = Vai.QuanLy)]
    public async Task<IActionResult> Duyet(List<string> ma)
    {
        await giuHang.DuyetAsync(ma);
        TempData["ThongBao"] = $"Đã duyệt và giữ hàng cho {string.Join(", ", ma)}.";
        return ma.Count == 1 ? RedirectToAction(nameof(ChiTiet), new { ma = ma[0] }) : RedirectToAction(nameof(Index));
    }

    [HttpPost("{ma}/BoGiu"), Authorize(Roles = Vai.QuanLy)]
    public async Task<IActionResult> BoGiu(string ma)
    {
        await giuHang.BoGiuAsync(ma);
        TempData["ThongBao"] = $"Đã bỏ giữ hàng của đơn {ma}; đơn quay lại Chờ duyệt.";
        return RedirectToAction(nameof(ChiTiet), new { ma });
    }
}

public record DonDatApiDuyet(List<string> Ma);

public record DonDatApiTao(string MaKho, string MaNoiNhan, DateOnly? HanGiao, string? GhiChu, List<DongDonDatForm> Dong, bool GuiDuyet = false);

[ApiController, Authorize(Roles = Vai.XemDonDat), Route("api/don-dat")]
public class DonDatApiController(DonDatService service, GiuHangService giuHang) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> DanhSach(string? maKho, TrangThaiDonDat? trangThai, int trang = 1)
    {
        var ds = await service.DanhSachAsync(null, maKho, trangThai, trang);
        return Ok(new { ds.Trang, ds.TongTrang, ds.TongSo, muc = ds.Muc });
    }

    [HttpGet("{ma}")]
    public async Task<IActionResult> ChiTiet(string ma) => Ok(await service.LayAsync(ma));

    [HttpPost, Authorize(Roles = Vai.DatHang)]
    public async Task<IActionResult> Tao(DonDatApiTao body)
    {
        var ma = await service.TaoAsync(new DonDatForm
        {
            MaKho = body.MaKho, MaNoiNhan = body.MaNoiNhan, HanGiao = body.HanGiao, GhiChu = body.GhiChu, Dong = body.Dong,
        }, body.GuiDuyet);
        return CreatedAtAction(nameof(ChiTiet), new { ma }, new { ma });
    }

    [HttpPost("{ma}/gui-duyet"), Authorize(Roles = Vai.DatHang)]
    public async Task<IActionResult> GuiDuyet(string ma)
    {
        await service.GuiDuyetAsync(ma);
        return NoContent();
    }

    [HttpPost("{ma}/huy")]
    public async Task<IActionResult> Huy(string ma)
    {
        await service.HuyAsync(ma);
        return NoContent();
    }

    [HttpPost("duyet"), Authorize(Roles = Vai.QuanLy)]
    public async Task<IActionResult> Duyet(DonDatApiDuyet body)
    {
        await giuHang.DuyetAsync(body.Ma);
        return NoContent();
    }

    [HttpPost("{ma}/bo-giu"), Authorize(Roles = Vai.QuanLy)]
    public async Task<IActionResult> BoGiu(string ma)
    {
        await giuHang.BoGiuAsync(ma);
        return NoContent();
    }
}

[Authorize, Route("TonKhaDung")]
public class TonKhaDungController(TonKhaDung service, ViTriService viTri) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(string? tuKhoa, string? maKho, bool chiConHang = true, int trang = 1)
    {
        ViewData["Loc"] = new { tuKhoa, maKho, chiConHang };
        ViewData["Kho"] = await viTri.KhoAsync();
        return View(await service.TraCuuAsync(tuKhoa, maKho, chiConHang, trang));
    }
}

[ApiController, Authorize, Route("api/ton-kha-dung")]
public class TonKhaDungApiController(TonKhaDung service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> TraCuu(string? tuKhoa, string? maKho, bool chiConHang = false, int trang = 1)
    {
        var ds = await service.TraCuuAsync(tuKhoa, maKho, chiConHang, trang);
        return Ok(new { ds.Trang, ds.TongTrang, ds.TongSo, muc = ds.Muc.Select(d => new { d.MaBienThe, d.TenHang, d.MaVach, d.DonViTinh, d.TheoKho, d.Tong }) });
    }
}
