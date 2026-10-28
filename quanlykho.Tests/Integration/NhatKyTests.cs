using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using Npgsql;
using quanlykho.Common;
using quanlykho.Common.PhanQuyen;
using quanlykho.Data;
using quanlykho.Domain;
using quanlykho.Features.DanhMuc;
using quanlykho.Features.KiemKe;
using quanlykho.Features.NhapKho;
using quanlykho.Features.NhatKyThaoTac;
using quanlykho.Features.XuatKho;
using quanlykho.Tests.HaTang;

namespace quanlykho.Tests.Integration;

[Collection(CsdlCollection.Ten)]
public class NhatKyTests(CsdlFixture csdl) : IAsyncLifetime
{
    private static readonly FakeTimeProvider Gio = new(new DateTimeOffset(2026, 11, 30, 3, 0, 0, TimeSpan.Zero));
    private string _chuoi = "";

    public async Task InitializeAsync() => _chuoi = await csdl.TaoCsdlCoDuLieuThuAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    private async Task Voi(Func<AppDbContext, NguoiDungHienTai, GhiNhatKy, Task> lam, string ma = "ND002", string vai = Vai.QuanLyKho)
    {
        await using var db = CsdlFixture.TaoDbContext(_chuoi);
        var h = NguoiThu.HienTai(db, ma, vai);
        await lam(db, h, new GhiNhatKy(db, h, Gio));
    }

    [Fact]
    public async Task NhatKyVaSoBienDong_CsdlChanSuaXoa()
    {
        await using (var db = CsdlFixture.TaoDbContext(_chuoi))
        {
            db.Lo.Add(new Lo { MaLo = "LT", MaBienThe = "AOTHUN-01", NgayNhap = Gio.GetUtcNow() });
            NguoiThu.NhatKy(db, NguoiThu.HienTai(db, "ND001", Vai.QuanTri)).Ghi("Thu", "Thu", "T1");
            await db.SaveChangesAsync();
            await using var tx = await db.Database.BeginTransactionAsync();
            await new CongGhiTon(db, NguoiThu.HienTai(db, "ND001", Vai.QuanTri), Gio).GhiAsync("k", "Thu", "T1", [new("LT", "K01-TA01-TG01-P02-H01-KE01-O01", 5)]);
            await tx.CommitAsync();
        }

        foreach (var sql in new[]
        {
            """UPDATE "NhatKy" SET "LoaiThaoTac" = 'Sua'""", """DELETE FROM "NhatKy" """, """TRUNCATE "NhatKy" """,
            """UPDATE "SoBienDong" SET "SoLuongThayDoi" = 99""", """DELETE FROM "SoBienDong" """, """TRUNCATE "SoBienDong" CASCADE""",
        })
        {
            await using var db = CsdlFixture.TaoDbContext(_chuoi);
            var loi = await Assert.ThrowsAsync<PostgresException>(() => db.Database.ExecuteSqlRawAsync(sql));
            Assert.Contains("chỉ được thêm", loi.MessageText);
        }

        await using var kt = CsdlFixture.TaoDbContext(_chuoi);
        Assert.Equal("Thu", (await kt.NhatKy.SingleAsync()).LoaiThaoTac);
        Assert.Equal(5, (await kt.SoBienDong.SingleAsync()).SoLuongThayDoi);
    }

