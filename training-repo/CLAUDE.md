# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

OrderHub — an internal order management system used as a training project for junior AI-agent practice. Single SQL Server database, no multi-tenancy or high-concurrency concerns. Full walkthrough/troubleshooting for humans lives in `../documents/README.md` (one level above this directory, at the git repo root); a practice-notes template is at `../documents/PROCESS.md`.

## Tech stack

- .NET 8 (`net8.0`) / ASP.NET Core MVC (Razor Views)
- EF Core 8 (8.0.11) + SQL Server
- Tests: xUnit 2.5.3, EF Core InMemory

## Commands

```powershell
dotnet build                                   # build the solution
dotnet run --project src/OrderHub.Web          # run the site (auto-migrates + seeds DB on first start)
dotnet test                                    # run all tests (EF Core InMemory, no SQL Server needed)
dotnet test --filter "FullyQualifiedName~OrderServiceCreateTests"   # run one test class
dotnet test --filter "DisplayName~ShouldRejectZeroQuantity"         # run one test by name
```

Resetting the local dev database (destructive — confirm with the user before running):
```powershell
dotnet ef database drop -f -p src/OrderHub.Infrastructure -s src/OrderHub.Web
dotnet run --project src/OrderHub.Web
```

## Architecture

Three-project layering, referenced top to bottom:

- `src/OrderHub.Web` — ASP.NET Core MVC (Razor Views). Controllers only translate between service calls and ViewModels; no business logic, no direct `DbContext` use.
- `src/OrderHub.Core` — domain models, service interfaces, and all business logic (discounting, stock, order status transitions). Services depend on repository interfaces (`I*Repository`), not `DbContext`.
- `src/OrderHub.Infrastructure` — EF Core `OrderHubDbContext`, repository implementations, migrations, seed data (`DbSeeder`). Only this layer touches `DbContext`.
- `tests/OrderHub.Tests` — xUnit against EF Core InMemory via `TestSetup` (`TestSetup.CreateContext()` + `TestSetup.CreateOrderService(db)` / `CreateProductService(db)`), never a real SQL Server.

### Conventions to follow when adding features

- Services return `ServiceResult<T>` (`OrderHub.Core.Common`) to express expected failures (validation, not-found, business rule violations) — don't throw for these; controllers turn `result.Errors` into `ModelState` errors.
- Views bind to a `ViewModel` (hand-mapped in the controller), never a domain model directly.
- Server-side validation uses DataAnnotations + `ModelState`; invalid input must render the form with errors, never 500.
- Money is always `decimal`. Discounts are centralized in `OrderService` (`GetDiscountRate`, `CalculateSubtotal`, `CalculateTotal`) — don't recompute them elsewhere. Tier discount: Standard 0%, Silver 5%, Gold 10%, applied once on the order total.
- Flash messages use `TempData["Success"]` / `TempData["Error"]` (rendered by the shared alert block in `Views/Shared/_Layout.cshtml`).
- New controller/service/repository work should mirror the existing `Products` vertical slice (`ProductsController` → `IProductService`/`ProductService` → `IProductRepository`/`ProductRepository`) for naming and layering.

### Order creation/cancellation flow (spans all three layers)

`OrderService.CreateOrderAsync` (`src/OrderHub.Core/Services/OrderService.cs`) is the central business-rule example: validates customer exists, lines are non-empty/non-duplicate/positive quantity, checks each product is active with sufficient stock, decrements stock, snapshots unit price (applying the customer's tier discount at line level for Gold only — total-level discount in `CalculateTotal` applies to all tiers), then persists via `IOrderRepository`. `CancelOrderAsync` restores stock and only allows cancelling `Pending`/`Confirmed` orders. Note the line-level Gold-only discount vs. the total-level all-tiers discount is an existing asymmetry in the code, not a bug to silently "fix."

### Data model

`Customer` (has `CustomerTier`), `Product` (unique `Sku`, `StockQuantity`, `IsActive`), `Order` → many `OrderItem` (`UnitPriceSnapshot` captured at order time, not live product price). Order deletion is `Restrict` on Customer/Product FKs; `OrderItem` cascades from `Order`.

## Important/dangerous files

- `src/OrderHub.Infrastructure/Migrations/**` — historical record, do not hand-edit.
- `src/OrderHub.Web/appsettings.json` / `appsettings.Development.json` — local connection string; confirm before changing.
- `appsettings.Production.json`, `*.pfx`, and user-secrets — never read or write these; they hold real credentials/certs, not training data.

## Workflow for questions/bug reports

Whenever the user asks a question or reports an issue, first write out your understanding of the issue, then investigate and find the solution, then list out all changes you would possibly make — and wait for the user's explicit permission before making any changes.

## Don'ts

- Don't add NuGet packages without asking first.
- Don't use `DbContext` directly in a Controller or Service — go through a repository.
- Don't refactor code unrelated to the current task just because you're nearby.
- Don't read or write any secrets files (`*.pfx`, `appsettings.Production.json`, user-secrets).
