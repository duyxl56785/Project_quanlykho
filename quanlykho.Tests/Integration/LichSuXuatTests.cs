using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using quanlykho.Common;
using quanlykho.Common.PhanQuyen;
using quanlykho.Data.Seed;
using quanlykho.Domain;
using quanlykho.Features.NhapKho;
using quanlykho.Features.XuatKho;
using quanlykho.Tests.HaTang;

namespace quanlykho.Tests.Integration;

[Collection(CsdlCollection.Ten)]
public class LichSuXuatTests(CsdlFixture csdl)
{
    [Fact]
    public async Task BaThangXuatBan_QuaCong_ChayLaiKhongNhanDoi_G3CoDeXuat()
    {
        var chuoi = await csdl.TaoCsdlCoDuLieuThuAsync();
        for (var lan = 0; lan < 2; lan++)
        {
            await using var db = CsdlFixture.TaoDbContext(chuoi);
            var http = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };
            await new LichSuXuat(db, http, new NguoiDungHienTai(http, db), new MaChungTu(db), TimeProvider.System).NapAsync();
        }

        await using (var db = CsdlFixture.TaoDbContext(chuoi))
        {
            Assert.Equal(2, await db.PhieuNhap.CountAsync(p => p.GhiChu == LichSuXuat.GhiChu));
            var xuat = await db.PhieuXuat.Where(p => p.LoaiPhieu == LoaiPhieuXuat.XuatBan).GroupBy(p => p.MaKho)
                .Select(g => new { g.Key, So = g.Count(), Dau = g.Min(p => p.NgayXuat) }).ToListAsync();
            Assert.All(xuat, x => Assert.InRange(x.So, 80, LichSuXuat.SoNgay));
            Assert.All(xuat, x => Assert.InRange((DateTimeOffset.UtcNow - x.Dau).TotalDays, LichSuXuat.SoNgay - 1, LichSuXuat.SoNgay + 1));

            // Quy tắc 6: tổng biến động từng kho bằng tồn; không tồn âm.
            var bienDong = await db.SoBienDong.Join(db.ViTri, s => s.MaViTri, v => v.MaViTri, (s, v) => new { v.MaKho, s.SoLuongThayDoi })
                .GroupBy(x => x.MaKho).ToDictionaryAsync(g => g.Key, g => g.Sum(x => x.SoLuongThayDoi));
            var ton = await db.TonKho.Join(db.ViTri, t => t.MaViTri, v => v.MaViTri, (t, v) => new { v.MaKho, t.SoLuong })
                .GroupBy(x => x.MaKho).ToDictionaryAsync(g => g.Key, g => g.Sum(x => x.SoLuong));
            Assert.Equal(bienDong, ton);
            Assert.False(await db.TonKho.AnyAsync(t => t.SoLuong < 0));
        }

        async Task<List<string>> DeXuat(string maKho, string ma)
        {
            await using var db = CsdlFixture.TaoDbContext(chuoi);
            var h = NguoiThu.HienTai(db, ma, Vai.QuanLyKho);
            var s = new DeXuatService(db, h, new TonKhaDung(db, h, TimeProvider.System),
                new DonMuaService(db, h, new MaChungTu(db), NguoiThu.NhatKy(db, h), TimeProvider.System), new ConfigurationBuilder().Build(), TimeProvider.System);
            return (await s.TinhAsync(maKho)).Where(d => d.KetQua.KetLuan == KetLuanG3.DeXuat).Select(d => d.DauVao.MaBienThe).ToList();
        }
        Assert.Equal(["GIAYA4-01", "SUATUOI-01"], (await DeXuat("K01", "ND002")).Order());
        Assert.Contains("MIGOI-01", await DeXuat("K02", "ND003"));
    }
}
