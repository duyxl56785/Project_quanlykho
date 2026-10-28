using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace quanlykho.Common.GiaoDien;

/// <summary>
/// &lt;bang-du-lieu trang="Model" thong-bao-rong="..."&gt; &lt;thead&gt;...&lt;/thead&gt;&lt;tbody&gt;...&lt;/tbody&gt; &lt;/bang-du-lieu&gt;
/// Bọc bảng chuẩn của hệ thống: trạng thái rỗng khi không có dòng, phân trang giữ nguyên các tham số lọc trên URL.
/// xep-dong="true": trên điện thoại mỗi dòng thành một thẻ, ô có data-nhan hiện kèm nhãn cột (CSS .bang-xep-dong).
/// </summary>
[HtmlTargetElement("bang-du-lieu")]
public class BangDuLieuTagHelper(HtmlEncoder encoder) : TagHelper
{
    public ITrangDuLieu Trang { get; set; } = null!;
    public string ThongBaoRong { get; set; } = "Chưa có dữ liệu.";
    public bool XepDong { get; set; }

    [ViewContext, HtmlAttributeNotBound]
    public ViewContext ViewContext { get; set; } = null!;

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = "div";
        output.Attributes.SetAttribute("class", XepDong ? "bang-du-lieu bang-xep-dong" : "bang-du-lieu");

        if (Trang.TongSo == 0)
        {
            output.Content.SetHtmlContent($"<div class=\"trang-rong\"><p class=\"mb-0\">{encoder.Encode(ThongBaoRong)}</p></div>");
            return;
        }

        var noiDung = (await output.GetChildContentAsync()).GetContent();
        output.Content.AppendHtml($"<div class=\"table-responsive\"><table class=\"table table-hover align-middle mb-0\">{noiDung}</table></div>");
        output.Content.AppendHtml(PhanTrang());
    }

    private string PhanTrang()
    {
        var tu = (Trang.Trang - 1) * Trang.KichThuocTrang + 1;
        var den = Math.Min(Trang.Trang * Trang.KichThuocTrang, Trang.TongSo);
        var html = $"<div class=\"phan-trang\"><span class=\"text-secondary\">{tu}–{den} / {DinhDang.So(Trang.TongSo)} dòng</span>";
        if (Trang.TongTrang > 1)
        {
            html += "<nav aria-label=\"Phân trang\"><ul class=\"pagination mb-0\">";
            html += Muc("‹", Trang.Trang - 1, Trang.Trang == 1, false, "Trang trước");
            foreach (var so in CacSoTrang())
                html += so is null
                    ? "<li class=\"page-item disabled\"><span class=\"page-link\">…</span></li>"
                    : Muc(so.Value.ToString(), so.Value, false, so == Trang.Trang, $"Trang {so}");
            html += Muc("›", Trang.Trang + 1, Trang.Trang == Trang.TongTrang, false, "Trang sau");
            html += "</ul></nav>";
        }
        return html + "</div>";
    }

    /// <summary>Trang đầu, trang cuối và 2 trang quanh trang hiện tại; null là dấu "…".</summary>
    private IEnumerable<int?> CacSoTrang()
    {
        int? truoc = null;
        for (var so = 1; so <= Trang.TongTrang; so++)
        {
            if (so != 1 && so != Trang.TongTrang && Math.Abs(so - Trang.Trang) > 2) continue;
            if (truoc is not null && so - truoc > 1) yield return null;
            yield return truoc = so;
        }
    }

    private string Muc(string chu, int trang, bool tat, bool hienTai, string nhan)
    {
        var lop = "page-item" + (tat ? " disabled" : "") + (hienTai ? " active" : "");
        var hien = hienTai ? " aria-current=\"page\"" : "";
        return $"<li class=\"{lop}\"><a class=\"page-link\" href=\"{encoder.Encode(Url(trang))}\" aria-label=\"{nhan}\"{hien}>{chu}</a></li>";
    }

    private string Url(int trang)
    {
        var request = ViewContext.HttpContext.Request;
        var thamSo = request.Query.Where(q => q.Key != "trang").SelectMany(q => q.Value.Select(v => (q.Key, v)))
            .Append(("trang", trang.ToString()))
            .Select(p => $"{Uri.EscapeDataString(p.Item1)}={Uri.EscapeDataString(p.Item2 ?? "")}");
        return $"{request.PathBase}{request.Path}?{string.Join("&", thamSo)}";
    }
}
