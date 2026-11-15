# Developer notes

## Boundaries and lifecycle

Razor pages own presentation and route authorization. ShopService is server-only business/data logic and is not exposed as an unauthenticated REST API. Every admin route requires Admin; checkout/history/success require an authenticated customer. Order history and success pages additionally filter by the claim user ID. Do not expose ShopService methods through new endpoints without adding authorization.

IDbContextFactory creates a separate context per operation, avoiding a DbContext shared across a long-running Blazor circuit. Read queries use AsNoTracking. Checkout uses a SQLite transaction and a conditional stock update; failure rolls back all earlier reservations. Status changes claim the previous status atomically before returning inventory. OrderLine deliberately has no FK to Product so snapshots survive product deletion.

CartState is scoped per circuit and persists arrays through ProtectedSessionStorage. Initialization and mutations are serialized with SemaphoreSlim. Cart and Checkout subscribe to Changed and unsubscribe on disposal. Login/Register are excluded from interactive routing and rendered statically so cookie forms and typed input survive authentication navigation. Checkout always distrusts cart prices and names.

ShopRules contains pure functions for immutable cart updates, totals, filtering and allowed order transitions. ProductCard is reused on Home and Catalog with an Item parameter. AuthenticationState is cascading; MainLayout updates its badge from the state event.

## Data and deployment

The configured SQLite path is relative to the app working directory unless absolute. Preserve the database and ASP.NET Core Data Protection keys between deployments. EnsureCreated supports initial setup; it is not a schema migration strategy. Back up real data before a schema change, and add EF migrations before adopting the project beyond a demo. Never delete a populated database just to apply a change.

The SQLite money columns retain the existing double conversion for schema compatibility. Ordinary catalog prices are validated to two decimal places, but large monetary aggregates may lose precision in floating-point storage. A production schema should use integer minor units with a reviewed data migration.

SeedData adds the demo categories/products only when the category table is empty. An existing category table is treated as an initialized catalog; deleting all products does not restore demo inventory at restart. Admin credentials come from environment variables, not source. Test credentials belong only to isolated test databases.

Production requires HTTPS and persistent Data Protection keys. The included app has no real payment gateway, password reset, account verification, distributed cart or multi-node database. Login rate limiting and a production identity provider are possible future work. SQLite serializes writers and the catalog is loaded in memory; pagination and a server database are needed for substantially larger data volumes.

## Tests and artifacts

- ShopRulesTests: pure functions, stock limits, overflow, filters and transitions.
- ShopServiceTests: actual service calls against relational SQLite, checkout rollback, validation, CRUD, history and cancellation.
- ConcurrencyTests: separate SQLite file per test, competing writers, reopening storage and startup regression.
- HttpIntegrationTests: WebApplicationFactory with a unique temporary database, real cookies, CSRF, customer/admin route checks and registration.
- tests/e2e: Playwright customer/admin workflows, mobile viewport, Edge and Firefox. The test server always uses a unique artifacts database.

Run dotnet test before browser tests so --no-build uses current binaries. Browser HTML reports are in tests/e2e/playwright-report, traces for failures in test-results, screenshots in artifacts. Do not run multiple browser suites simultaneously on port 5149. Generated files and database files are ignored by .gitignore.

There is no public JSON API. The HTTP surface is GET /health and the cookie form POST endpoints /auth/register, /auth/login, /auth/logout; all three POST routes validate antiforgery tokens and return 400 for invalid tokens. The remaining paths are UI pages listed in README.
