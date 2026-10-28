using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace quanlykho.Data.Migrations
{
    /// <inheritdoc />
    public partial class NhapExcel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TepDaNhap",
                columns: table => new
                {
                    MaBam = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                    TenTep = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Loai = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SoDong = table.Column<int>(type: "integer", nullable: false),
                    ThoiDiem = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    NguoiNhap = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TepDaNhap", x => x.MaBam);
                    table.ForeignKey(
                        name: "FK_TepDaNhap_NguoiDung_NguoiNhap",
                        column: x => x.NguoiNhap,
                        principalTable: "NguoiDung",
                        principalColumn: "MaNguoiDung",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TepDaNhap_NguoiNhap",
                table: "TepDaNhap",
                column: "NguoiNhap");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TepDaNhap");
        }
    }
}
