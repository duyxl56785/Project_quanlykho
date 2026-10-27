using Microsoft.EntityFrameworkCore;
using quanlykho.Data;

namespace quanlykho.Common;

/// <summary>Sinh mã chứng từ dạng tiền tố + 6 chữ số (PN000001) từ sequence PostgreSQL của từng tiền tố.</summary>
public class MaChungTu(AppDbContext db)
{
    /// <summary>PN phiếu nhập, PX phiếu xuất, DM đơn mua, DD đơn đặt hàng, LX lệnh xuất, DC điều chuyển, KK kiểm kê, L lô.</summary>
    public static readonly string[] TienTo = ["PN", "PX", "DM", "DD", "LX", "DC", "KK", "L"];

    public static string TenSequence(string tienTo) => $"MaChungTu_{tienTo}";

    public async Task<string> TaoAsync(string tienTo)
    {
        if (!TienTo.Contains(tienTo))
            throw new ArgumentException($"Tiền tố mã chứng từ không hợp lệ: {tienTo}", nameof(tienTo));

        var so = await db.Database
            .SqlQueryRaw<long>($"SELECT nextval('\"{TenSequence(tienTo)}\"') AS \"Value\"")
            .SingleAsync();
        return $"{tienTo}{so:D6}";
    }
}
