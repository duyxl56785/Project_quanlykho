using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using quanlykho.Common.PhanQuyen;
using quanlykho.Domain;

namespace quanlykho.Features.NhapKho;

[Authorize(Roles = Vai.TrongKho), Route("DonMua")]
public class DonMuaController(DonMuaService service) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(string? tuKhoa, string? maNhaCungCap, string? maKho, DateOnly? tuNgay, DateOnly? denNgay, TrangThaiDonMua? trangThai, int trang = 1)
    {
        ViewData["Loc"] = new { tuKhoa, maNhaCungCap, maKho, tuNgay, denNgay, trangThai };
        ViewData["DanhSach"] = await service.NapDanhSachAsync(new DonMuaForm());
        return View(await service.DanhSachAsync(tuKhoa, maNhaCungCap, maKho, tuNgay, denNgay, trangThai, trang));
    }

    [HttpGet("{ma}")]
    public async Task<IActionResult> ChiTiet(string ma) => View(await service.LayAsync(ma));

    [HttpGet("Them"), Authorize(Roles = Vai.QuanLy)]
    public async Task<IActionResult> Them() => View(await service.NapDanhSachAsync(new DonMuaForm { Dong = [new()] }));

    [HttpPost("Them"), Authorize(Roles = Vai.QuanLy)]
    public async Task<IActionResult> Them(DonMuaForm form)
    {
        await service.NapDanhSachAsync(form);
        if (!ModelState.IsValid)
            return View(form);
        var ma = await service.TaoAsync(form);
        TempData["ThongBao"] = $"Đã lập đơn mua {ma}.";
        return RedirectToAction(nameof(ChiTiet), new { ma });
    }

    [HttpGet("{ma}/Sua"), Authorize(Roles = Vai.QuanLy)]
    public async Task<IActionResult> Sua(string ma) => View(await service.LayFormAsync(ma));

    [HttpPost("{ma}/Sua"), Authorize(Roles = Vai.QuanLy)]
    public async Task<IActionResult> Sua(string ma, DonMuaForm form)
    {
        form.Ma = ma;
        await service.NapDanhSachAsync(form);
        if (!ModelState.IsValid)
            return View(form);
        await service.SuaAsync(ma, form);
        TempData["ThongBao"] = "Đã lưu đơn mua.";
        return RedirectToAction(nameof(ChiTiet), new { ma });
    }

    [HttpPost("{ma}/XacNhan"), Authorize(Roles = Vai.QuanLy)]
    public async Task<IActionResult> XacNhan(string ma)
    {
        await service.XacNhanAsync(ma);
        TempData["ThongBao"] = $"Đơn {ma} đã thành đơn chính thức.";
        return RedirectToAction(nameof(ChiTiet), new { ma });
    }

    [HttpPost("{ma}/Huy"), Authorize(Roles = Vai.QuanLy)]
    public async Task<IActionResult> Huy(string ma)
    {
        await service.HuyAsync(ma);
        TempData["ThongBao"] = $"Đã hủy đơn {ma}.";
        return RedirectToAction(nameof(ChiTiet), new { ma });
    }
}

public record DonMuaApiDong(string MaBienThe, decimal SoLuong, decimal DonGia, string? TenDonVi = null);

public record DonMuaApiTao(string MaNhaCungCap, string MaKho, string? GhiChu, List<DonMuaApiDong> Dong);

[ApiController, Authorize(Roles = Vai.TrongKho), Route("api/don-mua")]
public class DonMuaApiController(DonMuaService service, NguoiDungHienTai nguoiDung) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> DanhSach(string? maKho, TrangThaiDonMua? trangThai, int trang = 1)
    {
        var ds = await service.DanhSachAsync(null, null, maKho, null, null, trangThai, trang);
        var xemGia = User.IsInRole(Vai.QuanTri) || User.IsInRole(Vai.QuanLyKho);
        return Ok(new
        {
            ds.Trang, ds.TongTrang, ds.TongSo,
            muc = ds.Muc.Select(d => new { d.Ma, d.NhaCungCap, d.MaKho, d.NgayTao, tongTien = xemGia ? d.TongTien : (decimal?)null, trangThai = d.TrangThai.ToString(), d.Buoc }),
        });
    }

    [HttpGet("{ma}")]
    public async Task<IActionResult> ChiTiet(string ma)
    {
        var d = await service.LayAsync(ma);
        var xemGia = nguoiDung.VaiTro is VaiTro.QuanTriVien or VaiTro.QuanLyKho;
        return Ok(new
        {
            d.Ma, d.MaNhaCungCap, d.TenNhaCungCap, d.MaKho, d.NgayTao, trangThai = d.TrangThai.ToString(), d.Buoc, d.GhiChu, d.PhieuNhap,
            tongTien = xemGia ? d.TongTien : (decimal?)null,
            dong = d.Dong.Select(x => new
            {
                x.MaBienThe, x.TenHang, x.TenDonVi, x.HeSo, x.SoLuongDat, x.DaNhan, x.DangNhan, x.ChuaNhan,
                donGia = xemGia ? x.DonGia : (decimal?)null, thanhTien = xemGia ? x.ThanhTien : (decimal?)null,
            }),
        });
    }

    [HttpPost, Authorize(Roles = Vai.QuanLy)]
    public async Task<IActionResult> Tao(DonMuaApiTao body)
    {
        var ma = await service.TaoAsync(new DonMuaForm
        {
            MaNhaCungCap = body.MaNhaCungCap, MaKho = body.MaKho, GhiChu = body.GhiChu,
            Dong = body.Dong.Select(d => new DongDonMuaForm { MaBienThe = d.MaBienThe, TenDonVi = d.TenDonVi, SoLuong = d.SoLuong, DonGia = d.DonGia }).ToList(),
        });
        return CreatedAtAction(nameof(ChiTiet), new { ma }, new { ma });
    }
}
