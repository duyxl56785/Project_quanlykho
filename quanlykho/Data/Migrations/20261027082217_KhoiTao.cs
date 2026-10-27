using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace quanlykho.Data.Migrations
{
    /// <inheritdoc />
    public partial class KhoiTao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence(
                name: "MaChungTu_DC");

            migrationBuilder.CreateSequence(
                name: "MaChungTu_DD");

            migrationBuilder.CreateSequence(
                name: "MaChungTu_DM");

            migrationBuilder.CreateSequence(
                name: "MaChungTu_KK");

            migrationBuilder.CreateSequence(
                name: "MaChungTu_L");

            migrationBuilder.CreateSequence(
                name: "MaChungTu_LX");

            migrationBuilder.CreateSequence(
                name: "MaChungTu_PN");

            migrationBuilder.CreateSequence(
                name: "MaChungTu_PX");

            migrationBuilder.CreateTable(
                name: "NguoiDung",
                columns: table => new
                {
                    MaNguoiDung = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Ten = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SoDienThoai = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    MatKhauBam = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    VaiTro = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TrangThai = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SecurityStamp = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    SoLanDangNhapSai = table.Column<int>(type: "integer", nullable: false),
                    KhoaDenLuc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NguoiDung", x => x.MaNguoiDung);
                    table.CheckConstraint("CK_NguoiDung_Email", "\"Email\" = lower(\"Email\")");
                    table.CheckConstraint("CK_NguoiDung_SoLanDangNhapSai", "\"SoLanDangNhapSai\" >= 0");
                    table.CheckConstraint("CK_NguoiDung_TrangThai", "\"TrangThai\" IN ('DangHoatDong', 'DaKhoa')");
                    table.CheckConstraint("CK_NguoiDung_VaiTro", "\"VaiTro\" IN ('QuanTriVien', 'QuanLyKho', 'ThuKho', 'NhanVienBanHang')");
                });

            migrationBuilder.CreateTable(
                name: "NhaCungCap",
                columns: table => new
                {
                    MaNhaCungCap = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Ten = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DiaChi = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    NguoiLienHe = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SoDienThoai = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    TrangThai = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NhaCungCap", x => x.MaNhaCungCap);
                    table.CheckConstraint("CK_NhaCungCap_TrangThai", "\"TrangThai\" IN ('DangSuDung', 'NgungSuDung')");
                });

            migrationBuilder.CreateTable(
                name: "NhomHang",
                columns: table => new
                {
                    MaNhom = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    MaCha = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Ten = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NhomHang", x => x.MaNhom);
                    table.ForeignKey(
                        name: "FK_NhomHang_NhomHang_MaCha",
                        column: x => x.MaCha,
                        principalTable: "NhomHang",
                        principalColumn: "MaNhom",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NoiNhan",
                columns: table => new
                {
                    MaNoiNhan = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Ten = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DiaChi = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    NguoiLienHe = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SoDienThoai = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    TrangThai = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NoiNhan", x => x.MaNoiNhan);
                    table.CheckConstraint("CK_NoiNhan_TrangThai", "\"TrangThai\" IN ('DangSuDung', 'NgungSuDung')");
                });

            migrationBuilder.CreateTable(
                name: "ViTri",
                columns: table => new
                {
                    MaViTri = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    MaCha = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    MaKho = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Cap = table.Column<short>(type: "smallint", nullable: false),
                    Ten = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TenDayDu = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    ThuTu = table.Column<int>(type: "integer", nullable: false),
                    LoaiKhu = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    DieuKienBaoQuan = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Dai = table.Column<decimal>(type: "numeric(8,3)", precision: 8, scale: 3, nullable: true),
                    Rong = table.Column<decimal>(type: "numeric(8,3)", precision: 8, scale: 3, nullable: true),
                    Cao = table.Column<decimal>(type: "numeric(8,3)", precision: 8, scale: 3, nullable: true),
                    TaiTrongToiDa = table.Column<decimal>(type: "numeric(10,3)", precision: 10, scale: 3, nullable: true),
                    LaOMacDinh = table.Column<bool>(type: "boolean", nullable: false),
                    DangKhoa = table.Column<bool>(type: "boolean", nullable: false),
                    MaPhieuKiemKe = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    DiaChi = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    LoaiHangLuuTru = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    HeSoLapDay = table.Column<decimal>(type: "numeric(4,3)", precision: 4, scale: 3, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ViTri", x => x.MaViTri);
                    table.CheckConstraint("CK_ViTri_Cap", "\"Cap\" BETWEEN 1 AND 7");
                    table.CheckConstraint("CK_ViTri_DieuKienBaoQuan", "\"DieuKienBaoQuan\" IN ('Thuong', 'Lanh')");
                    table.CheckConstraint("CK_ViTri_HeSoLapDay", "\"HeSoLapDay\" IS NULL OR (\"HeSoLapDay\" > 0 AND \"HeSoLapDay\" <= 1)");
                    table.CheckConstraint("CK_ViTri_KichThuocDuong", "COALESCE(\"Dai\", 1) > 0 AND COALESCE(\"Rong\", 1) > 0 AND COALESCE(\"Cao\", 1) > 0 AND COALESCE(\"TaiTrongToiDa\", 1) > 0");
                    table.CheckConstraint("CK_ViTri_LoaiKhu", "\"LoaiKhu\" IN ('TiepNhan', 'LuuTru', 'CachLy', 'Xuat')");
                    table.CheckConstraint("CK_ViTri_MaCha", "(\"Cap\" = 1) = (\"MaCha\" IS NULL)");
                    table.CheckConstraint("CK_ViTri_MaKho", "\"Cap\" <> 1 OR \"MaKho\" = \"MaViTri\"");
                    table.CheckConstraint("CK_ViTri_SucChuaO", "\"Cap\" <> 7 OR (\"Dai\" IS NOT NULL AND \"Rong\" IS NOT NULL AND \"Cao\" IS NOT NULL AND \"TaiTrongToiDa\" IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_ViTri_ViTri_MaCha",
                        column: x => x.MaCha,
                        principalTable: "ViTri",
                        principalColumn: "MaViTri",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ViTri_ViTri_MaKho",
                        column: x => x.MaKho,
                        principalTable: "ViTri",
                        principalColumn: "MaViTri",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NhatKy",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    ThoiDiem = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    MaNguoiDung = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    MaKho = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    LoaiThaoTac = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DoiTuong = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    MaDoiTuong = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    ChiTiet = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NhatKy", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NhatKy_NguoiDung_MaNguoiDung",
                        column: x => x.MaNguoiDung,
                        principalTable: "NguoiDung",
                        principalColumn: "MaNguoiDung",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SanPham",
                columns: table => new
                {
                    MaSanPham = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Ten = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DonViTinh = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    MaNhom = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    DieuKienBaoQuan = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TheoDoiHanDung = table.Column<bool>(type: "boolean", nullable: false),
                    MoTa = table.Column<string>(type: "text", nullable: true),
                    TrangThai = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SanPham", x => x.MaSanPham);
                    table.CheckConstraint("CK_SanPham_DieuKienBaoQuan", "\"DieuKienBaoQuan\" IN ('Thuong', 'Lanh')");
                    table.CheckConstraint("CK_SanPham_TrangThai", "\"TrangThai\" IN ('DangSuDung', 'NgungSuDung')");
                    table.ForeignKey(
                        name: "FK_SanPham_NhomHang_MaNhom",
                        column: x => x.MaNhom,
                        principalTable: "NhomHang",
                        principalColumn: "MaNhom",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ThuocTinh",
                columns: table => new
                {
                    MaNhom = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Ten = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ThuTu = table.Column<int>(type: "integer", nullable: false),
                    GiaTriGoiY = table.Column<string[]>(type: "text[]", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ThuocTinh", x => new { x.MaNhom, x.Ten });
                    table.ForeignKey(
                        name: "FK_ThuocTinh_NhomHang_MaNhom",
                        column: x => x.MaNhom,
                        principalTable: "NhomHang",
                        principalColumn: "MaNhom",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DonMua",
                columns: table => new
                {
                    MaDonMua = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    MaNhaCungCap = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    MaKho = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    NguoiTao = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    NgayTao = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TrangThai = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    GhiChu = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DonMua", x => x.MaDonMua);
                    table.UniqueConstraint("AK_DonMua_MaDonMua_MaKho", x => new { x.MaDonMua, x.MaKho });
                    table.CheckConstraint("CK_DonMua_TrangThai", "\"TrangThai\" IN ('Nhap', 'ChuaThucHien', 'DangThucHien', 'HoanThanh', 'DaHuy')");
                    table.ForeignKey(
                        name: "FK_DonMua_NguoiDung_NguoiTao",
                        column: x => x.NguoiTao,
                        principalTable: "NguoiDung",
                        principalColumn: "MaNguoiDung",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DonMua_NhaCungCap_MaNhaCungCap",
                        column: x => x.MaNhaCungCap,
                        principalTable: "NhaCungCap",
                        principalColumn: "MaNhaCungCap",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DonMua_ViTri_MaKho",
                        column: x => x.MaKho,
                        principalTable: "ViTri",
                        principalColumn: "MaViTri",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NguoiDungKho",
                columns: table => new
                {
                    MaNguoiDung = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    MaKho = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NguoiDungKho", x => new { x.MaNguoiDung, x.MaKho });
                    table.ForeignKey(
                        name: "FK_NguoiDungKho_NguoiDung_MaNguoiDung",
                        column: x => x.MaNguoiDung,
                        principalTable: "NguoiDung",
                        principalColumn: "MaNguoiDung",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NguoiDungKho_ViTri_MaKho",
                        column: x => x.MaKho,
                        principalTable: "ViTri",
                        principalColumn: "MaViTri",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BienThe",
                columns: table => new
                {
                    MaBienThe = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    MaSanPham = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ThuocTinh = table.Column<string>(type: "jsonb", nullable: false),
                    DaiDongGoi = table.Column<decimal>(type: "numeric(8,3)", precision: 8, scale: 3, nullable: false),
                    RongDongGoi = table.Column<decimal>(type: "numeric(8,3)", precision: 8, scale: 3, nullable: false),
                    CaoDongGoi = table.Column<decimal>(type: "numeric(8,3)", precision: 8, scale: 3, nullable: false),
                    KhoiLuong = table.Column<decimal>(type: "numeric(10,3)", precision: 10, scale: 3, nullable: false),
                    MaVach = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    TrangThai = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BienThe", x => x.MaBienThe);
                    table.CheckConstraint("CK_BienThe_KhoiLuong", "\"KhoiLuong\" > 0");
                    table.CheckConstraint("CK_BienThe_KichThuoc", "\"DaiDongGoi\" > 0 AND \"RongDongGoi\" > 0 AND \"CaoDongGoi\" > 0");
                    table.CheckConstraint("CK_BienThe_TrangThai", "\"TrangThai\" IN ('DangSuDung', 'NgungSuDung')");
                    table.ForeignKey(
                        name: "FK_BienThe_SanPham_MaSanPham",
                        column: x => x.MaSanPham,
                        principalTable: "SanPham",
                        principalColumn: "MaSanPham",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PhieuNhap",
                columns: table => new
                {
                    MaPhieuNhap = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    LoaiPhieu = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    MaDonMua = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    MaKho = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    NguoiLap = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    NguoiXacNhan = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    NgayNhap = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TrangThai = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    GhiChu = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhieuNhap", x => x.MaPhieuNhap);
                    table.CheckConstraint("CK_PhieuNhap_DonMua", "(\"LoaiPhieu\" = 'MuaHang') = (\"MaDonMua\" IS NOT NULL)");
                    table.CheckConstraint("CK_PhieuNhap_LoaiPhieu", "\"LoaiPhieu\" IN ('MuaHang', 'NhapKhac', 'TonDauKy')");
                    table.CheckConstraint("CK_PhieuNhap_TrangThai", "\"TrangThai\" IN ('LuuTam', 'HoanTat', 'DaHuy')");
                    table.ForeignKey(
                        name: "FK_PhieuNhap_DonMua_MaDonMua_MaKho",
                        columns: x => new { x.MaDonMua, x.MaKho },
                        principalTable: "DonMua",
                        principalColumns: new[] { "MaDonMua", "MaKho" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PhieuNhap_NguoiDung_NguoiLap",
                        column: x => x.NguoiLap,
                        principalTable: "NguoiDung",
                        principalColumn: "MaNguoiDung",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PhieuNhap_NguoiDung_NguoiXacNhan",
                        column: x => x.NguoiXacNhan,
                        principalTable: "NguoiDung",
                        principalColumn: "MaNguoiDung",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PhieuNhap_ViTri_MaKho",
                        column: x => x.MaKho,
                        principalTable: "ViTri",
                        principalColumn: "MaViTri",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ChiTietDonMua",
                columns: table => new
                {
                    MaDonMua = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    MaBienThe = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TenDonVi = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    HeSo = table.Column<int>(type: "integer", nullable: false),
                    SoLuongDat = table.Column<decimal>(type: "numeric(14,3)", precision: 14, scale: 3, nullable: false),
                    DonGia = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChiTietDonMua", x => new { x.MaDonMua, x.MaBienThe });
                    table.CheckConstraint("CK_ChiTietDonMua_DonGia", "\"DonGia\" >= 0");
                    table.CheckConstraint("CK_ChiTietDonMua_HeSo", "\"HeSo\" >= 1");
                    table.CheckConstraint("CK_ChiTietDonMua_SoLuongDat", "\"SoLuongDat\" > 0");
                    table.ForeignKey(
                        name: "FK_ChiTietDonMua_BienThe_MaBienThe",
                        column: x => x.MaBienThe,
                        principalTable: "BienThe",
                        principalColumn: "MaBienThe",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChiTietDonMua_DonMua_MaDonMua",
                        column: x => x.MaDonMua,
                        principalTable: "DonMua",
                        principalColumn: "MaDonMua",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DonViQuyDoi",
                columns: table => new
                {
                    MaBienThe = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TenDonVi = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    HeSo = table.Column<int>(type: "integer", nullable: false),
                    MaVach = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    TrangThai = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DonViQuyDoi", x => new { x.MaBienThe, x.TenDonVi });
                    table.CheckConstraint("CK_DonViQuyDoi_HeSo", "\"HeSo\" > 1");
                    table.CheckConstraint("CK_DonViQuyDoi_TrangThai", "\"TrangThai\" IN ('DangSuDung', 'NgungSuDung')");
                    table.ForeignKey(
                        name: "FK_DonViQuyDoi_BienThe_MaBienThe",
                        column: x => x.MaBienThe,
                        principalTable: "BienThe",
                        principalColumn: "MaBienThe",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Lo",
                columns: table => new
                {
                    MaLo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    MaBienThe = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    HanDung = table.Column<DateOnly>(type: "date", nullable: true),
                    NgayNhap = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lo", x => x.MaLo);
                    table.ForeignKey(
                        name: "FK_Lo_BienThe_MaBienThe",
                        column: x => x.MaBienThe,
                        principalTable: "BienThe",
                        principalColumn: "MaBienThe",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MucTon",
                columns: table => new
                {
                    MaBienThe = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    MaKho = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ToiThieu = table.Column<decimal>(type: "numeric(14,3)", precision: 14, scale: 3, nullable: true),
                    ToiDa = table.Column<decimal>(type: "numeric(14,3)", precision: 14, scale: 3, nullable: true),
                    ThoiGianChoHang = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MucTon", x => new { x.MaBienThe, x.MaKho });
                    table.CheckConstraint("CK_MucTon_ThoiGianChoHang", "\"ThoiGianChoHang\" >= 0");
                    table.CheckConstraint("CK_MucTon_ToiDa", "\"ToiDa\" IS NULL OR \"ToiThieu\" IS NULL OR \"ToiDa\" > \"ToiThieu\"");
                    table.CheckConstraint("CK_MucTon_ToiThieu", "\"ToiThieu\" IS NULL OR \"ToiThieu\" >= 0");
                    table.ForeignKey(
                        name: "FK_MucTon_BienThe_MaBienThe",
                        column: x => x.MaBienThe,
                        principalTable: "BienThe",
                        principalColumn: "MaBienThe",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MucTon_ViTri_MaKho",
                        column: x => x.MaKho,
                        principalTable: "ViTri",
                        principalColumn: "MaViTri",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ChiTietPhieuNhap",
                columns: table => new
                {
                    MaPhieuNhap = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    STT = table.Column<int>(type: "integer", nullable: false),
                    MaLo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TenDonVi = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    HeSo = table.Column<int>(type: "integer", nullable: false),
                    SoLuongGoc = table.Column<decimal>(type: "numeric(14,3)", precision: 14, scale: 3, nullable: false),
                    SoLuongThucNhan = table.Column<decimal>(type: "numeric(14,3)", precision: 14, scale: 3, nullable: false),
                    DonGia = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ThanhTien = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false, computedColumnSql: "round(\"SoLuongGoc\" * \"DonGia\", 2)", stored: true),
                    ChatLuong = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChiTietPhieuNhap", x => new { x.MaPhieuNhap, x.STT });
                    table.CheckConstraint("CK_ChiTietPhieuNhap_ChatLuong", "\"ChatLuong\" IN ('Dat', 'Loi')");
                    table.CheckConstraint("CK_ChiTietPhieuNhap_DonGia", "\"DonGia\" >= 0");
                    table.CheckConstraint("CK_ChiTietPhieuNhap_HeSo", "\"HeSo\" >= 1");
                    table.CheckConstraint("CK_ChiTietPhieuNhap_SoLuong", "\"SoLuongGoc\" > 0 AND \"SoLuongThucNhan\" = \"SoLuongGoc\" * \"HeSo\"");
                    table.ForeignKey(
                        name: "FK_ChiTietPhieuNhap_Lo_MaLo",
                        column: x => x.MaLo,
                        principalTable: "Lo",
                        principalColumn: "MaLo",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ChiTietPhieuNhap_PhieuNhap_MaPhieuNhap",
                        column: x => x.MaPhieuNhap,
                        principalTable: "PhieuNhap",
                        principalColumn: "MaPhieuNhap",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SoBienDong",
                columns: table => new
                {
                    MaBienDong = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    MaLo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    MaViTri = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    SoLuongThayDoi = table.Column<decimal>(type: "numeric(14,3)", precision: 14, scale: 3, nullable: false),
                    LoaiChungTu = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    MaChungTu = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    MaKhoaChongLap = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ThoiDiem = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    NguoiThucHien = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SoBienDong", x => x.MaBienDong);
                    table.CheckConstraint("CK_SoBienDong_SoLuongThayDoi", "\"SoLuongThayDoi\" <> 0");
                    table.ForeignKey(
                        name: "FK_SoBienDong_Lo_MaLo",
                        column: x => x.MaLo,
                        principalTable: "Lo",
                        principalColumn: "MaLo",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SoBienDong_NguoiDung_NguoiThucHien",
                        column: x => x.NguoiThucHien,
                        principalTable: "NguoiDung",
                        principalColumn: "MaNguoiDung",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SoBienDong_ViTri_MaViTri",
                        column: x => x.MaViTri,
                        principalTable: "ViTri",
                        principalColumn: "MaViTri",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TonKho",
                columns: table => new
                {
                    MaLo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    MaViTri = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    SoLuong = table.Column<decimal>(type: "numeric(14,3)", precision: 14, scale: 3, nullable: false),
                    SoLuongDaGiu = table.Column<decimal>(type: "numeric(14,3)", precision: 14, scale: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TonKho", x => new { x.MaLo, x.MaViTri });
                    table.CheckConstraint("CK_TonKho_SoLuong", "\"SoLuong\" >= 0 AND \"SoLuongDaGiu\" >= 0 AND \"SoLuongDaGiu\" <= \"SoLuong\"");
                    table.ForeignKey(
                        name: "FK_TonKho_Lo_MaLo",
                        column: x => x.MaLo,
                        principalTable: "Lo",
                        principalColumn: "MaLo",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TonKho_ViTri_MaViTri",
                        column: x => x.MaViTri,
                        principalTable: "ViTri",
                        principalColumn: "MaViTri",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BienThe_MaSanPham_ThuocTinh",
                table: "BienThe",
                columns: new[] { "MaSanPham", "ThuocTinh" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BienThe_MaVach",
                table: "BienThe",
                column: "MaVach",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietDonMua_MaBienThe",
                table: "ChiTietDonMua",
                column: "MaBienThe");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietPhieuNhap_MaLo",
                table: "ChiTietPhieuNhap",
                column: "MaLo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DonMua_MaKho",
                table: "DonMua",
                column: "MaKho");

            migrationBuilder.CreateIndex(
                name: "IX_DonMua_MaNhaCungCap",
                table: "DonMua",
                column: "MaNhaCungCap");

            migrationBuilder.CreateIndex(
                name: "IX_DonMua_NguoiTao",
                table: "DonMua",
                column: "NguoiTao");

            migrationBuilder.CreateIndex(
                name: "IX_DonViQuyDoi_MaVach",
                table: "DonViQuyDoi",
                column: "MaVach",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Lo_MaBienThe_HanDung",
                table: "Lo",
                columns: new[] { "MaBienThe", "HanDung" });

            migrationBuilder.CreateIndex(
                name: "IX_MucTon_MaKho",
                table: "MucTon",
                column: "MaKho");

            migrationBuilder.CreateIndex(
                name: "IX_NguoiDung_Email",
                table: "NguoiDung",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NguoiDungKho_MaKho",
                table: "NguoiDungKho",
                column: "MaKho");

            migrationBuilder.CreateIndex(
                name: "IX_NhatKy_MaNguoiDung",
                table: "NhatKy",
                column: "MaNguoiDung");

            migrationBuilder.CreateIndex(
                name: "IX_NhatKy_ThoiDiem",
                table: "NhatKy",
                column: "ThoiDiem");

            migrationBuilder.CreateIndex(
                name: "IX_NhomHang_MaCha",
                table: "NhomHang",
                column: "MaCha");

            migrationBuilder.CreateIndex(
                name: "IX_PhieuNhap_MaDonMua_MaKho",
                table: "PhieuNhap",
                columns: new[] { "MaDonMua", "MaKho" });

            migrationBuilder.CreateIndex(
                name: "IX_PhieuNhap_MaKho_NgayNhap",
                table: "PhieuNhap",
                columns: new[] { "MaKho", "NgayNhap" });

            migrationBuilder.CreateIndex(
                name: "IX_PhieuNhap_NguoiLap",
                table: "PhieuNhap",
                column: "NguoiLap");

            migrationBuilder.CreateIndex(
                name: "IX_PhieuNhap_NguoiXacNhan",
                table: "PhieuNhap",
                column: "NguoiXacNhan");

            migrationBuilder.CreateIndex(
                name: "IX_SanPham_MaNhom",
                table: "SanPham",
                column: "MaNhom");

            migrationBuilder.CreateIndex(
                name: "IX_SoBienDong_MaChungTu",
                table: "SoBienDong",
                column: "MaChungTu");

            migrationBuilder.CreateIndex(
                name: "IX_SoBienDong_MaKhoaChongLap",
                table: "SoBienDong",
                column: "MaKhoaChongLap",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SoBienDong_MaLo_ThoiDiem",
                table: "SoBienDong",
                columns: new[] { "MaLo", "ThoiDiem" });

            migrationBuilder.CreateIndex(
                name: "IX_SoBienDong_MaViTri",
                table: "SoBienDong",
                column: "MaViTri");

            migrationBuilder.CreateIndex(
                name: "IX_SoBienDong_NguoiThucHien",
                table: "SoBienDong",
                column: "NguoiThucHien");

            migrationBuilder.CreateIndex(
                name: "IX_TonKho_MaViTri",
                table: "TonKho",
                column: "MaViTri");

            migrationBuilder.CreateIndex(
                name: "IX_ViTri_MaCha",
                table: "ViTri",
                column: "MaCha");

            migrationBuilder.CreateIndex(
                name: "IX_ViTri_MaKho_LoaiKhu",
                table: "ViTri",
                columns: new[] { "MaKho", "LoaiKhu" });

            migrationBuilder.CreateIndex(
                name: "IX_ViTri_OMacDinh",
                table: "ViTri",
                columns: new[] { "MaKho", "LoaiKhu" },
                unique: true,
                filter: "\"LaOMacDinh\"");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChiTietDonMua");

            migrationBuilder.DropTable(
                name: "ChiTietPhieuNhap");

            migrationBuilder.DropTable(
                name: "DonViQuyDoi");

            migrationBuilder.DropTable(
                name: "MucTon");

            migrationBuilder.DropTable(
                name: "NguoiDungKho");

            migrationBuilder.DropTable(
                name: "NhatKy");

            migrationBuilder.DropTable(
                name: "NoiNhan");

            migrationBuilder.DropTable(
                name: "SoBienDong");

            migrationBuilder.DropTable(
                name: "ThuocTinh");

            migrationBuilder.DropTable(
                name: "TonKho");

            migrationBuilder.DropTable(
                name: "PhieuNhap");

            migrationBuilder.DropTable(
                name: "Lo");

            migrationBuilder.DropTable(
                name: "DonMua");

            migrationBuilder.DropTable(
                name: "BienThe");

            migrationBuilder.DropTable(
                name: "NguoiDung");

            migrationBuilder.DropTable(
                name: "NhaCungCap");

            migrationBuilder.DropTable(
                name: "ViTri");

            migrationBuilder.DropTable(
                name: "SanPham");

            migrationBuilder.DropTable(
                name: "NhomHang");

            migrationBuilder.DropSequence(
                name: "MaChungTu_DC");

            migrationBuilder.DropSequence(
                name: "MaChungTu_DD");

            migrationBuilder.DropSequence(
                name: "MaChungTu_DM");

            migrationBuilder.DropSequence(
                name: "MaChungTu_KK");

            migrationBuilder.DropSequence(
                name: "MaChungTu_L");

            migrationBuilder.DropSequence(
                name: "MaChungTu_LX");

            migrationBuilder.DropSequence(
                name: "MaChungTu_PN");

            migrationBuilder.DropSequence(
                name: "MaChungTu_PX");
        }
    }
}
