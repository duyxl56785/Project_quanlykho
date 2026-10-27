using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using quanlykho.Domain;

namespace quanlykho.Data.Seed;

/// <summary>
/// Dữ liệu thử (mục 1.2.5): 2 kho, khoảng 40 ô, khoảng 50 biến thể thuộc 4 ngành, tài khoản mẫu cho 4 vai.
/// Chạy bằng: dotnet run --project quanlykho -- --seed. Chỉ thêm bản ghi chưa có (theo mã), chạy lại không nhân đôi.
/// Tồn ban đầu và lịch sử xuất được nạp ở các nhiệm vụ sau (cần cổng ghi tồn).
/// </summary>
public class DuLieuThu(AppDbContext db, IPasswordHasher<NguoiDung> hasher, IConfiguration config)
{
    public async Task NapAsync()
    {
        await ThemChuaCo(db.ViTri, x => x.MaViTri, TaoViTri("K01", "Kho Hà Nội", "Số 1 Cầu Giấy, Hà Nội").Concat(TaoViTri("K02", "Kho Bắc Ninh", "KCN Tiên Sơn, Bắc Ninh")));
        await ThemChuaCo(db.NhomHang, x => x.MaNhom, NhomHang);
        await ThemChuaCo(db.ThuocTinh, x => x.MaNhom + "|" + x.Ten, ThuocTinh);
        await ThemChuaCo(db.SanPham, x => x.MaSanPham, SanPham.Select(s => s.SanPham));
        await ThemChuaCo(db.BienThe, x => x.MaBienThe, TaoBienThe());
        await ThemChuaCo(db.NhaCungCap, x => x.MaNhaCungCap, NhaCungCap);
        await ThemChuaCo(db.NoiNhan, x => x.MaNoiNhan, NoiNhan);
        await ThemChuaCo(db.MucTon, x => x.MaBienThe + "|" + x.MaKho, MucTon);
        await ThemChuaCo(db.NguoiDung, x => x.MaNguoiDung, TaoNguoiDung());
        await ThemChuaCo(db.NguoiDungKho, x => x.MaNguoiDung + "|" + x.MaKho, NguoiDungKho);
    }

    private async Task ThemChuaCo<T>(DbSet<T> bang, Func<T, string> khoa, IEnumerable<T> dong) where T : class
    {
        var daCo = (await bang.AsNoTracking().ToListAsync()).Select(khoa).ToHashSet();
        bang.AddRange(dong.Where(d => !daCo.Contains(khoa(d))));
        await db.SaveChangesAsync();
    }

    // ---------- Kho và vị trí ----------

    private record Khu(string Ten, LoaiKhu Loai, DieuKienBaoQuan BaoQuan, int SoKe, int SoOMoiKe, decimal Dai, decimal Rong, decimal Cao, decimal TaiTrong);

    private static readonly Khu[] CacKhu =
    [
        new("Khu tiếp nhận", LoaiKhu.TiepNhan, DieuKienBaoQuan.Thuong, 1, 1, 4, 4, 2, 2000),
        new("Khu lưu trữ thường", LoaiKhu.LuuTru, DieuKienBaoQuan.Thuong, 2, 6, 1.2m, 1.0m, 1.0m, 300),
        new("Khu lưu trữ lạnh", LoaiKhu.LuuTru, DieuKienBaoQuan.Lanh, 1, 4, 1.0m, 0.8m, 1.0m, 200),
        new("Khu cách ly", LoaiKhu.CachLy, DieuKienBaoQuan.Thuong, 1, 1, 4, 4, 2, 2000),
        new("Khu xuất", LoaiKhu.Xuat, DieuKienBaoQuan.Thuong, 1, 1, 4, 4, 2, 2000),
    ];

