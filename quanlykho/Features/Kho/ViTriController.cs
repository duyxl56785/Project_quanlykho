using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using quanlykho.Common.PhanQuyen;

namespace quanlykho.Features.Kho;

[Authorize(Roles = Vai.TrongKho), Route("ViTri")]
public class ViTriController(ViTriService service) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(string? maKho)
    {
        var kho = await service.KhoAsync();
        maKho ??= kho.FirstOrDefault()?.Ma;
        ViewData["Kho"] = kho;
        ViewData["MaKho"] = maKho;
        return View(maKho is null ? new List<ViTriNut>() : await service.CayAsync(maKho));
    }

    [HttpGet("{ma}")]
    public async Task<IActionResult> ChiTiet(string ma) => View(await service.LayAsync(ma));

    [HttpGet("Them"), Authorize(Roles = Vai.QuanLy)]
    public async Task<IActionResult> Them(string maCha) => View(await service.FormThemAsync(maCha));

    [HttpPost("Them"), Authorize(Roles = Vai.QuanLy)]
    public async Task<IActionResult> Them(ViTriForm form)
    {
        await service.NapFormAsync(form);
        if (!ModelState.IsValid)
            return View(form);
        var ma = await service.ThemAsync(form);
        TempData["ThongBao"] = $"Đã thêm vị trí {ma}.";
        return RedirectToAction(nameof(ChiTiet), new { ma = form.MaCha });
    }

    [HttpGet("{ma}/Sua"), Authorize(Roles = Vai.QuanLy)]
    public async Task<IActionResult> Sua(string ma) => View(await service.FormSuaAsync(ma));

    [HttpPost("{ma}/Sua"), Authorize(Roles = Vai.QuanLy)]
    public async Task<IActionResult> Sua(string ma, ViTriForm form)
    {
        form.Ma = ma;
        await service.NapFormAsync(form);
        if (!ModelState.IsValid)
            return View(form);
        await service.SuaAsync(ma, form);
        TempData["ThongBao"] = "Đã lưu vị trí.";
        return RedirectToAction(nameof(ChiTiet), new { ma });
    }

    [HttpPost("{ma}/Khoa"), Authorize(Roles = Vai.QuanLy)]
    public async Task<IActionResult> Khoa(string ma, bool khoa)
    {
        await service.KhoaAsync(ma, khoa);
        TempData["ThongBao"] = khoa ? $"Đã khóa {ma} và các vị trí con." : $"Đã mở khóa {ma} và các vị trí con.";
        return RedirectToAction(nameof(ChiTiet), new { ma });
    }

    [HttpPost("{ma}/Xoa"), Authorize(Roles = Vai.QuanLy)]
    public async Task<IActionResult> Xoa(string ma)
    {
        var cha = (await service.LayAsync(ma)).Nut.MaCha;
        await service.XoaAsync(ma);
        TempData["ThongBao"] = $"Đã xóa vị trí {ma}.";
        return RedirectToAction(nameof(ChiTiet), new { ma = cha });
    }
}

[ApiController, Authorize(Roles = Vai.TrongKho), Route("api/vi-tri")]
public class ViTriApiController(ViTriService service, OPhuHop oPhuHop) : ControllerBase
{
    [HttpGet("o-phu-hop")]
    public async Task<IActionResult> TimO(string maLo, decimal soLuong, string? maKho = null) => Ok(await oPhuHop.TimAsync(maLo, soLuong, maKho));

    [HttpGet("cay")]
    public async Task<IActionResult> Cay(string maKho)
    {
        var nut = await service.CayAsync(maKho);
        return Ok(nut.Where(n => n.MaCha is null || nut.All(c => c.Ma != n.MaCha)).Select(Json));
    }

    [HttpPost, Authorize(Roles = Vai.QuanLy)]
    public async Task<IActionResult> Tao(ViTriForm body)
    {
        var ma = await service.ThemAsync(body);
        return Created($"/api/vi-tri/{ma}", new { ma });
    }

    [HttpPut("{ma}"), Authorize(Roles = Vai.QuanLy)]
    public async Task<IActionResult> Sua(string ma, ViTriForm body)
    {
        await service.SuaAsync(ma, body);
        return NoContent();
    }

    private static object Json(ViTriNut n) => new
    {
        n.Ma, n.Cap, n.Ten, n.LoaiKhu, n.DieuKienBaoQuan, n.Dai, n.Rong, n.Cao, n.TaiTrongToiDa, n.LaOMacDinh, n.DangKhoa, n.TonHienTai,
        con = n.Con.Select(Json),
    };
}
