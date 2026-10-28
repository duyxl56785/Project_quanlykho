using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using quanlykho.Common;
using quanlykho.Common.Loi;
using quanlykho.Common.PhanQuyen;
using quanlykho.Data;
using quanlykho.Domain;
using quanlykho.Features.DieuChuyen;
using quanlykho.Features.KiemKe;
using quanlykho.Features.XuatKho;
using quanlykho.Tests.HaTang;

namespace quanlykho.Tests.Integration;

/// <summary>Kịch bản Bảng 1.6: kiểm kê lệch và chuyển kho nhận thiếu; sau xử lý tổng biến động bằng tồn từng (lô, ô).</summary>
[Collection(CsdlCollection.Ten)]
public class DieuChinhTonTests(CsdlFixture csdl) : IAsyncLifetime
{
    private const string Ke = "K01-TA01-TG01-P02-H01-KE01-O";
    private const string CachLy = "K01-TA01-TG01-P04-H01-KE01-O01";
    private static readonly FakeTimeProvider Gio = new(new DateTimeOffset(2026, 11, 1, 3, 0, 0, TimeSpan.Zero));
    private static readonly DateOnly Han = new(2027, 1, 1);
    private string _chuoi = "";
    private UngDungFactory _app = null!;

    /// <summary>KE01: O01 áo L1 10, O02 áo L2 6, O03 áo L3 5, O04 điện thoại L4 4, O05 áo L5 3. Sữa LS 8 ở kho K01 ô lạnh (để chuyển kho).</summary>
    public async Task InitializeAsync()
    {
        _chuoi = await csdl.TaoCsdlCoDuLieuThuAsync();
        _app = new UngDungFactory { ChuoiKetNoi = _chuoi, Gio = Gio };
        await using var db = CsdlFixture.TaoDbContext(_chuoi);
        var lo = new (string Ma, string BienThe, string O, decimal SoLuong)[]
        {
            ("L1", "AOTHUN-01", Ke + "01", 10), ("L2", "AOTHUN-01", Ke + "02", 6), ("L3", "AOTHUN-01", Ke + "03", 5),
            ("L4", "DTA-01", Ke + "04", 4), ("L5", "AOTHUN-01", Ke + "05", 3), ("LS", "SUATUOI-01", "K01-TA01-TG01-P03-H01-KE01-O01", 8),
        };
        db.Lo.AddRange(lo.Select(l => new Lo { MaLo = l.Ma, MaBienThe = l.BienThe, HanDung = Han, NgayNhap = Gio.GetUtcNow() }));
        await db.SaveChangesAsync();
        await using var tx = await db.Database.BeginTransactionAsync();
        await new CongGhiTon(db, NguoiThu.HienTai(db, "ND001", Vai.QuanTri), Gio).GhiAsync("nap", "Thu", "T1", lo.Select(l => new DongGhiTon(l.Ma, l.O, l.SoLuong)).ToList());
        await tx.CommitAsync();
    }

    public async Task DisposeAsync() => await _app.DisposeAsync();

    private static KiemKeService KiemKe(AppDbContext db, string ma, string vai)
    {
        var h = NguoiThu.HienTai(db, ma, vai);
        return new KiemKeService(db, h, new MaChungTu(db), new TonKhaDung(db, h, Gio), new CongGhiTon(db, h, Gio), NguoiThu.NhatKy(db, h), Gio);
    }

    private async Task<T> VoiKiemKe<T>(Func<KiemKeService, Task<T>> lam, string ma = "ND002", string vai = Vai.QuanLyKho)
    {
        await using var db = CsdlFixture.TaoDbContext(_chuoi);
        return await lam(KiemKe(db, ma, vai));
    }

    private Task VoiKiemKe(Func<KiemKeService, Task> lam, string ma = "ND002", string vai = Vai.QuanLyKho) =>
        VoiKiemKe(async s => { await lam(s); return 0; }, ma, vai);

