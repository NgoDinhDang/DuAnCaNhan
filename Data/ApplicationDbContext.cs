using Microsoft.EntityFrameworkCore;
using STOREBOOKS.Models;

namespace STOREBOOKS.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }
        public DbSet<Payment> Payments { get; set; }

        public DbSet<Sach> Sach { get; set; }
        public DbSet<DanhMuc> DanhMuc { get; set; }
        public DbSet<TaiKhoan> TaiKhoan { get; set; }
        public DbSet<NguoiDung> NguoiDung { get; set; }
        public DbSet<DanhGia> DanhGia { get; set; }
        
        public DbSet<YeuThich> YeuThich { get; set; }

        public DbSet<ChatMessage> ChatMessages { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Khóa chính cho DanhMuc
            modelBuilder.Entity<DanhMuc>()
                .HasKey(dm => dm.MaDanhMuc);

            // Khóa chính cho Sach
            modelBuilder.Entity<Sach>()
                .HasKey(s => s.MaSach);

            // Quan hệ 1 - nhiều: Sach - DanhMuc
            modelBuilder.Entity<Sach>()
                .HasOne(s => s.DanhMuc)
                .WithMany(dm => dm.Saches)
                .HasForeignKey(s => s.MaDanhMuc)
                .OnDelete(DeleteBehavior.Restrict); // tránh xóa cascade

            // Khóa chính cho TaiKhoan
            modelBuilder.Entity<TaiKhoan>()
                .HasKey(tk => tk.MaTaiKhoan);

            // Khóa chính cho NguoiDung
            modelBuilder.Entity<NguoiDung>()
                .HasKey(nd => nd.MaNguoiDung);

            // Quan hệ 1-1: TaiKhoan - NguoiDung
            modelBuilder.Entity<NguoiDung>()
                .HasOne(nd => nd.TaiKhoan)
                .WithOne(tk => tk.NguoiDung)
                .HasForeignKey<NguoiDung>(nd => nd.MaTaiKhoan)
                .OnDelete(DeleteBehavior.Cascade); // Khi xóa tài khoản thì xóa thông tin người dùng

            // Quan hệ 1 - nhiều: NguoiDung - Order
            modelBuilder.Entity<Order>()
                .HasOne(o => o.NguoiDung)
                .WithMany() // hoặc .WithMany(nd => nd.Orders) nếu có danh sách Order trong NguoiDung
                .HasForeignKey(o => o.MaNguoiDung)
                .OnDelete(DeleteBehavior.Restrict); // tránh xóa cascade

            // Khóa chính cho Order
            modelBuilder.Entity<Order>()
                .HasKey(o => o.OrderId);

            // Khóa chính cho OrderDetail
            modelBuilder.Entity<OrderDetail>()
                .HasKey(od => od.OrderDetailId);

            // Quan hệ 1 - nhiều: Order - OrderDetail
            modelBuilder.Entity<OrderDetail>()
                .HasOne(od => od.Order)
                .WithMany(o => o.OrderDetails)
                .HasForeignKey(od => od.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            // Quan hệ 1 - nhiều: Sach - OrderDetail
            modelBuilder.Entity<OrderDetail>()
                .HasOne(od => od.Sach)
                .WithMany() // hoặc .WithMany(s => s.OrderDetails) nếu có
                .HasForeignKey(od => od.MaSach)
                .OnDelete(DeleteBehavior.Restrict);

            // Quan hệ cho ChatMessage
            modelBuilder.Entity<ChatMessage>()
                .HasKey(cm => cm.Id);

            modelBuilder.Entity<ChatMessage>()
                .HasOne(cm => cm.NguoiDung)
                .WithMany()
                .HasForeignKey(cm => cm.MaNguoiDung)
                .OnDelete(DeleteBehavior.SetNull);

            // Quan hệ cho Payment
            modelBuilder.Entity<Payment>()
                .HasKey(p => p.PaymentId);

            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Order)
                .WithMany()
                .HasForeignKey(p => p.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
