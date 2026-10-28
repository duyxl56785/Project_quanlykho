using System.Globalization;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using quanlykho.Common;
using quanlykho.Common.GiaoDien;
using quanlykho.Common.Loi;
using quanlykho.Common.PhanQuyen;
using quanlykho.Data;
using quanlykho.Domain;
using quanlykho.Features.DanhMuc;
using quanlykho.Features.Kho;
using static quanlykho.Features.NhapExcel.NhapExcelService;

namespace quanlykho.Features.NhapExcel;

/// <param name="MaPhieu">Phiếu nhập Tồn đầu kỳ (Lưu tạm) đã sinh; null khi tệp còn lỗi hoặc đã nhập.</param>
public record KetQuaTonDauKy(string TenTep, int SoDong, List<LoiDong> Loi, DateTimeOffset? DaNhapLuc, string? MaPhieu);

/// <summary>
/// Nhập tồn đầu kỳ từ Excel cho một kho (FR17): kiểm lỗi mọi dòng, gồm sức chứa ô theo Bảng 2.33 (dồn cả các dòng cùng ô);
/// hết lỗi thì sinh phiếu nhập loại Tồn đầu kỳ ở Lưu tạm, hàng vào thẳng ô ghi trong tệp khi quản lý kho xác nhận qua cổng ghi tồn.
/// Mã băm của tệp ở TepDaNhap chặn nhập lặp.
/// </summary>
public class NhapTonDauKyService(AppDbContext db, NguoiDungHienTai nguoiDung, MaChungTu maChungTu, GhiNhatKy nhatKy, TimeProvider gio)
{
    public const string Loai = "TonDauKy";
    public const string Sheet = "TonDauKy";

    public static readonly Cot[] CotTon = [new("Mã biến thể", true), new("Số lô", true), new("Hạn dùng"), new("Ô", true), new("Số lượng", true),
        new("Đơn vị"), new("Chất lượng"), new("Đơn giá")];

    private record DongTon(int Dong, string MaBienThe, string MaLo, DateOnly? HanDung, string MaViTri, KetQuaQuyDoi Q, decimal SoLuongGoc, ChatLuong ChatLuong, decimal DonGia);

    public async Task<List<LuaChon>> KhoAsync()
    {
        var kho = await nguoiDung.KhoDuocGiaoAsync();
        return await db.ViTri.Where(v => kho.Contains(v.MaViTri)).OrderBy(v => v.MaViTri).Select(v => new LuaChon(v.MaViTri, v.Ten)).ToListAsync();
    }