    private async Task<T> VoiChuyen<T>(string ma, string vai, Func<DieuChuyenService, Task<T>> lam)
    {
        await using var db = CsdlFixture.TaoDbContext(_chuoi);
        var h = NguoiThu.HienTai(db, ma, vai);
        return await lam(new DieuChuyenService(db, h, new MaChungTu(db), new CongGhiTon(db, h, Gio), new TonKhaDung(db, h, Gio), NguoiThu.NhatKy(db, h), Gio));
    }

    private async Task<Dictionary<(string Lo, string O), decimal>> Ton()
    {
        await using var db = CsdlFixture.TaoDbContext(_chuoi);
        return await db.TonKho.Where(t => t.SoLuong != 0).ToDictionaryAsync(t => (t.MaLo, t.MaViTri), t => t.SoLuong);
    }

    /// <summary>Quy tắc 6: tổng biến động trên sổ của từng (lô, ô) bằng tồn.</summary>
    private async Task TongBienDongBangTon()
    {
        await using var db = CsdlFixture.TaoDbContext(_chuoi);
        var bienDong = await db.SoBienDong.GroupBy(s => new { s.MaLo, s.MaViTri }).Select(g => new { g.Key.MaLo, g.Key.MaViTri, Tong = g.Sum(s => s.SoLuongThayDoi) }).ToListAsync();
        var ton = await db.TonKho.ToDictionaryAsync(t => (t.MaLo, t.MaViTri), t => t.SoLuong);
        Assert.All(bienDong, b => Assert.Equal(b.Tong, ton.GetValueOrDefault((b.MaLo, b.MaViTri))));
    }

    /// <summary>Kiểm kệ KE01 với số đếm cho trước theo lô; trả mã phiếu đang kiểm kê.</summary>
    private async Task<(string Ma, KiemKeChiTiet Phieu)> KiemKeLech(params (string Lo, decimal Dem)[] dem)
    {
        var ma = await VoiKiemKe(s => s.TaoAsync(new KiemKeForm { MaKho = "K01", PhamVi = PhamViKiemKe.ViTri, DoiTuong = ["K01-TA01-TG01-P02-H01-KE01"], ThanhVien = ["ND004"] }));
        await VoiKiemKe(s => s.BatDauAsync(ma, false));
        var p = await VoiKiemKe(s => s.LayAsync(ma));
        await VoiKiemKe(s => s.NhapSoDemAsync(ma, p.Dong.Select(d => new DongDemForm { STT = d.STT, SoLuongThucTe = dem.Single(x => x.Lo == d.MaLo).Dem }).ToList()), "ND004", Vai.ThuKho);
        return (ma, await VoiKiemKe(s => s.LayAsync(ma)));
    }

    private static int Stt(KiemKeChiTiet p, string? lo, string? bienThe = null, string? o = null) =>
        p.Dong.Single(d => d.MaLo == lo && (bienThe is null || d.MaBienThe == bienThe) && (o is null || d.MaViTri == o)).STT;

