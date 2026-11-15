using EasyShop.Web.Functional;
using EasyShop.Web.Models;
using Xunit;
namespace EasyShop.Tests;

public class ShopRulesTests
{
    private static Product Sample() => new() { Id = 7, Name = "Mouse", Price = 2500m, Stock = 3, CategoryId = 1 };
    [Fact] public void AddIncreasesQuantityWithoutChangingPreviousList() { IReadOnlyList<CartLine> initial = []; var one = ShopRules.Add(initial, Sample()); var two = ShopRules.Add(one, Sample()); Assert.Empty(initial); Assert.Single(one); Assert.Equal(2, two[0].Quantity); }
    [Fact] public void AddDoesNotExceedStock() { IReadOnlyList<CartLine> initial = []; var added = ShopRules.Add(initial, Sample(), 30); Assert.Equal(3, added[0].Quantity); }
    [Fact] public void TotalMultipliesPriceByQuantity() { Assert.Equal(7500m, ShopRules.Total([new CartLine(1, "Mouse", 2500, 3, "")])); }
    [Fact] public void ZeroQuantityRemovesProduct() { var items = new[] { new CartLine(7, "Mouse", 2500, 1, "") }; Assert.Empty(ShopRules.SetQuantity(items, 7, 0, 3)); }
    [Fact] public void CannotAddSoldOutProduct() { var p = Sample(); p.Stock = 0; Assert.Empty(ShopRules.Add([], p)); }
    [Fact] public void SearchCategoryStockAndSort() { var p = new[] { Sample(), new Product { Id = 8, Name = "Monitor", Price = 9000, Stock = 0, CategoryId = 1 }, new Product { Id = 9, Name = "Mouse Pad", Price = 900, Stock = 10, CategoryId = 2 } }; var result = ShopRules.Filter(p, "mouse", 1, "low", true).ToList(); Assert.Single(result); Assert.Equal(7, result[0].Id); }
    [Theory][InlineData("Pending", "Processing", true)][InlineData("Pending", "Completed", false)][InlineData("Processing", "Completed", true)][InlineData("Completed", "Cancelled", false)] public void StatusRules(string from, string to, bool expected) => Assert.Equal(expected, ShopRules.CanChangeStatus(from, to));

    [Fact] public void LargeAdditionDoesNotOverflow() { var p = Sample(); var added = ShopRules.Add(ShopRules.Add([], p), p, int.MaxValue); Assert.Equal(3, added[0].Quantity); }
    [Theory][InlineData(-1)][InlineData(0)] public void NonPositiveAddIsIgnored(int count) => Assert.Empty(ShopRules.Add([], Sample(), count));
    [Fact] public void QuantityIsClampedToStock() { var lines = ShopRules.Add([], Sample(), 3); Assert.Equal(2, ShopRules.SetQuantity(lines, 7, 10, 2)[0].Quantity); Assert.Empty(ShopRules.SetQuantity(lines, 7, 10, 0)); }
    [Theory][InlineData("low", 9)][InlineData("high", 8)] public void FilterSortsByPrice(string sort, int first) { var items = new[] { Sample(), new Product { Id = 8, Name = "Monitor", Price = 9000 }, new Product { Id = 9, Name = "Pen", Price = 100 } }; Assert.Equal(first, ShopRules.Filter(items, null, null, sort, false).First().Id); }
    [Theory][InlineData("Pending", "Cancelled", true)][InlineData("Processing", "Cancelled", true)][InlineData("Cancelled", "Pending", false)][InlineData("Pending", "Pending", false)][InlineData("Processing", "Pending", false)] public void AdditionalStatusRules(string from, string to, bool expected) => Assert.Equal(expected, ShopRules.CanChangeStatus(from, to));
}
