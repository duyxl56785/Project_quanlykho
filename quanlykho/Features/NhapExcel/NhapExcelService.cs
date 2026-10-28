using System.Globalization;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using quanlykho.Common;
using quanlykho.Common.Loi;
using quanlykho.Common.PhanQuyen;
using quanlykho.Data;
using quanlykho.Domain;
using quanlykho.Features.DanhMuc;

namespace quanlykho.Features.NhapExcel;

public record Cot(string TieuDe, bool BatBuoc = false);

/// <param name="Dong">Số dòng trong Excel (dòng 1 là tiêu đề).</param>
public record LoiDong(string Sheet, int Dong, string NoiDung);

/// <param name="XemTruoc">Tối đa 10 dòng đầu hợp lệ của mỗi sheet, dạng "mã – mô tả".</param>
public record KetQuaKiemTra(string MaBam, string TenTep, int SoSanPham, int SoBienThe, int SoNhaCungCap, List<LoiDong> Loi, DateTimeOffset? DaNhapLuc,
    Dictionary<string, List<string>> XemTruoc)
{
    public int SoDong => SoSanPham + SoBienThe + SoNhaCungCap;
    public bool GhiDuoc => Loi.Count == 0 && DaNhapLuc is null && SoDong > 0;
}

/// <summary>
/// Nhập danh mục từ Excel (FR17): kiểm lỗi mọi dòng, chỉ ghi khi hết lỗi, ghi cả tệp trong một transaction;
/// mã băm SHA-256 của tệp lưu ở TepDaNhap để chặn nhập lặp. Tệp đã kiểm tra được giữ tạm theo mã băm chờ bấm Ghi.
/// </summary>
public class NhapExcelService(AppDbContext db, NguoiDungHienTai nguoiDung, GhiNhatKy nhatKy, TimeProvider gio)
{
    public const string Loai = "DanhMuc";
    public const int KichThuocToiDa = 5 * 1024 * 1024;
    public const string SheetSanPham = "SanPham", SheetBienThe = "BienThe", SheetNhaCungCap = "NhaCungCap";

    public static readonly Cot[] CotSanPham = [new("Mã sản phẩm", true), new("Tên sản phẩm", true), new("Đơn vị tính", true), new("Mã nhóm", true),
        new("Bảo quản"), new("Theo dõi hạn dùng"), new("Mô tả")];
    public static readonly Cot[] CotBienThe = [new("Mã biến thể", true), new("Mã sản phẩm", true), new("Thuộc tính"),
        new("Dài (m)", true), new("Rộng (m)", true), new("Cao (m)", true), new("Khối lượng (kg)", true), new("Mã vạch")];
    public static readonly Cot[] CotNhaCungCap = [new("Mã nhà cung cấp", true), new("Tên nhà cung cấp", true), new("Địa chỉ"), new("Người liên hệ"), new("Số điện thoại")];

    private static readonly string ThuMucTam = Path.Combine(Path.GetTempPath(), "quanlykho-nhap-excel");

    private record DuLieu(List<SanPham> SanPham, List<BienThe> BienThe, List<NhaCungCap> NhaCungCap, List<LoiDong> Loi);

