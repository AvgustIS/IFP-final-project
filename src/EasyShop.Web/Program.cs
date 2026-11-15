using System.Security.Claims;
using EasyShop.Web.Components;
using EasyShop.Web.Data;
using EasyShop.Web.Models;
using EasyShop.Web.Services;
using EasyShop.Web.State;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.EntityFrameworkCore;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options =>
{ options.LoginPath = "/login"; options.AccessDeniedPath = "/denied"; options.Cookie.Name = "EasyShop.Auth"; options.Cookie.HttpOnly = true; options.Cookie.SameSite = SameSiteMode.Lax; options.ExpireTimeSpan = TimeSpan.FromDays(7); });
builder.Services.AddAuthorization(); builder.Services.AddAntiforgery();
builder.Services.AddDbContextFactory<ShopDbContext>(o => o.UseSqlite(builder.Configuration.GetConnectionString("Shop") ?? "Data Source=easyshop.db"));
builder.Services.AddScoped<Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage.ProtectedSessionStorage>();
builder.Services.AddScoped<ShopService>(); builder.Services.AddScoped<CartState>(); builder.Services.AddHealthChecks();
var app = builder.Build();
await SeedData.InitializeAsync(app.Services);
if (!app.Environment.IsDevelopment()) { app.UseExceptionHandler("/error"); app.UseHsts(); }
app.UseHttpsRedirection(); app.UseStaticFiles(); app.UseAuthentication(); app.UseAuthorization(); app.UseAntiforgery();
app.MapHealthChecks("/health");
app.MapPost("/auth/login", async (HttpContext ctx, IDbContextFactory<ShopDbContext> factory, IAntiforgery anti) =>
{
    if (!await anti.IsRequestValidAsync(ctx)) return Results.BadRequest("Invalid or expired form. Reload the page and try again."); var form = await ctx.Request.ReadFormAsync(); var email = form["email"].ToString().Trim().ToLowerInvariant(); var pass = form["password"].ToString();
    await using var db = await factory.CreateDbContextAsync(); var user = await db.Users.FirstOrDefaultAsync(x => x.Email == email);
    if (user is null || new PasswordHasher<ShopUser>().VerifyHashedPassword(user, user.PasswordHash, pass) == PasswordVerificationResult.Failed)
        return Results.Redirect("/login?error=Invalid+email+or+password");
    var claims = new[] { new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), new Claim(ClaimTypes.Name, user.Name), new Claim(ClaimTypes.Email, user.Email), new Claim(ClaimTypes.Role, user.Role) };
    await ctx.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)));
    return Results.Redirect("/");
});
app.MapPost("/auth/register", async (HttpContext ctx, IDbContextFactory<ShopDbContext> factory, IAntiforgery anti) =>
{
    if (!await anti.IsRequestValidAsync(ctx)) return Results.BadRequest("Invalid or expired form. Reload the page and try again."); var form = await ctx.Request.ReadFormAsync(); var name = form["name"].ToString().Trim(); var email = form["email"].ToString().Trim().ToLowerInvariant(); var password = form["password"].ToString();
    if (name.Length < 2 || name.Length > 120 || !System.Net.Mail.MailAddress.TryCreate(email, out var address) || address.Address != email || email.Length > 120 || password.Length < 8 || password.Length > 128) return Results.Redirect("/register?error=Please+check+your+details");
    await using var db = await factory.CreateDbContextAsync(); if (await db.Users.AnyAsync(x => x.Email == email)) return Results.Redirect("/register?error=Email+already+registered");
    var user = new ShopUser { Name = name, Email = email }; user.PasswordHash = new PasswordHasher<ShopUser>().HashPassword(user, password); db.Users.Add(user);
    try { await db.SaveChangesAsync(); } catch (DbUpdateException) { return Results.Redirect("/register?error=Email+already+registered"); }
    return Results.Redirect("/login?success=Registration+complete.+Please+sign+in");
});
app.MapPost("/auth/logout", async (HttpContext ctx, IAntiforgery anti) => { if (!await anti.IsRequestValidAsync(ctx)) return Results.BadRequest("Invalid or expired form. Reload the page and try again."); await ctx.SignOutAsync(); return Results.Redirect("/"); });
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
app.Run();
public partial class Program { }
