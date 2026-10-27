using System.Net;
using System.Text;
using ZXing;
using ZXing.OneD;

namespace quanlykho.Features.InNhan;

/// <summary>Sinh mã vạch Code 128 dạng SVG (KE_HOACH A13): mỗi vạch đen là một hình chữ nhật, giãn theo khổ nhãn khi in.</summary>
public static class NhanMaVach
{
    public static string Svg(string ma)
    {
        var m = new Code128Writer().encode(ma, BarcodeFormat.CODE_128, 0, 1);
        var d = new StringBuilder();
        for (var x = 0; x < m.Width; x++)
        {
            if (!m[x, 0])
                continue;
            var rong = 1;
            while (x + rong < m.Width && m[x + rong, 0])
                rong++;
            d.Append($"M{x} 0h{rong}v1h-{rong}z");
            x += rong;
        }
        return $"""<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {m.Width} 1" preserveAspectRatio="none" shape-rendering="crispEdges" role="img" aria-label="{WebUtility.HtmlEncode(ma)}"><path d="{d}"/></svg>""";
    }
}

public record NhanIn(string Ma, string Dong1, string? Dong2 = null);

public record TrangNhan(string TieuDe, string KhoNhan, string QuayLai, List<NhanIn> Nhan)
{
    public static readonly string[] CacKho = ["50x30", "70x40", "100x50"];
    public static string ChonKho(string? kho) => CacKho.Contains(kho) ? kho! : CacKho[0];
}
