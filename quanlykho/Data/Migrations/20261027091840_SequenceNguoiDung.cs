using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace quanlykho.Data.Migrations
{
    /// <inheritdoc />
    public partial class SequenceNguoiDung : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence(
                name: "MaChungTu_ND");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropSequence(
                name: "MaChungTu_ND");
        }
    }
}