    public async Task<byte[]> TaoMauAsync()
    {
        using var wb = new XLWorkbook();
        TaoSheet(wb, SheetSanPham, CotSanPham);
        TaoSheet(wb, SheetBienThe, CotBienThe);
        TaoSheet(wb, SheetNhaCungCap, CotNhaCungCap);

        var hd = wb.AddWorksheet("HuongDan");
        string[] dong =
        [
            "Cột có dấu * là bắt buộc. Mỗi dòng một mục, không để trống dòng ở giữa; dòng 1 là tiêu đề, không đổi tên cột.",
            "Mã: tối đa 20 ký tự, chữ không dấu, số, gạch ngang, gạch dưới; không trùng trong tệp và không trùng mã đã có.",
            "Bảo quản: Thường hoặc Lạnh (để trống là Thường). Theo dõi hạn dùng: Có hoặc Không (để trống là Không).",
            "Biến thể: Mã sản phẩm là sản phẩm trong sheet SanPham hoặc đã có trong hệ thống. Thuộc tính ghi dạng Size=M; Màu=Trắng, tên thuộc tính theo nhóm hàng (xem sheet NhomHang).",
            "Kích thước đóng gói tính bằng mét, khối lượng bằng kg, số thập phân dùng dấu chấm (1.25). Mã vạch không trùng hàng khác.",
            "Hệ thống kiểm lỗi toàn bộ tệp trước; chỉ ghi khi không còn lỗi. Một tệp chỉ nhập được một lần.",
        ];
        for (var i = 0; i < dong.Length; i++)
            hd.Cell(i + 1, 1).Value = dong[i];
        hd.Column(1).Width = 140;

        var nhom = wb.AddWorksheet("NhomHang");
        nhom.Cell(1, 1).Value = "Mã nhóm";
        nhom.Cell(1, 2).Value = "Tên nhóm";
        nhom.Cell(1, 3).Value = "Thuộc tính";
        nhom.Row(1).Style.Font.Bold = true;
        var thuocTinh = (await db.ThuocTinh.OrderBy(t => t.ThuTu).ToListAsync()).ToLookup(t => t.MaNhom, t => t.Ten);
        var r = 2;
        foreach (var n in await db.NhomHang.OrderBy(n => n.MaNhom).ToListAsync())
        {
            nhom.Cell(r, 1).Value = n.MaNhom;
            nhom.Cell(r, 2).Value = n.Ten;
            nhom.Cell(r++, 3).Value = string.Join(", ", thuocTinh[n.MaNhom]);
        }
        nhom.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    public async Task<KetQuaKiemTra> KiemTraAsync(Stream tep, string tenTep)
    {
        using var ms = new MemoryStream();
        await tep.CopyToAsync(ms);
        if (ms.Length == 0 || ms.Length > KichThuocToiDa)
            throw new NghiepVuException($"Tệp rỗng hoặc lớn hơn {KichThuocToiDa / 1024 / 1024} MB.");
        var noiDung = ms.ToArray();
        var maBam = Convert.ToHexStringLower(SHA256.HashData(noiDung));
        var kq = await KiemTraNoiDungAsync(noiDung, maBam, Path.GetFileName(tenTep));
        if (kq.GhiDuoc)
        {
            Directory.CreateDirectory(ThuMucTam);
            await File.WriteAllBytesAsync(DuongDanTam(maBam), noiDung);
        }
        return kq;
    }

    /// <summary>Ghi tệp đã kiểm tra (giữ tạm theo mã băm); kiểm lại toàn bộ trước khi ghi.</summary>
    public async Task<KetQuaKiemTra> GhiAsync(string maBam, string tenTep)
    {
        if (!Regex.IsMatch(maBam, "^[0-9a-f]{64}$") || !File.Exists(DuongDanTam(maBam)))
            throw new NghiepVuException("Không tìm thấy tệp đã kiểm tra; hãy tải tệp lên lại.");
        var noiDung = await File.ReadAllBytesAsync(DuongDanTam(maBam));

        await using var tx = await db.Database.BeginTransactionAsync();
        // Khóa theo mã băm để hai lần bấm Ghi cùng lúc không cùng ghi.
        await db.Database.ExecuteSqlAsync($"SELECT pg_advisory_xact_lock(hashtext({maBam}))");
        var (kq, dl) = await DocVaKiemTraAsync(noiDung, maBam, Path.GetFileName(tenTep));
        if (kq.DaNhapLuc is not null)
            throw new NghiepVuException($"Tệp này đã được nhập lúc {Common.GiaoDien.DinhDang.NgayGio(kq.DaNhapLuc)}.");
        if (!kq.GhiDuoc)
            throw new NghiepVuException($"Tệp còn {kq.Loi.Count} lỗi; sửa rồi tải lên lại.");

        db.SanPham.AddRange(dl.SanPham);
        db.BienThe.AddRange(dl.BienThe);
        db.NhaCungCap.AddRange(dl.NhaCungCap);
        db.TepDaNhap.Add(new TepDaNhap { MaBam = maBam, TenTep = kq.TenTep, Loai = Loai, SoDong = kq.SoDong, ThoiDiem = gio.GetUtcNow(), NguoiNhap = nguoiDung.Ma });
        nhatKy.Ghi("NhapExcelDanhMuc", nameof(TepDaNhap), maBam[..20],
            new { tenTep = kq.TenTep, sanPham = kq.SoSanPham, bienThe = kq.SoBienThe, nhaCungCap = kq.SoNhaCungCap });
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException e) when (e.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw new XungDotException("Dữ liệu vừa thay đổi (mã hoặc mã vạch đã có); tải tệp lên lại để kiểm tra.");
        }
        await tx.CommitAsync();
        File.Delete(DuongDanTam(maBam));
        return kq;
    }

