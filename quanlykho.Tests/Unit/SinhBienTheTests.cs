using quanlykho.Features.DanhMuc;

namespace quanlykho.Tests.Unit;

public class SinhBienTheTests
{
    [Fact]
    public void HaiThuocTinh_TaoMoiToHop()
    {
        var toHop = SinhBienThe.ToHop([("Size", ["S", "M"]), ("Màu", ["Đen", "Trắng", "Đỏ"])]);

        Assert.Equal(6, toHop.Count);
        Assert.Contains(toHop, t => t["Size"] == "M" && t["Màu"] == "Đỏ");
        Assert.Equal(6, toHop.Select(t => $"{t["Size"]}|{t["Màu"]}").Distinct().Count());
    }

    [Fact]
    public void KhongThuocTinh_MotToHopRong() => Assert.Equal([new Dictionary<string, string>()], SinhBienThe.ToHop([]));

    [Fact]
    public void ThuocTinhKhongCoGiaTri_BoQua_GiaTriTrungVaKhoangTrang_Loai()
    {
        var toHop = SinhBienThe.ToHop([("Size", [" S", "S ", "", "M"]), ("Màu", [])]);

        Assert.Equal(2, toHop.Count);
        Assert.All(toHop, t => Assert.Equal(["Size"], t.Keys));
    }

    [Fact]
    public void CungToHop_KhongPhuThuocThuTuKhoa()
    {
        Assert.True(SinhBienThe.CungToHop(new Dictionary<string, string> { ["A"] = "1", ["B"] = "2" }, new Dictionary<string, string> { ["B"] = "2", ["A"] = "1" }));
        Assert.False(SinhBienThe.CungToHop(new Dictionary<string, string> { ["A"] = "1" }, new Dictionary<string, string> { ["A"] = "1", ["B"] = "2" }));
    }
}
