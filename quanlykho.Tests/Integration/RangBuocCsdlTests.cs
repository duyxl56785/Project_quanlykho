using Microsoft.EntityFrameworkCore;
using Npgsql;
using quanlykho.Common;
using quanlykho.Data;
using quanlykho.Domain;
using quanlykho.Tests.HaTang;

namespace quanlykho.Tests.Integration;

/// <summary>Ràng buộc của migration KhoiTao: CSDL tự chặn dữ liệu sai dù code có lỗi.</summary>
[Collection(CsdlCollection.Ten)]
public class RangBuocCsdlTests(CsdlFixture csdl) : IAsyncLifetime
{
    private string _chuoiKetNoi = "";

    public async Task InitializeAsync()
    {
        _chuoiKetNoi = await csdl.TaoCsdlAsync();
        await using var db = Db();
        db.NguoiDung.Add(new NguoiDung { MaNguoiDung = "ND01", Ten = "Thủ kho", Email = "tk@kho.test", MatKhauBam = "x", SecurityStamp = "s", VaiTro = VaiTro.ThuKho });
        db.ViTri.AddRange(Kho("K01"), Kho("K02"));
        db.NhomHang.Add(new NhomHang { MaNhom = "TP", Ten = "Thực phẩm" });
        db.SanPham.Add(new SanPham { MaSanPham = "SUA", Ten = "Sữa tươi", DonViTinh = "chai", MaNhom = "TP", TheoDoiHanDung = true });
        db.BienThe.Add(BienThe("SUA-01", new() { ["Quy cách"] = "1L", ["Vị"] = "Không đường" }));
        db.Lo.Add(new Lo { MaLo = "L000001", MaBienThe = "SUA-01", NgayNhap = DateTimeOffset.UtcNow });
        db.NhaCungCap.Add(new NhaCungCap { MaNhaCungCap = "NCC01", Ten = "Vinamilk" });
        db.DonMua.Add(new DonMua { MaDonMua = "DM000001", MaNhaCungCap = "NCC01", MaKho = "K01", NguoiTao = "ND01", NgayTao = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private AppDbContext Db() => CsdlFixture.TaoDbContext(_chuoiKetNoi);

    private static ViTri Kho(string ma) => new() { MaViTri = ma, MaKho = ma, Cap = 1, Ten = ma, TenDayDu = ma };

    private static BienThe BienThe(string ma, Dictionary<string, string> thuocTinh) => new()
    {
        MaBienThe = ma, MaSanPham = "SUA", ThuocTinh = thuocTinh,
        DaiDongGoi = 0.1m, RongDongGoi = 0.1m, CaoDongGoi = 0.25m, KhoiLuong = 1.05m
    };

    private static async Task<string> LoiCsdl(AppDbContext db)
    {
        var loi = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        return Assert.IsType<PostgresException>(loi.InnerException).SqlState;
    }

    [Fact]
    public async Task Migration_ChaySachTrenCsdlTrong()
    {
        await using var db = Db();

        Assert.Empty(await db.Database.GetPendingMigrationsAsync());
        Assert.Equal(31, db.Model.GetEntityTypes().Count());
    }

    [Fact]
    public async Task TonKhoAm_BiTuChoi()
    {
        await using var db = Db();
        db.TonKho.Add(new TonKho { MaLo = "L000001", MaViTri = "K01", SoLuong = -1 });

        Assert.Equal(PostgresErrorCodes.CheckViolation, await LoiCsdl(db));
    }

    [Fact]
    public async Task SoDaGiuVuotTon_BiTuChoi()
    {
        await using var db = Db();
        db.TonKho.Add(new TonKho { MaLo = "L000001", MaViTri = "K01", SoLuong = 5, SoLuongDaGiu = 6 });

        Assert.Equal(PostgresErrorCodes.CheckViolation, await LoiCsdl(db));
    }

    [Fact]
    public async Task TrungMaKhoaChongLap_BiTuChoi()
    {
        await using var db = Db();
        SoBienDong Dong() => new() { MaLo = "L000001", MaViTri = "K01", SoLuongThayDoi = 1, LoaiChungTu = "Thu", MaChungTu = "PN000001", MaKhoaChongLap = "khoa#1", ThoiDiem = DateTimeOffset.UtcNow, NguoiThucHien = "ND01" };
        db.SoBienDong.AddRange(Dong(), Dong());

        Assert.Equal(PostgresErrorCodes.UniqueViolation, await LoiCsdl(db));
    }

    [Fact]
    public async Task PhieuNhapKhacKhoVoiDonMua_BiTuChoi()
    {
        await using var db = Db();
        db.PhieuNhap.Add(new PhieuNhap { MaPhieuNhap = "PN000001", LoaiPhieu = LoaiPhieuNhap.MuaHang, MaDonMua = "DM000001", MaKho = "K02", NguoiLap = "ND01", NgayNhap = DateTimeOffset.UtcNow });

        Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, await LoiCsdl(db));
    }

    [Fact]
    public async Task ThanhTien_DoCsdlTinh()
    {
        await using (var db = Db())
        {
            db.PhieuNhap.Add(new PhieuNhap
            {
                MaPhieuNhap = "PN000001", LoaiPhieu = LoaiPhieuNhap.MuaHang, MaDonMua = "DM000001", MaKho = "K01", NguoiLap = "ND01", NgayNhap = DateTimeOffset.UtcNow,
                ChiTiet = [new ChiTietPhieuNhap { STT = 1, MaLo = "L000001", TenDonVi = "thùng", HeSo = 24, SoLuongGoc = 3, SoLuongThucNhan = 72, DonGia = 1000.5m }]
            });
            await db.SaveChangesAsync();
        }

        await using var doc = Db();
        Assert.Equal(3001.50m, (await doc.ChiTietPhieuNhap.SingleAsync()).ThanhTien);
    }

    [Fact]
    public async Task SoThucNhanKhacSoGocNhanHeSo_BiTuChoi()
    {
        await using var db = Db();
        db.PhieuNhap.Add(new PhieuNhap
        {
            MaPhieuNhap = "PN000002", LoaiPhieu = LoaiPhieuNhap.NhapKhac, MaKho = "K01", NguoiLap = "ND01", NgayNhap = DateTimeOffset.UtcNow,
            ChiTiet = [new ChiTietPhieuNhap { STT = 1, MaLo = "L000001", TenDonVi = "thùng", HeSo = 24, SoLuongGoc = 10, SoLuongThucNhan = 10, DonGia = 0 }]
        });

        Assert.Equal(PostgresErrorCodes.CheckViolation, await LoiCsdl(db));
    }

    [Fact]
    public async Task BienTheTrungToHopThuocTinh_BiTuChoi()
    {
        await using var db = Db();
        // Cùng tổ hợp, khác thứ tự khóa: jsonb coi là bằng nhau.
        db.BienThe.Add(BienThe("SUA-02", new() { ["Vị"] = "Không đường", ["Quy cách"] = "1L" }));

        Assert.Equal(PostgresErrorCodes.UniqueViolation, await LoiCsdl(db));
    }

    [Fact]
    public async Task OThieuKichThuoc_BiTuChoi()
    {
        await using var db = Db();
        db.ViTri.Add(new ViTri { MaViTri = "K01-O01", MaCha = "K01", MaKho = "K01", Cap = 7, Ten = "Ô 1", TenDayDu = "K01 / Ô 1" });

        Assert.Equal(PostgresErrorCodes.CheckViolation, await LoiCsdl(db));
    }

    [Fact]
    public async Task MaChungTu_TangDanTheoTienTo()
    {
        await using var db = Db();
        var ma = new MaChungTu(db);

        Assert.Equal("PN000001", await ma.TaoAsync("PN"));
        Assert.Equal("PN000002", await ma.TaoAsync("PN"));
        Assert.Equal("L000001", await ma.TaoAsync("L"));
        await Assert.ThrowsAsync<ArgumentException>(() => ma.TaoAsync("XX"));
    }
}