    private async Task<KetQuaKiemTra> KiemTraNoiDungAsync(byte[] noiDung, string maBam, string tenTep) => (await DocVaKiemTraAsync(noiDung, maBam, tenTep)).Kq;

    private async Task<(KetQuaKiemTra Kq, DuLieu Dl)> DocVaKiemTraAsync(byte[] noiDung, string maBam, string tenTep)
    {
        var daNhap = await db.TepDaNhap.Where(t => t.MaBam == maBam).Select(t => (DateTimeOffset?)t.ThoiDiem).SingleOrDefaultAsync();
        XLWorkbook wb;
        try
        {
            wb = new XLWorkbook(new MemoryStream(noiDung));
        }
        catch (Exception)
        {
            throw new NghiepVuException("Không đọc được tệp; hãy dùng tệp .xlsx theo mẫu của hệ thống.");
        }
        using (wb)
        {
            var dl = await DocAsync(wb);
            if (dl.Loi.Count == 0 && dl.SanPham.Count + dl.BienThe.Count + dl.NhaCungCap.Count == 0)
                dl.Loi.Add(new LoiDong("", 0, "Tệp không có dòng dữ liệu nào."));
            var xemTruoc = new Dictionary<string, List<string>>
            {
                [SheetSanPham] = dl.SanPham.Take(10).Select(x => $"{x.MaSanPham} – {x.Ten} ({x.DonViTinh}, nhóm {x.MaNhom})").ToList(),
                [SheetBienThe] = dl.BienThe.Take(10).Select(x => $"{x.MaBienThe} – {x.MaSanPham} {string.Join(" ", x.ThuocTinh.Values)}").ToList(),
                [SheetNhaCungCap] = dl.NhaCungCap.Take(10).Select(x => $"{x.MaNhaCungCap} – {x.Ten}").ToList(),
            };
            return (new KetQuaKiemTra(maBam, tenTep, dl.SanPham.Count, dl.BienThe.Count, dl.NhaCungCap.Count, dl.Loi, daNhap, xemTruoc), dl);
        }
    }

