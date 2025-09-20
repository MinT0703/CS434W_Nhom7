CREATE DATABASE fashion_store;
USE fashion_store;

/* =============================
   ROLES
============================= */
CREATE TABLE dbo.roles (
    VaiTroId   INT IDENTITY(1,1) PRIMARY KEY,
    TenVaiTro  NVARCHAR(50) NOT NULL    -- 'Khách hàng', 'Nhân viên', 'Admin'
);

/* =============================
   USERS
============================= */
CREATE TABLE dbo.users (
    NguoiDungId   INT IDENTITY(1,1) PRIMARY KEY,
    Email         VARCHAR(255) NOT NULL,
    MatKhauHash   VARCHAR(255) NOT NULL,
    HoTen         NVARCHAR(255) NULL,
    SoDienThoai   VARCHAR(30)  NULL,
    DiaChi        NVARCHAR(500) NULL,
    NgayTao       DATETIME2(0) NOT NULL CONSTRAINT DF_users_NgayTao DEFAULT SYSUTCDATETIME(),
    VaiTroId      INT NOT NULL,
    CONSTRAINT FK_users_roles FOREIGN KEY (VaiTroId) REFERENCES dbo.roles(VaiTroId),
    CONSTRAINT UQ_users_email UNIQUE (Email)
);

/* =============================
   CATEGORIES  (tạo KHÔNG kèm FK cascade)
============================= */
CREATE TABLE dbo.categories (
    DanhMucId     INT IDENTITY(1,1) PRIMARY KEY,
    DanhMucChaId  INT NULL,
    Ten           NVARCHAR(255) NOT NULL,
    DuongDan      VARCHAR(255) NOT NULL
);
GO

/* Thêm FK tự tham chiếu nhưng KHÔNG cascade để tránh multiple cascade paths */
ALTER TABLE dbo.categories
ADD CONSTRAINT FK_categories_parent
FOREIGN KEY (DanhMucChaId) REFERENCES dbo.categories(DanhMucId)
ON DELETE NO ACTION
ON UPDATE NO ACTION;
GO

/* Trigger mô phỏng hành vi ON DELETE SET NULL cho self-reference */
CREATE OR ALTER TRIGGER dbo.trg_categories_delete
ON dbo.categories
INSTEAD OF DELETE
AS
BEGIN
    SET NOCOUNT ON;

    -- Set NULL phần con trỏ về danh mục cha đang bị xóa
    UPDATE c
      SET DanhMucChaId = NULL
    FROM dbo.categories AS c
    INNER JOIN deleted AS d
        ON c.DanhMucChaId = d.DanhMucId;

    -- Sau đó xóa chính các danh mục cha
    DELETE c
    FROM dbo.categories AS c
    INNER JOIN deleted AS d
        ON c.DanhMucId = d.DanhMucId;
END
GO

/* =============================
   PRODUCTS
============================= */
CREATE TABLE dbo.products (
    SanPhamId     INT IDENTITY(1,1) PRIMARY KEY,
    DanhMucId     INT NOT NULL,
    Ten           NVARCHAR(255) NOT NULL,
    ThuongHieu    NVARCHAR(100) NULL,
    MoTa          NVARCHAR(1000) NULL,
    GiaGoc        DECIMAL(12,2) NOT NULL,
    DangBan       BIT NOT NULL CONSTRAINT DF_products_DangBan DEFAULT (1),
    NgayTao       DATETIME2(0) NOT NULL CONSTRAINT DF_products_NgayTao DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_products_categories FOREIGN KEY (DanhMucId) REFERENCES dbo.categories(DanhMucId)
);

/* =============================
   PRODUCT VARIANTS
============================= */
CREATE TABLE dbo.product_variants (
    BienTheId     INT IDENTITY(1,1) PRIMARY KEY,
    SanPhamId     INT NOT NULL,
    MauSac        NVARCHAR(100) NULL,
    KichCo        NVARCHAR(50) NULL,
    SKU           VARCHAR(100) NOT NULL,
    Gia           DECIMAL(12,2) NOT NULL,
    SoLuongTon    INT NOT NULL CONSTRAINT DF_variants_SoLuongTon DEFAULT (0),
    CONSTRAINT FK_variants_products FOREIGN KEY (SanPhamId) REFERENCES dbo.products(SanPhamId),
    CONSTRAINT UQ_variants_sku UNIQUE (SKU)
);

/* =============================
   PRODUCT IMAGES
============================= */
CREATE TABLE dbo.product_images (
    HinhAnhId     INT IDENTITY(1,1) PRIMARY KEY,
    SanPhamId     INT NOT NULL,
    BienTheId     INT NULL,
    DuongDan      VARCHAR(1024) NOT NULL,
    LaAnhChinh    BIT NOT NULL CONSTRAINT DF_images_LaAnhChinh DEFAULT (0),
    CONSTRAINT FK_images_products FOREIGN KEY (SanPhamId) REFERENCES dbo.products(SanPhamId),
    CONSTRAINT FK_images_variants FOREIGN KEY (BienTheId) REFERENCES dbo.product_variants(BienTheId)
);

/* =============================
   SHOPPING CARTS  (dùng GUID)
============================= */
CREATE TABLE dbo.shopping_carts (
    GioHangId   UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_carts_GioHangId DEFAULT NEWID() PRIMARY KEY,
    NguoiDungId INT NOT NULL,
    NgayTao     DATETIME2(0) NOT NULL CONSTRAINT DF_carts_NgayTao DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_carts_users FOREIGN KEY (NguoiDungId) REFERENCES dbo.users(NguoiDungId),
    CONSTRAINT UQ_cart_user UNIQUE (NguoiDungId)
);

