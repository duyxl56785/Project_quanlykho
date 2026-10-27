using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace quanlykho.Data.Configurations;

/// <summary>Kiểu và ràng buộc dùng chung theo Bảng 2.34.</summary>
public static class CauHinhChung
{
    public const int Ma = 20;
    public const int MaViTri = 40;
    public const int Ten = 200;
    public const int MaVach = 64;

    public static PropertyBuilder<T> KichThuoc<T>(this PropertyBuilder<T> p) => p.HasPrecision(8, 3);
    public static PropertyBuilder<T> KhoiLuong<T>(this PropertyBuilder<T> p) => p.HasPrecision(10, 3);
    public static PropertyBuilder<T> SoLuong<T>(this PropertyBuilder<T> p) => p.HasPrecision(14, 3);
    public static PropertyBuilder<T> Tien<T>(this PropertyBuilder<T> p) => p.HasPrecision(18, 2);

    /// <summary>CHECK cột enum chỉ nhận tên các giá trị của TEnum (cột null luôn qua).</summary>
    public static void ChiNhan<TEnum>(this TableBuilder t, string cot) where TEnum : struct, Enum =>
        t.HasCheckConstraint($"CK_{TenBang(t)}_{cot}",
            $"\"{cot}\" IN ({string.Join(", ", Enum.GetNames<TEnum>().Select(n => $"'{n}'"))})");

    public static void Check(this TableBuilder t, string ten, string sql) =>
        t.HasCheckConstraint($"CK_{TenBang(t)}_{ten}", sql);

    // Trong ToTable(t => ...) không đặt tên bảng, t.Name rỗng; tên bảng trùng tên entity.
    private static string TenBang(TableBuilder t) => t.Metadata.GetTableName() ?? t.Metadata.ClrType.Name;
}