    [Fact]
    public async Task CacThaoTacGhiNhatKy_TraCuuTheoBoLoc()
    {
        // Ngừng rồi dùng lại nhà cung cấp và biến thể; lưu không đổi trạng thái thì không ghi.
        await Voi(async (db, _, nk) =>
        {
            var s = new DoiTacService(db, nk);
            var f = await s.LayAsync(LoaiDoiTac.NhaCungCap, "NCC01");
            f.TrangThai = TrangThaiSuDung.NgungSuDung;
            await s.SuaAsync("NCC01", f);
            await s.SuaAsync("NCC01", f);
            f.TrangThai = TrangThaiSuDung.DangSuDung;
            await s.SuaAsync("NCC01", f);
            var dm = new DanhMucService(db, nk, new QuyDoiDonVi(db));
            var b = await dm.LayBienTheAsync("AOTHUN-02");
            b.TrangThai = TrangThaiSuDung.NgungSuDung;
            await dm.SuaBienTheAsync("AOTHUN-02", b);
        }, "ND001", Vai.QuanTri);

        // Đơn mua nháp: xác nhận; phiếu kiểm kê: hủy.
        await Voi(async (db, h, nk) =>
        {
            var dm = new DonMuaService(db, h, new MaChungTu(db), nk, Gio);
            var ma = await dm.TaoAsync(new DonMuaForm { MaNhaCungCap = "NCC01", MaKho = "K01", Dong = [new() { MaBienThe = "AOTHUN-01", SoLuong = 5, DonGia = 1 }] }, laNhap: true);
            await dm.XacNhanAsync(ma);
        });
        await Voi(async (db, h, nk) =>
        {
            var kk = new KiemKeService(db, h, new MaChungTu(db), new TonKhaDung(db, h, Gio), new CongGhiTon(db, h, Gio), nk, Gio);
            var ma = await kk.TaoAsync(new KiemKeForm { MaKho = "K01", PhamVi = PhamViKiemKe.ViTri, DoiTuong = ["K01-TA01-TG01-P02-H01-KE01"], ThanhVien = ["ND004"] });
            await kk.HuyAsync(ma);
        });

        await using var db = CsdlFixture.TaoDbContext(_chuoi);
        var tra = new NhatKyService(db);
        var tatCa = (await tra.TraCuuAsync(new LocNhatKy(), 1)).Trang.Muc;
        Assert.Equal(["NgungSuDung", "DungLai", "NgungSuDung", "XacNhanDonMua", "LapKiemKe", "HuyKiemKe"], tatCa.Reverse().Select(n => n.LoaiThaoTac));
        Assert.Equal([("NhaCungCap", "NCC01"), ("NhaCungCap", "NCC01"), ("BienThe", "AOTHUN-02")],
            tatCa.Where(n => n.LoaiThaoTac is "NgungSuDung" or "DungLai").Reverse().Select(n => (n.DoiTuong, n.MaDoiTuong!)));

        Assert.Equal(3, (await tra.TraCuuAsync(new LocNhatKy { MaNguoiDung = "ND001" }, 1)).Trang.TongSo);
        Assert.Equal(3, (await tra.TraCuuAsync(new LocNhatKy { MaKho = "K01" }, 1)).Trang.TongSo);
        var huy = Assert.Single((await tra.TraCuuAsync(new LocNhatKy { LoaiThaoTac = "HuyKiemKe" }, 1)).Trang.Muc);
        Assert.Equal(("ND002", "Quản lý kho Hà Nội"), (huy.MaNguoiDung!, huy.TenNguoiDung!));
        Assert.Equal(2, (await tra.TraCuuAsync(new LocNhatKy { MaDoiTuong = "KK" }, 1)).Trang.TongSo);
        Assert.Equal(6, (await tra.TraCuuAsync(new LocNhatKy { TuNgay = new(2026, 11, 30), DenNgay = new(2026, 11, 30) }, 1)).Trang.TongSo);
        Assert.Equal(0, (await tra.TraCuuAsync(new LocNhatKy { DenNgay = new(2026, 11, 29) }, 1)).Trang.TongSo);
        var sai = await tra.TraCuuAsync(new LocNhatKy { TuNgay = new(2026, 11, 30), DenNgay = new(2026, 11, 1) }, 1);
        Assert.Equal((0, "Ngày bắt đầu không được sau ngày kết thúc."), (sai.Trang.TongSo, sai.Loc.Loi));
        Assert.Contains(new Common.GiaoDien.LuaChon("HuyKiemKe", "Hủy kiểm kê"), (await tra.TraCuuAsync(new LocNhatKy(), 1)).LoaiThaoTac);
    }

    [Fact]
    public async Task ManHinh_ChiQuanTri()
    {
        await using var app = new UngDungFactory { ChuoiKetNoi = _chuoi, Gio = Gio };
        Assert.Equal(HttpStatusCode.OK, (await app.TaoClient(Vai.QuanTri, "ND001").GetAsync("/NhatKy")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await app.TaoClient(Vai.QuanTri, "ND001").GetAsync("/api/nhat-ky?LoaiThaoTac=DoiQuyen")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await app.TaoClient(Vai.QuanLyKho, "ND002").GetAsync("/api/nhat-ky")).StatusCode);
    }
}