    public static byte[] TaoMau()
    {
        using var wb = new XLWorkbook();
        TaoSheet(wb, Sheet, CotTon);
        var hd = wb.AddWorksheet("HuongDan");
        string[] dong =
        [
            "Một tệp cho một kho, chọn kho trên màn hình. Cột có dấu * là bắt buộc; dòng 1 là tiêu đề, không đổi tên cột.",
            "Số lô: mã lô đang dán trên hàng (tối đa 20 ký tự không dấu); nhiều dòng cùng số lô là cùng một lô ở nhiều ô, phải cùng biến thể và hạn dùng.",
            "Hạn dùng (yyyy-MM-dd hoặc dd/MM/yyyy) bắt buộc với hàng có theo dõi hạn dùng.",
            "Ô: mã ô cấp 7 của kho. Hàng Đạt vào ô khu lưu trữ (đúng điều kiện bảo quản) hoặc khu tiếp nhận; hàng Lỗi vào ô khu cách ly. Ô phải còn đủ sức chứa.",
            "Số lượng theo Đơn vị (để trống là đơn vị tính chính); Chất lượng: Đạt hoặc Lỗi (để trống là Đạt); Đơn giá theo đơn vị đó, có thể để trống.",
            "Hệ thống kiểm lỗi toàn bộ tệp; hết lỗi thì sinh phiếu nhập Tồn đầu kỳ ở trạng thái Lưu tạm, quản lý kho xác nhận mới ghi tồn. Một tệp chỉ nhập được một lần.",
        ];
        for (var i = 0; i < dong.Length; i++)
            hd.Cell(i + 1, 1).Value = dong[i];
        hd.Column(1).Width = 140;
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    public async Task<KetQuaTonDauKy> NhapAsync(string maKho, Stream tep, string tenTep)
    {
        if (nguoiDung.VaiTro is not (VaiTro.QuanTriVien or VaiTro.QuanLyKho))
            throw new KhongCoQuyenException("Chỉ quản lý kho nhập tồn đầu kỳ.");
        await nguoiDung.DamBaoQuyenKhoAsync(maKho);
        using var ms = new MemoryStream();
        await tep.CopyToAsync(ms);
        if (ms.Length == 0 || ms.Length > KichThuocToiDa)
            throw new NghiepVuException($"Tệp rỗng hoặc lớn hơn {KichThuocToiDa / 1024 / 1024} MB.");
        var noiDung = ms.ToArray();
        var maBam = Convert.ToHexStringLower(SHA256.HashData(noiDung));
        tenTep = Path.GetFileName(tenTep);

        await using var tx = await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlAsync($"SELECT pg_advisory_xact_lock(hashtext({maBam}))");
        var daNhap = await db.TepDaNhap.Where(t => t.MaBam == maBam).Select(t => (DateTimeOffset?)t.ThoiDiem).SingleOrDefaultAsync();
        if (daNhap is not null)
            return new KetQuaTonDauKy(tenTep, 0, [], daNhap, null);

        XLWorkbook wb;
        try
        {
            wb = new XLWorkbook(new MemoryStream(noiDung));
        }
        catch (Exception)
        {
            throw new NghiepVuException("Không đọc được tệp; hãy dùng tệp .xlsx theo mẫu tồn đầu kỳ.");
        }
        var loi = new List<LoiDong>();
        List<DongTon> dong;
        using (wb)
            dong = await KiemTraAsync(maKho, Bang(wb, Sheet, CotTon, loi), loi);
        if (loi.Count == 0 && dong.Count == 0)
            loi.Add(new LoiDong(Sheet, 0, "Tệp không có dòng dữ liệu nào."));
        if (loi.Count > 0)
            return new KetQuaTonDauKy(tenTep, dong.Count, loi, null, null);

        var ma = await maChungTu.TaoAsync("PN");
        var loCo = await db.Lo.Where(l => dong.Select(d => d.MaLo).Contains(l.MaLo)).Select(l => l.MaLo).ToListAsync();
        db.Lo.AddRange(dong.GroupBy(d => d.MaLo).Where(g => !loCo.Contains(g.Key))
            .Select(g => new Lo { MaLo = g.Key, MaBienThe = g.First().MaBienThe, HanDung = g.First().HanDung, NgayNhap = gio.GetUtcNow() }));
        db.PhieuNhap.Add(new PhieuNhap
        {
            MaPhieuNhap = ma, LoaiPhieu = LoaiPhieuNhap.TonDauKy, MaKho = maKho, NguoiLap = nguoiDung.Ma, NgayNhap = gio.GetUtcNow(),
            TrangThai = TrangThaiPhieu.LuuTam, GhiChu = $"Tồn đầu kỳ từ tệp {tenTep}",
            ChiTiet = dong.Select((d, i) => new ChiTietPhieuNhap
            {
                STT = i + 1, MaLo = d.MaLo, TenDonVi = d.Q.TenDonVi, HeSo = d.Q.HeSo, SoLuongGoc = d.SoLuongGoc, SoLuongThucNhan = d.Q.SoLuong,
                DonGia = d.DonGia, ChatLuong = d.ChatLuong, MaViTri = d.MaViTri,
            }).ToList(),
        });
        db.TepDaNhap.Add(new TepDaNhap { MaBam = maBam, TenTep = tenTep, Loai = Loai, SoDong = dong.Count, ThoiDiem = gio.GetUtcNow(), NguoiNhap = nguoiDung.Ma, MaChungTu = ma });
        nhatKy.Ghi("NhapTonDauKy", nameof(PhieuNhap), ma, new { tenTep, soDong = dong.Count }, maKho: maKho);
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return new KetQuaTonDauKy(tenTep, dong.Count, [], null, ma);
    }

    private async Task<List<DongTon>> KiemTraAsync(string maKho, List<DongDoc> doc, List<LoiDong> loi)
    {
        if (loi.Count > 0)
            return [];
        var maHang = doc.Select(d => d[0]).Distinct().ToList();
        var hang = await db.BienThe.Where(b => maHang.Contains(b.MaBienThe) && b.TrangThai == TrangThaiSuDung.DangSuDung && b.SanPham.TrangThai == TrangThaiSuDung.DangSuDung)
            .Select(b => new { b.MaBienThe, b.SanPham.TheoDoiHanDung, b.SanPham.DieuKienBaoQuan, DonVi = new DonViHang(b.DaiDongGoi, b.RongDongGoi, b.CaoDongGoi, b.KhoiLuong) })
            .ToDictionaryAsync(b => b.MaBienThe);
        var donVi = await new QuyDoiDonVi(db).BangAsync(hang.Keys);
        var maO = doc.Select(d => d[3]).Distinct().ToList();
        var o = await db.ViTri.Where(v => v.MaKho == maKho && v.Cap == CapViTri.CapCuoi && maO.Contains(v.MaViTri)).ToDictionaryAsync(v => v.MaViTri);
        var heSo = await db.ViTri.Where(v => v.MaViTri == maKho).Select(v => v.HeSoLapDay).SingleAsync() ?? OPhuHop.HeSoLapDayMacDinh;
        // Hàng đang có trong các ô của tệp; mỗi dòng hợp lệ được cộng dồn để các dòng sau cùng ô kiểm đúng chỗ còn lại.
        var dangCo = (await db.TonKho.Where(t => maO.Contains(t.MaViTri) && t.SoLuong > 0)
                .Join(db.Lo, t => t.MaLo, l => l.MaLo, (t, l) => new { t.MaViTri, t.SoLuong, l.MaBienThe, l.BienThe.DaiDongGoi, l.BienThe.RongDongGoi, l.BienThe.CaoDongGoi, l.BienThe.KhoiLuong })
                .ToListAsync())
            .GroupBy(x => x.MaViTri)
            .ToDictionary(g => g.Key, g => g.Select(x => new HangTrongO(x.MaBienThe, new DonViHang(x.DaiDongGoi, x.RongDongGoi, x.CaoDongGoi, x.KhoiLuong), x.SoLuong)).ToList());
        var maLo = doc.Select(d => d[1]).Distinct().ToList();
        var loCo = await db.Lo.Where(l => maLo.Contains(l.MaLo))
            .Select(l => new { l.MaLo, l.MaBienThe, l.HanDung, CoBienDong = db.SoBienDong.Any(s => s.MaLo == l.MaLo) }).ToDictionaryAsync(l => l.MaLo);
        var loTrongTep = new Dictionary<string, (string MaBienThe, DateOnly? HanDung)>();

        var ketQua = new List<DongTon>();
        foreach (var d in doc)
        {
            var l = new Kiem(Sheet, d.Dong, loi);
            var maBienThe = l.Chuoi(d[0], "Mã biến thể", true, 20);
            var h = hang.GetValueOrDefault(maBienThe);
            if (maBienThe.Length > 0 && h is null)
                l.Them($"Biến thể {maBienThe} không có hoặc đã ngừng sử dụng.");
            var lo = l.Chuoi(d[1], "Số lô", true, 20);
            if (lo.Length > 0 && !Regex.IsMatch(lo, MaHopLe.Mau))
                l.Them($"Số lô {lo} không hợp lệ: {MaHopLe.Loi}");
            var han = NgayDoc(d[2], l);
            if (h is { TheoDoiHanDung: true } && han is null && d[2].Length == 0)
                l.Them($"Hàng {maBienThe} theo dõi hạn dùng: nhập hạn dùng.");
            if (h is { TheoDoiHanDung: false })
                han = null;
            var chatLuong = BoDau(d[6]) switch
            {
                "" or "dat" or "đạt" => ChatLuong.Dat,
                "loi" or "lỗi" => ChatLuong.Loi,
                _ => l.Them<ChatLuong>($"Chất lượng \"{d[6]}\" không hợp lệ (Đạt hoặc Lỗi)."),
            };
            var maViTri = l.Chuoi(d[3], "Ô", true, 40);
            var vt = o.GetValueOrDefault(maViTri);
            if (maViTri.Length > 0 && vt is null)
                l.Them($"Ô {maViTri} không phải ô (cấp 7) của kho {maKho}.");
            else if (vt is not null)
            {
                if (vt.DangKhoa)
                    l.Them($"Ô {maViTri} đang khóa.");
                if (vt.MaPhieuKiemKe is not null)
                    l.Them($"Ô {maViTri} đang khóa để kiểm kê ({vt.MaPhieuKiemKe}).");
                if (chatLuong == ChatLuong.Loi ? vt.LoaiKhu != LoaiKhu.CachLy : vt.LoaiKhu is not (LoaiKhu.LuuTru or LoaiKhu.TiepNhan))
                    l.Them(chatLuong == ChatLuong.Loi ? $"Hàng Lỗi phải vào ô khu cách ly; {maViTri} không phải." : $"Ô {maViTri} không thuộc khu lưu trữ hay tiếp nhận.");
                else if (h is not null && vt.LoaiKhu == LoaiKhu.LuuTru && vt.DieuKienBaoQuan != h.DieuKienBaoQuan)
                    l.Them($"Ô {maViTri} không đúng điều kiện bảo quản của hàng ({CapViTri.TenBaoQuan(h.DieuKienBaoQuan)}).");
            }
            var soLuongGoc = l.SoDuong(d[4], "Số lượng");
            KetQuaQuyDoi? q = null;
            if (h is not null && soLuongGoc > 0)
            {
                try { q = QuyDoiDonVi.QuyVeChinh(donVi[maBienThe], d[5], soLuongGoc, maBienThe); }
                catch (NghiepVuException e) { l.Them(e.Message); }
            }
            var donGia = 0m;
            if (d[7].Length > 0 && (!decimal.TryParse(d[7], NumberStyles.Number, CultureInfo.InvariantCulture, out donGia) || donGia < 0))
                l.Them($"Đơn giá \"{d[7]}\" không hợp lệ.");

            if (lo.Length > 0 && h is not null)
            {
                if (loCo.TryGetValue(lo, out var co) && (co.CoBienDong || co.MaBienThe != maBienThe || co.HanDung != han))
                    l.Them($"Số lô {lo} đã có trong hệ thống.");
                else if (loTrongTep.TryGetValue(lo, out var truoc) && truoc != (maBienThe, han))
                    l.Them($"Số lô {lo} đã ghi ở dòng khác với biến thể hoặc hạn dùng khác.");
                else
                    loTrongTep.TryAdd(lo, (maBienThe, han));
            }

            if (!l.CoLoi && vt is not null && h is not null && q is not null)
            {
                var trongO = dangCo.TryGetValue(maViTri, out var x) ? x : dangCo[maViTri] = [];
                var sc = SucChua.Tinh(new OChua(vt.Dai ?? 0, vt.Rong ?? 0, vt.Cao ?? 0, vt.TaiTrongToiDa ?? 0), heSo, maBienThe, h.DonVi, trongO);
                if (!sc.VuaKichThuoc)
                    l.Them($"Hàng {maBienThe} không vừa kích thước ô {maViTri}.");
                else if (q.SoLuong > sc.QMax)
                    l.Them($"Ô {maViTri} chỉ còn xếp được tối đa {DinhDang.So(sc.QMax)} (tính cả các dòng trước cùng ô), dòng này cần {DinhDang.So(q.SoLuong)}.");
                else
                    trongO.Add(new HangTrongO(maBienThe, h.DonVi, q.SoLuong));
            }
            if (!l.CoLoi)
                ketQua.Add(new DongTon(d.Dong, maBienThe, lo, han, maViTri, q!, soLuongGoc, chatLuong, donGia));
        }
        return ketQua;
    }

    private static DateOnly? NgayDoc(string s, Kiem l)
    {
        if (s.Length == 0)
            return null;
        if (double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var serial))
            return DateOnly.FromDateTime(DateTime.FromOADate(serial));
        if (DateOnly.TryParseExact(s, ["yyyy-MM-dd", "dd/MM/yyyy", "d/M/yyyy"], CultureInfo.InvariantCulture, DateTimeStyles.None, out var ngay))
            return ngay;
        return l.Them<DateOnly?>($"Hạn dùng \"{s}\" không đúng dạng ngày (yyyy-MM-dd hoặc dd/MM/yyyy).");
    }
}
