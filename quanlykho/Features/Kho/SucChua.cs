namespace quanlykho.Features.Kho;

/// <summary>Ba chiều (m) và khối lượng (kg) của một đơn vị tính chính.</summary>
public record DonViHang(decimal Dai, decimal Rong, decimal Cao, decimal KhoiLuong)
{
    public decimal TheTich => Dai * Rong * Cao;
}

/// <summary>Hàng đang nằm trong ô: biến thể, kích thước một đơn vị, số lượng.</summary>
public record HangTrongO(string MaBienThe, DonViHang DonVi, decimal SoLuong);

public record OChua(decimal Dai, decimal Rong, decimal Cao, decimal TaiTrongToiDa)
{
    public decimal TheTich => Dai * Rong * Cao;
}

public record KetQuaSucChua(bool VuaKichThuoc, decimal SoTheoKhongGian, decimal SoTheoTaiTrong, decimal QMax, decimal TheTichConLai, decimal TaiTrongConLai);

/// <summary>Kiểm tra sức chứa khi xếp một biến thể vào ô (Bảng 2.33, mục 2.16.3). Lớp thuần, toàn bộ decimal.</summary>
public static class SucChua
{
    public static bool VuaKichThuoc(OChua o, DonViHang h)
    {
        decimal[] a = [o.Dai, o.Rong, o.Cao], b = [h.Dai, h.Rong, h.Cao];
        Array.Sort(a);
        Array.Sort(b);
        return b[0] <= a[0] && b[1] <= a[1] && b[2] <= a[2];
    }

    /// <summary>Số đơn vị xếp được theo lưới trong ô trống, thử 6 cách xoay.</summary>
    public static decimal SoTheoLuoi(OChua o, DonViHang h)
    {
        decimal[][] xoay =
        [
            [h.Dai, h.Rong, h.Cao], [h.Dai, h.Cao, h.Rong], [h.Rong, h.Dai, h.Cao],
            [h.Rong, h.Cao, h.Dai], [h.Cao, h.Dai, h.Rong], [h.Cao, h.Rong, h.Dai],
        ];
        return xoay.Max(x => Math.Floor(o.Dai / x[0]) * Math.Floor(o.Rong / x[1]) * Math.Floor(o.Cao / x[2]));
    }

    public static KetQuaSucChua Tinh(OChua o, decimal heSoLapDay, string maBienThe, DonViHang h, IReadOnlyCollection<HangTrongO> dangCo)
    {
        dangCo = dangCo.Where(x => x.SoLuong > 0).ToList();
        var theTichDung = dangCo.Sum(x => x.DonVi.TheTich * x.SoLuong);
        var taiTrongConLai = o.TaiTrongToiDa - dangCo.Sum(x => x.DonVi.KhoiLuong * x.SoLuong);
        var vua = VuaKichThuoc(o, h);

        var khongGian = !vua ? 0
            : dangCo.All(x => x.MaBienThe == maBienThe)
                ? SoTheoLuoi(o, h) - dangCo.Sum(x => x.SoLuong)
                : Math.Floor((o.TheTich * heSoLapDay - theTichDung) / h.TheTich);
        var taiTrong = Math.Floor(taiTrongConLai / h.KhoiLuong);
        return new KetQuaSucChua(vua, Math.Max(0, khongGian), Math.Max(0, taiTrong), Math.Max(0, Math.Floor(Math.Min(khongGian, taiTrong))),
            Math.Max(0, o.TheTich - theTichDung), Math.Max(0, taiTrongConLai));
    }
}