/* =============================
   CART ITEMS
============================= */
CREATE TABLE dbo.cart_items (
    ChiTietGioHangId INT IDENTITY(1,1) PRIMARY KEY,
    GioHangId        UNIQUEIDENTIFIER NOT NULL,
    BienTheId        INT NOT NULL,
    SoLuong          INT NOT NULL CONSTRAINT DF_cartitems_SoLuong DEFAULT (1),
    DonGia           DECIMAL(12,2) NOT NULL,
    CONSTRAINT FK_cartitems_carts FOREIGN KEY (GioHangId) REFERENCES dbo.shopping_carts(GioHangId),
    CONSTRAINT FK_cartitems_variants FOREIGN KEY (BienTheId) REFERENCES dbo.product_variants(BienTheId),
    CONSTRAINT UQ_cart_variant UNIQUE (GioHangId, BienTheId)
);

/* =============================
   COUPONS
============================= */
CREATE TABLE dbo.coupons (
    PhieuGiamGiaId INT IDENTITY(1,1) PRIMARY KEY,
    MaCode     VARCHAR(100) NOT NULL,
    Kieu       INT NOT NULL,                 -- 0: số tiền, 1: phần trăm
    GiaTri     DECIMAL(12,2) NOT NULL,
    HetHanLuc  DATETIME2(0) NULL,
    CONSTRAINT UQ_coupons_code UNIQUE (MaCode)
);

/* =============================
   ORDERS
============================= */
CREATE TABLE dbo.orders (
    DonHangId      INT IDENTITY(1,1) PRIMARY KEY,
    NguoiDungId    INT NOT NULL,
    PhieuGiamGiaId INT NULL,
    TrangThai      INT NOT NULL CONSTRAINT DF_orders_TrangThai DEFAULT (0),  -- 0:Mới,1:ĐãTT,2:ĐangGiao,3:Hoàn tất,4:Hủy
    TamTinh        DECIMAL(12,2) NOT NULL CONSTRAINT DF_orders_TamTinh DEFAULT (0),
    GiamGia        DECIMAL(12,2) NOT NULL CONSTRAINT DF_orders_GiamGia DEFAULT (0),
    PhiVanChuyen   DECIMAL(12,2) NOT NULL CONSTRAINT DF_orders_PhiVC DEFAULT (0),
    TongThanhToan  DECIMAL(12,2) NOT NULL CONSTRAINT DF_orders_Tong DEFAULT (0),
    TenNhan        NVARCHAR(255) NULL,
    SDTNhan        VARCHAR(30) NULL,
    DiaChiNhan     NVARCHAR(500) NULL,
    NgayTao        DATETIME2(0) NOT NULL CONSTRAINT DF_orders_NgayTao DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_orders_users    FOREIGN KEY (NguoiDungId)    REFERENCES dbo.users(NguoiDungId),
    CONSTRAINT FK_orders_coupons  FOREIGN KEY (PhieuGiamGiaId) REFERENCES dbo.coupons(PhieuGiamGiaId)
);

/* =============================
   ORDER ITEMS
============================= */
CREATE TABLE dbo.order_items (
    ChiTietDonHangId INT IDENTITY(1,1) PRIMARY KEY,
    DonHangId        INT NOT NULL,
    BienTheId        INT NOT NULL,
    TenSanPham       NVARCHAR(255) NOT NULL,
    ThuocTinh        NVARCHAR(255) NULL,
    SoLuong          INT NOT NULL,
    DonGia           DECIMAL(12,2) NOT NULL,
    ThanhTien        DECIMAL(12,2) NOT NULL,
    CONSTRAINT FK_orderitems_orders   FOREIGN KEY (DonHangId) REFERENCES dbo.orders(DonHangId),
    CONSTRAINT FK_orderitems_variants FOREIGN KEY (BienTheId) REFERENCES dbo.product_variants(BienTheId)
);

/* =============================
   PAYMENTS
============================= */
CREATE TABLE dbo.payments (
    ThanhToanId INT IDENTITY(1,1) PRIMARY KEY,
    DonHangId   INT NOT NULL,
    PhuongThuc  NVARCHAR(100) NOT NULL,
    SoTien      DECIMAL(12,2) NOT NULL,
    TrangThai   INT NOT NULL CONSTRAINT DF_payments_TrangThai DEFAULT (0), -- 0:Chờ xử lý,1:Thành công,2:Thất bại
    NgayTao     DATETIME2(0) NOT NULL CONSTRAINT DF_payments_NgayTao DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_payments_orders FOREIGN KEY (DonHangId) REFERENCES dbo.orders(DonHangId)
);

/* =============================
   WISHLISTS
============================= */
CREATE TABLE dbo.wishlists (
    YeuThichId  INT IDENTITY(1,1) PRIMARY KEY,
    NguoiDungId INT NOT NULL,
    SanPhamId   INT NOT NULL,
    NgayThem    DATETIME2(0) NOT NULL CONSTRAINT DF_wishlists_NgayThem DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_wishlist_users    FOREIGN KEY (NguoiDungId) REFERENCES dbo.users(NguoiDungId),
    CONSTRAINT FK_wishlist_products FOREIGN KEY (SanPhamId) REFERENCES dbo.products(SanPhamId),
    CONSTRAINT UQ_wishlist_user_product UNIQUE (NguoiDungId, SanPhamId)
);
GO
