using EasyShop.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
namespace EasyShop.Web.Data;

public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope(); var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<ShopDbContext>>(); await using var db = await factory.CreateDbContextAsync(); await db.Database.EnsureCreatedAsync();
        var seedCatalog = !await db.Categories.AnyAsync();
        if (seedCatalog) { db.Categories.AddRange(new Category { Name = "Electronics" }, new Category { Name = "Accessories" }, new Category { Name = "Home" }, new Category { Name = "Stationery" }); await db.SaveChangesAsync(); }
        if (seedCatalog)
        {
            var cats = await db.Categories.OrderBy(x => x.Id).ToArrayAsync();
            var entries = new (string, string, decimal, int, int)[] { ("Wireless Headphones", "Comfortable wireless audio for everyday life.", 16990, 18, 0), ("Bluetooth Speaker", "Compact speaker with rich sound.", 12990, 24, 0), ("USB-C Charger", "Fast charger for your devices.", 6990, 42, 0), ("Smart Desk Lamp", "Adjustable brightness for working and studying.", 11990, 20, 2), ("Classic Backpack", "Everyday backpack with laptop pocket.", 14990, 15, 1), ("Reusable Bottle", "Durable water bottle for travel.", 4990, 32, 1), ("Minimalist Wallet", "Slim pocket wallet.", 7990, 16, 1), ("Ceramic Mug", "Your favorite morning coffee mug.", 3990, 25, 2), ("Desk Organizer", "Keep your desk tidy.", 5990, 22, 2), ("Notebook Set", "Three quality paper notebooks.", 2990, 45, 3), ("Gel Pen Pack", "Smooth-writing pens in multiple colors.", 1990, 75, 3), ("Wireless Mouse", "Reliable mouse for laptops and desktops.", 8990, 27, 0) };
            for (int i = 0; i < entries.Length; i++) { var a = entries[i]; db.Products.Add(new Product { Name = a.Item1, Description = a.Item2, Price = a.Item3, Stock = a.Item4, CategoryId = cats[a.Item5].Id, ImageUrl = $"/images/product-{i + 1}.svg" }); }
            await db.SaveChangesAsync();
        }
        var email = Environment.GetEnvironmentVariable("EASYSHOP_ADMIN_EMAIL")?.Trim().ToLowerInvariant(); var pass = Environment.GetEnvironmentVariable("EASYSHOP_ADMIN_PASSWORD");
        if (!string.IsNullOrWhiteSpace(email) && !string.IsNullOrWhiteSpace(pass) && pass.Length >= 12 && !await db.Users.AnyAsync(x => x.Email == email.ToLower()))
        { var u = new ShopUser { Name = "Administrator", Email = email.ToLower(), Role = "Admin" }; u.PasswordHash = new PasswordHasher<ShopUser>().HashPassword(u, pass); db.Users.Add(u); await db.SaveChangesAsync(); }
    }
}
