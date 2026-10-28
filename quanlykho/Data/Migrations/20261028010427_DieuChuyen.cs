using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace quanlykho.Data.Migrations
{
    /// <inheritdoc />
    public partial class DieuChuyen : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PhieuDieuChuyen",
                columns: table => new
                {
                    MaPhieu = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    MaKhoDi = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    MaKhoDen = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    NgayChuyen = table.Column<DateOnly>(type: "date", nullable: false),
                    PhuongTien = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    NguoiVanChuyen = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    LyDo = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    NguoiLap = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    NgayLap = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ThoiDiemXuat = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    NguoiNhan = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    ThoiDiemNhan = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    TrangThai = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhieuDieuChuyen", x => x.MaPhieu);
                    table.CheckConstraint("CK_PhieuDieuChuyen_HaiKhoKhacNhau", "\"MaKhoDi\" <> \"MaKhoDen\"");
                    table.CheckConstraint("CK_PhieuDieuChuyen_TrangThai", "\"TrangThai\" IN ('LuuTam', 'ChoNhapKho', 'NhanMotPhan', 'HoanThanh', 'DaHuy')");
                    table.ForeignKey(
                        name: "FK_PhieuDieuChuyen_NguoiDung_NguoiLap",
                        column: x => x.NguoiLap,
                        principalTable: "NguoiDung",
                        principalColumn: "MaNguoiDung",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PhieuDieuChuyen_NguoiDung_NguoiNhan",
                        column: x => x.NguoiNhan,
                        principalTable: "NguoiDung",
                        principalColumn: "MaNguoiDung",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PhieuDieuChuyen_ViTri_MaKhoDen",
                        column: x => x.MaKhoDen,
                        principalTable: "ViTri",
                        principalColumn: "MaViTri",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PhieuDieuChuyen_ViTri_MaKhoDi",
                        column: x => x.MaKhoDi,
                        principalTable: "ViTri",
                        principalColumn: "MaViTri",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ChiTietDieuChuyen",
                columns: table => new
                {
                    MaPhieu = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    STT = table.Column<int>(type: "integer", nullable: false),
                    MaLo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    MaViTriDi = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    TenDonVi = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    HeSo = table.Column<int>(type: "integer", nullable: false),
                    SoLuongGoc = table.Column<decimal>(type: "numeric(14,3)", precision: 14, scale: 3, nullable: false),
                    SoLuong = table.Column<decimal>(type: "numeric(14,3)", precision: 14, scale: 3, nullable: false),
                    SoLuongNhan = table.Column<decimal>(type: "numeric(14,3)", precision: 14, scale: 3, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChiTietDieuChuyen", x => new { x.MaPhieu, x.STT });
                    table.CheckConstraint("CK_ChiTietDieuChuyen_HeSo", "\"HeSo\" >= 1");
                    table.CheckConstraint("CK_ChiTietDieuChuyen_SoLuong", "\"SoLuong\" > 0 AND \"SoLuong\" = \"SoLuongGoc\" * \"HeSo\"");
                    table.CheckConstraint("CK_ChiTietDieuChuyen_SoLuongNhan", "\"SoLuongNhan\" IS NULL OR (\"SoLuongNhan\" >= 0 AND \"SoLuongNhan\" <= \"SoLuong\")");
                    table.ForeignKey(
                        name: "FK_ChiTietDieuChuyen_Lo_MaLo",
                        column: x => x.MaLo,
                        principalTable: "Lo",
                        principalColumn: "MaLo",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChiTietDieuChuyen_PhieuDieuChuyen_MaPhieu",
                        column: x => x.MaPhieu,
                        principalTable: "PhieuDieuChuyen",
                        principalColumn: "MaPhieu",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChiTietDieuChuyen_ViTri_MaViTriDi",
                        column: x => x.MaViTriDi,
                        principalTable: "ViTri",
                        principalColumn: "MaViTri",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietDieuChuyen_MaLo",
                table: "ChiTietDieuChuyen",
                column: "MaLo");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietDieuChuyen_MaViTriDi",
                table: "ChiTietDieuChuyen",
                column: "MaViTriDi");

            migrationBuilder.CreateIndex(
                name: "IX_PhieuDieuChuyen_MaKhoDen_TrangThai",
                table: "PhieuDieuChuyen",
                columns: new[] { "MaKhoDen", "TrangThai" });

            migrationBuilder.CreateIndex(
                name: "IX_PhieuDieuChuyen_MaKhoDi_TrangThai",
                table: "PhieuDieuChuyen",
                columns: new[] { "MaKhoDi", "TrangThai" });

            migrationBuilder.CreateIndex(
                name: "IX_PhieuDieuChuyen_NguoiLap",
                table: "PhieuDieuChuyen",
                column: "NguoiLap");

            migrationBuilder.CreateIndex(
                name: "IX_PhieuDieuChuyen_NguoiNhan",
                table: "PhieuDieuChuyen",
                column: "NguoiNhan");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChiTietDieuChuyen");

            migrationBuilder.DropTable(
                name: "PhieuDieuChuyen");
        }
    }
}
