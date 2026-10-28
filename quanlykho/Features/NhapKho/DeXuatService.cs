using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using quanlykho.Common.GiaoDien;
using quanlykho.Common.Loi;
using quanlykho.Common.PhanQuyen;
using quanlykho.Data;
using quanlykho.Domain;
using quanlykho.Features.XuatKho;

namespace quanlykho.Features.NhapKho;

/// <param name="MaNhaCungCap">Nhà cung cấp và đơn giá (theo đơn vị chính) của lần mua gần nhất, để chọn sẵn.</param>
public record DongDeXuat(HangG3 DauVao, KetQuaG3 KetQua, string TenHang, string DonViTinh, string? MaNhaCungCap, decimal DonGia);

public record DeXuatTrang(string MaKho, List<LuaChon> DanhSachKho, int SoNgayDuHang, List<DongDeXuat> Dong, List<LuaChon> DanhSachNhaCungCap);

public class DongChonG3
{
    public bool Chon { get; set; }
    public string MaBienThe { get; set; } = "";
    public decimal SoLuong { get; set; }
    public string? MaNhaCungCap { get; set; }
}

public class DeXuatForm
{
    public string MaKho { get; set; } = "";
    public List<DongChonG3> Dong { get; set; } = [];
    public string? LyDoBoQua { get; set; }
}

/// <summary>Gợi ý G3 chỉ để tham khảo (quy tắc 13): chỉ tạo đơn mua nháp khi quản lý kho bấm; nháp thành chính thức khi xác nhận.</summary>
public class DeXuatService(AppDbContext db, NguoiDungHienTai nguoiDung, TonKhaDung tonKhaDung, DonMuaService donMua, IConfiguration config, TimeProvider gio)
{
    public const string GhiChuDon = "Đề xuất nhập bổ sung (G3)";
    private static readonly TrangThaiDonMua[] ChuaXong = [TrangThaiDonMua.Nhap, TrangThaiDonMua.ChuaThucHien, TrangThaiDonMua.DangThucHien];

    public int SoNgayDuHang => config.GetValue("G3:SoNgayDuHang", 30);

    public async Task<DeXuatTrang> TrangAsync(string? maKho)
    {
        var kho = await nguoiDung.KhoDuocGiaoAsync();
        maKho = string.IsNullOrWhiteSpace(maKho) ? kho.FirstOrDefault() ?? throw new NghiepVuException("Bạn chưa được giao kho nào.") : maKho;
        return new DeXuatTrang(maKho,
            await db.ViTri.Where(v => kho.Contains(v.MaViTri)).OrderBy(v => v.MaViTri).Select(v => new LuaChon(v.MaViTri, v.Ten)).ToListAsync(),
            SoNgayDuHang, await TinhAsync(maKho),
            await db.NhaCungCap.Where(n => n.TrangThai == TrangThaiSuDung.DangSuDung).OrderBy(n => n.MaNhaCungCap)
                .Select(n => new LuaChon(n.MaNhaCungCap, n.Ten)).ToListAsync());
    }