    /// <summary>Cây 7 cấp của một kho: kho, tòa 1, tầng 1, 5 khu, mỗi khu 1 hàng, các kệ, các ô (19 ô mỗi kho).</summary>
    private static IEnumerable<ViTri> TaoViTri(string maKho, string tenKho, string diaChi)
    {
        var kho = new ViTri { MaViTri = maKho, MaKho = maKho, Cap = 1, Ten = tenKho, TenDayDu = tenKho, ThuTu = 1, DiaChi = diaChi, LoaiHangLuuTru = "Tổng hợp", HeSoLapDay = 0.85m };
        var toa = Con(kho, "TA", 1, "Tòa 1");
        var tang = Con(toa, "TG", 1, "Tầng 1");
        yield return kho;
        yield return toa;
        yield return tang;

        for (var k = 0; k < CacKhu.Length; k++)
        {
            var khu = CacKhu[k];
            var phong = Con(tang, "P", k + 1, khu.Ten, khu);
            var hang = Con(phong, "H", 1, "Hàng 1", khu);
            yield return phong;
            yield return hang;
            for (var ke = 1; ke <= khu.SoKe; ke++)
            {
                var viTriKe = Con(hang, "KE", ke, $"Kệ {ke}", khu);
                yield return viTriKe;
                for (var o = 1; o <= khu.SoOMoiKe; o++)
                {
                    var viTriO = Con(viTriKe, "O", o, $"Ô {o}", khu);
                    viTriO.Dai = khu.Dai;
                    viTriO.Rong = khu.Rong;
                    viTriO.Cao = khu.Cao;
                    viTriO.TaiTrongToiDa = khu.TaiTrong;
                    viTriO.LaOMacDinh = khu.Loai is LoaiKhu.TiepNhan or LoaiKhu.CachLy;
                    yield return viTriO;
                }
            }
        }
    }

    private static ViTri Con(ViTri cha, string tienTo, int so, string ten, Khu? khu = null) => new()
    {
        MaViTri = $"{cha.MaViTri}-{tienTo}{so:D2}",
        MaCha = cha.MaViTri,
        MaKho = cha.MaKho,
        Cap = (short)(cha.Cap + 1),
        Ten = ten,
        TenDayDu = $"{cha.TenDayDu} / {ten}",
        ThuTu = so,
        LoaiKhu = khu?.Loai,
        DieuKienBaoQuan = khu?.BaoQuan,
    };

    // ---------- Danh mục ----------

    private static readonly NhomHang[] NhomHang =
    [
        new() { MaNhom = "TT", Ten = "Thời trang" },
        new() { MaNhom = "DT", Ten = "Điện tử" },
        new() { MaNhom = "TP", Ten = "Thực phẩm" },
        new() { MaNhom = "VPP", Ten = "Văn phòng phẩm" },
    ];

    private static readonly ThuocTinh[] ThuocTinh =
    [
        new() { MaNhom = "TT", Ten = "Size", ThuTu = 1, GiaTriGoiY = ["S", "M", "L", "XL"] },
        new() { MaNhom = "TT", Ten = "Màu", ThuTu = 2, GiaTriGoiY = ["Trắng", "Đen", "Xanh"] },
        new() { MaNhom = "DT", Ten = "Dung lượng", ThuTu = 1, GiaTriGoiY = ["128GB", "256GB"] },
        new() { MaNhom = "DT", Ten = "Màu", ThuTu = 2, GiaTriGoiY = ["Đen", "Trắng", "Xanh"] },
        new() { MaNhom = "TP", Ten = "Quy cách", ThuTu = 1, GiaTriGoiY = ["180ml", "1L", "100g", "75g", "500ml", "1.5L"] },
        new() { MaNhom = "TP", Ten = "Hương vị", ThuTu = 2, GiaTriGoiY = [] },
    ];

    /// <summary>Sản phẩm, kích thước đóng gói (m), khối lượng (kg) một đơn vị tính chính, và giá trị từng thuộc tính để sinh tổ hợp.</summary>
    private record MauSanPham(SanPham SanPham, decimal Dai, decimal Rong, decimal Cao, decimal KhoiLuong, (string Ten, string[] GiaTri)[] ThuocTinh);

    private static MauSanPham SP(string ma, string ten, string dvt, string nhom, decimal dai, decimal rong, decimal cao, decimal kg,
        DieuKienBaoQuan baoQuan = DieuKienBaoQuan.Thuong, bool hanDung = false, params (string, string[])[] thuocTinh) =>
        new(new SanPham { MaSanPham = ma, Ten = ten, DonViTinh = dvt, MaNhom = nhom, DieuKienBaoQuan = baoQuan, TheoDoiHanDung = hanDung }, dai, rong, cao, kg, thuocTinh);