    [Fact]
    public async Task KiemKeLech_SauLoai_SinhChungTuQuaCong_HoanThanhMoKhoa()
    {
        // L1 thiếu 2 (mất), L2 thừa 1, L3 thiếu 2 (hư), L4 thiếu 1 (hết hạn), L5 thiếu 3 thực ra nằm ở O06 (sai vị trí).
        var (ma, p) = await KiemKeLech(("L1", 8), ("L2", 7), ("L3", 3), ("L4", 3), ("L5", 0));
        await VoiKiemKe(s => s.ThemDongAsync(ma, new ThemDongKiemKe(Ke + "06", "L5", 3)), "ND004", Vai.ThuKho);
        p = await VoiKiemKe(s => s.LayAsync(ma));
        Assert.Equal(6, p.CoChenhLech);

        await Assert.ThrowsAsync<KhongCoQuyenException>(() => VoiKiemKe(s => s.DuyetDieuChinhAsync(ma, [new() { STT = 1, Loai = LoaiChenhLech.Thieu, KetLuan = "Mất" }], "x"), "ND004", Vai.ThuKho));
        Assert.Contains("kết luận", (await Assert.ThrowsAsync<NghiepVuException>(() => VoiKiemKe(s => s.DuyetDieuChinhAsync(ma, [new() { STT = Stt(p, "L1"), Loai = LoaiChenhLech.Thieu }], "x")))).Message);
        Assert.Contains("không hợp", (await Assert.ThrowsAsync<NghiepVuException>(() => VoiKiemKe(s => s.DuyetDieuChinhAsync(ma, [new() { STT = Stt(p, "L1"), Loai = LoaiChenhLech.Thua, KetLuan = "?" }], "x")))).Message);
        Assert.Contains("phải khớp", (await Assert.ThrowsAsync<NghiepVuException>(() => VoiKiemKe(s => s.DuyetDieuChinhAsync(ma,
            [new() { STT = Stt(p, "L5", o: Ke + "06"), Loai = LoaiChenhLech.SaiViTri, KetLuan = "Xếp nhầm" }], "x")))).Message);

        DongDuyetForm[] duyet =
        [
            new() { STT = Stt(p, "L1"), Loai = LoaiChenhLech.Thieu, KetLuan = "Mất, chưa rõ nguyên nhân" },
            new() { STT = Stt(p, "L2"), Loai = LoaiChenhLech.Thua, KetLuan = "Nhập thiếu sổ lần trước" },
            new() { STT = Stt(p, "L3"), Loai = LoaiChenhLech.HuHong, KetLuan = "Rách bao bì" },
            new() { STT = Stt(p, "L4"), Loai = LoaiChenhLech.HetHan, KetLuan = "Hộp hết hạn bảo hành" },
            new() { STT = Stt(p, "L5", o: Ke + "05"), Loai = LoaiChenhLech.SaiViTri, KetLuan = "Xếp nhầm ô", MaViTriDung = Ke + "06" },
            new() { STT = Stt(p, "L5", o: Ke + "06"), Loai = LoaiChenhLech.SaiViTri, KetLuan = "Xếp nhầm ô" },
        ];
        Assert.Equal(KetQuaGhi.DaGhi, await VoiKiemKe(s => s.DuyetDieuChinhAsync(ma, duyet, "dc-1")));
        Assert.Equal(KetQuaGhi.DaGhiTruocDo, await VoiKiemKe(s => s.DuyetDieuChinhAsync(ma, duyet, "dc-1")));

        var ton = await Ton();
        Assert.Equal(8, ton[("L1", Ke + "01")]);
        Assert.Equal(7, ton[("L2", Ke + "02")]);
        Assert.Equal((3m, 2m), (ton[("L3", Ke + "03")], ton[("L3", CachLy)]));
        Assert.Equal((3m, 1m), (ton[("L4", Ke + "04")], ton[("L4", CachLy)]));
        Assert.False(ton.ContainsKey(("L5", Ke + "05")));
        Assert.Equal(3, ton[("L5", Ke + "06")]);
        await TongBienDongBangTon();

        await using var db = CsdlFixture.TaoDbContext(_chuoi);
        var phieu = await db.PhieuKiemKe.Include(x => x.ChiTiet).SingleAsync(x => x.MaPhieu == ma);
        Assert.Equal(TrangThaiKiemKe.HoanThanh, phieu.TrangThai);
        Assert.All(phieu.ChiTiet.Where(c => c.SoLuongThucTe != c.SoLuongSoSach), c => Assert.Equal("ND002", c.NguoiDuyet));
        Assert.False(await db.ViTri.AnyAsync(v => v.MaPhieuKiemKe != null));
        var px = await db.PhieuXuat.Include(x => x.ChiTiet).SingleAsync(x => x.LyDo == $"Điều chỉnh kiểm kê {ma}");
        Assert.Equal((LoaiPhieuXuat.XuatKhac, "L1", 2m), (px.LoaiPhieu, px.ChiTiet.Single().MaLo, px.ChiTiet.Single().SoLuong));
        var pn = await db.PhieuNhap.Include(x => x.ChiTiet).SingleAsync(x => x.GhiChu == $"Điều chỉnh kiểm kê {ma}");
        Assert.Equal((LoaiPhieuNhap.NhapKhac, TrangThaiPhieu.HoanTat, "L2", 1m), (pn.LoaiPhieu, pn.TrangThai, pn.ChiTiet.Single().MaLo, pn.ChiTiet.Single().SoLuongThucNhan));
        Assert.Equal(3, await db.ChuyenViTri.CountAsync(c => c.Loai == LoaiChuyenViTri.ChuyenTrongKho));
        Assert.True(await db.NhatKy.AnyAsync(n => n.LoaiThaoTac == "DuyetDieuChinhKiemKe" && n.MaDoiTuong == ma));
    }

