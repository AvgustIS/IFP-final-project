# EasyShop KZ — .NET 10 Blazor Store

A small university final project: responsive product catalog, searchable categories, shopping cart, customer accounts, checkout, order history, and an admin dashboard.

## Stack
- .NET 10, ASP.NET Core Blazor Web App (Interactive Server)
- EF Core 10 + SQLite (persistent `easyshop.db`)
- Cookie authentication and ASP.NET Core password hashing
- Pure C# business rules and xUnit automated tests

## Run on Windows, macOS or Linux

Install **.NET 10 SDK**. From repository root:

```bash
dotnet restore EasyShop.sln
dotnet build EasyShop.sln
dotnet test EasyShop.sln
dotnet run --project src/EasyShop.Web
```

Open the localhost URL displayed by the terminal. In the launch profile the HTTP URL is `http://localhost:5138`.

### Create your own admin account
For security, no hardcoded admin password is shipped. Set these environment variables **before first run** (password at least 12 characters):

**PowerShell:**
```powershell
$env:EASYSHOP_ADMIN_EMAIL="admin@easyshop.local"
$env:EASYSHOP_ADMIN_PASSWORD="ChangeThisPassword123!"
dotnet run --project src/EasyShop.Web
```

**macOS/Linux:**
```bash
EASYSHOP_ADMIN_EMAIL=admin@easyshop.local EASYSHOP_ADMIN_PASSWORD='ChangeThisPassword123!' dotnet run --project src/EasyShop.Web
```

These commands use an **example password**: choose your own private password. Admin is seeded only when those variables are present. If you add them after first run, seeding still checks and creates the account on startup. Regular customers can register from `/register`.

## Navigation
- `/` — homepage; `/catalog` — searchable product catalog; `/product/{id}` — details
- `/cart` — cart; `/checkout` — checkout (sign-in required); `/orders` — personal orders
- `/login`, `/register` — accounts
- `/admin` — dashboard (admin only); `/admin/products`, `/admin/categories`, `/admin/orders`

## Architecture
```text
Components/Pages       interactive Blazor UI
Components/Shared      reusable ProductCard
Models                 EF entities and immutable CartLine record
Data                   ShopDbContext + idempotent demo seeding
State                  per-circuit CartState service
Services               business logic / database transactions
Functional             pure ShopRules methods
Tests                  xUnit domain and SQLite integration tests
```

## Notes on scope
- Checkout recalculates prices from DB and uses atomic conditional stock decrement inside a transaction.
- Cancellation returns stock. **Admin deletion of products keeps order snapshots** (OrderLine stores name and price).
- Cart state is saved in protected browser session storage. It survives refresh and sign-in in the same tab; closing the tab ends that browser session. Prices and stock are always checked again at checkout.
- Payments are **simulated only**, no payment details are collected or processed.
- SQLite `EnsureCreated()` is used for simple first-run setup, not EF migrations. For database schema changes during development, delete the demo `easyshop.db` file and relaunch (existing orders will be erased).
- This is an educational example, not a production payment application.

## Verification

```powershell
dotnet test EasyShop.sln
cd tests/e2e
npm.cmd ci
npx.cmd playwright install msedge firefox
npx.cmd playwright test
```

On macOS/Linux use `npm` and `npx` without `.cmd`. Browser tests launch an isolated store on port 5149, create a unique SQLite database under `artifacts/`, and use test-only credentials. They never open the normal store database. Edge must be installed for the Edge projects; Firefox is installed by Playwright. The mobile project emulates a Pixel 7 viewport in Edge; it is not a physical-device test.

See [QA report](docs/QA_REPORT_RU.md), [requirements mapping](docs/REQUIREMENTS_RU.md), and [user guide](docs/USER_GUIDE_RU.md). The editable presentation is [the instructor version](docs/for_teacher/EasyShop_KZ_Defense_EN.pptx); fill in the participants' full names before defense.

The original requirements also require personal participation and an oral defense. Source code and automated tests cannot certify those academic conditions.

## Defense materials

- [Presentation for the instructor (English, 12 slides)](docs/for_teacher/EasyShop_KZ_Defense_EN.pptx)

Fill in the team members' full names on the title slide before submitting the presentation.
