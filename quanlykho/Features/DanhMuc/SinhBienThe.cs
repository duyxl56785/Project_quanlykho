namespace quanlykho.Features.DanhMuc;

public static class SinhBienThe
{
    /// <summary>Mọi tổ hợp giá trị; thuộc tính không có giá trị nào thì không dùng. Không thuộc tính nào: một tổ hợp rỗng.</summary>
    public static List<Dictionary<string, string>> ToHop(IEnumerable<(string Ten, IEnumerable<string> GiaTri)> thuocTinh)
    {
        IEnumerable<Dictionary<string, string>> ketQua = [new()];
        foreach (var (ten, giaTri) in thuocTinh)
        {
            var cacGiaTri = giaTri.Select(g => g.Trim()).Where(g => g.Length > 0).Distinct().ToList();
            if (cacGiaTri.Count == 0) continue;
            ketQua = ketQua.SelectMany(d => cacGiaTri.Select(g => new Dictionary<string, string>(d) { [ten.Trim()] = g })).ToList();
        }
        return ketQua.ToList();
    }

    public static bool CungToHop(IReadOnlyDictionary<string, string> a, IReadOnlyDictionary<string, string> b) =>
        a.Count == b.Count && a.All(kv => b.TryGetValue(kv.Key, out var v) && v == kv.Value);

    public static string MoTa(IReadOnlyDictionary<string, string> thuocTinh) =>
        thuocTinh.Count == 0 ? "Không có thuộc tính" : string.Join(", ", thuocTinh.Select(kv => $"{kv.Key}: {kv.Value}"));
}
