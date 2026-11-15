using EasyShop.Web.Data;
using EasyShop.Web.Models;
using EasyShop.Web.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;
namespace EasyShop.Tests;

public sealed class ShopServiceTests : IAsyncLifetime
{
    private readonly SqliteConnection connection = new("Data Source=:memory:");
    private DbContextOptions<ShopDbContext> options = null!;
    private ShopService shop = null!;
    private static readonly CheckoutDetails Details = new("Customer", "+77001234567", "Astana, Street 1", "Cash on Delivery");
    private static CartLine Line(int id = 1, int quantity = 1) => new(id, "Untrusted name", 1, quantity, "");
    private sealed class Factory(DbContextOptions<ShopDbContext> options) : IDbContextFactory<ShopDbContext> { public ShopDbContext CreateDbContext() => new(options); }
    public async Task InitializeAsync()
    {
        await connection.OpenAsync(); options = new DbContextOptionsBuilder<ShopDbContext>().UseSqlite(connection).Options;
        await using var db = new ShopDbContext(options); await db.Database.EnsureCreatedAsync();
        db.Categories.Add(new Category { Name = "Electronics" });
        db.Users.AddRange(new ShopUser { Email = "one@example.com", Name = "One" }, new ShopUser { Email = "two@example.com", Name = "Two" });
        await db.SaveChangesAsync();
        db.Products.AddRange(new Product { Name = "Mouse", Price = 2500.25m, Stock = 3, CategoryId = 1 }, new Product { Name = "Keyboard", Price = 5000, Stock = 1, CategoryId = 1 });
        await db.SaveChangesAsync(); shop = new(new Factory(options));
    }
    public async Task DisposeAsync() => await connection.DisposeAsync();
    [Fact]
    public async Task CheckoutUsesDatabasePriceAndStoresDeliveryAndSnapshots()
    {
        var result = await shop.CheckoutAsync(1, [Line(quantity: 2)], Details); Assert.True(result.Success, result.Message);
        var order = Assert.Single(await shop.OrdersAsync(1)); Assert.Equal(5000.50m, order.Total); Assert.Equal("Mouse", Assert.Single(order.Items).Name);
        Assert.Equal(Details.Address, order.Address); Assert.Equal(1, (await shop.ProductAsync(1))!.Stock);
        Assert.Empty(await shop.OrdersAsync(2));
    }
    [Fact]
    public async Task FailedCheckoutRollsBackEarlierReservations()
    {
        var result = await shop.CheckoutAsync(1, [Line(), Line(2, 2)], Details); Assert.False(result.Success);
        Assert.Equal(3, (await shop.ProductAsync(1))!.Stock); Assert.Empty(await shop.OrdersAsync());
    }
    [Fact]
    public async Task DeletedProductMakesCheckoutFailWithoutChangingStock()
    {
        Assert.False((await shop.CheckoutAsync(1, [Line(), Line(99)], Details)).Success);
        Assert.Equal(3, (await shop.ProductAsync(1))!.Stock);
    }
    [Theory][InlineData(0)][InlineData(-1)][InlineData(999)] public async Task InvalidCustomerCannotCheckout(int id) => Assert.False((await shop.CheckoutAsync(id, [Line()], Details)).Success);
    [Fact]
    public async Task EmptyDuplicateAndNegativeCartAreRejected()
    {
        Assert.False((await shop.CheckoutAsync(1, [], Details)).Success);
        Assert.False((await shop.CheckoutAsync(1, [Line(), Line()], Details)).Success);
        Assert.False((await shop.CheckoutAsync(1, [Line(quantity: -1)], Details)).Success);
    }
    [Fact]
    public async Task InvalidDeliveryAndPaymentAreRejected()
    {
        foreach (var details in new[] { Details with { Name = " " }, Details with { Address = " " }, Details with { Phone = "" }, Details with { PaymentMethod = "Unknown" }, Details with { Name = new string('x', 121) }, Details with { Phone = new string('1', 36) }, Details with { Address = new string('x', 301) } })
            Assert.False((await shop.CheckoutAsync(1, [Line()], details)).Success);
    }
    [Fact]
    public async Task CancellationReturnsStockExactlyOnce()
    {
        var order = await shop.CheckoutAsync(1, [Line(quantity: 2)], Details);
        Assert.True((await shop.ChangeStatusAsync(order.Id!.Value, "Cancelled")).Success);
        Assert.False((await shop.ChangeStatusAsync(order.Id.Value, "Cancelled")).Success);
        Assert.Equal(3, (await shop.ProductAsync(1))!.Stock);
        Assert.Equal(0m, (await shop.StatsAsync()).Item4);
    }
    [Fact]
    public async Task CompletedOrderCannotBeCancelled()
    {
        var order = await shop.CheckoutAsync(1, [Line()], Details); int id = order.Id!.Value;
        Assert.False((await shop.ChangeStatusAsync(id, "Completed")).Success);
        Assert.True((await shop.ChangeStatusAsync(id, "Processing")).Success);
        Assert.True((await shop.ChangeStatusAsync(id, "Completed")).Success);
        Assert.False((await shop.ChangeStatusAsync(id, "Cancelled")).Success);
        Assert.Equal(2, (await shop.ProductAsync(1))!.Stock);
    }
    [Fact]
    public async Task DeletingProductPreservesOrderHistoryAndCancellation()
    {
        var order = await shop.CheckoutAsync(1, [Line()], Details);
        Assert.True((await shop.DeleteProductAsync(1)).Success);
        Assert.Equal("Mouse", Assert.Single(Assert.Single(await shop.OrdersAsync()).Items).Name);
        Assert.True((await shop.ChangeStatusAsync(order.Id!.Value, "Cancelled")).Success);
    }
    [Fact]
    public async Task ProductCrudAndValidation()
    {
        var draft = new Product { Name = " New ", CategoryId = 1, Price = 123.45m, Stock = 0, ImageUrl = "" };
        Assert.True((await shop.SaveProductAsync(draft)).Success);
        var created = (await shop.ProductsAsync()).Single(x => x.Name == "New"); Assert.Equal("/images/product-1.svg", created.ImageUrl);
        created.Name = "Updated"; created.ImageUrl = ""; Assert.True((await shop.SaveProductAsync(created)).Success);
        Assert.Equal("Updated", (await shop.ProductAsync(created.Id))!.Name);
        Assert.True((await shop.DeleteProductAsync(created.Id)).Success); Assert.Null(await shop.ProductAsync(created.Id));
        foreach (var bad in new[] { new Product { Name = "", CategoryId = 1 }, new Product { Name = "Bad", Price = 0, CategoryId = 1 }, new Product { Name = "Bad", Price = 100000000, CategoryId = 1 }, new Product { Name = "Bad", Price = 1.001m, CategoryId = 1 }, new Product { Name = "Bad", Stock = -1, CategoryId = 1 }, new Product { Name = "Bad", CategoryId = 999 }, new Product { Name = "Bad", CategoryId = 1, ImageUrl = "javascript:alert(1)" } })
            Assert.False((await shop.SaveProductAsync(bad)).Success);
    }
    [Fact]
    public async Task CategoryCrudProtectsProductsAndRejectsDuplicates()
    {
        Assert.False((await shop.DeleteCategoryAsync(1)).Success);
        Assert.True((await shop.SaveCategoryAsync(new Category { Name = " Books " })).Success);
        Assert.False((await shop.SaveCategoryAsync(new Category { Name = "Books" })).Success);
        var category = (await shop.CategoriesAsync()).Single(x => x.Name == "Books"); category.Name = "Reading";
        Assert.True((await shop.SaveCategoryAsync(category)).Success);
        Assert.True((await shop.DeleteCategoryAsync(category.Id)).Success);
        Assert.False((await shop.SaveCategoryAsync(new Category { Name = " " })).Success);
    }
    [Fact]
    public async Task MissingEntitiesReturnFailures()
    {
        Assert.False((await shop.SaveProductAsync(new Product { Id = 99, Name = "Missing", CategoryId = 1 })).Success);
        Assert.False((await shop.SaveCategoryAsync(new Category { Id = 99, Name = "Missing" })).Success);
        Assert.False((await shop.DeleteProductAsync(99)).Success);
        Assert.False((await shop.DeleteCategoryAsync(99)).Success);
        Assert.False((await shop.ChangeStatusAsync(99, "Processing")).Success);
    }
}
