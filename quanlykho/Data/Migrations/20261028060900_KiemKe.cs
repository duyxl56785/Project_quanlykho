using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace quanlykho.Data.Migrations
{
    /// <inheritdoc />
    public partial class KiemKe : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PhieuKiemKe",
                columns: table => new
                {
                    MaPhieu = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    MaKho = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    PhamVi = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    DoiTuong = table.Column<List<string>>(type: "text[]", nullable: false),
                    MucDich = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    NgayKiem = table.Column<DateOnly>(type: "date", nullable: false),
                    NguoiLap = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    NgayLap = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ThoiDiemBatDau = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ThoiDiemHoanThanh = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    TrangThai = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhieuKiemKe", x => x.MaPhieu);
                    table.CheckConstraint("CK_PhieuKiemKe_PhamVi", "\"PhamVi\" IN ('ToanKho', 'ViTri', 'Lo')");
                    table.CheckConstraint("CK_PhieuKiemKe_TrangThai", "\"TrangThai\" IN ('ChuaThucHien', 'DangKiemKe', 'HoanThanh', 'DaHuy')");
                    table.ForeignKey(
                        name: "FK_PhieuKiemKe_NguoiDung_NguoiLap",
                        column: x => x.NguoiLap,
                        principalTable: "NguoiDung",
                        principalColumn: "MaNguoiDung",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PhieuKiemKe_ViTri_MaKho",
                        column: x => x.MaKho,
                        principalTable: "ViTri",
                        principalColumn: "MaViTri",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ChiTietKiemKe",
                columns: table => new
                {
                    MaPhieu = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    STT = table.Column<int>(type: "integer", nullable: false),
                    MaViTri = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    MaLo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    MaBienThe = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SoLuongSoSach = table.Column<decimal>(type: "numeric(14,3)", precision: 14, scale: 3, nullable: false),
                    SoLuongThucTe = table.Column<decimal>(type: "numeric(14,3)", precision: 14, scale: 3, nullable: true),
                    NgoaiDanhSach = table.Column<bool>(type: "boolean", nullable: false),
                    NguoiDem = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    ThoiDiemDem = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChiTietKiemKe", x => new { x.MaPhieu, x.STT });
                    table.CheckConstraint("CK_ChiTietKiemKe_SoLuong", "\"SoLuongSoSach\" >= 0 AND (\"SoLuongThucTe\" IS NULL OR \"SoLuongThucTe\" >= 0)");
                    table.ForeignKey(
                        name: "FK_ChiTietKiemKe_BienThe_MaBienThe",
                        column: x => x.MaBienThe,
                        principalTable: "BienThe",
                        principalColumn: "MaBienThe",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChiTietKiemKe_Lo_MaLo",
                        column: x => x.MaLo,
                        principalTable: "Lo",
                        principalColumn: "MaLo",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChiTietKiemKe_NguoiDung_NguoiDem",
                        column: x => x.NguoiDem,
                        principalTable: "NguoiDung",
                        principalColumn: "MaNguoiDung",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChiTietKiemKe_PhieuKiemKe_MaPhieu",
                        column: x => x.MaPhieu,
                        principalTable: "PhieuKiemKe",
                        principalColumn: "MaPhieu",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChiTietKiemKe_ViTri_MaViTri",
                        column: x => x.MaViTri,
                        principalTable: "ViTri",
                        principalColumn: "MaViTri",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ThanhVienKiemKe",
                columns: table => new
                {
                    MaPhieu = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    MaNguoiDung = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ThanhVienKiemKe", x => new { x.MaPhieu, x.MaNguoiDung });
                    table.ForeignKey(
                        name: "FK_ThanhVienKiemKe_NguoiDung_MaNguoiDung",
                        column: x => x.MaNguoiDung,
                        principalTable: "NguoiDung",
                        principalColumn: "MaNguoiDung",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ThanhVienKiemKe_PhieuKiemKe_MaPhieu",
                        column: x => x.MaPhieu,
                        principalTable: "PhieuKiemKe",
                        principalColumn: "MaPhieu",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietKiemKe_MaBienThe",
                table: "ChiTietKiemKe",
                column: "MaBienThe");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietKiemKe_MaLo",
                table: "ChiTietKiemKe",
                column: "MaLo");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietKiemKe_MaPhieu_MaViTri_MaLo_MaBienThe",
                table: "ChiTietKiemKe",
                columns: new[] { "MaPhieu", "MaViTri", "MaLo", "MaBienThe" },
                unique: true)
                .Annotation("Npgsql:NullsDistinct", false);

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietKiemKe_MaViTri",
                table: "ChiTietKiemKe",
                column: "MaViTri");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietKiemKe_NguoiDem",
                table: "ChiTietKiemKe",
                column: "NguoiDem");

            migrationBuilder.CreateIndex(
                name: "IX_PhieuKiemKe_MaKho_TrangThai",
                table: "PhieuKiemKe",
                columns: new[] { "MaKho", "TrangThai" });

            migrationBuilder.CreateIndex(
                name: "IX_PhieuKiemKe_NguoiLap",
                table: "PhieuKiemKe",
                column: "NguoiLap");

            migrationBuilder.CreateIndex(
                name: "IX_ThanhVienKiemKe_MaNguoiDung",
                table: "ThanhVienKiemKe",
                column: "MaNguoiDung");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChiTietKiemKe");

            migrationBuilder.DropTable(
                name: "ThanhVienKiemKe");

            migrationBuilder.DropTable(
                name: "PhieuKiemKe");
        }
    }
}
