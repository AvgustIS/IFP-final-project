using EasyShop.Web.Data;
using EasyShop.Web.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;
namespace EasyShop.Tests;

public class SqliteIntegrationTests
{
    [Fact]
    public async Task DbStoresProductsAndRejectsDuplicateEmails()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:"); await connection.OpenAsync(); var options = new DbContextOptionsBuilder<ShopDbContext>().UseSqlite(connection).Options;
        await using var db = new ShopDbContext(options); await db.Database.EnsureCreatedAsync(); db.Categories.Add(new Category { Name = "Electronics" }); await db.SaveChangesAsync(); db.Products.Add(new Product { Name = "Mouse", Price = 2000, Stock = 3, CategoryId = 1 }); db.Users.Add(new ShopUser { Email = "test@example.com", Name = "Test", PasswordHash = "hash" }); await db.SaveChangesAsync();
        Assert.Equal(1, await db.Products.CountAsync()); db.Users.Add(new ShopUser { Email = "test@example.com", Name = "Other", PasswordHash = "hash" }); await Assert.ThrowsAsync<DbUpdateException>(async () => await db.SaveChangesAsync());
    }
    [Fact]
    public async Task ConditionalStockDecrementCannotOversell()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:"); await connection.OpenAsync(); var options = new DbContextOptionsBuilder<ShopDbContext>().UseSqlite(connection).Options;
        await using var db = new ShopDbContext(options); await db.Database.EnsureCreatedAsync(); db.Categories.Add(new Category { Name = "Electronics" }); await db.SaveChangesAsync(); db.Products.Add(new Product { Name = "Mouse", Price = 2500, Stock = 1, CategoryId = 1 }); await db.SaveChangesAsync();
        var first = await db.Products.Where(x => x.Id == 1 && x.Stock >= 1).ExecuteUpdateAsync(s => s.SetProperty(x => x.Stock, x => x.Stock - 1)); var second = await db.Products.Where(x => x.Id == 1 && x.Stock >= 1).ExecuteUpdateAsync(s => s.SetProperty(x => x.Stock, x => x.Stock - 1)); Assert.Equal(1, first); Assert.Equal(0, second);
    }
}
