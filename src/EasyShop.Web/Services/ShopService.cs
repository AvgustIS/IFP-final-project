using EasyShop.Web.Data;
using EasyShop.Web.Models;
using EasyShop.Web.Functional;
using Microsoft.EntityFrameworkCore;
namespace EasyShop.Web.Services;

public sealed class ShopService(IDbContextFactory<ShopDbContext> dbFactory)
{
    public async Task<List<Product>> ProductsAsync() { await using var db = await dbFactory.CreateDbContextAsync(); return await db.Products.Include(x => x.Category).AsNoTracking().OrderBy(x => x.Id).ToListAsync(); }
    public async Task<Product?> ProductAsync(int id) { await using var db = await dbFactory.CreateDbContextAsync(); return await db.Products.Include(x => x.Category).AsNoTracking().FirstOrDefaultAsync(x => x.Id == id); }
    public async Task<List<Category>> CategoriesAsync() { await using var db = await dbFactory.CreateDbContextAsync(); return await db.Categories.AsNoTracking().OrderBy(x => x.Name).ToListAsync(); }
    public async Task<List<ShopOrder>> OrdersAsync(int? userId = null) { await using var db = await dbFactory.CreateDbContextAsync(); var q = db.Orders.Include(x => x.Items).AsNoTracking().AsQueryable(); if (userId.HasValue) q = q.Where(x => x.UserId == userId.Value); return await q.OrderByDescending(x => x.CreatedUtc).ToListAsync(); }
    public async Task<(int, int, int, decimal)> StatsAsync() { await using var db = await dbFactory.CreateDbContextAsync(); return (await db.Products.CountAsync(), await db.Users.CountAsync(), await db.Orders.CountAsync(), (await db.Orders.Where(x => x.Status != "Cancelled").Select(x => x.Total).ToListAsync()).Sum()); }
    public async Task<ServiceResult> CheckoutAsync(int userId, IReadOnlyList<CartLine> cart, CheckoutDetails details)
    {
        if (userId <= 0) return ServiceResult.Fail("Please sign in first."); if (cart.Count == 0) return ServiceResult.Fail("Your cart is empty.");
        if (string.IsNullOrWhiteSpace(details.Name) || string.IsNullOrWhiteSpace(details.Address) || string.IsNullOrWhiteSpace(details.Phone)) return ServiceResult.Fail("Complete all delivery fields.");
        if (details.Name.Length > 120 || details.Address.Length > 300 || details.Phone.Length > 35) return ServiceResult.Fail("Delivery details are too long.");
        if (details.PaymentMethod is not ("Cash on Delivery" or "Demo Card")) return ServiceResult.Fail("Invalid payment method.");
        if (cart.Any(x => x.Quantity < 1) || cart.Select(x => x.ProductId).Distinct().Count() != cart.Count) return ServiceResult.Fail("Invalid cart contents.");
        await using var db = await dbFactory.CreateDbContextAsync();
        try
        {
            await using var tx = await db.Database.BeginTransactionAsync();
            if (!await db.Users.AnyAsync(x => x.Id == userId)) return ServiceResult.Fail("Please sign in first.");
            var order = new ShopOrder { UserId = userId, CustomerName = details.Name.Trim(), Phone = details.Phone.Trim(), Address = details.Address.Trim(), PaymentMethod = details.PaymentMethod };
            foreach (var cartLine in cart)
            {
                var product = await db.Products.SingleOrDefaultAsync(x => x.Id == cartLine.ProductId);
                if (product is null) return ServiceResult.Fail("One product no longer exists.");
                // Atomic conditional decrement: two checkouts cannot reserve the same final unit.
                var changed = await db.Products.Where(x => x.Id == product.Id && x.Stock >= cartLine.Quantity)
                  .ExecuteUpdateAsync(s => s.SetProperty(x => x.Stock, x => x.Stock - cartLine.Quantity));
                if (changed != 1) return ServiceResult.Fail($"Not enough stock for {product.Name}.");
                order.Items.Add(new OrderLine { ProductId = product.Id, Name = product.Name, UnitPrice = product.Price, Quantity = cartLine.Quantity });
            }
            order.Total = order.Items.Sum(x => x.Quantity * x.UnitPrice); db.Orders.Add(order); await db.SaveChangesAsync(); await tx.CommitAsync(); return ServiceResult.Ok("Order placed successfully!", order.Id);
        }
        catch (DbUpdateException) { return ServiceResult.Fail("Database error while placing order. Try again."); }
        catch (Microsoft.Data.Sqlite.SqliteException) { return ServiceResult.Fail("The store is busy. Please try again."); }
    }
    public async Task<ServiceResult> SaveProductAsync(Product product)
    {
        if (string.IsNullOrWhiteSpace(product.Name) || product.Name.Length > 140 || product.Price < 1 || product.Price > 99999999 || decimal.Round(product.Price, 2) != product.Price || product.Stock < 0 || product.Stock > 1000000) return ServiceResult.Fail("Enter a valid product name, price and stock.");
        var image = string.IsNullOrWhiteSpace(product.ImageUrl) ? "/images/product-1.svg" : product.ImageUrl.Trim();
        if (!(image.StartsWith("/images/", StringComparison.Ordinal) && !image.Contains("..") && !image.Contains('\\')) && !(Uri.TryCreate(image, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps)) return ServiceResult.Fail("Use a local /images/ path or an HTTPS image URL.");
        await using var db = await dbFactory.CreateDbContextAsync(); if (!await db.Categories.AnyAsync(x => x.Id == product.CategoryId)) return ServiceResult.Fail("Choose a valid category.");
        if (product.Id == 0) db.Products.Add(new Product { Name = product.Name.Trim(), Description = product.Description, Price = product.Price, Stock = product.Stock, CategoryId = product.CategoryId, ImageUrl = image });
        else { var current = await db.Products.FindAsync(product.Id); if (current is null) return ServiceResult.Fail("Product not found."); current.Name = product.Name.Trim(); current.Description = product.Description; current.Price = product.Price; current.Stock = product.Stock; current.CategoryId = product.CategoryId; current.ImageUrl = image; }
        return await SaveAsync(db, "Product saved.");
    }
    public async Task<ServiceResult> DeleteProductAsync(int id) { await using var db = await dbFactory.CreateDbContextAsync(); var p = await db.Products.FindAsync(id); if (p is null) return ServiceResult.Fail("Product not found."); db.Products.Remove(p); return await SaveAsync(db, "Product removed."); }
    public async Task<ServiceResult> SaveCategoryAsync(Category category)
    {
        if (string.IsNullOrWhiteSpace(category.Name) || category.Name.Length > 80) return ServiceResult.Fail("Enter a valid category name."); await using var db = await dbFactory.CreateDbContextAsync(); if (await db.Categories.AnyAsync(x => x.Name == category.Name.Trim() && x.Id != category.Id)) return ServiceResult.Fail("Category already exists.");
        if (category.Id == 0) db.Categories.Add(new Category { Name = category.Name.Trim() }); else { var current = await db.Categories.FindAsync(category.Id); if (current is null) return ServiceResult.Fail("Category not found."); current.Name = category.Name.Trim(); }
        return await SaveAsync(db, "Category saved.");
    }
    public async Task<ServiceResult> DeleteCategoryAsync(int id) { await using var db = await dbFactory.CreateDbContextAsync(); if (await db.Products.AnyAsync(x => x.CategoryId == id)) return ServiceResult.Fail("Move or delete products in this category first."); var category = await db.Categories.FindAsync(id); if (category is null) return ServiceResult.Fail("Category not found."); db.Categories.Remove(category); return await SaveAsync(db, "Category removed."); }
    public async Task<ServiceResult> ChangeStatusAsync(int id, string next)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        try
        {
            await using var tx = await db.Database.BeginTransactionAsync();
            var current = await db.Orders.AsNoTracking().Where(x => x.Id == id).Select(x => x.Status).SingleOrDefaultAsync();
            if (current is null) return ServiceResult.Fail("Order not found.");
            if (!ShopRules.CanChangeStatus(current, next)) return ServiceResult.Fail("Invalid status transition.");
            // Claim the transition before returning stock; a competing cancellation cannot do it twice.
            var changed = await db.Orders.Where(x => x.Id == id && x.Status == current).ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, next));
            if (changed != 1) return ServiceResult.Fail("Order changed. Refresh and try again.");
            if (next == "Cancelled")
                foreach (var item in await db.OrderLines.Where(x => x.ShopOrderId == id).ToListAsync())
                    await db.Products.Where(x => x.Id == item.ProductId).ExecuteUpdateAsync(s => s.SetProperty(x => x.Stock, x => x.Stock + item.Quantity));
            await tx.CommitAsync(); return ServiceResult.Ok("Status updated.");
        }
        catch (Microsoft.Data.Sqlite.SqliteException) { return ServiceResult.Fail("The store is busy. Please try again."); }
    }
    private static async Task<ServiceResult> SaveAsync(ShopDbContext db, string message)
    {
        try { await db.SaveChangesAsync(); return ServiceResult.Ok(message); }
        catch (DbUpdateConcurrencyException) { return ServiceResult.Fail("This record changed. Refresh and try again."); }
        catch (DbUpdateException) { return ServiceResult.Fail("Unable to save. Check for duplicate categories or related records, then refresh and try again."); }
        catch (Microsoft.Data.Sqlite.SqliteException) { return ServiceResult.Fail("The store is busy. Please try again."); }
    }
}