    [Fact]
    public async Task SaiBienThe_XuatLoSai_NhapLoMoiCungHan_DuyetTungPhan()
    {
        // L1 ghi là AOTHUN-01 nhưng 4 cái thực ra là AOTHUN-02; thủ kho đếm L1 còn 6 và thấy 4 AOTHUN-02 không nhãn lô.
        var (ma, p) = await KiemKeLech(("L1", 6), ("L2", 6), ("L3", 5), ("L4", 4), ("L5", 3));
        await VoiKiemKe(s => s.ThemDongAsync(ma, new ThemDongKiemKe(Ke + "01", "AOTHUN-02", 4)), "ND004", Vai.ThuKho);
        p = await VoiKiemKe(s => s.LayAsync(ma));

        await VoiKiemKe(s => s.DuyetDieuChinhAsync(ma,
        [
            new() { STT = Stt(p, "L1"), Loai = LoaiChenhLech.SaiBienThe, KetLuan = "Nhãn dán nhầm", MaBienTheDung = "AOTHUN-02" },
            new() { STT = Stt(p, null, "AOTHUN-02"), Loai = LoaiChenhLech.SaiBienThe, KetLuan = "Nhãn dán nhầm" },
        ], "sbt"));

        await using var db = CsdlFixture.TaoDbContext(_chuoi);
        var loMoi = await db.Lo.SingleAsync(l => l.MaBienThe == "AOTHUN-02");
        Assert.Equal(Han, loMoi.HanDung);
        var ton = await Ton();
        Assert.Equal((6m, 4m), (ton[("L1", Ke + "01")], ton[(loMoi.MaLo, Ke + "01")]));
        Assert.Equal(TrangThaiKiemKe.HoanThanh, (await db.PhieuKiemKe.SingleAsync(x => x.MaPhieu == ma)).TrangThai);
        await TongBienDongBangTon();
    }

