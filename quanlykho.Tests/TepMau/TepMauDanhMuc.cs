using ClosedXML.Excel;
using quanlykho.Features.NhapExcel;

namespace quanlykho.Tests.TepMau;

/// <summary>
/// Tệp thử nhập danh mục 300 dòng: 40 sản phẩm (nhóm Thời trang), 240 biến thể (Size S, M, L × Màu Trắng, Đen), 20 nhà cung cấp.
/// Bản có lỗi chứa đúng 5 dòng lỗi ở DongLoi. Ghi ra tệp .xlsx: đặt GHI_TEP_MAU=&lt;thư mục&gt; rồi dotnet test --filter TepMau_GhiRaDia.
/// </summary>
public static class TepMauDanhMuc
{
    public static readonly (string Sheet, int Dong)[] DongLoi =
        [("SanPham", 6), ("SanPham", 11), ("BienThe", 51), ("BienThe", 101), ("NhaCungCap", 3)];

    public static byte[] Tao(bool coLoi)
    {
        using var wb = new XLWorkbook();
        var sp = Sheet(wb, NhapExcelService.SheetSanPham, NhapExcelService.CotSanPham);
        var bt = Sheet(wb, NhapExcelService.SheetBienThe, NhapExcelService.CotBienThe);
        var ncc = Sheet(wb, NhapExcelService.SheetNhaCungCap, NhapExcelService.CotNhaCungCap);

        var dongBt = 2;
        for (var i = 1; i <= 40; i++)
        {
            var ma = $"SPX{i:D3}";
            Ghi(sp, i + 1, ma, $"Áo thun nhập Excel {i}", "cái", "TT", "Thường", "Không", "");
            var k = 0;
            foreach (var size in new[] { "S", "M", "L" })
                foreach (var mau in new[] { "Trắng", "Đen" })
                {
                    Ghi(bt, dongBt, $"{ma}-{++k:D2}", ma, $"Size={size}; Màu={mau}", "0.3", "0.25", "0.05", "0.2", $"2990000{dongBt:D6}");
                    dongBt++;
                }
        }
        for (var i = 1; i <= 20; i++)
            Ghi(ncc, i + 1, $"NCCX{i:D2}", $"Nhà cung cấp nhập Excel {i}", $"Số {i} Hà Nội", "Nguyễn Văn A", $"0900000{i:D3}");

        if (coLoi)
        {
            sp.Cell(6, 4).Value = "KHONGCO";
            sp.Cell(11, 3).Value = "";
            bt.Cell(51, 7).Value = "abc";
            bt.Cell(101, 1).Value = bt.Cell(100, 1).Value;
            ncc.Cell(3, 1).Value = "NCC01";
        }
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    private static IXLWorksheet Sheet(XLWorkbook wb, string ten, Cot[] cot)
    {
        var ws = wb.AddWorksheet(ten);
        for (var i = 0; i < cot.Length; i++)
            ws.Cell(1, i + 1).Value = cot[i].TieuDe + (cot[i].BatBuoc ? "*" : "");
        return ws;
    }

    private static void Ghi(IXLWorksheet ws, int dong, params string[] o)
    {
        for (var i = 0; i < o.Length; i++)
            ws.Cell(dong, i + 1).Value = o[i];
    }
}
