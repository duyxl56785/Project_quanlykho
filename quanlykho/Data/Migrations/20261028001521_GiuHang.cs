using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace quanlykho.Data.Migrations
{
    /// <inheritdoc />
    public partial class GiuHang : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GiuHang",
                columns: table => new
                {
                    MaDonDat = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    MaLo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    MaViTri = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    SoLuong = table.Column<decimal>(type: "numeric(14,3)", precision: 14, scale: 3, nullable: false),
                    ThuTu = table.Column<int>(type: "integer", nullable: false),
                    MaGoiY = table.Column<long>(type: "bigint", nullable: true),
                    ThoiDiem = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    NguoiGiu = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GiuHang", x => new { x.MaDonDat, x.MaLo, x.MaViTri });
                    table.CheckConstraint("CK_GiuHang_SoLuong", "\"SoLuong\" > 0");
                    table.ForeignKey(
                        name: "FK_GiuHang_DonDatHang_MaDonDat",
                        column: x => x.MaDonDat,
                        principalTable: "DonDatHang",
                        principalColumn: "MaDonDat",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GiuHang_LichSuGoiY_MaGoiY",
                        column: x => x.MaGoiY,
                        principalTable: "LichSuGoiY",
                        principalColumn: "MaGoiY",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GiuHang_Lo_MaLo",
                        column: x => x.MaLo,
                        principalTable: "Lo",
                        principalColumn: "MaLo",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GiuHang_NguoiDung_NguoiGiu",
                        column: x => x.NguoiGiu,
                        principalTable: "NguoiDung",
                        principalColumn: "MaNguoiDung",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GiuHang_ViTri_MaViTri",
                        column: x => x.MaViTri,
                        principalTable: "ViTri",
                        principalColumn: "MaViTri",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GiuHang_MaGoiY",
                table: "GiuHang",
                column: "MaGoiY");

            migrationBuilder.CreateIndex(
                name: "IX_GiuHang_MaLo_MaViTri",
                table: "GiuHang",
                columns: new[] { "MaLo", "MaViTri" });

            migrationBuilder.CreateIndex(
                name: "IX_GiuHang_MaViTri",
                table: "GiuHang",
                column: "MaViTri");

            migrationBuilder.CreateIndex(
                name: "IX_GiuHang_NguoiGiu",
                table: "GiuHang",
                column: "NguoiGiu");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GiuHang");
        }
    }
}
