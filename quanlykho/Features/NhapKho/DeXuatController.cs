using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using quanlykho.Common.PhanQuyen;

namespace quanlykho.Features.NhapKho;

[Authorize(Roles = Vai.QuanLy), Route("DeXuatNhap")]
public class DeXuatController(DeXuatService service) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(string? maKho) => View(await service.TrangAsync(maKho));

    [HttpPost("TaoDon")]
    public async Task<IActionResult> TaoDon(DeXuatForm form)
    {
        var ma = await service.TaoDonNhapAsync(form);
        TempData["ThongBao"] = $"Đã tạo {ma.Count} đơn mua nháp: {string.Join(", ", ma)}. Kiểm tra giá rồi bấm Xác nhận để thành đơn chính thức.";
        return ma.Count == 1
            ? RedirectToAction(nameof(DonMuaController.ChiTiet), "DonMua", new { ma = ma[0] })
            : RedirectToAction(nameof(DonMuaController.Index), "DonMua", new { maKho = form.MaKho, trangThai = Domain.TrangThaiDonMua.Nhap });
    }

    [HttpPost("BoQua")]
    public async Task<IActionResult> BoQua(DeXuatForm form)
    {
        var so = await service.BoQuaAsync(form);
        TempData["ThongBao"] = $"Đã ghi bỏ qua {so} đề xuất.";
        return RedirectToAction(nameof(Index), new { maKho = form.MaKho });
    }
}

public record DeXuatApiDong(string MaBienThe, decimal SoLuong, string MaNhaCungCap);

public record DeXuatApiTao(string MaKho, List<DeXuatApiDong> Dong);

public record DeXuatApiBoQua(string MaKho, List<string> MaBienThe, string LyDo);

[ApiController, Authorize(Roles = Vai.QuanLy), Route("api/de-xuat-nhap")]
public class DeXuatApiController(DeXuatService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> DanhSach(string maKho) => Ok((await service.TinhAsync(maKho)).Select(d => new
    {
        d.DauVao.MaBienThe, d.TenHang, d.DonViTinh, ketLuan = d.KetQua.KetLuan.ToString(), d.KetQua.SoDeXuat, d.KetQua.LyDo,
        khaDung = d.DauVao.KhaDung, nhuCauNgay = Math.Round(d.KetQua.NhuCau, 3), soNgayLichSu = d.KetQua.SoNgay, toiThieu = d.DauVao.ToiThieu,
        thoiGianCho = d.DauVao.ThoiGianCho, choNhap = d.DauVao.ChoNhap, diemDatHang = Math.Round(d.KetQua.DiemDatHang, 3), d.MaNhaCungCap, d.DonGia,
    }));

    [HttpPost("don-nhap")]
    public async Task<IActionResult> TaoDon(DeXuatApiTao body) => Ok(new
    {
        donMua = await service.TaoDonNhapAsync(new DeXuatForm
        {
            MaKho = body.MaKho,
            Dong = body.Dong.Select(d => new DongChonG3 { Chon = true, MaBienThe = d.MaBienThe, SoLuong = d.SoLuong, MaNhaCungCap = d.MaNhaCungCap }).ToList(),
        }),
    });

    [HttpPost("bo-qua")]
    public async Task<IActionResult> BoQua(DeXuatApiBoQua body) => Ok(new
    {
        so = await service.BoQuaAsync(new DeXuatForm
        {
            MaKho = body.MaKho, LyDoBoQua = body.LyDo,
            Dong = body.MaBienThe.Select(m => new DongChonG3 { Chon = true, MaBienThe = m }).ToList(),
        }),
    });
}
