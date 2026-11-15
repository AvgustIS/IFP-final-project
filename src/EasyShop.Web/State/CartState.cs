using EasyShop.Web.Functional;
using EasyShop.Web.Models;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
namespace EasyShop.Web.State;

public sealed class CartState(ProtectedSessionStorage storage)
{
    private const string StorageKey = "EasyShop.Cart";
    private IReadOnlyList<CartLine> _items = [];
    private bool initialized;
    private readonly SemaphoreSlim gate = new(1, 1);
    public IReadOnlyList<CartLine> Items => _items;
    public int Count => _items.Sum(x => x.Quantity);
    public decimal Total => ShopRules.Total(_items);
    public event Action? Changed;
    public async Task InitializeAsync()
    {
        await gate.WaitAsync();
        try
        {
            if (initialized) return;
            try
            {
                var saved = await storage.GetAsync<CartLine[]>(StorageKey);
                if (saved.Success && saved.Value is not null) _items = saved.Value;
            }
            catch (System.Security.Cryptography.CryptographicException) { await storage.DeleteAsync(StorageKey); }
            catch (System.Text.Json.JsonException) { await storage.DeleteAsync(StorageKey); }
            initialized = true; Changed?.Invoke();
        }
        finally { gate.Release(); }
    }
    private async Task UpdateAsync(Func<IReadOnlyList<CartLine>, IReadOnlyList<CartLine>> update)
    {
        await InitializeAsync();
        await gate.WaitAsync();
        try
        {
            var items = update(_items);
            // Persist before publishing so a full-page login redirect cannot drop the latest update.
            await storage.SetAsync(StorageKey, items.ToArray());
            _items = items; Changed?.Invoke();
        }
        finally { gate.Release(); }
    }
    public Task Add(Product p) => UpdateAsync(items => ShopRules.Add(items, p));
    public Task Change(int id, int count, int max) => UpdateAsync(items => ShopRules.SetQuantity(items, id, count, max));
    public Task Remove(int id) => UpdateAsync(items => items.Where(x => x.ProductId != id).ToArray());
    public Task Clear() => UpdateAsync(_ => []);
}
