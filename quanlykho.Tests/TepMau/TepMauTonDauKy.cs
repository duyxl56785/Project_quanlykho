using ClosedXML.Excel;
using quanlykho.Features.NhapExcel;

namespace quanlykho.Tests.TepMau;

/// <summary>
/// Tệp thử tồn đầu kỳ kho K01, 300 dòng: áo AOTHUN-01..08 mỗi dòng 2 cái rải đều 12 ô lưu trữ thường (KE01, KE02),
/// cứ 10 dòng có 1 dòng sữa 3 hộp ở 4 ô lạnh (có hạn dùng). Bản vượt sức chứa đổi dòng VuotSucChua thành 50.000 cái (quá tải trọng ô).
/// </summary>
public static class TepMauTonDauKy
{
    public const int VuotSucChua = 151;

    public static string O(int i) => i % 10 == 0
        ? $"K01-TA01-TG01-P03-H01-KE01-O{i / 10 % 4 + 1:D2}"
        : $"K01-TA01-TG01-P02-H01-KE{i % 12 / 6 + 1:D2}-O{i % 6 + 1:D2}";

    public static byte[] Tao(bool vuotSucChua = false, string tienToLo = "LTD")
    {
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet(NhapTonDauKyService.Sheet);
        var cot = NhapTonDauKyService.CotTon;
        for (var c = 0; c < cot.Length; c++)
            ws.Cell(1, c + 1).Value = cot[c].TieuDe + (cot[c].BatBuoc ? "*" : "");
        for (var i = 0; i < 300; i++)
        {
            var r = i + 2;
            var sua = i % 10 == 0;
            string[] o = sua
                ? ["SUATUOI-01", $"{tienToLo}{i:D3}", "2027-06-30", O(i), "3", "", "Đạt", "8000"]
                : [$"AOTHUN-{i % 8 + 1:D2}", $"{tienToLo}{i:D3}", "", O(i), r == VuotSucChua && vuotSucChua ? "50000" : "2", "", "", "45000"];
            for (var c = 0; c < o.Length; c++)
                ws.Cell(r, c + 1).Value = o[c];
        }
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }
}