    private static readonly MauSanPham[] SanPham =
    [
        SP("AOTHUN", "Áo thun cổ tròn", "cái", "TT", 0.30m, 0.25m, 0.03m, 0.20m, thuocTinh: [("Size", ["S", "M", "L", "XL"]), ("Màu", ["Trắng", "Đen"])]),
        SP("SOMI", "Áo sơ mi", "cái", "TT", 0.35m, 0.28m, 0.04m, 0.30m, thuocTinh: [("Size", ["S", "M", "L", "XL"]), ("Màu", ["Trắng", "Xanh"])]),
        SP("QJEAN", "Quần jean", "cái", "TT", 0.40m, 0.30m, 0.05m, 0.60m, thuocTinh: [("Size", ["28", "29", "30", "31", "32"]), ("Màu", ["Xanh"])]),
        SP("AOKHOAC", "Áo khoác gió", "cái", "TT", 0.40m, 0.30m, 0.08m, 0.50m, thuocTinh: [("Size", ["M", "L", "XL"]), ("Màu", ["Đen"])]),
        SP("DTA", "Điện thoại A", "chiếc", "DT", 0.18m, 0.10m, 0.06m, 0.45m, thuocTinh: [("Dung lượng", ["128GB", "256GB"]), ("Màu", ["Đen", "Trắng"])]),
        SP("DTB", "Điện thoại B", "chiếc", "DT", 0.18m, 0.10m, 0.06m, 0.40m, thuocTinh: [("Dung lượng", ["128GB", "256GB"]), ("Màu", ["Đen", "Xanh"])]),
        SP("TAINGHE", "Tai nghe không dây", "chiếc", "DT", 0.12m, 0.10m, 0.05m, 0.15m, thuocTinh: [("Màu", ["Đen", "Trắng"])]),
        SP("SUATUOI", "Sữa tươi tiệt trùng", "hộp", "TP", 0.07m, 0.05m, 0.12m, 0.20m, DieuKienBaoQuan.Lanh, true, ("Quy cách", ["180ml", "1L"]), ("Hương vị", ["Không đường", "Có đường"])),
        SP("SUACHUA", "Sữa chua", "hộp", "TP", 0.06m, 0.06m, 0.06m, 0.11m, DieuKienBaoQuan.Lanh, true, ("Quy cách", ["100g"]), ("Hương vị", ["Có đường", "Nha đam", "Dâu"])),
        SP("MIGOI", "Mì gói", "gói", "TP", 0.15m, 0.12m, 0.04m, 0.08m, DieuKienBaoQuan.Thuong, true, ("Quy cách", ["75g"]), ("Hương vị", ["Tôm chua cay", "Gà", "Bò"])),
        SP("NUOCSUOI", "Nước suối", "chai", "TP", 0.08m, 0.08m, 0.30m, 1.55m, DieuKienBaoQuan.Thuong, true, ("Quy cách", ["500ml", "1.5L"])),
        SP("BUTBI", "Bút bi", "cây", "VPP", 0.15m, 0.01m, 0.01m, 0.01m),
        SP("GIAYA4", "Giấy A4 70gsm", "ram", "VPP", 0.30m, 0.21m, 0.05m, 2.30m),
        SP("VOKE", "Vở kẻ ngang 200 trang", "quyển", "VPP", 0.25m, 0.18m, 0.02m, 0.35m),
        SP("BAMKIM", "Bấm kim số 10", "cái", "VPP", 0.12m, 0.05m, 0.06m, 0.15m),
    ];

    /// <summary>Biến thể là mọi tổ hợp giá trị thuộc tính; mã MaSanPham-01..., mã vạch 893 + 10 chữ số.</summary>
    private static IEnumerable<BienThe> TaoBienThe()
    {
        var soMaVach = 0;
        foreach (var mau in SanPham)
        {
            IEnumerable<Dictionary<string, string>> toHop = [new()];
            foreach (var (ten, giaTri) in mau.ThuocTinh)
                toHop = toHop.SelectMany(d => giaTri.Select(g => new Dictionary<string, string>(d) { [ten] = g })).ToList();

            var stt = 0;
            foreach (var thuocTinh in toHop)
                yield return new BienThe
                {
                    MaBienThe = $"{mau.SanPham.MaSanPham}-{++stt:D2}",
                    MaSanPham = mau.SanPham.MaSanPham,
                    ThuocTinh = thuocTinh,
                    DaiDongGoi = mau.Dai, RongDongGoi = mau.Rong, CaoDongGoi = mau.Cao, KhoiLuong = mau.KhoiLuong,
                    MaVach = $"893{++soMaVach:D10}",
                };
        }
    }

