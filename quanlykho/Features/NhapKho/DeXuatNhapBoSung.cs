using static quanlykho.Common.GiaoDien.DinhDang;

namespace quanlykho.Features.NhapKho;

public enum KetLuanG3 { DeXuat, DuChoNhap, ChuaCan, ThieuDuLieu }

/// <summary>Số liệu một biến thể ở một kho, theo đơn vị tính chính.</summary>
/// <param name="TongXuat">Tổng xuất bán (phiếu xuất Hoàn tất) trong 90 ngày gần nhất.</param>
/// <param name="ChoNhap">Phần chưa nhận của đơn mua chưa hoàn thành (kể cả nháp).</param>
public record HangG3(string MaBienThe, decimal KhaDung, DateOnly? NgayXuatDau, decimal TongXuat, decimal ToiThieu, int ThoiGianCho, decimal ChoNhap);

public record KetQuaG3(string MaBienThe, KetLuanG3 KetLuan, int SoNgay, decimal NhuCau, decimal DiemDatHang, decimal SoDeXuat, string LyDo);

/// <summary>
/// Bài toán G3 (mục 1.2.3): d = tổng xuất / N, N là số ngày từ lần xuất đầu tiên (tính cả hôm nay, tối đa 90);
/// đề xuất khi N ≥ 14, d > 0 và A ≤ d × L + m, số lượng ceil(d × (L + T) − A − số chờ nhập). Lớp thuần, toàn bộ decimal.
/// </summary>
public static class DeXuatNhapBoSung
{
    public const int SoNgayLichSuToiThieu = 14;
    public const int SoNgayLichSuToiDa = 90;

    public static KetQuaG3 Tinh(HangG3 h, DateOnly homNay, int soNgayDuHang = 30)
    {
        var n = h.NgayXuatDau is { } dau ? Math.Min(SoNgayLichSuToiDa, homNay.DayNumber - dau.DayNumber + 1) : 0;
        if (n < SoNgayLichSuToiThieu)
            return new(h.MaBienThe, KetLuanG3.ThieuDuLieu, n, 0, 0, 0,
                n == 0 ? "Chưa có lịch sử xuất bán." : $"Mới có {n} ngày lịch sử xuất, cần ít nhất {SoNgayLichSuToiThieu}.");
        if (h.TongXuat <= 0)
            return new(h.MaBienThe, KetLuanG3.ThieuDuLieu, n, 0, 0, 0, $"Không xuất bán trong {n} ngày gần nhất (d = 0).");

        // Nhân trước rồi mới chia cho N để số chia hết không bị lệch khi làm tròn lên.
        var d = h.TongXuat / n;
        var diem = h.TongXuat * h.ThoiGianCho / n + h.ToiThieu;
        var coSo = $"d = {So(h.TongXuat)}/{n} ngày = {So(Math.Round(d, 3))}/ngày; điểm đặt hàng d × {h.ThoiGianCho} + {So(h.ToiThieu)} = {So(Math.Round(diem, 3))}";
        if (h.KhaDung > diem)
            return new(h.MaBienThe, KetLuanG3.ChuaCan, n, d, diem, 0, $"Tồn khả dụng {So(h.KhaDung)} lớn hơn điểm đặt hàng. {coSo}.");

        var so = Math.Ceiling(h.TongXuat * (h.ThoiGianCho + soNgayDuHang) / n - h.KhaDung - h.ChoNhap);
        if (so <= 0)
            return new(h.MaBienThe, KetLuanG3.DuChoNhap, n, d, diem, 0,
                $"Tồn khả dụng {So(h.KhaDung)} không quá điểm đặt hàng nhưng đã có {So(h.ChoNhap)} đang chờ nhập, đủ dùng {h.ThoiGianCho + soNgayDuHang} ngày. {coSo}.");
        return new(h.MaBienThe, KetLuanG3.DeXuat, n, d, diem, so,
            $"Tồn khả dụng {So(h.KhaDung)} không quá điểm đặt hàng. {coSo}. Nhập đủ dùng {h.ThoiGianCho} + {soNgayDuHang} ngày, trừ tồn và {So(h.ChoNhap)} đang chờ nhập.");
    }
}
