using InventoryManagement.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Core.Data;

public class InventoryDbContext : DbContext
{
    public InventoryDbContext(DbContextOptions<InventoryDbContext> options) : base(options)
    {
    }

    public DbSet<Users> Users { get; set; }
    public DbSet<UsersInRole> UsersInRoles { get; set; }
    public DbSet<Company> Companies { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<Inward> Inwards { get; set; }
    public DbSet<InwardItem> InwardItems { get; set; }
    public DbSet<InwardBarcodeItem> InwardBarcodeItems { get; set; }
    public DbSet<Outward> Outwards { get; set; }
    public DbSet<OutwardDetail> OutwardDetails { get; set; }
    public DbSet<SaleReturn> SaleReturns { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

    
        // Configure relationships
        modelBuilder.Entity<UsersInRole>()
            .HasOne(uir => uir.User)
            .WithMany()
            .HasForeignKey(uir => uir.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<UsersInRole>()
            .HasOne(uir => uir.Company)
            .WithMany()
            .HasForeignKey(uir => uir.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Company>()
            .HasIndex(c => c.Code)
            .IsUnique();

        modelBuilder.Entity<Company>()
            .Property(c => c.CompanyType)
            .HasConversion<int>();

        modelBuilder.Entity<Category>()
            .HasOne(c => c.ParentCategory)
            .WithMany(c => c.SubCategories)
            .HasForeignKey(c => c.ParentCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Product>()
            .HasOne(p => p.Category)
            .WithMany(c => c.Products)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Inward>()
            .HasOne(i => i.ShipMentCompany)
            .WithMany()
            .HasForeignKey(i => i.ShipMentCompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<InwardItem>()
            .HasOne(ii => ii.Inward)
            .WithMany(i => i.InwardItems)
            .HasForeignKey(ii => ii.InwardId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<InwardItem>()
            .HasOne(ii => ii.Product)
            .WithMany(p => p.InwardItems)
            .HasForeignKey(ii => ii.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<InwardBarcodeItem>()
            .HasOne(b => b.Inward)
            .WithMany(i => i.InwardBarcodeItems)
            .HasForeignKey(b => b.InwardId)
            .OnDelete(DeleteBehavior.Restrict); // or DeleteBehavior.NoAction

        modelBuilder.Entity<InwardBarcodeItem>()
            .HasOne(b => b.InwardItem)
            .WithMany(i => i.InwardBarcodeItems)
            .HasForeignKey(b => b.InwardItemId)
            .OnDelete(DeleteBehavior.Cascade); // keep cascade here


        modelBuilder.Entity<Outward>()
            .HasOne(o => o.BillToCompany)
            .WithMany()
            .HasForeignKey(o => o.BillToCompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<OutwardDetail>()
            .HasOne(od => od.Outward)
            .WithMany(o => o.OutwardDetails)
            .HasForeignKey(od => od.OutwardId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<OutwardDetail>()
            .HasOne(od => od.InwardBarcodeItem)
            .WithMany(ibi => ibi.OutwardDetails)
            .HasForeignKey(od => od.InwardBarcodeItemId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<SaleReturn>()
            .HasOne(sr => sr.BillToCompany)
            .WithMany()
            .HasForeignKey(sr => sr.BillToCompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        // Configure decimal precision
        modelBuilder.Entity<InwardItem>()
            .Property(ii => ii.Quantity)
            .HasPrecision(18, 2);

        // Configure indexes
        modelBuilder.Entity<Product>()
            .HasIndex(p => p.SKU)
            .IsUnique();

        modelBuilder.Entity<InwardBarcodeItem>()
            .HasIndex(ibi => ibi.BarcodeNo)
            .IsUnique();

        modelBuilder.Entity<OutwardDetail>()
            .HasIndex(od => od.BarcodeNo);
    }
}
