using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using quanlykho.Data;
using quanlykho.Data.Seed;
using quanlykho.Domain;
using quanlykho.Tests.HaTang;

namespace quanlykho.Tests.Integration;

[Collection(CsdlCollection.Ten)]
public class DuLieuDemoTests(CsdlFixture csdl)
{
    [Fact]
    public async Task NapDuTinhHuongDemo_ChayLaiKhongNhanDoi()
    {
        var chuoi = await csdl.TaoCsdlCoDuLieuThuAsync();
        await using var app = new UngDungFactory { ChuoiKetNoi = chuoi };
        for (var lan = 0; lan < 2; lan++)
        {
            await using var scope = app.Services.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<LichSuXuat>().NapAsync();
            await scope.ServiceProvider.GetRequiredService<DuLieuDemo>().NapAsync();
        }

        await using var db = CsdlFixture.TaoDbContext(chuoi);
        Assert.Equal(1, await db.PhieuNhap.CountAsync(p => p.GhiChu == DuLieuDemo.GhiChu && p.TrangThai == TrangThaiPhieu.HoanTat));
        Assert.Equal(3, await db.DonViQuyDoi.CountAsync());
        Assert.Equal(TrangThaiDonMua.ChuaThucHien, await db.DonMua.Where(d => d.GhiChu == DuLieuDemo.GhiChu).Select(d => d.TrangThai).SingleAsync());
        Assert.Equal(2, await db.DonDatHang.CountAsync(d => d.GhiChu == DuLieuDemo.GhiChu && d.TrangThai == TrangThaiDonDat.ChoDuyet && d.NguoiTao == "ND007"));
        Assert.Equal(TrangThaiDieuChuyen.ChoNhapKho, await db.PhieuDieuChuyen.Select(p => p.TrangThai).SingleAsync());

        var homNay = DateOnly.FromDateTime(DateTime.UtcNow);
        var ton = await (from t in db.TonKho
                         join v in db.ViTri on t.MaViTri equals v.MaViTri
                         join l in db.Lo on t.MaLo equals l.MaLo
                         where v.MaKho == "K01" && t.SoLuong > 0
                         select new { l.MaBienThe, l.HanDung, v.LoaiKhu, t.SoLuong }).ToListAsync();
        Assert.Contains(ton, t => t.MaBienThe == "SUATUOI-01" && t.HanDung < homNay && t.LoaiKhu == LoaiKhu.LuuTru && t.SoLuong == 12);
        Assert.Contains(ton, t => t.MaBienThe == "SUATUOI-01" && t.HanDung > homNay && t.HanDung < homNay.AddDays(30) && t.SoLuong == 96);
        Assert.Contains(ton, t => t.MaBienThe == "AOTHUN-03" && t.LoaiKhu == LoaiKhu.TiepNhan && t.SoLuong == 8);
        Assert.Contains(ton, t => t.MaBienThe == "AOTHUN-02" && t.LoaiKhu == LoaiKhu.CachLy && t.SoLuong == 10);
    }
}
