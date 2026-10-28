using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using quanlykho.Common;
using quanlykho.Common.PhanQuyen;
using quanlykho.Domain;
using quanlykho.Features.Kho;

namespace quanlykho.Features.XuatKho;

[Authorize(Roles = Vai.TrongKho), Route("LenhXuat")]
public class LenhXuatController(LenhXuatService service, ViTriService viTri) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(string? maKho, TrangThaiLenhXuat? trangThai, int trang = 1)
    {
        ViewData["Loc"] = new { maKho, trangThai };
        ViewData["Kho"] = await viTri.KhoAsync();
        return View(await service.DanhSachAsync(maKho, trangThai, trang));
    }

    [HttpGet("{ma}")]
    public async Task<IActionResult> ChiTiet(string ma) => View(await service.LayAsync(ma));

    [HttpPost("Them"), Authorize(Roles = Vai.QuanLy)]
    public async Task<IActionResult> Them(string maDonDat, string nguoiThucHien)
    {
        var ma = await service.TaoAsync(new LapLenhYeuCau(maDonDat, nguoiThucHien));
        TempData["ThongBao"] = $"Đã lập lệnh xuất {ma}.";
        return RedirectToAction(nameof(ChiTiet), new { ma });
    }

    [HttpPost("{ma}/XongLay")]
    public async Task<IActionResult> XongLay(string ma)
    {
        await service.XongLayAsync(ma);
        TempData["ThongBao"] = "Đã báo lấy xong; chờ quản lý kho kiểm tra.";
        return RedirectToAction(nameof(ChiTiet), new { ma });
    }

    [HttpPost("{ma}/KiemTra"), Authorize(Roles = Vai.QuanLy)]
    public async Task<IActionResult> KiemTra(string ma, bool dung, string? ghiChu)
    {
        await service.KiemTraAsync(ma, new KiemTraYeuCau(dung, ghiChu));
        TempData["ThongBao"] = dung ? "Hàng đã lấy đúng; chờ xác nhận xuất." : "Đã trả lệnh về bước lấy hàng.";
        return RedirectToAction(nameof(ChiTiet), new { ma });
    }

    [HttpPost("{ma}/XacNhan"), Authorize(Roles = Vai.QuanLy)]
    public async Task<IActionResult> XacNhan(string ma, string? khoaChongLap)
    {
        var (px, _) = await service.XacNhanAsync(ma, khoaChongLap);
        TempData["ThongBao"] = $"Đã xuất kho, phiếu xuất {px}.";
        return RedirectToAction(nameof(ChiTiet), new { ma });
    }
}

[ApiController, Authorize(Roles = Vai.TrongKho), Route("api/lenh-xuat")]
public class LenhXuatApiController(LenhXuatService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> DanhSach(string? maKho, TrangThaiLenhXuat? trangThai, int trang = 1)
    {
        var ds = await service.DanhSachAsync(maKho, trangThai, trang);
        return Ok(new { ds.Trang, ds.TongTrang, ds.TongSo, muc = ds.Muc });
    }

    [HttpGet("{ma}")]
    public async Task<IActionResult> ChiTiet(string ma) => Ok(await service.LayAsync(ma));

    [HttpPost, Authorize(Roles = Vai.QuanLy)]
    public async Task<IActionResult> Tao(LapLenhYeuCau body)
    {
        var ma = await service.TaoAsync(body);
        return CreatedAtAction(nameof(ChiTiet), new { ma }, new { ma });
    }

    [HttpPost("{ma}/lay")]
    public async Task<IActionResult> Lay(string ma, LayHangYeuCau body) => StatusCode(201, new { maDong = await service.LayHangAsync(ma, body) });

    [HttpDelete("{ma}/lay/{maDong:long}")]
    public async Task<IActionResult> XoaLay(string ma, long maDong)
    {
        await service.XoaDongLayAsync(ma, maDong);
        return NoContent();
    }

    [HttpPost("{ma}/xong-lay")]
    public async Task<IActionResult> XongLay(string ma)
    {
        await service.XongLayAsync(ma);
        return NoContent();
    }

    [HttpPost("{ma}/kiem-tra"), Authorize(Roles = Vai.QuanLy)]
    public async Task<IActionResult> KiemTra(string ma, KiemTraYeuCau body)
    {
        await service.KiemTraAsync(ma, body);
        return NoContent();
    }

    [HttpPost("{ma}/xac-nhan"), Authorize(Roles = Vai.QuanLy)]
    public async Task<IActionResult> XacNhan(string ma, [FromHeader(Name = "Idempotency-Key")] string? khoaChongLap)
    {
        var (px, kq) = await service.XacNhanAsync(ma, khoaChongLap);
        return Ok(new { maPhieuXuat = px, daGhiTruocDo = kq == KetQuaGhi.DaGhiTruocDo });
    }
}

[Authorize(Roles = Vai.TrongKho), Route("PhieuXuat")]
public class PhieuXuatController(PhieuXuatService service, ViTriService viTri) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(string? maKho, LoaiPhieuXuat? loai, int trang = 1)
    {
        ViewData["Loc"] = new { maKho, loai };
        ViewData["Kho"] = await viTri.KhoAsync();
        return View(await service.DanhSachAsync(maKho, loai, trang));
    }

    [HttpGet("{ma}")]
    public async Task<IActionResult> ChiTiet(string ma) => View(await service.LayAsync(ma));

    [HttpGet("Them"), Authorize(Roles = Vai.QuanLy)]
    public async Task<IActionResult> Them(string? maKho) => View(await service.NapFormAsync(new PhieuXuatForm { MaKho = maKho ?? "", Dong = [new()] }));

    [HttpPost("Them"), Authorize(Roles = Vai.QuanLy)]
    public async Task<IActionResult> Them(PhieuXuatForm form)
    {
        await service.NapFormAsync(form);
        if (!ModelState.IsValid)
            return View(form);
        var (ma, _) = await service.TaoKhacAsync(form);
        TempData["ThongBao"] = $"Đã xuất kho, phiếu xuất {ma}.";
        return RedirectToAction(nameof(ChiTiet), new { ma });
    }
}

public record PhieuXuatApiTao(string MaKho, LoaiPhieuXuat LoaiPhieu, string? MaNhaCungCap, string LyDo, List<DongPhieuXuatForm> Dong);

[ApiController, Authorize(Roles = Vai.TrongKho), Route("api/phieu-xuat")]
public class PhieuXuatApiController(PhieuXuatService service) : ControllerBase
{
    [HttpGet("{ma}")]
    public async Task<IActionResult> ChiTiet(string ma) => Ok(await service.LayAsync(ma));

    [HttpPost, Authorize(Roles = Vai.QuanLy)]
    public async Task<IActionResult> Tao(PhieuXuatApiTao body, [FromHeader(Name = "Idempotency-Key")] string? khoaChongLap)
    {
        var (ma, kq) = await service.TaoKhacAsync(new PhieuXuatForm
        {
            MaKho = body.MaKho, LoaiPhieu = body.LoaiPhieu, MaNhaCungCap = body.MaNhaCungCap, LyDo = body.LyDo, Dong = body.Dong, KhoaChongLap = khoaChongLap ?? "",
        });
        return kq == KetQuaGhi.DaGhiTruocDo ? Ok(new { ma, daGhiTruocDo = true }) : CreatedAtAction(nameof(ChiTiet), new { ma }, new { ma, daGhiTruocDo = false });
    }
}
