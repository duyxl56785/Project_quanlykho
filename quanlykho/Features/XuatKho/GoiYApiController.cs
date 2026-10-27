using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using quanlykho.Common.PhanQuyen;

namespace quanlykho.Features.XuatKho;

public record PhanHoiGoiY(bool ChapNhan, string? MaChungTu = null);

[ApiController, Authorize(Roles = Vai.TrongKho), Route("api/goi-y")]
public class GoiYApiController(GoiYService service) : ControllerBase
{
    [HttpGet("lay-hang")]
    public async Task<IActionResult> LayHang(string maKho, string maBienThe, decimal soLuong) => Ok(await service.LayHangAsync(maKho, maBienThe, soLuong));

    [HttpPost("{ma:long}/phan-hoi")]
    public async Task<IActionResult> PhanHoi(long ma, PhanHoiGoiY body)
    {
        await service.PhanHoiAsync(ma, body.ChapNhan, body.MaChungTu);
        return NoContent();
    }
}
