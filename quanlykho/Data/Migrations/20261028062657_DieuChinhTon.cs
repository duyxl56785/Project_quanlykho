using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace quanlykho.Data.Migrations
{
    /// <inheritdoc />
    public partial class DieuChinhTon : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ChiTietPhieuNhap_MaLo",
                table: "ChiTietPhieuNhap");

            migrationBuilder.AddColumn<string>(
                name: "KetLuan",
                table: "ChiTietKiemKe",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LoaiChenhLech",
                table: "ChiTietKiemKe",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MaBienTheDung",
                table: "ChiTietKiemKe",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MaChungTuXuLy",
                table: "ChiTietKiemKe",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MaViTriDung",
                table: "ChiTietKiemKe",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NguoiDuyet",
                table: "ChiTietKiemKe",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ThoiDiemDuyet",
                table: "ChiTietKiemKe",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CachXuLy",
                table: "ChiTietDieuChuyen",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LyDoXuLy",
                table: "ChiTietDieuChuyen",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NguoiXuLy",
                table: "ChiTietDieuChuyen",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ThoiDiemXuLy",
                table: "ChiTietDieuChuyen",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietPhieuNhap_MaLo",
                table: "ChiTietPhieuNhap",
                column: "MaLo");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietKiemKe_MaBienTheDung",
                table: "ChiTietKiemKe",
                column: "MaBienTheDung");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietKiemKe_MaViTriDung",
                table: "ChiTietKiemKe",
                column: "MaViTriDung");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietKiemKe_NguoiDuyet",
                table: "ChiTietKiemKe",
                column: "NguoiDuyet");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ChiTietKiemKe_DaDuyet",
                table: "ChiTietKiemKe",
                sql: "\"NguoiDuyet\" IS NULL OR (\"LoaiChenhLech\" IS NOT NULL AND \"KetLuan\" IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ChiTietKiemKe_LoaiChenhLech",
                table: "ChiTietKiemKe",
                sql: "\"LoaiChenhLech\" IN ('Thieu', 'Thua', 'HuHong', 'HetHan', 'SaiViTri', 'SaiBienThe')");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietDieuChuyen_NguoiXuLy",
                table: "ChiTietDieuChuyen",
                column: "NguoiXuLy");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ChiTietDieuChuyen_CachXuLy",
                table: "ChiTietDieuChuyen",
                sql: "\"CachXuLy\" IN ('NhapKhoDen', 'NhapLaiKhoDi', 'GhiGiamHaoHut')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ChiTietDieuChuyen_XuLy",
                table: "ChiTietDieuChuyen",
                sql: "\"CachXuLy\" IS NULL OR (\"LyDoXuLy\" IS NOT NULL AND \"NguoiXuLy\" IS NOT NULL)");

            migrationBuilder.AddForeignKey(
                name: "FK_ChiTietDieuChuyen_NguoiDung_NguoiXuLy",
                table: "ChiTietDieuChuyen",
                column: "NguoiXuLy",
                principalTable: "NguoiDung",
                principalColumn: "MaNguoiDung",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ChiTietKiemKe_BienThe_MaBienTheDung",
                table: "ChiTietKiemKe",
                column: "MaBienTheDung",
                principalTable: "BienThe",
                principalColumn: "MaBienThe",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ChiTietKiemKe_NguoiDung_NguoiDuyet",
                table: "ChiTietKiemKe",
                column: "NguoiDuyet",
                principalTable: "NguoiDung",
                principalColumn: "MaNguoiDung",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ChiTietKiemKe_ViTri_MaViTriDung",
                table: "ChiTietKiemKe",
                column: "MaViTriDung",
                principalTable: "ViTri",
                principalColumn: "MaViTri",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ChiTietDieuChuyen_NguoiDung_NguoiXuLy",
                table: "ChiTietDieuChuyen");

            migrationBuilder.DropForeignKey(
                name: "FK_ChiTietKiemKe_BienThe_MaBienTheDung",
                table: "ChiTietKiemKe");

            migrationBuilder.DropForeignKey(
                name: "FK_ChiTietKiemKe_NguoiDung_NguoiDuyet",
                table: "ChiTietKiemKe");

            migrationBuilder.DropForeignKey(
                name: "FK_ChiTietKiemKe_ViTri_MaViTriDung",
                table: "ChiTietKiemKe");

            migrationBuilder.DropIndex(
                name: "IX_ChiTietPhieuNhap_MaLo",
                table: "ChiTietPhieuNhap");

            migrationBuilder.DropIndex(
                name: "IX_ChiTietKiemKe_MaBienTheDung",
                table: "ChiTietKiemKe");

            migrationBuilder.DropIndex(
                name: "IX_ChiTietKiemKe_MaViTriDung",
                table: "ChiTietKiemKe");

            migrationBuilder.DropIndex(
                name: "IX_ChiTietKiemKe_NguoiDuyet",
                table: "ChiTietKiemKe");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ChiTietKiemKe_DaDuyet",
                table: "ChiTietKiemKe");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ChiTietKiemKe_LoaiChenhLech",
                table: "ChiTietKiemKe");

            migrationBuilder.DropIndex(
                name: "IX_ChiTietDieuChuyen_NguoiXuLy",
                table: "ChiTietDieuChuyen");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ChiTietDieuChuyen_CachXuLy",
                table: "ChiTietDieuChuyen");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ChiTietDieuChuyen_XuLy",
                table: "ChiTietDieuChuyen");

            migrationBuilder.DropColumn(
                name: "KetLuan",
                table: "ChiTietKiemKe");

            migrationBuilder.DropColumn(
                name: "LoaiChenhLech",
                table: "ChiTietKiemKe");

            migrationBuilder.DropColumn(
                name: "MaBienTheDung",
                table: "ChiTietKiemKe");

            migrationBuilder.DropColumn(
                name: "MaChungTuXuLy",
                table: "ChiTietKiemKe");

            migrationBuilder.DropColumn(
                name: "MaViTriDung",
                table: "ChiTietKiemKe");

            migrationBuilder.DropColumn(
                name: "NguoiDuyet",
                table: "ChiTietKiemKe");

            migrationBuilder.DropColumn(
                name: "ThoiDiemDuyet",
                table: "ChiTietKiemKe");

            migrationBuilder.DropColumn(
                name: "CachXuLy",
                table: "ChiTietDieuChuyen");

            migrationBuilder.DropColumn(
                name: "LyDoXuLy",
                table: "ChiTietDieuChuyen");

            migrationBuilder.DropColumn(
                name: "NguoiXuLy",
                table: "ChiTietDieuChuyen");

            migrationBuilder.DropColumn(
                name: "ThoiDiemXuLy",
                table: "ChiTietDieuChuyen");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietPhieuNhap_MaLo",
                table: "ChiTietPhieuNhap",
                column: "MaLo",
                unique: true);
        }
    }
}
