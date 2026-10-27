using System.Text.RegularExpressions;
using quanlykho.Features.InNhan;
using ZXing;

namespace quanlykho.Tests.Unit;

public class NhanMaVachTests
{
    /// <summary>Vẽ lại SVG thành ảnh xám (mỗi module 3 điểm ảnh) rồi giải mã bằng ZXing.</summary>
    public static string? GiaiMa(string svg)
    {
        var rong = int.Parse(Regex.Match(svg, @"viewBox=""0 0 (\d+) 1""").Groups[1].Value) * 3;
        const int cao = 30;
        var anh = Enumerable.Repeat((byte)255, rong * cao).ToArray();
        foreach (Match v in Regex.Matches(svg, @"M(\d+) 0h(\d+)"))
            for (var x = int.Parse(v.Groups[1].Value) * 3; x < (int.Parse(v.Groups[1].Value) + int.Parse(v.Groups[2].Value)) * 3; x++)
                for (var y = 0; y < cao; y++)
                    anh[y * rong + x] = 0;
        var doc = new BarcodeReaderGeneric { Options = { PossibleFormats = [BarcodeFormat.CODE_128] } };
        return doc.Decode(new RGBLuminanceSource(anh, rong, cao, RGBLuminanceSource.BitmapFormat.Gray8))?.Text;
    }

    [Theory]
    [InlineData("L000002")]
    [InlineData("K01-TA01-TG01-P02-H01-KE01-O01")]
    [InlineData("SUATUOI-01")]
    public void SvgGiaiMaLaiRaDungMa(string ma)
    {
        var svg = NhanMaVach.Svg(ma);
        Assert.StartsWith("<svg", svg);
        Assert.Equal(ma, GiaiMa(svg));
    }
}
