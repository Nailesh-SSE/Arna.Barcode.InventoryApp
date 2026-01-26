using InventoryManagement.Core.Entities;
using InventoryManagement.Core.Entities.SP_Entities;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace InventoryManagement.Core.Data;

public class InventoryDbContext : DbContext
{
    public InventoryDbContext(DbContextOptions<InventoryDbContext> options) : base(options)
    {
    }

    public DbSet<Users> Users { get; set; }
    public DbSet<UsersInRole> UsersInRoles { get; set; }
    public DbSet<Roles> Roles { get; set; }             
    public DbSet<FormMaster> FormMasters { get; set; }   
    public DbSet<RoleFormPermission> RoleFormPermissions { get; set; }
    public DbSet<Company> Companies { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<Inward> Inwards { get; set; }
    public DbSet<InwardItem> InwardItems { get; set; }
    public DbSet<InwardBarcodeItem> InwardBarcodeItems { get; set; }
    public DbSet<Outward> Outwards { get; set; }
    public DbSet<OutwardDetail> OutwardDetails { get; set; }
    public DbSet<SaleReturn> SaleReturns { get; set; }
    public DbSet<Colour> Colour { get; set; }
    public DbSet<Platform> Platform { get; set; }
    public DbSet<SaleReturnItems> SaleReturnItems { get; set; }
    public DbSet<ErrorLog> ErrorLog { get; set; }  

    [NotMapped]
    public DbSet<InventoryReportDTO> InventoryReportDTO { get; set; }

    //[NotMapped]
    //public DbSet<BarcodeTrackDTO> BarcodeTrackDTO { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<InventoryReportDTO>().HasNoKey();
        //modelBuilder.Entity<BarcodeTrackDTO>().HasNoKey();

        // Configure relationships
        modelBuilder.Entity<Colour>()
            .HasIndex(c => new { c.Name, c.Code })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

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
       .HasFilter("[IsDeleted] = 0");


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

        modelBuilder.Entity<OutwardDetail>();
        modelBuilder.Entity<SaleReturn>(entity =>
        {
            entity.HasMany(e => e.SaleReturnItems)
                  .WithOne(e => e.SaleReturn)
                  .HasForeignKey(e => e.SaleReturnId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SaleReturn>()
            .HasOne(sr => sr.BillToCompany)
            .WithMany()
            .HasForeignKey(sr => sr.BillToCompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        // Configure decimal precision
        modelBuilder.Entity<InwardItem>()
            .Property(ii => ii.ItemQuantity)
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

        modelBuilder.Entity<Roles>()
            .HasIndex(r => r.Name)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        modelBuilder.Entity<InwardItem>()
    .Property(x => x.BoxQuantity)
    .HasPrecision(18, 4);

    }
}
