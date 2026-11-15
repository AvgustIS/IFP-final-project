using System.ComponentModel.DataAnnotations;
namespace EasyShop.Web.Models;

public class ShopUser { public int Id { get; set; } [MaxLength(120)] public string Email { get; set; } = ""; public string PasswordHash { get; set; } = ""; public string Name { get; set; } = ""; public string Role { get; set; } = "Customer"; }
public class Category { public int Id { get; set; } [Required, MaxLength(80)] public string Name { get; set; } = ""; public List<Product> Products { get; set; } = []; }
public class Product { public int Id { get; set; } [Required, MaxLength(140)] public string Name { get; set; } = ""; public string Description { get; set; } = ""; [Range(1, 99999999)] public decimal Price { get; set; } = 1000; [Range(0, 1000000)] public int Stock { get; set; } = 10; public string ImageUrl { get; set; } = "/images/product-1.svg"; public int CategoryId { get; set; } public Category? Category { get; set; } }
public class ShopOrder { public int Id { get; set; } public int UserId { get; set; } public ShopUser? User { get; set; } public DateTime CreatedUtc { get; set; } = DateTime.UtcNow; public string CustomerName { get; set; } = ""; public string Phone { get; set; } = ""; public string Address { get; set; } = ""; public string PaymentMethod { get; set; } = "Cash on Delivery"; public string Status { get; set; } = "Pending"; public decimal Total { get; set; } public List<OrderLine> Items { get; set; } = []; }
public class OrderLine { public int Id { get; set; } public int ShopOrderId { get; set; } public ShopOrder? ShopOrder { get; set; } public int ProductId { get; set; } public string Name { get; set; } = ""; public int Quantity { get; set; } public decimal UnitPrice { get; set; } }
public record CartLine(int ProductId, string Name, decimal UnitPrice, int Quantity, string ImageUrl);
public record CheckoutDetails(string Name, string Phone, string Address, string PaymentMethod);
public record ServiceResult(bool Success, string Message, int? Id = null) { public static ServiceResult Ok(string message, int? id = null) => new(true, message, id); public static ServiceResult Fail(string message) => new(false, message); }
