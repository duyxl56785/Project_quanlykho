using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace quanlykho.Data.Migrations
{
    /// <inheritdoc />
    public partial class XepHang : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence(
                name: "MaChungTu_CV");

            migrationBuilder.CreateTable(
                name: "ChuyenViTri",
                columns: table => new
                {
                    MaChuyen = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Loai = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    MaKho = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    MaLo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    MaViTriDi = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    MaViTriDen = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    SoLuong = table.Column<decimal>(type: "numeric(14,3)", precision: 14, scale: 3, nullable: false),
                    ThoiDiem = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    NguoiThucHien = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChuyenViTri", x => x.MaChuyen);
                    table.CheckConstraint("CK_ChuyenViTri_HaiOKhacNhau", "\"MaViTriDi\" <> \"MaViTriDen\"");
                    table.CheckConstraint("CK_ChuyenViTri_Loai", "\"Loai\" IN ('XepHang', 'ChuyenTrongKho')");
                    table.CheckConstraint("CK_ChuyenViTri_SoLuong", "\"SoLuong\" > 0");
                    table.ForeignKey(
                        name: "FK_ChuyenViTri_Lo_MaLo",
                        column: x => x.MaLo,
                        principalTable: "Lo",
                        principalColumn: "MaLo",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChuyenViTri_NguoiDung_NguoiThucHien",
                        column: x => x.NguoiThucHien,
                        principalTable: "NguoiDung",
                        principalColumn: "MaNguoiDung",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChuyenViTri_ViTri_MaKho",
                        column: x => x.MaKho,
                        principalTable: "ViTri",
                        principalColumn: "MaViTri",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChuyenViTri_ViTri_MaViTriDen",
                        column: x => x.MaViTriDen,
                        principalTable: "ViTri",
                        principalColumn: "MaViTri",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChuyenViTri_ViTri_MaViTriDi",
                        column: x => x.MaViTriDi,
                        principalTable: "ViTri",
                        principalColumn: "MaViTri",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChuyenViTri_MaKho_ThoiDiem",
                table: "ChuyenViTri",
                columns: new[] { "MaKho", "ThoiDiem" });

            migrationBuilder.CreateIndex(
                name: "IX_ChuyenViTri_MaLo",
                table: "ChuyenViTri",
                column: "MaLo");

            migrationBuilder.CreateIndex(
                name: "IX_ChuyenViTri_MaViTriDen",
                table: "ChuyenViTri",
                column: "MaViTriDen");

            migrationBuilder.CreateIndex(
                name: "IX_ChuyenViTri_MaViTriDi",
                table: "ChuyenViTri",
                column: "MaViTriDi");

            migrationBuilder.CreateIndex(
                name: "IX_ChuyenViTri_NguoiThucHien",
                table: "ChuyenViTri",
                column: "NguoiThucHien");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChuyenViTri");

            migrationBuilder.DropSequence(
                name: "MaChungTu_CV");
        }
    }
}
