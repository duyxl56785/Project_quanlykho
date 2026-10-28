using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace quanlykho.Data.Migrations
{
    /// <inheritdoc />
    public partial class TonDauKy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MaChungTu",
                table: "TepDaNhap",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MaViTri",
                table: "ChiTietPhieuNhap",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietPhieuNhap_MaViTri",
                table: "ChiTietPhieuNhap",
                column: "MaViTri");

            migrationBuilder.AddForeignKey(
                name: "FK_ChiTietPhieuNhap_ViTri_MaViTri",
                table: "ChiTietPhieuNhap",
                column: "MaViTri",
                principalTable: "ViTri",
                principalColumn: "MaViTri",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChiTietPhieuNhap_ViTri_MaViTri",
                table: "ChiTietPhieuNhap");

            migrationBuilder.DropIndex(
                name: "IX_ChiTietPhieuNhap_MaViTri",
                table: "ChiTietPhieuNhap");

            migrationBuilder.DropColumn(
                name: "MaChungTu",
                table: "TepDaNhap");

            migrationBuilder.DropColumn(
                name: "MaViTri",
                table: "ChiTietPhieuNhap");
        }
    }
}
