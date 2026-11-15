using EasyShop.Web.Models;
using Microsoft.EntityFrameworkCore;
namespace EasyShop.Web.Data;

public class ShopDbContext(DbContextOptions<ShopDbContext> options) : DbContext(options)
{
    public DbSet<ShopUser> Users => Set<ShopUser>(); public DbSet<Category> Categories => Set<Category>(); public DbSet<Product> Products => Set<Product>(); public DbSet<ShopOrder> Orders => Set<ShopOrder>(); public DbSet<OrderLine> OrderLines => Set<OrderLine>();
    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<ShopUser>().HasIndex(x => x.Email).IsUnique(); b.Entity<Category>().HasIndex(x => x.Name).IsUnique();
        b.Entity<Product>().Property(x => x.Price).HasConversion<double>(); b.Entity<ShopOrder>().Property(x => x.Total).HasConversion<double>(); b.Entity<OrderLine>().Property(x => x.UnitPrice).HasConversion<double>();
        b.Entity<ShopOrder>().HasMany(x => x.Items).WithOne(x => x.ShopOrder).HasForeignKey(x => x.ShopOrderId);
        b.Entity<ShopOrder>().HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
    }
}
