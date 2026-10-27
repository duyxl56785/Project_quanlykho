using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using quanlykho.Data;
using quanlykho.Data.Seed;
using quanlykho.Domain;
using quanlykho.Tests.HaTang;

namespace quanlykho.Tests.Integration;

[Collection(CsdlCollection.Ten)]
public class DuLieuThuTests(CsdlFixture csdl)
{
    private const string MatKhau = "MatKhauThu@1";

    private static DuLieuThu TaoSeeder(AppDbContext db) => new(db, new PasswordHasher<NguoiDung>(),
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["DuLieuThu:MatKhau"] = MatKhau }).Build());

    [Fact]
    public async Task NapHaiLan_KhongNhanDoi_VaDuSoLuong()
    {
        var chuoi = await csdl.TaoCsdlAsync();
        await using (var db = CsdlFixture.TaoDbContext(chuoi)) await TaoSeeder(db).NapAsync();
        await using (var db = CsdlFixture.TaoDbContext(chuoi)) await TaoSeeder(db).NapAsync();

        await using var doc = CsdlFixture.TaoDbContext(chuoi);
        Assert.Equal(2, await doc.ViTri.CountAsync(v => v.Cap == 1));
        Assert.Equal(38, await doc.ViTri.CountAsync(v => v.Cap == 7));
        Assert.Equal(4, await doc.NhomHang.CountAsync());
        Assert.Equal(50, await doc.BienThe.CountAsync());
        Assert.Equal(4, await doc.BienThe.Select(b => b.SanPham.MaNhom).Distinct().CountAsync());
        Assert.Equal(3, await doc.NhaCungCap.CountAsync());
        Assert.Equal(7, await doc.NguoiDung.CountAsync());
        Assert.Equal(4, await doc.NguoiDung.Select(n => n.VaiTro).Distinct().CountAsync());
    }

    [Fact]
    public async Task MoiKhoCoOMacDinhTiepNhanVaCachLy_VaOLuuTruCoSucChua()
    {
        var chuoi = await csdl.TaoCsdlAsync();
        await using (var db = CsdlFixture.TaoDbContext(chuoi)) await TaoSeeder(db).NapAsync();

        await using var doc = CsdlFixture.TaoDbContext(chuoi);
        foreach (var kho in new[] { "K01", "K02" })
        {
            var macDinh = await doc.ViTri.Where(v => v.MaKho == kho && v.LaOMacDinh).Select(v => v.LoaiKhu).ToListAsync();
            Assert.Equal([LoaiKhu.TiepNhan, LoaiKhu.CachLy], macDinh.Order().Cast<LoaiKhu>());
        }
        Assert.True(await doc.ViTri.Where(v => v.Cap == 7 && v.LoaiKhu == LoaiKhu.LuuTru)
            .AllAsync(v => v.Dai > 0 && v.TaiTrongToiDa > 0 && v.DieuKienBaoQuan != null));
        Assert.Equal("K01-TA01-TG01-P02-H01-KE01-O01",
            await doc.ViTri.Where(v => v.MaKho == "K01" && v.Cap == 7 && v.LoaiKhu == LoaiKhu.LuuTru && v.DieuKienBaoQuan == DieuKienBaoQuan.Thuong)
                .OrderBy(v => v.MaViTri).Select(v => v.MaViTri).FirstAsync());
    }

    [Fact]
    public async Task TaiKhoanMau_DungMatKhauCauHinh_VaGanKhoDung()
    {
        var chuoi = await csdl.TaoCsdlAsync();
        await using (var db = CsdlFixture.TaoDbContext(chuoi)) await TaoSeeder(db).NapAsync();

        await using var doc = CsdlFixture.TaoDbContext(chuoi);
        var hasher = new PasswordHasher<NguoiDung>();
        foreach (var nd in await doc.NguoiDung.ToListAsync())
            Assert.NotEqual(PasswordVerificationResult.Failed, hasher.VerifyHashedPassword(nd, nd.MatKhauBam, MatKhau));

        // Quản lý kho và thủ kho đều có kho; K01 có hai thủ kho để thử người kiểm khác người lấy.
        var canKho = await doc.NguoiDung.Where(n => n.VaiTro == VaiTro.QuanLyKho || n.VaiTro == VaiTro.ThuKho).Select(n => n.MaNguoiDung).ToListAsync();
        var coKho = await doc.NguoiDungKho.Select(k => k.MaNguoiDung).Distinct().ToListAsync();
        Assert.Empty(canKho.Except(coKho));
        Assert.Equal(2, await doc.NguoiDungKho.CountAsync(k => k.MaKho == "K01" && doc.NguoiDung.Any(n => n.MaNguoiDung == k.MaNguoiDung && n.VaiTro == VaiTro.ThuKho)));
    }
}
