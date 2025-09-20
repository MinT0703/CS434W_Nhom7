using Microsoft.EntityFrameworkCore;

namespace FashionStore.Api.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> opt) : base(opt) {}

        public DbSet<User> Users => Set<User>();
        public DbSet<Role> Roles => Set<Role>();
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<Product> Products => Set<Product>();
        public DbSet<ProductVariant> Variants => Set<ProductVariant>();
        public DbSet<ProductImage> Images => Set<ProductImage>();
        public DbSet<Coupon> Coupons => Set<Coupon>();
        public DbSet<Order> Orders => Set<Order>();
        public DbSet<OrderItem> OrderItems => Set<OrderItem>();
        public DbSet<ShoppingCart> Carts => Set<ShoppingCart>();
        public DbSet<CartItem> CartItems => Set<CartItem>();

        protected override void OnModelCreating(ModelBuilder mb)
        {
            // roles
            mb.Entity<Role>().ToTable("roles", "dbo");
            mb.Entity<Role>().HasKey(x => x.VaiTroId);

            // users
            mb.Entity<User>().ToTable("users", "dbo");
            mb.Entity<User>().HasKey(x => x.NguoiDungId);
            mb.Entity<User>()
              .HasOne<Role>().WithMany()
              .HasForeignKey(x => x.VaiTroId);

            // categories
            mb.Entity<Category>().ToTable("categories", "dbo");
            mb.Entity<Category>().HasKey(x => x.DanhMucId);
            mb.Entity<Category>()
              .HasOne<Category>()
              .WithMany()
              .HasForeignKey(x => x.DanhMucChaId)
              .OnDelete(DeleteBehavior.NoAction);

            // products
            mb.Entity<Product>().ToTable("products", "dbo");
            mb.Entity<Product>().HasKey(x => x.SanPhamId);
            mb.Entity<Product>()
              .HasOne<Category>().WithMany()
              .HasForeignKey(x => x.DanhMucId);

            // variants
            mb.Entity<ProductVariant>().ToTable("product_variants", "dbo");
            mb.Entity<ProductVariant>().HasKey(x => x.BienTheId);
            mb.Entity<ProductVariant>()
              .HasOne<Product>().WithMany()
              .HasForeignKey(x => x.SanPhamId);

            // images
            mb.Entity<ProductImage>().ToTable("product_images", "dbo");
            mb.Entity<ProductImage>().HasKey(x => x.HinhAnhId);
            mb.Entity<ProductImage>()
              .HasOne<Product>().WithMany()
              .HasForeignKey(x => x.SanPhamId);
            mb.Entity<ProductImage>()
              .HasOne<ProductVariant>().WithMany()
              .HasForeignKey(x => x.BienTheId);

            // coupons
            mb.Entity<Coupon>().ToTable("coupons", "dbo").HasKey(x => x.PhieuGiamGiaId);

            // orders
            mb.Entity<Order>().ToTable("orders", "dbo").HasKey(x => x.DonHangId);
            mb.Entity<Order>()
              .HasOne<User>().WithMany()
              .HasForeignKey(x => x.NguoiDungId);
            mb.Entity<Order>()
              .HasOne<Coupon>().WithMany()
              .HasForeignKey(x => x.PhieuGiamGiaId);

            // order_items
            mb.Entity<OrderItem>().ToTable("order_items", "dbo").HasKey(x => x.ChiTietDonHangId);
            mb.Entity<OrderItem>()
              .HasOne<Order>().WithMany()
              .HasForeignKey(x => x.DonHangId);
            mb.Entity<OrderItem>()
              .HasOne<ProductVariant>().WithMany()
              .HasForeignKey(x => x.BienTheId);

            // carts
            mb.Entity<ShoppingCart>().ToTable("shopping_carts", "dbo").HasKey(x => x.GioHangId);
            mb.Entity<ShoppingCart>()
              .HasOne<User>().WithMany()
              .HasForeignKey(x => x.NguoiDungId);

            // cart_items
            mb.Entity<CartItem>().ToTable("cart_items", "dbo").HasKey(x => x.ChiTietGioHangId);
            mb.Entity<CartItem>()
              .HasOne<ShoppingCart>().WithMany()
              .HasForeignKey(x => x.GioHangId);
            mb.Entity<CartItem>()
              .HasOne<ProductVariant>().WithMany()
              .HasForeignKey(x => x.BienTheId);
        }
    }

    // ====== POCOs (tối thiểu cột cần dùng) ======
    public class Role { public int VaiTroId { get; set; } public string TenVaiTro { get; set; } = ""; }

    public class User {
        public int NguoiDungId { get; set; }
        public string Email { get; set; } = "";
        public string MatKhauHash { get; set; } = "";
        public string? HoTen { get; set; }
        public string? SoDienThoai { get; set; }
        public string? DiaChi { get; set; }
        public DateTime NgayTao { get; set; }
        public int VaiTroId { get; set; }
    }

    public class Category {
        public int DanhMucId { get; set; }
        public int? DanhMucChaId { get; set; }
        public string Ten { get; set; } = "";
        public string DuongDan { get; set; } = ""; // dùng để filter "jeans, accessories, makeup"
    }

    public class Product {
        public int SanPhamId { get; set; }
        public int DanhMucId { get; set; }
        public string Ten { get; set; } = "";
        public string? ThuongHieu { get; set; }
        public string? MoTa { get; set; }
        public decimal GiaGoc { get; set; }
        public bool DangBan { get; set; }
        public DateTime NgayTao { get; set; }
    }

    public class ProductVariant {
        public int BienTheId { get; set; }
        public int SanPhamId { get; set; }
        public string? MauSac { get; set; }
        public string? KichCo { get; set; }
        public string SKU { get; set; } = "";
        public decimal Gia { get; set; }
        public int SoLuongTon { get; set; }
    }

    public class ProductImage {
        public int HinhAnhId { get; set; }
        public int SanPhamId { get; set; }
        public int? BienTheId { get; set; }
        public string DuongDan { get; set; } = "";
        public bool LaAnhChinh { get; set; }
    }

    public class Coupon {
        public int PhieuGiamGiaId { get; set; }
        public string MaCode { get; set; } = "";
        public int Kieu { get; set; } // 0: amount, 1: percent
        public decimal GiaTri { get; set; }
        public DateTime? HetHanLuc { get; set; }
    }

    public class Order {
        public int DonHangId { get; set; }
        public int NguoiDungId { get; set; }
        public int? PhieuGiamGiaId { get; set; }
        public int TrangThai { get; set; }
        public decimal TamTinh { get; set; }
        public decimal GiamGia { get; set; }
        public decimal PhiVanChuyen { get; set; }
        public decimal TongThanhToan { get; set; }
        public string? TenNhan { get; set; }
        public string? SDTNhan { get; set; }
        public string? DiaChiNhan { get; set; }
        public DateTime NgayTao { get; set; }
    }

    public class OrderItem {
        public int ChiTietDonHangId { get; set; }
        public int DonHangId { get; set; }
        public int BienTheId { get; set; }
        public string TenSanPham { get; set; } = "";
        public string? ThuocTinh { get; set; }
        public int SoLuong { get; set; }
        public decimal DonGia { get; set; }
        public decimal ThanhTien { get; set; }
    }

    public class ShoppingCart {
        public Guid GioHangId { get; set; }
        public int NguoiDungId { get; set; }
        public DateTime NgayTao { get; set; }
    }

    public class CartItem {
        public int ChiTietGioHangId { get; set; }
        public Guid GioHangId { get; set; }
        public int BienTheId { get; set; }
        public int SoLuong { get; set; }
        public decimal DonGia { get; set; }
    }
}
