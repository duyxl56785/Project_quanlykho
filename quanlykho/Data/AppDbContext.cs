using Microsoft.EntityFrameworkCore;
using quanlykho.Common;
using quanlykho.Domain;

namespace quanlykho.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<NhomHang> NhomHang => Set<NhomHang>();
    public DbSet<ThuocTinh> ThuocTinh => Set<ThuocTinh>();
    public DbSet<SanPham> SanPham => Set<SanPham>();
    public DbSet<BienThe> BienThe => Set<BienThe>();
    public DbSet<DonViQuyDoi> DonViQuyDoi => Set<DonViQuyDoi>();
    public DbSet<NhaCungCap> NhaCungCap => Set<NhaCungCap>();
    public DbSet<NoiNhan> NoiNhan => Set<NoiNhan>();
    public DbSet<MucTon> MucTon => Set<MucTon>();
    public DbSet<ViTri> ViTri => Set<ViTri>();
    public DbSet<NguoiDung> NguoiDung => Set<NguoiDung>();
    public DbSet<NguoiDungKho> NguoiDungKho => Set<NguoiDungKho>();
    public DbSet<ChuyenViTri> ChuyenViTri => Set<ChuyenViTri>();
    public DbSet<DonDatHang> DonDatHang => Set<DonDatHang>();
    public DbSet<ChiTietDonDat> ChiTietDonDat => Set<ChiTietDonDat>();
    public DbSet<LichSuGoiY> LichSuGoiY => Set<LichSuGoiY>();
    public DbSet<GiuHang> GiuHang => Set<GiuHang>();
    public DbSet<LenhXuat> LenhXuat => Set<LenhXuat>();
    public DbSet<ChiTietLenhXuat> ChiTietLenhXuat => Set<ChiTietLenhXuat>();
    public DbSet<DongLayHang> DongLayHang => Set<DongLayHang>();
    public DbSet<PhieuXuat> PhieuXuat => Set<PhieuXuat>();
    public DbSet<ChiTietPhieuXuat> ChiTietPhieuXuat => Set<ChiTietPhieuXuat>();
    public DbSet<PhieuDieuChuyen> PhieuDieuChuyen => Set<PhieuDieuChuyen>();
    public DbSet<ChiTietDieuChuyen> ChiTietDieuChuyen => Set<ChiTietDieuChuyen>();
    public DbSet<PhieuKiemKe> PhieuKiemKe => Set<PhieuKiemKe>();
    public DbSet<ThanhVienKiemKe> ThanhVienKiemKe => Set<ThanhVienKiemKe>();
    public DbSet<ChiTietKiemKe> ChiTietKiemKe => Set<ChiTietKiemKe>();
    public DbSet<Lo> Lo => Set<Lo>();
    public DbSet<TonKho> TonKho => Set<TonKho>();
    public DbSet<SoBienDong> SoBienDong => Set<SoBienDong>();
    public DbSet<DonMua> DonMua => Set<DonMua>();
    public DbSet<ChiTietDonMua> ChiTietDonMua => Set<ChiTietDonMua>();
    public DbSet<PhieuNhap> PhieuNhap => Set<PhieuNhap>();
    public DbSet<ChiTietPhieuNhap> ChiTietPhieuNhap => Set<ChiTietPhieuNhap>();
    public DbSet<NhatKy> NhatKy => Set<NhatKy>();

    protected override void ConfigureConventions(ModelConfigurationBuilder c)
    {
        c.Properties<VaiTro>().HaveConversion<string>().HaveMaxLength(20);
        c.Properties<TrangThaiTaiKhoan>().HaveConversion<string>().HaveMaxLength(20);
        c.Properties<TrangThaiSuDung>().HaveConversion<string>().HaveMaxLength(20);
        c.Properties<DieuKienBaoQuan>().HaveConversion<string>().HaveMaxLength(20);
        c.Properties<LoaiKhu>().HaveConversion<string>().HaveMaxLength(20);
        c.Properties<ChatLuong>().HaveConversion<string>().HaveMaxLength(20);
        c.Properties<TrangThaiDonMua>().HaveConversion<string>().HaveMaxLength(20);
        c.Properties<TrangThaiPhieu>().HaveConversion<string>().HaveMaxLength(20);
        c.Properties<LoaiPhieuNhap>().HaveConversion<string>().HaveMaxLength(20);
        c.Properties<LoaiChuyenViTri>().HaveConversion<string>().HaveMaxLength(20);
        c.Properties<TrangThaiDonDat>().HaveConversion<string>().HaveMaxLength(20);
        c.Properties<LoaiGoiY>().HaveConversion<string>().HaveMaxLength(20);
        c.Properties<TrangThaiLenhXuat>().HaveConversion<string>().HaveMaxLength(20);
        c.Properties<LoaiPhieuXuat>().HaveConversion<string>().HaveMaxLength(20);
        c.Properties<TrangThaiDieuChuyen>().HaveConversion<string>().HaveMaxLength(20);
        c.Properties<TrangThaiKiemKe>().HaveConversion<string>().HaveMaxLength(20);
        c.Properties<PhamViKiemKe>().HaveConversion<string>().HaveMaxLength(20);
        c.Properties<LoaiChenhLech>().HaveConversion<string>().HaveMaxLength(20);
        c.Properties<XuLyChenhLech>().HaveConversion<string>().HaveMaxLength(20);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        foreach (var tienTo in MaChungTu.TienTo)
            modelBuilder.HasSequence<long>(MaChungTu.TenSequence(tienTo));

        // Không xóa cứng dữ liệu đã có tham chiếu: mọi khóa ngoại là RESTRICT.
        foreach (var fk in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
            fk.DeleteBehavior = DeleteBehavior.Restrict;
    }
}