    private static readonly NhaCungCap[] NhaCungCap =
    [
        new() { MaNhaCungCap = "NCC01", Ten = "Công ty May mặc Việt Tiến Phát", DiaChi = "Quận 3, TP Hồ Chí Minh", NguoiLienHe = "Nguyễn Văn An", SoDienThoai = "0281234567" },
        new() { MaNhaCungCap = "NCC02", Ten = "Công ty Điện tử Sao Mai", DiaChi = "Cầu Giấy, Hà Nội", NguoiLienHe = "Trần Thị Bình", SoDienThoai = "0241234567" },
        new() { MaNhaCungCap = "NCC03", Ten = "Công ty Thực phẩm và Văn phòng phẩm Xanh", DiaChi = "Gia Lâm, Hà Nội", NguoiLienHe = "Lê Văn Cường", SoDienThoai = "0247654321" },
    ];

    private static readonly NoiNhan[] NoiNhan =
    [
        new() { MaNoiNhan = "NN01", Ten = "Cửa hàng Cầu Giấy", DiaChi = "Cầu Giấy, Hà Nội", NguoiLienHe = "Phạm Thu Hà", SoDienThoai = "0912345678" },
        new() { MaNoiNhan = "NN02", Ten = "Đại lý Từ Sơn", DiaChi = "Từ Sơn, Bắc Ninh", NguoiLienHe = "Đỗ Minh Đức", SoDienThoai = "0987654321" },
    ];

    private static readonly MucTon[] MucTon =
    [
        new() { MaBienThe = "SUATUOI-01", MaKho = "K01", ToiThieu = 50, ToiDa = 500, ThoiGianChoHang = 3 },
        new() { MaBienThe = "SUATUOI-01", MaKho = "K02", ToiThieu = 30, ToiDa = 300, ThoiGianChoHang = 3 },
        new() { MaBienThe = "AOTHUN-03", MaKho = "K01", ToiThieu = 20, ToiDa = 200, ThoiGianChoHang = 10 },
        new() { MaBienThe = "DTA-01", MaKho = "K01", ToiThieu = 5, ToiDa = 50, ThoiGianChoHang = 7 },
        new() { MaBienThe = "MIGOI-01", MaKho = "K02", ToiThieu = 100, ToiDa = 1000, ThoiGianChoHang = 5 },
        new() { MaBienThe = "GIAYA4-01", MaKho = "K01", ToiThieu = 10, ToiDa = 100, ThoiGianChoHang = 5 },
    ];

    // ---------- Tài khoản ----------

    private IEnumerable<NguoiDung> TaoNguoiDung()
    {
        var matKhau = config["DuLieuThu:MatKhau"]
            ?? throw new InvalidOperationException("Thiếu cấu hình DuLieuThu:MatKhau (appsettings.Development.json).");

        NguoiDung Tao(string ma, string ten, string email, VaiTro vaiTro)
        {
            var nd = new NguoiDung { MaNguoiDung = ma, Ten = ten, Email = email, VaiTro = vaiTro, SecurityStamp = Guid.NewGuid().ToString("N") };
            nd.MatKhauBam = hasher.HashPassword(nd, matKhau);
            return nd;
        }

        return
        [
            Tao("ND001", "Quản trị viên", "admin@kho.test", VaiTro.QuanTriVien),
            Tao("ND002", "Quản lý kho Hà Nội", "ql.k01@kho.test", VaiTro.QuanLyKho),
            Tao("ND003", "Quản lý kho Bắc Ninh", "ql.k02@kho.test", VaiTro.QuanLyKho),
            Tao("ND004", "Thủ kho Hà Nội 1", "tk1.k01@kho.test", VaiTro.ThuKho),
            Tao("ND005", "Thủ kho Hà Nội 2", "tk2.k01@kho.test", VaiTro.ThuKho),
            Tao("ND006", "Thủ kho Bắc Ninh", "tk.k02@kho.test", VaiTro.ThuKho),
            Tao("ND007", "Nhân viên bán hàng", "banhang@kho.test", VaiTro.NhanVienBanHang),
        ];
    }

    private static readonly NguoiDungKho[] NguoiDungKho =
    [
        new() { MaNguoiDung = "ND002", MaKho = "K01" },
        new() { MaNguoiDung = "ND003", MaKho = "K02" },
        new() { MaNguoiDung = "ND004", MaKho = "K01" },
        new() { MaNguoiDung = "ND005", MaKho = "K01" },
        new() { MaNguoiDung = "ND006", MaKho = "K02" },
    ];
}
