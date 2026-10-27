using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using quanlykho.Common.Loi;
using quanlykho.Common.PhanQuyen;
using quanlykho.Domain;
using quanlykho.Features.Kho;
using quanlykho.Features.NhapKho;
using static quanlykho.Common.GiaoDien.DinhDang;

namespace quanlykho.Features.InNhan;

[Authorize(Roles = Vai.TrongKho), Route("InNhan")]
public class InNhanController(ViTriService viTri, PhieuNhapService phieuNhap) : Controller
{
    [HttpGet("O")]
    public async Task<IActionResult> O([FromQuery] List<string> ma, string? kho)
    {
        var nhan = await viTri.NhanOAsync(ma);
        return View("In", new TrangNhan($"In nhãn ô ({nhan.Count})", TrangNhan.ChonKho(kho), "/ViTri", nhan));
    }

    [HttpGet("Lo")]
    public async Task<IActionResult> Lo(string maPhieu, int soBan = 1, string? kho = null)
    {
        if (soBan is < 1 or > 50)
            throw new NghiepVuException("Số bản in mỗi lô từ 1 đến 50.");
        var p = await phieuNhap.LayAsync(maPhieu);
        if (p.TrangThai != TrangThaiPhieu.HoanTat)
            throw new NghiepVuException("Chỉ in nhãn lô của hóa đơn nhập đã xác nhận.");
        var nhan = p.Dong.SelectMany(d => Enumerable.Repeat(new NhanIn(d.MaLo, $"{d.MaBienThe} – {d.TenHang}",
            string.Join(" · ", new[] { d.HanDung is null ? null : $"HSD {Ngay(d.HanDung)}", $"{So(d.SoLuong)} {d.TenDonVi}" }.OfType<string>())), soBan)).ToList();
        return View("In", new TrangNhan($"In nhãn lô – hóa đơn nhập {p.Ma}", TrangNhan.ChonKho(kho), $"/PhieuNhap/{p.Ma}", nhan));
    }
}
