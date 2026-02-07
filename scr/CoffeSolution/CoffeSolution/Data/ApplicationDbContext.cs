using Microsoft.EntityFrameworkCore;
using CoffeSolution.Models.Entities;

namespace CoffeSolution.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // Core Entities
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Store> Stores => Set<Store>();
    public DbSet<Menu> Menus => Set<Menu>();
    public DbSet<MenuAction> MenuActions => Set<MenuAction>();

    // Junction Tables
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<UserStore> UserStores => Set<UserStore>();
    public DbSet<RoleMenuPermission> RoleMenuPermissions => Set<RoleMenuPermission>();

    // Business Entities
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderDetail> OrderDetails => Set<OrderDetail>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<WarehouseReceipt> WarehouseReceipts => Set<WarehouseReceipt>();
    public DbSet<WarehouseReceiptDetail> WarehouseReceiptDetails => Set<WarehouseReceiptDetail>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ===============================
        // USER RELATIONSHIPS
        // ===============================

        // User - Admin (Self-referencing): User được quản lý bởi Admin
        modelBuilder.Entity<User>()
            .HasOne(u => u.Admin)
            .WithMany(u => u.ManagedUsers)
            .HasForeignKey(u => u.AdminId)
            .OnDelete(DeleteBehavior.Restrict);

        // ===============================
        // JUNCTION TABLES
        // ===============================

        // UserRole: User ↔ Role (Many-to-Many)
        modelBuilder.Entity<UserRole>()
            .HasKey(ur => new { ur.UserId, ur.RoleId });

        modelBuilder.Entity<UserRole>()
            .HasOne(ur => ur.User)
            .WithMany(u => u.UserRoles)
            .HasForeignKey(ur => ur.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UserRole>()
            .HasOne(ur => ur.Role)
            .WithMany(r => r.UserRoles)
            .HasForeignKey(ur => ur.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        // UserStore: User ↔ Store (Many-to-Many)
        modelBuilder.Entity<UserStore>()
            .HasKey(us => new { us.UserId, us.StoreId });

        modelBuilder.Entity<UserStore>()
            .HasOne(us => us.User)
            .WithMany(u => u.UserStores)
            .HasForeignKey(us => us.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UserStore>()
            .HasOne(us => us.Store)
            .WithMany(s => s.UserStores)
            .HasForeignKey(us => us.StoreId)
            .OnDelete(DeleteBehavior.Cascade);

        // ===============================
        // MENU & PERMISSION
        // ===============================

        // Menu - Parent (Self-referencing)
        modelBuilder.Entity<Menu>()
            .HasOne(m => m.Parent)
            .WithMany(m => m.Children)
            .HasForeignKey(m => m.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        // MenuAction - Menu
        modelBuilder.Entity<MenuAction>()
            .HasOne(ma => ma.Menu)
            .WithMany(m => m.MenuActions)
            .HasForeignKey(ma => ma.MenuId)
            .OnDelete(DeleteBehavior.Cascade);

        // RoleMenuPermission: Role ↔ Menu ↔ MenuAction
        modelBuilder.Entity<RoleMenuPermission>()
            .HasOne(rmp => rmp.Role)
            .WithMany(r => r.RoleMenuPermissions)
            .HasForeignKey(rmp => rmp.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<RoleMenuPermission>()
            .HasOne(rmp => rmp.Menu)
            .WithMany(m => m.RoleMenuPermissions)
            .HasForeignKey(rmp => rmp.MenuId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<RoleMenuPermission>()
            .HasOne(rmp => rmp.MenuAction)
            .WithMany(ma => ma.RoleMenuPermissions)
            .HasForeignKey(rmp => rmp.MenuActionId)
            .OnDelete(DeleteBehavior.Restrict);

        // Unique constraint: 1 Role chỉ có 1 quyền trên 1 Menu + 1 Action
        modelBuilder.Entity<RoleMenuPermission>()
            .HasIndex(rmp => new { rmp.RoleId, rmp.MenuId, rmp.MenuActionId })
            .IsUnique();

        // ===============================
        // STORE RELATIONSHIPS
        // ===============================

        // Store - Owner (Admin sở hữu)
        modelBuilder.Entity<Store>()
            .HasOne(s => s.Owner)
            .WithMany()
            .HasForeignKey(s => s.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);

        // ===============================
        // PRODUCT RELATIONSHIPS
        // ===============================

        modelBuilder.Entity<Product>()
            .HasOne(p => p.Store)
            .WithMany(s => s.Products)
            .HasForeignKey(p => p.StoreId)
            .OnDelete(DeleteBehavior.Cascade);

        // ===============================
        // ORDER RELATIONSHIPS
        // ===============================

        modelBuilder.Entity<Order>()
            .HasOne(o => o.Store)
            .WithMany(s => s.Orders)
            .HasForeignKey(o => o.StoreId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Order>()
            .HasOne(o => o.Staff)
            .WithMany(u => u.Orders)
            .HasForeignKey(o => o.StaffId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<OrderDetail>()
            .HasOne(od => od.Order)
            .WithMany(o => o.OrderDetails)
            .HasForeignKey(od => od.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<OrderDetail>()
            .HasOne(od => od.Product)
            .WithMany(p => p.OrderDetails)
            .HasForeignKey(od => od.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        // ===============================
        // WAREHOUSE RELATIONSHIPS
        // ===============================

        modelBuilder.Entity<WarehouseReceipt>()
            .HasOne(wr => wr.Store)
            .WithMany(s => s.WarehouseReceipts)
            .HasForeignKey(wr => wr.StoreId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<WarehouseReceipt>()
            .HasOne(wr => wr.Supplier)
            .WithMany(s => s.WarehouseReceipts)
            .HasForeignKey(wr => wr.SupplierId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<WarehouseReceiptDetail>()
            .HasOne(wrd => wrd.WarehouseReceipt)
            .WithMany(wr => wr.Details)
            .HasForeignKey(wrd => wrd.WarehouseReceiptId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<WarehouseReceiptDetail>()
            .HasOne(wrd => wrd.Product)
            .WithMany(p => p.WarehouseReceiptDetails)
            .HasForeignKey(wrd => wrd.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        // ===============================
        // INDEXES
        // ===============================

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Username)
            .IsUnique();

        modelBuilder.Entity<Role>()
            .HasIndex(r => r.Name)
            .IsUnique();

        modelBuilder.Entity<Menu>()
            .HasIndex(m => m.Code)
            .IsUnique();

        modelBuilder.Entity<Order>()
            .HasIndex(o => o.OrderCode)
            .IsUnique();

        modelBuilder.Entity<WarehouseReceipt>()
            .HasIndex(wr => wr.ReceiptCode)
            .IsUnique();

        // ===============================
        // DECIMAL PRECISION
        // ===============================

        modelBuilder.Entity<Product>()
            .Property(p => p.Price)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Order>()
            .Property(o => o.TotalAmount)
            .HasPrecision(18, 2);

        modelBuilder.Entity<OrderDetail>()
            .Property(od => od.UnitPrice)
            .HasPrecision(18, 2);

        modelBuilder.Entity<OrderDetail>()
            .Property(od => od.Amount)
            .HasPrecision(18, 2);

        modelBuilder.Entity<WarehouseReceipt>()
            .Property(wr => wr.TotalAmount)
            .HasPrecision(18, 2);

        modelBuilder.Entity<WarehouseReceiptDetail>()
            .Property(wrd => wrd.UnitPrice)
            .HasPrecision(18, 2);

        modelBuilder.Entity<WarehouseReceiptDetail>()
            .Property(wrd => wrd.Amount)
            .HasPrecision(18, 2);
    }
}
