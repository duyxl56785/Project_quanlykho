using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace quanlykho.Data.Migrations
{
    /// <summary>Quy tắc 14 và 4: nhật ký thao tác và sổ biến động chỉ thêm; CSDL tự chặn sửa, xóa, kể cả câu lệnh SQL chạy tay.</summary>
    public partial class NhatKyChiThem : Migration
    {
        private static readonly string[] Bang = ["NhatKy", "SoBienDong"];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE FUNCTION chi_them() RETURNS trigger LANGUAGE plpgsql AS $$
                BEGIN
                    RAISE EXCEPTION 'Bảng % chỉ được thêm, không được sửa hay xóa.', TG_TABLE_NAME;
                END $$;
                """);
            foreach (var bang in Bang)
            {
                migrationBuilder.Sql($"""CREATE TRIGGER "{bang}_ChiThem" BEFORE UPDATE OR DELETE ON "{bang}" FOR EACH ROW EXECUTE FUNCTION chi_them();""");
                migrationBuilder.Sql($"""CREATE TRIGGER "{bang}_KhongTruncate" BEFORE TRUNCATE ON "{bang}" FOR EACH STATEMENT EXECUTE FUNCTION chi_them();""");
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var bang in Bang)
            {
                migrationBuilder.Sql($"""DROP TRIGGER "{bang}_KhongTruncate" ON "{bang}";""");
                migrationBuilder.Sql($"""DROP TRIGGER "{bang}_ChiThem" ON "{bang}";""");
            }
            migrationBuilder.Sql("DROP FUNCTION chi_them();");
        }
    }
}
