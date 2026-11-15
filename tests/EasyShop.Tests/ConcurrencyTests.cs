using EasyShop.Web.Data;
using EasyShop.Web.Models;
using EasyShop.Web.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
namespace EasyShop.Tests;

public sealed class ConcurrencyTests : IAsyncLifetime
{
    private readonly string file = Path.Combine(Path.GetTempPath(), $"easyshop-concurrency-{Guid.NewGuid():N}.db");
    private PooledDbContextFactory<ShopDbContext> factory = null!;
    private ShopService shop = null!;
    private static readonly CheckoutDetails Delivery = new("Test", "123456789", "Test address", "Demo Card");
    public async Task InitializeAsync()
    {
        factory = new(new DbContextOptionsBuilder<ShopDbContext>().UseSqlite($"Data Source={file};Default Timeout=10").Options); shop = new(factory);
        await using var db = await factory.CreateDbContextAsync(); await db.Database.EnsureCreatedAsync();
        db.Categories.Add(new Category { Name = "Test" }); db.Users.Add(new ShopUser { Name = "Test", Email = "test@example.com" }); await db.SaveChangesAsync();
        db.Products.Add(new Product { Name = "Last unit", Stock = 1, Price = 1234.56m, CategoryId = 1 }); await db.SaveChangesAsync();
    }
    public Task DisposeAsync() { Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools(); foreach (var suffix in new[] { "", "-wal", "-shm" }) if (File.Exists(file + suffix)) File.Delete(file + suffix); return Task.CompletedTask; }
    [Fact]
    public async Task TwoConcurrentCheckoutsCannotBuySameLastUnit()
    {
        var cart = new[] { new CartLine(1, "Ignored", 0, 1, "") };
        var results = await Task.WhenAll(Task.Run(() => shop.CheckoutAsync(1, cart, Delivery)), Task.Run(() => shop.CheckoutAsync(1, cart, Delivery)));
        Assert.Single(results, x => x.Success); Assert.Single(await shop.OrdersAsync()); Assert.Equal(0, (await shop.ProductAsync(1))!.Stock);
    }
    [Fact]
    public async Task TwoConcurrentCancellationsReturnStockOnlyOnce()
    {
        var order = await shop.CheckoutAsync(1, [new CartLine(1, "Ignored", 0, 1, "")], Delivery); int id = order.Id!.Value;
        var results = await Task.WhenAll(Task.Run(() => shop.ChangeStatusAsync(id, "Cancelled")), Task.Run(() => shop.ChangeStatusAsync(id, "Cancelled")));
        Assert.Single(results, x => x.Success); Assert.Equal(1, (await shop.ProductAsync(1))!.Stock);
    }
    [Fact]
    public async Task OrdersSurviveNewDatabaseContextFactory()
    {
        await shop.CheckoutAsync(1, [new CartLine(1, "Ignored", 0, 1, "")], Delivery);
        var reopened = new ShopService(new PooledDbContextFactory<ShopDbContext>(new DbContextOptionsBuilder<ShopDbContext>().UseSqlite($"Data Source={file}").Options));
        Assert.Equal(1234.56m, Assert.Single(await reopened.OrdersAsync(1)).Total); Assert.Equal(0, (await reopened.ProductAsync(1))!.Stock);
    }
    [Fact]
    public async Task StartupDoesNotRestoreDeletedInventoryOrAssumeFourExistingCategories()
    {
        await shop.DeleteProductAsync(1);
        var services = new ServiceCollection(); services.AddDbContextFactory<ShopDbContext>(o => o.UseSqlite($"Data Source={file}"));
        await using var provider = services.BuildServiceProvider(); await SeedData.InitializeAsync(provider); await SeedData.InitializeAsync(provider);
        Assert.Empty(await shop.ProductsAsync()); Assert.Single(await shop.CategoriesAsync());
    }
}