    private async Task<DuLieu> DocAsync(XLWorkbook wb)
    {
        var loi = new List<LoiDong>();
        var sp = Bang(wb, SheetSanPham, CotSanPham, loi);
        var bt = Bang(wb, SheetBienThe, CotBienThe, loi);
        var ncc = Bang(wb, SheetNhaCungCap, CotNhaCungCap, loi);
        if (loi.Count > 0)
            return new([], [], [], loi);

        var nhomCo = await db.NhomHang.Select(n => n.MaNhom).ToHashSetAsync();
        var thuocTinh = (await db.ThuocTinh.Select(t => new { t.MaNhom, t.Ten }).ToListAsync()).ToLookup(t => t.MaNhom, t => t.Ten);
        var spCo = await db.SanPham.ToDictionaryAsync(s => s.MaSanPham, s => s.MaNhom);
        var btCo = await db.BienThe.Select(b => b.MaBienThe).ToHashSetAsync();
        var toHopCo = (await db.BienThe.Select(b => new { b.MaSanPham, b.ThuocTinh }).ToListAsync()).Select(b => b.MaSanPham + "|" + ToHop(b.ThuocTinh)).ToHashSet();
        var maVachCo = (await db.BienThe.Where(b => b.MaVach != null).Select(b => b.MaVach!).ToListAsync())
            .Concat(await db.DonViQuyDoi.Where(d => d.MaVach != null).Select(d => d.MaVach!).ToListAsync()).ToHashSet();
        var nccCo = await db.NhaCungCap.Select(n => n.MaNhaCungCap).ToHashSetAsync();

        var sanPham = new List<SanPham>();
        var maSpTrongTep = new Dictionary<string, string>();
        foreach (var d in sp)
        {
            var l = new Kiem(SheetSanPham, d.Dong, loi);
            var ma = l.Ma(d[0], "Mã sản phẩm", spCo.ContainsKey, maSpTrongTep.ContainsKey);
            var ten = l.Chuoi(d[1], "Tên sản phẩm", true, 200);
            var dvt = l.Chuoi(d[2], "Đơn vị tính", true, 20);
            var nhom = l.Chuoi(d[3], "Mã nhóm", true, 20);
            if (nhom.Length > 0 && !nhomCo.Contains(nhom))
                l.Them($"Mã nhóm {nhom} không có.");
            var baoQuan = BoDau(d[4]) switch
            {
                "" or "thuong" => DieuKienBaoQuan.Thuong,
                "lanh" => DieuKienBaoQuan.Lanh,
                _ => l.Them<DieuKienBaoQuan>($"Bảo quản \"{d[4]}\" không hợp lệ (Thường hoặc Lạnh)."),
            };
            var hanDung = BoDau(d[5]) switch
            {
                "" or "khong" or "0" or "false" => false,
                "co" or "1" or "true" or "x" => true,
                _ => l.Them<bool>($"Theo dõi hạn dùng \"{d[5]}\" không hợp lệ (Có hoặc Không)."),
            };
            if (ma.Length > 0)
                maSpTrongTep.TryAdd(ma, nhomCo.Contains(nhom) ? nhom : "");
            if (!l.CoLoi)
                sanPham.Add(new SanPham
                {
                    MaSanPham = ma, Ten = ten, DonViTinh = dvt, MaNhom = nhom, DieuKienBaoQuan = baoQuan, TheoDoiHanDung = hanDung,
                    MoTa = d[6].Length == 0 ? null : d[6], TrangThai = TrangThaiSuDung.DangSuDung,
                });
        }

        var bienThe = new List<BienThe>();
        var maBtTrongTep = new HashSet<string>();
        var toHopTrongTep = new HashSet<string>();
        var maVachTrongTep = new HashSet<string>();
        foreach (var d in bt)
        {
            var l = new Kiem(SheetBienThe, d.Dong, loi);
            var ma = l.Ma(d[0], "Mã biến thể", btCo.Contains, maBtTrongTep.Contains);
            var maSp = l.Chuoi(d[1], "Mã sản phẩm", true, 20);
            string? nhom = null;
            if (maSp.Length > 0 && !maSpTrongTep.TryGetValue(maSp, out nhom) && !spCo.TryGetValue(maSp, out nhom))
                l.Them($"Sản phẩm {maSp} không có trong tệp hay trong hệ thống.");
            var tt = new Dictionary<string, string>();
            foreach (var cap in d[2].Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var kv = cap.Split('=', 2, StringSplitOptions.TrimEntries);
                if (kv.Length != 2 || kv[0].Length == 0 || kv[1].Length == 0)
                    l.Them($"Thuộc tính \"{cap}\" phải có dạng Tên=Giá trị.");
                else if (!tt.TryAdd(kv[0], kv[1]))
                    l.Them($"Thuộc tính {kv[0]} ghi hai lần.");
            }
            // Nhóm không xác định (sản phẩm lỗi) thì bỏ qua kiểm thuộc tính để không báo lỗi dây chuyền.
            if (!string.IsNullOrEmpty(nhom))
            {
                var hopLe = thuocTinh[nhom].ToHashSet();
                foreach (var k in tt.Keys.Where(k => !hopLe.Contains(k)))
                    l.Them($"Nhóm {nhom} không có thuộc tính {k}.");
            }
            var dai = l.SoDuong(d[3], "Dài");
            var rong = l.SoDuong(d[4], "Rộng");
            var cao = l.SoDuong(d[5], "Cao");
            var kl = l.SoDuong(d[6], "Khối lượng");
            var maVach = l.Chuoi(d[7], "Mã vạch", false, 64);
            if (maVach.Length > 0 && (maVachCo.Contains(maVach) || !maVachTrongTep.Add(maVach)))
                l.Them($"Mã vạch {maVach} trùng hàng khác.");
            var toHop = maSp + "|" + ToHop(tt);
            if (maSp.Length > 0 && (toHopCo.Contains(toHop) || toHopTrongTep.Contains(toHop)))
                l.Them($"Sản phẩm {maSp} đã có biến thể cùng tổ hợp thuộc tính.");
            if (ma.Length > 0)
                maBtTrongTep.Add(ma);
            toHopTrongTep.Add(toHop);
            if (!l.CoLoi)
                bienThe.Add(new BienThe
                {
                    MaBienThe = ma, MaSanPham = maSp, ThuocTinh = tt, DaiDongGoi = dai, RongDongGoi = rong, CaoDongGoi = cao, KhoiLuong = kl,
                    MaVach = maVach.Length == 0 ? null : maVach, TrangThai = TrangThaiSuDung.DangSuDung,
                });
        }

        var nhaCungCap = new List<NhaCungCap>();
        var maNccTrongTep = new HashSet<string>();
        foreach (var d in ncc)
        {
            var l = new Kiem(SheetNhaCungCap, d.Dong, loi);
            var ma = l.Ma(d[0], "Mã nhà cung cấp", nccCo.Contains, maNccTrongTep.Contains);
            var ten = l.Chuoi(d[1], "Tên nhà cung cấp", true, 200);
            var diaChi = l.Chuoi(d[2], "Địa chỉ", false, 300);
            var lienHe = l.Chuoi(d[3], "Người liên hệ", false, 100);
            var dienThoai = l.Chuoi(d[4], "Số điện thoại", false, 20);
            if (ma.Length > 0)
                maNccTrongTep.Add(ma);
            if (!l.CoLoi)
                nhaCungCap.Add(new NhaCungCap
                {
                    MaNhaCungCap = ma, Ten = ten, DiaChi = Null(diaChi), NguoiLienHe = Null(lienHe), SoDienThoai = Null(dienThoai), TrangThai = TrangThaiSuDung.DangSuDung,
                });
        }
        return new(sanPham, bienThe, nhaCungCap, loi.OrderBy(x => x.Sheet == SheetSanPham ? 0 : x.Sheet == SheetBienThe ? 1 : 2).ThenBy(x => x.Dong).ToList());
    }

