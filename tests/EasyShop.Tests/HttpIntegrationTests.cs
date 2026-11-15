using System.Net;
using System.Text.RegularExpressions;
using EasyShop.Web.Data;
using EasyShop.Web.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
namespace EasyShop.Tests;

public sealed class HttpIntegrationTests : IClassFixture<ShopApplication>
{
    private readonly ShopApplication app;
    public HttpIntegrationTests(ShopApplication app) => this.app = app;
    private HttpClient Client() => app.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    private static async Task<string> Token(HttpClient client, string page)
    {
        var html = await client.GetStringAsync(page);
        return WebUtility.HtmlDecode(Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value);
    }
    private static async Task<HttpResponseMessage> Login(HttpClient client, string email, string password) => await client.PostAsync("/auth/login", new FormUrlEncodedContent(new Dictionary<string, string> { { "__RequestVerificationToken", await Token(client, "/login") }, { "email", email }, { "password", password } }));
    [Theory]
    [InlineData("/")]
    [InlineData("/catalog")]
    [InlineData("/product/1")]
    [InlineData("/product/9999")]
    [InlineData("/cart")]
    [InlineData("/login")]
    [InlineData("/register")]
    [InlineData("/denied")]
    [InlineData("/error")]
    [InlineData("/health")]
    [InlineData("/app.css")]
    [InlineData("/images/product-1.svg")]
    public async Task PublicRoutesRender(string route) { using var client = Client(); Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(route)).StatusCode); }
    [Theory]
    [InlineData("/checkout")]
    [InlineData("/orders")]
    [InlineData("/order-success/1")]
    [InlineData("/admin")]
    [InlineData("/admin/products")]
    [InlineData("/admin/categories")]
    [InlineData("/admin/orders")]
    public async Task AnonymousProtectedRoutesRedirectToLogin(string route) { using var client = Client(); var response = await client.GetAsync(route); Assert.Equal(HttpStatusCode.Redirect, response.StatusCode); Assert.Contains("/login", response.Headers.Location!.ToString()); }
    [Theory]
    [InlineData("login")]
    [InlineData("register")]
    [InlineData("logout")]
    public async Task AuthPostsRequireAntiforgeryToken(string action) { using var client = Client(); Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync("/auth/" + action, new FormUrlEncodedContent([]))).StatusCode); }
    [Fact]
    public async Task RegistrationLoginRolesAndLogout()
    {
        using var client = Client(); var email = $"test-{Guid.NewGuid():N}@example.com";
        var fields = new Dictionary<string, string> { { "__RequestVerificationToken", await Token(client, "/register") }, { "name", "Test User" }, { "email", email }, { "password", "TestPassword123!" } };
        var registered = await client.PostAsync("/auth/register", new FormUrlEncodedContent(fields)); Assert.Contains("success=", registered.Headers.Location!.ToString());
        fields["__RequestVerificationToken"] = await Token(client, "/register"); var duplicate = await client.PostAsync("/auth/register", new FormUrlEncodedContent(fields)); Assert.Contains("already", duplicate.Headers.Location!.ToString());
        var wrong = await Login(client, email, "wrong"); Assert.Contains("error=", wrong.Headers.Location!.ToString());
        var valid = await Login(client, email.ToUpperInvariant(), "TestPassword123!"); Assert.Equal("/", valid.Headers.Location!.ToString());
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/orders")).StatusCode);
        var denied = await client.GetAsync("/admin"); Assert.Contains("/denied", denied.Headers.Location!.ToString());
        var logout = await client.PostAsync("/auth/logout", new FormUrlEncodedContent(new Dictionary<string, string> { { "__RequestVerificationToken", await Token(client, "/orders") } })); Assert.Equal(HttpStatusCode.Redirect, logout.StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, (await client.GetAsync("/orders")).StatusCode);
    }
    [Theory]
    [InlineData("/admin")]
    [InlineData("/admin/products")]
    [InlineData("/admin/categories")]
    [InlineData("/admin/orders")]
    public async Task AdminCanAccessManagementRoutes(string route) { using var client = Client(); await Login(client, "admin@test.local", "AdminTestPassword123!"); Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(route)).StatusCode); }
    [Fact]
    public async Task InvalidRegistrationIsRejected()
    {
        using var client = Client(); var response = await client.PostAsync("/auth/register", new FormUrlEncodedContent(new Dictionary<string, string> { { "__RequestVerificationToken", await Token(client, "/register") }, { "name", "A" }, { "email", "bad" }, { "password", "short" } })); Assert.Contains("error=", response.Headers.Location!.ToString());
    }
}
public sealed class ShopApplication : WebApplicationFactory<Program>
{
    private readonly string database = Path.Combine(Path.GetTempPath(), $"easyshop-http-{Guid.NewGuid():N}.db");
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development"); builder.UseSetting("ConnectionStrings:Shop", $"Data Source={database}");
    }
    public ShopApplication()
    {
        using var scope = Services.CreateScope(); using var db = scope.ServiceProvider.GetRequiredService<IDbContextFactory<ShopDbContext>>().CreateDbContext();
        var admin = new ShopUser { Name = "Test Admin", Email = "admin@test.local", Role = "Admin" }; admin.PasswordHash = new PasswordHasher<ShopUser>().HashPassword(admin, "AdminTestPassword123!"); db.Users.Add(admin); db.SaveChanges();
    }
    protected override void Dispose(bool disposing) { base.Dispose(disposing); SqliteCleanup(); }
    private void SqliteCleanup() { Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools(); foreach (var suffix in new[] { "", "-shm", "-wal" }) if (File.Exists(database + suffix)) File.Delete(database + suffix); }
}
