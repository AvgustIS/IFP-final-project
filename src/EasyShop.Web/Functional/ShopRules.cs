using EasyShop.Web.Models;
namespace EasyShop.Web.Functional;

public static class ShopRules
{
    public static decimal Total(IEnumerable<CartLine> lines) => lines.Sum(x => x.UnitPrice * x.Quantity);
    public static IReadOnlyList<CartLine> Add(IReadOnlyList<CartLine> lines, Product product, int count = 1)
    {
        if (count <= 0 || product.Stock <= 0) return lines.ToArray();
        var previous = lines.FirstOrDefault(x => x.ProductId == product.Id);
        var quantity = Math.Min(product.Stock, (int)Math.Min(int.MaxValue, (long)(previous?.Quantity ?? 0) + count));
        return lines.Where(x => x.ProductId != product.Id).Append(new CartLine(product.Id, product.Name, product.Price, quantity, product.ImageUrl)).ToArray();
    }
    public static IReadOnlyList<CartLine> SetQuantity(IReadOnlyList<CartLine> lines, int id, int quantity, int stock) =>
     quantity <= 0 ? lines.Where(x => x.ProductId != id).ToArray() : lines.Select(x => x.ProductId == id ? x with { Quantity = Math.Min(quantity, stock) } : x).Where(x => x.Quantity > 0).ToArray();
    public static IEnumerable<Product> Filter(IEnumerable<Product> source, string? search, int? category, string sort, bool onlyStock)
    {
        var query = source.Where(x => (string.IsNullOrWhiteSpace(search) || x.Name.Contains(search, StringComparison.OrdinalIgnoreCase)) && (!category.HasValue || x.CategoryId == category.Value) && (!onlyStock || x.Stock > 0));
        return sort switch { "low" => query.OrderBy(x => x.Price), "high" => query.OrderByDescending(x => x.Price), _ => query.OrderBy(x => x.Id) };
    }
    public static bool CanChangeStatus(string current, string next) => (current, next) switch
    { ("Pending", "Processing") => true, ("Pending", "Cancelled") => true, ("Processing", "Completed") => true, ("Processing", "Cancelled") => true, _ => false };
}