    internal record DongDoc(int Dong, string[] O)
    {
        public string this[int i] => O[i];
    }

    /// <summary>Đọc một sheet theo tiêu đề cột (bỏ dấu *, không phân biệt hoa thường); thiếu sheet hoặc cột bắt buộc là lỗi cả tệp.</summary>
    internal static List<DongDoc> Bang(XLWorkbook wb, string ten, Cot[] cot, List<LoiDong> loi)
    {
        if (!wb.TryGetWorksheet(ten, out var ws))
        {
            loi.Add(new LoiDong(ten, 0, $"Thiếu sheet {ten}; hãy dùng tệp mẫu."));
            return [];
        }
        var tieuDe = ws.Row(1).CellsUsed().GroupBy(c => ChuanTieuDe(c.GetString())).ToDictionary(g => g.Key, g => g.First().Address.ColumnNumber);
        var viTri = cot.Select(c => tieuDe.GetValueOrDefault(ChuanTieuDe(c.TieuDe))).ToArray();
        foreach (var c in cot.Where((c, i) => c.BatBuoc && viTri[i] == 0))
            loi.Add(new LoiDong(ten, 1, $"Thiếu cột {c.TieuDe}."));

        var ketQua = new List<DongDoc>();
        var cuoi = ws.LastRowUsed()?.RowNumber() ?? 1;
        for (var r = 2; r <= cuoi; r++)
        {
            var o = viTri.Select(c => c == 0 ? "" : ChuoiO(ws.Cell(r, c))).ToArray();
            if (o.All(x => x.Length == 0))
                continue;
            ketQua.Add(new DongDoc(r, o));
        }
        return ketQua;
    }

