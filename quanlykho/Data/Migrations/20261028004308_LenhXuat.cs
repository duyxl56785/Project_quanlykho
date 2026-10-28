using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace quanlykho.Data.Migrations
{
    /// <inheritdoc />
    public partial class LenhXuat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LenhXuat",
                columns: table => new
                {
                    MaLenh = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    MaDonDat = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    MaKho = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    NguoiLap = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    NgayLap = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    NguoiThucHien = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    NguoiKiemTra = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    GhiChuKiemTra = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    TrangThai = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LenhXuat", x => x.MaLenh);
                    table.CheckConstraint("CK_LenhXuat_TrangThai", "\"TrangThai\" IN ('ChuaThucHien', 'ChoLayHang', 'ChoKiemTra', 'ChoXacNhan', 'HoanThanh')");
                    table.ForeignKey(
                        name: "FK_LenhXuat_DonDatHang_MaDonDat",
                        column: x => x.MaDonDat,
                        principalTable: "DonDatHang",
                        principalColumn: "MaDonDat",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LenhXuat_NguoiDung_NguoiKiemTra",
                        column: x => x.NguoiKiemTra,
                        principalTable: "NguoiDung",
                        principalColumn: "MaNguoiDung",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LenhXuat_NguoiDung_NguoiLap",
                        column: x => x.NguoiLap,
                        principalTable: "NguoiDung",
                        principalColumn: "MaNguoiDung",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LenhXuat_NguoiDung_NguoiThucHien",
                        column: x => x.NguoiThucHien,
                        principalTable: "NguoiDung",
                        principalColumn: "MaNguoiDung",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LenhXuat_ViTri_MaKho",
                        column: x => x.MaKho,
                        principalTable: "ViTri",
                        principalColumn: "MaViTri",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ChiTietLenhXuat",
                columns: table => new
                {
                    MaLenh = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    MaBienThe = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TenDonVi = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    HeSo = table.Column<int>(type: "integer", nullable: false),
                    SoLuongGoc = table.Column<decimal>(type: "numeric(14,3)", precision: 14, scale: 3, nullable: false),
                    SoLuongYeuCau = table.Column<decimal>(type: "numeric(14,3)", precision: 14, scale: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChiTietLenhXuat", x => new { x.MaLenh, x.MaBienThe });
                    table.CheckConstraint("CK_ChiTietLenhXuat_HeSo", "\"HeSo\" >= 1");
                    table.CheckConstraint("CK_ChiTietLenhXuat_SoLuongYeuCau", "\"SoLuongYeuCau\" > 0 AND \"SoLuongYeuCau\" = \"SoLuongGoc\" * \"HeSo\"");
                    table.ForeignKey(
                        name: "FK_ChiTietLenhXuat_BienThe_MaBienThe",
                        column: x => x.MaBienThe,
                        principalTable: "BienThe",
                        principalColumn: "MaBienThe",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChiTietLenhXuat_LenhXuat_MaLenh",
                        column: x => x.MaLenh,
                        principalTable: "LenhXuat",
                        principalColumn: "MaLenh",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DongLayHang",
                columns: table => new
                {
                    MaDong = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    MaLenh = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    MaBienThe = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    MaLo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    MaViTri = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    SoLuong = table.Column<decimal>(type: "numeric(14,3)", precision: 14, scale: 3, nullable: false),
                    SoLuongGiuThem = table.Column<decimal>(type: "numeric(14,3)", precision: 14, scale: 3, nullable: false),
                    NguoiLay = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ThoiDiem = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DongLayHang", x => x.MaDong);
                    table.CheckConstraint("CK_DongLayHang_SoLuong", "\"SoLuong\" > 0 AND \"SoLuongGiuThem\" >= 0 AND \"SoLuongGiuThem\" <= \"SoLuong\"");
                    table.ForeignKey(
                        name: "FK_DongLayHang_BienThe_MaBienThe",
                        column: x => x.MaBienThe,
                        principalTable: "BienThe",
                        principalColumn: "MaBienThe",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DongLayHang_LenhXuat_MaLenh",
                        column: x => x.MaLenh,
                        principalTable: "LenhXuat",
                        principalColumn: "MaLenh",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DongLayHang_Lo_MaLo",
                        column: x => x.MaLo,
                        principalTable: "Lo",
                        principalColumn: "MaLo",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DongLayHang_NguoiDung_NguoiLay",
                        column: x => x.NguoiLay,
                        principalTable: "NguoiDung",
                        principalColumn: "MaNguoiDung",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DongLayHang_ViTri_MaViTri",
                        column: x => x.MaViTri,
                        principalTable: "ViTri",
                        principalColumn: "MaViTri",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PhieuXuat",
                columns: table => new
                {
                    MaPhieuXuat = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    LoaiPhieu = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    MaLenh = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    MaKho = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    MaNhaCungCap = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    NguoiLap = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    NguoiXacNhan = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    NgayXuat = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TrangThai = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    LyDo = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhieuXuat", x => x.MaPhieuXuat);
                    table.CheckConstraint("CK_PhieuXuat_LoaiPhieu", "\"LoaiPhieu\" IN ('XuatBan', 'XuatKhac', 'TraNhaCungCap')");
                    table.CheckConstraint("CK_PhieuXuat_TheoLoai", "(\"LoaiPhieu\" = 'XuatBan') = (\"MaLenh\" IS NOT NULL) AND (\"LoaiPhieu\" = 'TraNhaCungCap') = (\"MaNhaCungCap\" IS NOT NULL)");
                    table.CheckConstraint("CK_PhieuXuat_TrangThai", "\"TrangThai\" IN ('LuuTam', 'HoanTat', 'DaHuy')");
                    table.ForeignKey(
                        name: "FK_PhieuXuat_LenhXuat_MaLenh",
                        column: x => x.MaLenh,
                        principalTable: "LenhXuat",
                        principalColumn: "MaLenh",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PhieuXuat_NguoiDung_NguoiLap",
                        column: x => x.NguoiLap,
                        principalTable: "NguoiDung",
                        principalColumn: "MaNguoiDung",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PhieuXuat_NguoiDung_NguoiXacNhan",
                        column: x => x.NguoiXacNhan,
                        principalTable: "NguoiDung",
                        principalColumn: "MaNguoiDung",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PhieuXuat_NhaCungCap_MaNhaCungCap",
                        column: x => x.MaNhaCungCap,
                        principalTable: "NhaCungCap",
                        principalColumn: "MaNhaCungCap",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PhieuXuat_ViTri_MaKho",
                        column: x => x.MaKho,
                        principalTable: "ViTri",
                        principalColumn: "MaViTri",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ChiTietPhieuXuat",
                columns: table => new
                {
                    MaPhieuXuat = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    STT = table.Column<int>(type: "integer", nullable: false),
                    MaLo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    MaViTri = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    TenDonVi = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    HeSo = table.Column<int>(type: "integer", nullable: false),
                    SoLuongGoc = table.Column<decimal>(type: "numeric(14,3)", precision: 14, scale: 3, nullable: false),
                    SoLuong = table.Column<decimal>(type: "numeric(14,3)", precision: 14, scale: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChiTietPhieuXuat", x => new { x.MaPhieuXuat, x.STT });
                    table.CheckConstraint("CK_ChiTietPhieuXuat_HeSo", "\"HeSo\" >= 1");
                    table.CheckConstraint("CK_ChiTietPhieuXuat_SoLuong", "\"SoLuong\" > 0 AND \"SoLuong\" = \"SoLuongGoc\" * \"HeSo\"");
                    table.ForeignKey(
                        name: "FK_ChiTietPhieuXuat_Lo_MaLo",
                        column: x => x.MaLo,
                        principalTable: "Lo",
                        principalColumn: "MaLo",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChiTietPhieuXuat_PhieuXuat_MaPhieuXuat",
                        column: x => x.MaPhieuXuat,
                        principalTable: "PhieuXuat",
                        principalColumn: "MaPhieuXuat",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChiTietPhieuXuat_ViTri_MaViTri",
                        column: x => x.MaViTri,
                        principalTable: "ViTri",
                        principalColumn: "MaViTri",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietLenhXuat_MaBienThe",
                table: "ChiTietLenhXuat",
                column: "MaBienThe");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietPhieuXuat_MaLo",
                table: "ChiTietPhieuXuat",
                column: "MaLo");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietPhieuXuat_MaViTri",
                table: "ChiTietPhieuXuat",
                column: "MaViTri");

            migrationBuilder.CreateIndex(
                name: "IX_DongLayHang_MaBienThe",
                table: "DongLayHang",
                column: "MaBienThe");

            migrationBuilder.CreateIndex(
                name: "IX_DongLayHang_MaLenh",
                table: "DongLayHang",
                column: "MaLenh");

            migrationBuilder.CreateIndex(
                name: "IX_DongLayHang_MaLo",
                table: "DongLayHang",
                column: "MaLo");

            migrationBuilder.CreateIndex(
                name: "IX_DongLayHang_MaViTri",
                table: "DongLayHang",
                column: "MaViTri");

            migrationBuilder.CreateIndex(
                name: "IX_DongLayHang_NguoiLay",
                table: "DongLayHang",
                column: "NguoiLay");

            migrationBuilder.CreateIndex(
                name: "IX_LenhXuat_MaDonDat",
                table: "LenhXuat",
                column: "MaDonDat",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LenhXuat_MaKho_TrangThai",
                table: "LenhXuat",
                columns: new[] { "MaKho", "TrangThai" });

            migrationBuilder.CreateIndex(
                name: "IX_LenhXuat_NguoiKiemTra",
                table: "LenhXuat",
                column: "NguoiKiemTra");

            migrationBuilder.CreateIndex(
                name: "IX_LenhXuat_NguoiLap",
                table: "LenhXuat",
                column: "NguoiLap");

            migrationBuilder.CreateIndex(
                name: "IX_LenhXuat_NguoiThucHien",
                table: "LenhXuat",
                column: "NguoiThucHien");

            migrationBuilder.CreateIndex(
                name: "IX_PhieuXuat_MaKho_NgayXuat",
                table: "PhieuXuat",
                columns: new[] { "MaKho", "NgayXuat" });

            migrationBuilder.CreateIndex(
                name: "IX_PhieuXuat_MaLenh",
                table: "PhieuXuat",
                column: "MaLenh");

            migrationBuilder.CreateIndex(
                name: "IX_PhieuXuat_MaNhaCungCap",
                table: "PhieuXuat",
                column: "MaNhaCungCap");

            migrationBuilder.CreateIndex(
                name: "IX_PhieuXuat_NguoiLap",
                table: "PhieuXuat",
                column: "NguoiLap");

            migrationBuilder.CreateIndex(
                name: "IX_PhieuXuat_NguoiXacNhan",
                table: "PhieuXuat",
                column: "NguoiXacNhan");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChiTietLenhXuat");

            migrationBuilder.DropTable(
                name: "ChiTietPhieuXuat");

            migrationBuilder.DropTable(
                name: "DongLayHang");

            migrationBuilder.DropTable(
                name: "PhieuXuat");

            migrationBuilder.DropTable(
                name: "LenhXuat");
        }
    }
}
