using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace quanlykho.Data.Migrations
{
    /// <inheritdoc />
    public partial class LichSuGoiY : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LichSuGoiY",
                columns: table => new
                {
                    MaGoiY = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    Loai = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    MaKho = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    MaBienThe = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    ThoiDiem = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    NguoiDung = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    DauVao = table.Column<string>(type: "jsonb", nullable: false),
                    KetQua = table.Column<string>(type: "jsonb", nullable: false),
                    ChapNhan = table.Column<bool>(type: "boolean", nullable: true),
                    ThoiDiemPhanHoi = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    MaChungTu = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LichSuGoiY", x => x.MaGoiY);
                    table.CheckConstraint("CK_LichSuGoiY_Loai", "\"Loai\" IN ('LayHang', 'NhapBoSung')");
                    table.ForeignKey(
                        name: "FK_LichSuGoiY_BienThe_MaBienThe",
                        column: x => x.MaBienThe,
                        principalTable: "BienThe",
                        principalColumn: "MaBienThe",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LichSuGoiY_NguoiDung_NguoiDung",
                        column: x => x.NguoiDung,
                        principalTable: "NguoiDung",
                        principalColumn: "MaNguoiDung",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LichSuGoiY_ViTri_MaKho",
                        column: x => x.MaKho,
                        principalTable: "ViTri",
                        principalColumn: "MaViTri",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LichSuGoiY_Loai_ThoiDiem",
                table: "LichSuGoiY",
                columns: new[] { "Loai", "ThoiDiem" });

            migrationBuilder.CreateIndex(
                name: "IX_LichSuGoiY_MaBienThe",
                table: "LichSuGoiY",
                column: "MaBienThe");

            migrationBuilder.CreateIndex(
                name: "IX_LichSuGoiY_MaKho",
                table: "LichSuGoiY",
                column: "MaKho");

            migrationBuilder.CreateIndex(
                name: "IX_LichSuGoiY_NguoiDung",
                table: "LichSuGoiY",
                column: "NguoiDung");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LichSuGoiY");
        }
    }
}
