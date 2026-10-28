using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace quanlykho.Data.Migrations
{
    /// <inheritdoc />
    public partial class DonDatHang : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DonDatHang",
                columns: table => new
                {
                    MaDonDat = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    MaKho = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    MaNoiNhan = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    NguoiTao = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    NgayTao = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    HanGiao = table.Column<DateOnly>(type: "date", nullable: true),
                    TrangThai = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    GhiChu = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DonDatHang", x => x.MaDonDat);
                    table.CheckConstraint("CK_DonDatHang_TrangThai", "\"TrangThai\" IN ('Nhap', 'ChoDuyet', 'DaDuyet', 'DangXuat', 'HoanThanh', 'DaHuy')");
                    table.ForeignKey(
                        name: "FK_DonDatHang_NguoiDung_NguoiTao",
                        column: x => x.NguoiTao,
                        principalTable: "NguoiDung",
                        principalColumn: "MaNguoiDung",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DonDatHang_NoiNhan_MaNoiNhan",
                        column: x => x.MaNoiNhan,
                        principalTable: "NoiNhan",
                        principalColumn: "MaNoiNhan",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DonDatHang_ViTri_MaKho",
                        column: x => x.MaKho,
                        principalTable: "ViTri",
                        principalColumn: "MaViTri",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ChiTietDonDat",
                columns: table => new
                {
                    MaDonDat = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    MaBienThe = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TenDonVi = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    HeSo = table.Column<int>(type: "integer", nullable: false),
                    SoLuongDat = table.Column<decimal>(type: "numeric(14,3)", precision: 14, scale: 3, nullable: false),
                    SoLuongDaXuat = table.Column<decimal>(type: "numeric(14,3)", precision: 14, scale: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChiTietDonDat", x => new { x.MaDonDat, x.MaBienThe });
                    table.CheckConstraint("CK_ChiTietDonDat_HeSo", "\"HeSo\" >= 1");
                    table.CheckConstraint("CK_ChiTietDonDat_SoLuongDat", "\"SoLuongDat\" > 0");
                    table.CheckConstraint("CK_ChiTietDonDat_SoLuongDaXuat", "\"SoLuongDaXuat\" >= 0 AND \"SoLuongDaXuat\" <= \"SoLuongDat\" * \"HeSo\"");
                    table.ForeignKey(
                        name: "FK_ChiTietDonDat_BienThe_MaBienThe",
                        column: x => x.MaBienThe,
                        principalTable: "BienThe",
                        principalColumn: "MaBienThe",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChiTietDonDat_DonDatHang_MaDonDat",
                        column: x => x.MaDonDat,
                        principalTable: "DonDatHang",
                        principalColumn: "MaDonDat",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietDonDat_MaBienThe",
                table: "ChiTietDonDat",
                column: "MaBienThe");

            migrationBuilder.CreateIndex(
                name: "IX_DonDatHang_MaKho_TrangThai",
                table: "DonDatHang",
                columns: new[] { "MaKho", "TrangThai" });

            migrationBuilder.CreateIndex(
                name: "IX_DonDatHang_MaNoiNhan",
                table: "DonDatHang",
                column: "MaNoiNhan");

            migrationBuilder.CreateIndex(
                name: "IX_DonDatHang_NguoiTao",
                table: "DonDatHang",
                column: "NguoiTao");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChiTietDonDat");

            migrationBuilder.DropTable(
                name: "DonDatHang");
        }
    }
}
