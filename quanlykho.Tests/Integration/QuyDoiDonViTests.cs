using System.Net;
using Microsoft.EntityFrameworkCore;
using quanlykho.Common.Loi;
using quanlykho.Common.PhanQuyen;
using quanlykho.Data;
using quanlykho.Domain;
using quanlykho.Features.DanhMuc;
using quanlykho.Tests.HaTang;

namespace quanlykho.Tests.Integration;

[Collection(CsdlCollection.Ten)]
public class QuyDoiDonViTests(CsdlFixture csdl) : IAsyncLifetime
{
    private const string Sua = "SUATUOI-01";
    private string _chuoi = "";

    public async Task InitializeAsync() => _chuoi = await csdl.TaoCsdlCoDuLieuThuAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task<T> Voi<T>(Func<DanhMucService, QuyDoiDonVi, Task<T>> lam)
    {
        await using var db = CsdlFixture.TaoDbContext(_chuoi);
        var q = new QuyDoiDonVi(db);
        return await lam(new DanhMucService(db, NguoiThu.NhatKy(db, NguoiThu.HienTai(db, "ND001", Vai.QuanTri)), q), q);
    }

    /// <summary>Lưu biểu mẫu biến thể sữa với danh sách đơn vị cho trước.</summary>
    private Task<int> LuuDonVi(params DonViQuyDoiDong[] donVi) => Voi(async (s, _) =>
    {
        var f = await s.LayBienTheAsync(Sua);
        f.DonVi = [.. donVi];
        await s.SuaBienTheAsync(Sua, f);
        return 0;
    });

    private static DonViQuyDoiDong DonVi(string ten, decimal heSo, string? maVach = null, TrangThaiSuDung trangThai = TrangThaiSuDung.DangSuDung) =>
        new() { TenDonVi = ten, HeSo = heSo, MaVach = maVach, TrangThai = trangThai };

    private async Task<string> Loi(params DonViQuyDoiDong[] donVi) => (await Assert.ThrowsAsync<NghiepVuException>(() => LuuDonVi(donVi))).Message;

    [Fact]
    public async Task KhaiBao_QuyDoi_TraMaVach()
    {
        await LuuDonVi(DonVi("thùng", 48, "THUNG-SUA"), DonVi("lốc", 4));

        Assert.Equal([new("hộp", 1), new("lốc", 4), new("thùng", 48)], await Voi((_, q) => q.DonViAsync(Sua)));
        Assert.Equal(new KetQuaQuyDoi("thùng", 48, 480), await Voi((_, q) => q.QuyVeChinhAsync(Sua, "thùng", 10)));
        Assert.Equal("4 thùng 43 hộp", await Voi((_, q) => q.HienThiAsync(Sua, 235)));
        Assert.Equal(new MaVachHang(Sua, "thùng", 48), await Voi((_, q) => q.TraMaVachAsync("THUNG-SUA")));
        await using (var db = CsdlFixture.TaoDbContext(_chuoi))
        {
            var maVachHop = await db.BienThe.Where(b => b.MaBienThe == Sua).Select(b => b.MaVach).SingleAsync();
            Assert.Equal(new MaVachHang(Sua, "hộp", 1), await new QuyDoiDonVi(db).TraMaVachAsync(maVachHop!));
        }
        Assert.Null(await Voi((_, q) => q.TraMaVachAsync("KHONG-CO")));

        // Biểu mẫu nạp lại đủ đơn vị.
        Assert.Equal(["lốc", "thùng"], (await Voi((s, _) => s.LayBienTheAsync(Sua))).DonVi.Select(d => d.TenDonVi));
    }

    [Fact]
    public async Task HeSoKhongNguyenHoacNhoHon2_TenTrung_MaVachTrung_BiTuChoi()
    {
        Assert.Contains("số nguyên lớn hơn 1", await Loi(DonVi("thùng", 1.5m)));
        Assert.Contains("số nguyên lớn hơn 1", await Loi(DonVi("thùng", 1)));
        Assert.Contains("hai lần", await Loi(DonVi("thùng", 24), DonVi("Thùng", 12)));
        Assert.Contains("trùng đơn vị tính chính", await Loi(DonVi("Hộp", 2)));
        await using (var db = CsdlFixture.TaoDbContext(_chuoi))
        {
            var maVachKhac = await db.BienThe.Where(b => b.MaBienThe != Sua && b.MaVach != null).Select(b => b.MaVach).FirstAsync();
            Assert.Contains("đã dùng", await Loi(DonVi("thùng", 24, maVachKhac)));
        }
        await using var kt = CsdlFixture.TaoDbContext(_chuoi);
        Assert.Equal(0, await kt.DonViQuyDoi.CountAsync());
    }

    [Fact]
    public async Task DonViDaCoChungTu_ChiNgungSuDung()
    {
        await LuuDonVi(DonVi("thùng", 48));
        await using (var db = CsdlFixture.TaoDbContext(_chuoi))
        {
            db.DonMua.Add(new DonMua
            {
                MaDonMua = "DMQD", MaNhaCungCap = "NCC03", MaKho = "K01", NguoiTao = "ND002", NgayTao = DateTimeOffset.UtcNow, TrangThai = TrangThaiDonMua.ChuaThucHien,
                ChiTiet = [new ChiTietDonMua { MaBienThe = Sua, TenDonVi = "thùng", HeSo = 48, SoLuongDat = 2, DonGia = 1 }],
            });
            await db.SaveChangesAsync();
        }

        Assert.Contains("chỉ ngừng sử dụng", await Loi(DonVi("thùng", 24)));
        Assert.Contains("chỉ ngừng sử dụng", await Loi());
        await LuuDonVi(DonVi("thùng", 48, trangThai: TrangThaiSuDung.NgungSuDung));

        Assert.Equal([new("hộp", 1)], await Voi((_, q) => q.DonViAsync(Sua)));
        await Assert.ThrowsAsync<NghiepVuException>(() => Voi((_, q) => q.QuyVeChinhAsync(Sua, "thùng", 1)));
    }

    [Fact]
    public async Task ManHinhBienThe_CoDonViQuyDoi()
    {
        await using var app = new UngDungFactory { ChuoiKetNoi = _chuoi };
        var html = await app.TaoClient(Vai.QuanTri, "ND001").GetStringAsync($"/DanhMuc/SanPham/BienThe/{Sua}");
        Assert.Contains("Đơn vị quy đổi", html);
        Assert.Contains("DonVi[__i__].HeSo", html);
        Assert.Equal(HttpStatusCode.OK, (await app.TaoClient(Vai.QuanTri, "ND001").GetAsync("/Styleguide")).StatusCode);
    }
}