    /// <summary>Mọi biến thể đang dùng của kho có lịch sử xuất bán hoặc có mức tồn; dòng đề xuất xếp trước.</summary>
    public async Task<List<DongDeXuat>> TinhAsync(string maKho)
    {
        await nguoiDung.DamBaoQuyenKhoAsync(maKho);
        var homNay = tonKhaDung.HomNay;
        var tu = BatDauNgay(homNay.AddDays(1 - DeXuatNhapBoSung.SoNgayLichSuToiDa));

        var xuat = await (from p in db.PhieuXuat
                          where p.MaKho == maKho && p.LoaiPhieu == LoaiPhieuXuat.XuatBan && p.TrangThai == TrangThaiPhieu.HoanTat
                          from c in p.ChiTiet
                          join l in db.Lo on c.MaLo equals l.MaLo
                          group new { p.NgayXuat, c.SoLuong } by l.MaBienThe into g
                          select new { g.Key, Dau = g.Min(x => x.NgayXuat), Tong = g.Where(x => x.NgayXuat >= tu).Sum(x => x.SoLuong) })
            .ToDictionaryAsync(x => x.Key);
        var mucTon = await db.MucTon.Where(m => m.MaKho == maKho).ToDictionaryAsync(m => m.MaBienThe);

        var ungVien = xuat.Keys.Union(mucTon.Keys).ToList();
        var hang = await db.BienThe.Where(b => ungVien.Contains(b.MaBienThe)
                && b.TrangThai == TrangThaiSuDung.DangSuDung && b.SanPham.TrangThai == TrangThaiSuDung.DangSuDung)
            .Select(b => new { b.MaBienThe, b.SanPham.Ten, b.ThuocTinh, b.SanPham.DonViTinh }).ToListAsync();
        var ma = hang.Select(h => h.MaBienThe).ToList();
        var khaDung = await tonKhaDung.TheoBienTheAsync(maKho, ma);
        var choNhap = await ChoNhapAsync(maKho, ma);
        var muaGanNhat = (await (from c in db.ChiTietDonMua
                                 join d in db.DonMua on c.MaDonMua equals d.MaDonMua
                                 where ma.Contains(c.MaBienThe) && d.TrangThai != TrangThaiDonMua.DaHuy
                                 select new { c.MaBienThe, d.MaNhaCungCap, d.NgayTao, d.MaDonMua, c.DonGia, c.HeSo }).ToListAsync())
            .GroupBy(x => x.MaBienThe)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.NgayTao).ThenByDescending(x => x.MaDonMua).First());

        return hang.Select(h =>
            {
                var x = xuat.GetValueOrDefault(h.MaBienThe);
                var m = mucTon.GetValueOrDefault(h.MaBienThe);
                var vao = new HangG3(h.MaBienThe, khaDung.GetValueOrDefault(h.MaBienThe),
                    x is null ? null : DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(x.Dau, DinhDang.GioVietNam).DateTime),
                    x?.Tong ?? 0, m?.ToiThieu ?? 0, m?.ThoiGianChoHang ?? 7, choNhap.GetValueOrDefault(h.MaBienThe));
                var mua = muaGanNhat.GetValueOrDefault(h.MaBienThe);
                return new DongDeXuat(vao, DeXuatNhapBoSung.Tinh(vao, homNay, SoNgayDuHang), DonMuaService.MoTaHang(h.Ten, h.ThuocTinh), h.DonViTinh,
                    mua?.MaNhaCungCap, mua is null ? 0 : Math.Round(mua.DonGia / mua.HeSo, 2));
            })
            .OrderBy(d => d.KetQua.KetLuan).ThenBy(d => d.DauVao.MaBienThe, StringComparer.Ordinal).ToList();
    }

    /// <summary>Gom dòng đã chọn theo nhà cung cấp, mỗi nhà cung cấp một đơn mua nháp; ghi LichSuGoiY cho từng đơn.</summary>
    public async Task<List<string>> TaoDonNhapAsync(DeXuatForm form)
    {
        await nguoiDung.DamBaoQuyenKhoAsync(form.MaKho);
        await using var tx = await db.Database.BeginTransactionAsync();
        // Khóa dòng kho để hai lần bấm đồng thời tính lần lượt: lần sau thấy đơn nháp của lần trước trong số chờ nhập.
        await db.Database.ExecuteSqlAsync($"SELECT 1 FROM \"ViTri\" WHERE \"MaViTri\" = {form.MaKho} FOR UPDATE");
        var (chon, deXuat) = await KiemTraChonAsync(form);
        var thieuNcc = chon.FirstOrDefault(c => string.IsNullOrWhiteSpace(c.MaNhaCungCap));
        if (thieuNcc is not null)
            throw new NghiepVuException($"Chọn nhà cung cấp cho {thieuNcc.MaBienThe}.");
        if (chon.Any(c => c.SoLuong <= 0))
            throw new NghiepVuException("Số lượng nhập phải lớn hơn 0.");

        var maDon = new List<string>();
        foreach (var nhom in chon.GroupBy(c => c.MaNhaCungCap!).OrderBy(g => g.Key))
        {
            var ma = await donMua.TaoAsync(new DonMuaForm
            {
                MaNhaCungCap = nhom.Key, MaKho = form.MaKho, GhiChu = GhiChuDon,
                Dong = nhom.Select(c => new DongDonMuaForm { MaBienThe = c.MaBienThe, SoLuong = c.SoLuong, DonGia = deXuat[c.MaBienThe].DonGia }).ToList(),
            }, laNhap: true);
            db.LichSuGoiY.Add(LichSu(form.MaKho, null, ma, nhom.Select(c => deXuat[c.MaBienThe].DauVao),
                nhom.Select(c => new { c.MaBienThe, deXuat[c.MaBienThe].KetQua.SoDeXuat, SoChon = c.SoLuong, deXuat[c.MaBienThe].KetQua.LyDo })));
            maDon.Add(ma);
        }
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return maDon;
    }

    /// <summary>Quản lý kho không làm theo đề xuất: ghi lý do vào LichSuGoiY với ChapNhan = false.</summary>
    public async Task<int> BoQuaAsync(DeXuatForm form)
    {
        if (string.IsNullOrWhiteSpace(form.LyDoBoQua))
            throw new NghiepVuException("Nhập lý do bỏ qua đề xuất.");
        var (chon, deXuat) = await KiemTraChonAsync(form);
        var lyDo = form.LyDoBoQua.Trim();
        foreach (var c in chon)
        {
            var ls = LichSu(form.MaKho, c.MaBienThe, null, [deXuat[c.MaBienThe].DauVao], new { deXuat[c.MaBienThe].KetQua.SoDeXuat, deXuat[c.MaBienThe].KetQua.LyDo, LyDoBoQua = lyDo });
            ls.ChapNhan = false;
            ls.ThoiDiemPhanHoi = ls.ThoiDiem;
            db.LichSuGoiY.Add(ls);
        }
        await db.SaveChangesAsync();
        return chon.Count;
    }

    private async Task<(List<DongChonG3> Chon, Dictionary<string, DongDeXuat> DeXuat)> KiemTraChonAsync(DeXuatForm form)
    {
        var chon = form.Dong.Where(d => d.Chon).ToList();
        if (chon.Count == 0)
            throw new NghiepVuException("Chọn ít nhất một dòng đề xuất.");
        if (chon.GroupBy(c => c.MaBienThe).Any(g => g.Count() > 1))
            throw new NghiepVuException("Mỗi biến thể chỉ chọn một dòng.");
        // Tính lại lúc bấm: đơn nháp vừa tạo đã vào số chờ nhập nên bấm lặp không tạo đơn trùng.
        var deXuat = (await TinhAsync(form.MaKho)).Where(d => d.KetQua.KetLuan == KetLuanG3.DeXuat).ToDictionary(d => d.DauVao.MaBienThe);
        var khongCon = chon.FirstOrDefault(c => !deXuat.ContainsKey(c.MaBienThe));
        if (khongCon is not null)
            throw new NghiepVuException($"{khongCon.MaBienThe} không còn trong danh sách đề xuất (có thể đã có đơn mua chờ nhập). Tải lại trang để xem số mới.");
        return (chon, deXuat);
    }

    private LichSuGoiY LichSu(string maKho, string? maBienThe, string? maChungTu, IEnumerable<HangG3> dauVao, object ketQua) => new()
    {
        Loai = LoaiGoiY.NhapBoSung, MaKho = maKho, MaBienThe = maBienThe, MaChungTu = maChungTu, ThoiDiem = gio.GetUtcNow(), NguoiDung = nguoiDung.Ma,
        DauVao = JsonSerializer.Serialize(new { HomNay = tonKhaDung.HomNay, SoNgayDuHang, Hang = dauVao }, JsonSerializerOptions.Web),
        KetQua = JsonSerializer.Serialize(ketQua, JsonSerializerOptions.Web),
    };

    /// <summary>Phần chưa nhận (theo đơn vị chính) của đơn mua chưa hoàn thành, kể cả đơn nháp.</summary>
    private async Task<Dictionary<string, decimal>> ChoNhapAsync(string maKho, List<string> ma) =>
        (await (from c in db.ChiTietDonMua
                join d in db.DonMua on c.MaDonMua equals d.MaDonMua
                where d.MaKho == maKho && ChuaXong.Contains(d.TrangThai) && ma.Contains(c.MaBienThe)
                select new
                {
                    c.MaBienThe,
                    Dat = c.SoLuongDat * c.HeSo,
                    DaNhan = (from p in db.PhieuNhap
                              where p.MaDonMua == c.MaDonMua && p.TrangThai == TrangThaiPhieu.HoanTat
                              from ct in p.ChiTiet
                              join l in db.Lo on ct.MaLo equals l.MaLo
                              where l.MaBienThe == c.MaBienThe
                              select (decimal?)ct.SoLuongThucNhan).Sum() ?? 0,
                }).ToListAsync())
        .GroupBy(x => x.MaBienThe).ToDictionary(g => g.Key, g => g.Sum(x => Math.Max(0, x.Dat - x.DaNhan)));

    private static DateTimeOffset BatDauNgay(DateOnly ngay)
    {
        var gioDau = ngay.ToDateTime(TimeOnly.MinValue);
        return new DateTimeOffset(gioDau, DinhDang.GioVietNam.GetUtcOffset(gioDau)).ToUniversalTime();
    }
}
