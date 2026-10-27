using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using quanlykho.Common.PhanQuyen;
using quanlykho.Domain;

namespace quanlykho.Features.NhapKho;

[Authorize(Roles = Vai.TrongKho), Route("PhieuNhap")]
public class PhieuNhapController(PhieuNhapService service) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(string? tuKhoa, string? maKho, TrangThaiPhieu? trangThai, int trang = 1)
    {
        ViewData["Loc"] = new { tuKhoa, maKho, trangThai };
        ViewData["DanhSach"] = await service.NapDanhSachAsync(new PhieuNhapForm());
        return View(await service.DanhSachAsync(tuKhoa, maKho, trangThai, trang));
    }

    [HttpGet("{ma}")]
    public async Task<IActionResult> ChiTiet(string ma) => View(await service.LayAsync(ma));

    [HttpGet("Them")]
    public async Task<IActionResult> Them(string? maDonMua) =>
        View(await service.NapDanhSachAsync(new PhieuNhapForm { MaDonMua = maDonMua }));

    [HttpPost("Them")]
    public async Task<IActionResult> Them(PhieuNhapForm form)
    {
        await service.NapDanhSachAsync(form);
        if (!ModelState.IsValid)
            return View(form);
        var ma = await service.TaoAsync(form);
        return RedirectToAction(nameof(ChiTiet), new { ma });
    }

    [HttpPost("{ma}/Huy")]
    public async Task<IActionResult> Huy(string ma)
    {
        await service.HuyAsync(ma);
        TempData["ThongBao"] = $"Đã hủy hóa đơn nhập {ma}.";
        return RedirectToAction(nameof(ChiTiet), new { ma });
    }
}

public record PhieuNhapApiTao(LoaiPhieuNhap LoaiPhieu, string? MaDonMua, string? MaKho, string? GhiChu);

[ApiController, Authorize(Roles = Vai.TrongKho), Route("api/phieu-nhap")]
public class PhieuNhapApiController(PhieuNhapService service) : ControllerBase
{
    // Bỏ trường null: JSON trả cho thủ kho không có trường giá.
    private static readonly JsonSerializerOptions BoNull = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() },
    };

    [HttpGet("{ma}")]
    public async Task<IActionResult> ChiTiet(string ma)
    {
        var p = await service.LayAsync(ma);
        return new JsonResult(new
        {
            p.Ma, p.LoaiPhieu, p.MaDonMua, p.TenNhaCungCap, p.MaKho, p.NgayNhap, p.TrangThai, p.GhiChu, p.TongTien,
            dong = p.Dong.Select(d => new { d.STT, d.MaLo, d.MaBienThe, d.TenHang, d.TenDonVi, d.SoLuong, d.HanDung, d.ChatLuong, d.DonGia, d.ThanhTien }),
        }, BoNull);
    }

    [HttpPost]
    public async Task<IActionResult> Tao(PhieuNhapApiTao body)
    {
        var ma = await service.TaoAsync(new PhieuNhapForm { LoaiPhieu = body.LoaiPhieu, MaDonMua = body.MaDonMua, MaKho = body.MaKho, GhiChu = body.GhiChu });
        return CreatedAtAction(nameof(ChiTiet), new { ma }, new { ma });
    }

    [HttpPost("{ma}/dong")]
    public async Task<IActionResult> ThemDong(string ma, DongPhieuNhapForm body)
    {
        var (stt, maLo) = await service.ThemDongAsync(ma, body);
        return CreatedAtAction(nameof(ChiTiet), new { ma }, new { stt, maLo });
    }

    [HttpDelete("{ma}/dong/{stt:int}")]
    public async Task<IActionResult> XoaDong(string ma, int stt)
    {
        await service.XoaDongAsync(ma, stt);
        return NoContent();
    }
}