    internal static string ChuoiO(IXLCell c) => c.Value.Type switch
    {
        XLDataType.Blank => "",
        XLDataType.Number => c.Value.GetNumber().ToString(CultureInfo.InvariantCulture),
        XLDataType.DateTime => c.Value.GetDateTime().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        _ => c.Value.ToString(CultureInfo.InvariantCulture).Trim(),
    };

    private static string ChuanTieuDe(string s) => s.Replace("*", "").Trim().ToLowerInvariant();

    internal static string BoDau(string s) => s.Trim().ToLowerInvariant() switch
    {
        "thường" => "thuong",
        "lạnh" => "lanh",
        "có" => "co",
        "không" => "khong",
        var x => x,
    };

    private static string ToHop(Dictionary<string, string> tt) => string.Join(";", tt.OrderBy(k => k.Key, StringComparer.Ordinal).Select(k => $"{k.Key}={k.Value}"));

    private static string? Null(string s) => s.Length == 0 ? null : s;

    private static string DuongDanTam(string maBam) => Path.Combine(ThuMucTam, maBam + ".xlsx");

    internal static void TaoSheet(XLWorkbook wb, string ten, Cot[] cot)
    {
        var ws = wb.AddWorksheet(ten);
        for (var i = 0; i < cot.Length; i++)
            ws.Cell(1, i + 1).Value = cot[i].TieuDe + (cot[i].BatBuoc ? "*" : "");
        ws.Row(1).Style.Font.Bold = true;
        ws.Columns(1, cot.Length).Width = 22;
        ws.SheetView.FreezeRows(1);
    }

    /// <summary>Gom lỗi của một dòng.</summary>
    internal sealed class Kiem(string sheet, int dong, List<LoiDong> loi)
    {
        public bool CoLoi { get; private set; }

        public void Them(string noiDung)
        {
            loi.Add(new LoiDong(sheet, dong, noiDung));
            CoLoi = true;
        }

        public T Them<T>(string noiDung)
        {
            Them(noiDung);
            return default!;
        }

        public string Chuoi(string giaTri, string ten, bool batBuoc, int toiDa)
        {
            if (batBuoc && giaTri.Length == 0)
                Them($"Thiếu {ten}.");
            else if (giaTri.Length > toiDa)
                Them($"{ten} dài quá {toiDa} ký tự.");
            return giaTri;
        }

        public string Ma(string giaTri, string ten, Func<string, bool> daCo, Func<string, bool> trungTep)
        {
            if (giaTri.Length == 0)
                Them($"Thiếu {ten}.");
            else if (!Regex.IsMatch(giaTri, MaHopLe.Mau))
                Them($"{ten} {giaTri} không hợp lệ: {MaHopLe.Loi}");
            else if (trungTep(giaTri))
                Them($"{ten} {giaTri} trùng một dòng khác trong tệp.");
            else if (daCo(giaTri))
                Them($"{ten} {giaTri} đã có trong hệ thống.");
            return giaTri;
        }

        public decimal SoDuong(string giaTri, string ten)
        {
            if (giaTri.Length == 0)
                return Them<decimal>($"Thiếu {ten}.");
            if (!decimal.TryParse(giaTri, NumberStyles.Number, CultureInfo.InvariantCulture, out var so))
                return Them<decimal>($"{ten} \"{giaTri}\" không phải số.");
            if (so <= 0 || so >= 100000)
                return Them<decimal>($"{ten} phải lớn hơn 0 và nhỏ hơn 100000.");
            return Math.Round(so, 3);
        }
    }
}