    [Fact]
    public async Task ThuaKhongNhanLo_TaoLoMoi_BatBuocHanDung_VaKhongHuySauKhiDuyet()
    {
        var (ma, p) = await KiemKeLech(("L1", 10), ("L2", 5), ("L3", 5), ("L4", 4), ("L5", 3));
        await VoiKiemKe(s => s.ThemDongAsync(ma, new ThemDongKiemKe(Ke + "06", "SUATUOI-01", 2)), "ND004", Vai.ThuKho);
        p = await VoiKiemKe(s => s.LayAsync(ma));
        var sua = Stt(p, null, "SUATUOI-01");

        await VoiKiemKe(s => s.DuyetDieuChinhAsync(ma, [new() { STT = Stt(p, "L2"), Loai = LoaiChenhLech.Thieu, KetLuan = "Mất" }], "a"));
        Assert.Equal(TrangThaiKiemKe.DangKiemKe, (await VoiKiemKe(s => s.LayAsync(ma))).TrangThai);
        await Assert.ThrowsAsync<NghiepVuException>(() => VoiKiemKe(s => s.HuyAsync(ma)));
        await Assert.ThrowsAsync<NghiepVuException>(() => VoiKiemKe(s => s.NhapSoDemAsync(ma, [new() { STT = Stt(p, "L2"), SoLuongThucTe = 6 }]), "ND004", Vai.ThuKho));

        Assert.Contains("hạn dùng", (await Assert.ThrowsAsync<NghiepVuException>(() => VoiKiemKe(s => s.DuyetDieuChinhAsync(ma,
            [new() { STT = sua, Loai = LoaiChenhLech.Thua, KetLuan = "Hàng thừa" }], "b")))).Message);
        await VoiKiemKe(s => s.DuyetDieuChinhAsync(ma, [new() { STT = sua, Loai = LoaiChenhLech.Thua, KetLuan = "Hàng thừa", HanDung = new DateOnly(2026, 12, 1) }], "c"));

        await using var db = CsdlFixture.TaoDbContext(_chuoi);
        Assert.Equal(TrangThaiKiemKe.HoanThanh, (await db.PhieuKiemKe.SingleAsync(x => x.MaPhieu == ma)).TrangThai);
        Assert.Equal(new DateOnly(2026, 12, 1), (await db.Lo.SingleAsync(l => l.MaBienThe == "SUATUOI-01" && l.MaLo != "LS")).HanDung);
        await TongBienDongBangTon();
    }

    [Fact]
    public async Task ChuyenKhoNhanThieu_NhapKhoDen_NhapLaiKhoDi_GhiGiamHaoHut()
    {
        var ma = await VoiChuyen("ND002", Vai.QuanLyKho, s => s.TaoAsync(new DieuChuyenForm
        {
            MaKhoDi = "K01", MaKhoDen = "K02", KhoaChongLap = "luu",
            Dong = [new() { LoO = $"L1|{Ke}01", SoLuong = 5 }, new() { LoO = $"L2|{Ke}02", SoLuong = 4 }, new() { LoO = $"L3|{Ke}03", SoLuong = 3 }],
        }, true));
        await VoiChuyen("ND006", Vai.ThuKho, s => s.NhanAsync(ma, [new() { STT = 1, SoLuongNhan = 3 }, new() { STT = 2, SoLuongNhan = 1 }, new() { STT = 3, SoLuongNhan = 1 }], "nhan"));
        var tongDau = (await Ton()).Values.Sum();

        await Assert.ThrowsAsync<KhongCoQuyenException>(() => VoiChuyen("ND006", Vai.ThuKho, s => s.XuLyChenhLechAsync(ma, new(1, XuLyChenhLech.NhapKhoDen, "Tìm thấy"), "x")));
        await Assert.ThrowsAsync<NghiepVuException>(() => VoiChuyen("ND003", Vai.QuanLyKho, s => s.XuLyChenhLechAsync(ma, new(1, XuLyChenhLech.NhapKhoDen, ""), "x")));
        await Assert.ThrowsAsync<KhongCoQuyenException>(() => VoiChuyen("ND003", Vai.QuanLyKho, s => s.XuLyChenhLechAsync(ma, new(1, XuLyChenhLech.NhapLaiKhoDi, "Chưa đi"), "x")));

        Assert.Equal(KetQuaGhi.DaGhi, await VoiChuyen("ND003", Vai.QuanLyKho, s => s.XuLyChenhLechAsync(ma, new(1, XuLyChenhLech.NhapKhoDen, "Tìm thấy trên xe"), "x1")));
        Assert.Equal(KetQuaGhi.DaGhiTruocDo, await VoiChuyen("ND003", Vai.QuanLyKho, s => s.XuLyChenhLechAsync(ma, new(1, XuLyChenhLech.NhapKhoDen, "Tìm thấy trên xe"), "x1")));
        await VoiChuyen("ND002", Vai.QuanLyKho, s => s.XuLyChenhLechAsync(ma, new(2, XuLyChenhLech.NhapLaiKhoDi, "Bốc thiếu lên xe"), "x2"));
        Assert.Equal(TrangThaiDieuChuyen.NhanMotPhan, (await VoiChuyen("ND002", Vai.QuanLyKho, s => s.LayAsync(ma))).TrangThai);
        await VoiChuyen("ND002", Vai.QuanLyKho, s => s.XuLyChenhLechAsync(ma, new(3, XuLyChenhLech.GhiGiamHaoHut, "Rơi vỡ dọc đường"), "x3"));

        var ton = await Ton();
        Assert.Equal(5, ton[("L1", "K02-TA01-TG01-P01-H01-KE01-O01")]);
        Assert.Equal(6 - 4 + 3, ton[("L2", Ke + "02")]);
        Assert.Equal(tongDau + 2 + 3, ton.Values.Sum());
        var p = await VoiChuyen("ND002", Vai.QuanLyKho, s => s.LayAsync(ma));
        Assert.Equal(TrangThaiDieuChuyen.HoanThanh, p.TrangThai);
        Assert.Equal([XuLyChenhLech.NhapKhoDen, XuLyChenhLech.NhapLaiKhoDi, XuLyChenhLech.GhiGiamHaoHut], p.Dong.Select(d => d.CachXuLy!.Value));
        await TongBienDongBangTon();
        await using var db = CsdlFixture.TaoDbContext(_chuoi);
        Assert.Equal(3, await db.NhatKy.CountAsync(n => n.LoaiThaoTac == "XuLyChenhLechDieuChuyen" && n.MaDoiTuong == ma));
    }

