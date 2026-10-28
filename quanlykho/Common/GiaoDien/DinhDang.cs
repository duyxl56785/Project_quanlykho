using System.Globalization;

namespace quanlykho.Common.GiaoDien;

/// <summary>
/// Hiển thị số, tiền, ngày theo kiểu Việt Nam. Văn hóa request giữ InvariantCulture nên mọi chỗ hiển thị phải qua đây.
/// Dùng trong view: @So(x), @Tien(x), @Ngay(x) (đã import tĩnh trong _ViewImports).
/// </summary>
public static class DinhDang
{
    public static readonly CultureInfo VanHoa = CultureInfo.GetCultureInfo("vi-VN");
    public static readonly TimeZoneInfo GioVietNam = TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");

    /// <summary>1234.5 → "1.234,5"; bỏ số 0 thừa sau dấu phẩy (tối đa 3 chữ số thập phân).</summary>
    public static string So(decimal? so) => so?.ToString("#,##0.###", VanHoa) ?? "";

    /// <summary>1234500 → "1.234.500 ₫".</summary>
    public static string Tien(decimal? so) => so is null ? "" : so.Value.ToString("#,##0.##", VanHoa) + " ₫";

    public static string Ngay(DateOnly? ngay) => ngay?.ToString("dd/MM/yyyy", VanHoa) ?? "";

    /// <summary>Thời điểm lưu UTC, hiển thị theo giờ Việt Nam.</summary>
    public static string Ngay(DateTimeOffset? thoiDiem) =>
        thoiDiem is null ? "" : TimeZoneInfo.ConvertTime(thoiDiem.Value, GioVietNam).ToString("dd/MM/yyyy", VanHoa);

    public static string NgayGio(DateTimeOffset? thoiDiem) =>
        thoiDiem is null ? "" : TimeZoneInfo.ConvertTime(thoiDiem.Value, GioVietNam).ToString("dd/MM/yyyy HH:mm", VanHoa);

    /// <summary>Đầu ngày (00:00 giờ Việt Nam) dưới dạng UTC, để lọc cột timestamptz theo ngày.</summary>
    public static DateTimeOffset BatDauNgay(DateOnly ngay)
    {
        var gioDau = ngay.ToDateTime(TimeOnly.MinValue);
        return new DateTimeOffset(gioDau, GioVietNam.GetUtcOffset(gioDau)).ToUniversalTime();
    }
}