    [Fact]
    public async Task ManHinh_DuyetDieuChinh_XuLyChenhLech()
    {
        var (ma, p) = await KiemKeLech(("L1", 9), ("L2", 6), ("L3", 5), ("L4", 4), ("L5", 3));
        var quanLy = _app.TaoClient(Vai.QuanLyKho, "ND002");
        Assert.Contains("Duyệt điều chỉnh", await quanLy.GetStringAsync($"/KiemKe/{ma}"));
        var r = await NguoiThu.PostFormAsync(quanLy, $"/KiemKe/{ma}/DuyetDieuChinh", $"/KiemKe/{ma}", new()
        {
            ["khoaChongLap"] = "form", ["duyet[0].STT"] = Stt(p, "L1").ToString(), ["duyet[0].Loai"] = "Thieu", ["duyet[0].KetLuan"] = "Mất",
        });
        Assert.Equal(HttpStatusCode.Redirect, r.StatusCode);
        Assert.Contains("Hoàn thành", await quanLy.GetStringAsync($"/KiemKe/{ma}"));

        var dc = await VoiChuyen("ND002", Vai.QuanLyKho, s => s.TaoAsync(new DieuChuyenForm
        {
            MaKhoDi = "K01", MaKhoDen = "K02", KhoaChongLap = "luu", Dong = [new() { LoO = $"L2|{Ke}02", SoLuong = 2 }],
        }, true));
        await VoiChuyen("ND006", Vai.ThuKho, s => s.NhanAsync(dc, [new() { STT = 1, SoLuongNhan = 1 }], "nhan"));
        Assert.Contains("Xử lý chênh lệch", await quanLy.GetStringAsync($"/DieuChuyen/{dc}"));
        var xl = await NguoiThu.PostFormAsync(quanLy, $"/DieuChuyen/{dc}/XuLy", $"/DieuChuyen/{dc}", new()
        {
            ["stt"] = "1", ["cachXuLy"] = "NhapLaiKhoDi", ["lyDo"] = "Chưa bốc lên xe", ["khoaChongLap"] = "xl",
        });
        Assert.Equal(HttpStatusCode.Redirect, xl.StatusCode);
        Assert.Equal(TrangThaiDieuChuyen.HoanThanh, (await VoiChuyen("ND002", Vai.QuanLyKho, s => s.LayAsync(dc))).TrangThai);
    }
}
