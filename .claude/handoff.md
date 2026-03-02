# ZenPharm Project Handoff Document

> **Purpose**: This document provides everything an AI agent needs to understand, maintain, and extend the ZenPharm project. Read this thoroughly before making any changes.

---

## 1. Project Overview

**ZenPharm** is an Australian pharmacy management SaaS platform built for independent community pharmacies. It was forked from `zentech-biz` (a generic multi-tenant SaaS template by Zentech Consulting) and customised with pharmacy-specific features: product catalogues, schedule class compliance (Australian TGA), stock management, patient records, and PBS integration readiness.

### Positioning

- **Internal origin**: Zentech Consulting's reusable SaaS template (`zentech-biz`) provides the core multi-tenant infrastructure
- **External delivery**: Deployed as "ZenPharm" -- a branded pharmacy management solution
- **Model**: Each pharmacy tenant gets an isolated database; shared platform catalogue for master product data

### Technology Stack

| Component | Stack | Purpose | Port |
|-----------|-------|---------|------|
| `api/` | .NET 8 + Dapper + SQL Server | REST API backend | `:51003` |
| `public/` | React 19 + TypeScript + Vite + Tailwind CSS | Marketing site + AI Consultant widget | `:51000` |
| `admin/` | React 18 + TypeScript + Vite + Ant Design | Pharmacy admin panel | `:51001` |

### Key Dependencies (API)

| Package | Version | Purpose |
|---------|---------|---------|
| Dapper | 2.1.66 | Micro-ORM for SQL Server |
| BCrypt.Net-Next | 4.0.3 | Password hashing |
| Microsoft.Data.SqlClient | 6.1.2 | SQL Server driver |
| JwtBearer | 8.0.13 | JWT authentication |
| Swashbuckle | 9.0.6 | Swagger/OpenAPI |
| System.IdentityModel.Tokens.Jwt | 8.7.0 | JWT token generation |

### Key Dependencies (Tests)

| Package | Version | Purpose |
|---------|---------|---------|
| xUnit | 2.9.3 | Test framework |
| NSubstitute | 5.3.0 | Mocking library |
| Microsoft.NET.Test.Sdk | 17.12.0 | Test runner |

---

## 2. Architecture

### Multi-Tenancy Model

ZenPharm uses a **database-per-tenant** architecture with two database tiers:

```
                     Request Flow
                     ============

  Client Request
       |
       v
  TenantMiddleware
       |  (extracts subdomain from Host header)
       v
  TenantResolver  <--- ICatalogDb (singleton)
       |  (queries Tenants table, 5-min cache)
       v
  TenantContext
       |  (injected into HttpContext.Items)
       v
  ITenantDb (scoped)
       |  (connection string from TenantContext)
       v
  Feature Manager  (e.g. ClientManager, ProductManager)
```

**Catalog DB** (`ZenPharmCatalog`) -- singleton, platform-wide:
- Tenant registry (subdomains, connection strings, branding)
- Plans and subscriptions (Stripe integration stubs)
- Master product catalogue (shared across all tenants)

**Tenant DB** (`ZenPharmTenant-{name}`) -- one per tenant:
- AdminUsers, RefreshTokens (authentication)
- Clients, Services, Employees, Bookings, Schedules
- TenantProducts (imported from master catalogue + local inventory)
- StockMovements (inventory audit trail)
- KnowledgeEntries, AiChatSessions, AiChatMessages

### Core Interfaces

| Interface | Lifetime | Purpose |
|-----------|----------|---------|
| `ICatalogDb` | Singleton | Creates connections to the Catalog DB |
| `ITenantDb` | Scoped | Creates connections to the current tenant's DB |
| `IConnectionStringProtector` | Singleton | AES-256-GCM encrypt/decrypt for stored connection strings |
| `ITenantResolver` | Singleton | Subdomain-to-TenantContext resolution with 5-min cache |
| `TenantContext` | Scoped (per-request) | Immutable record with tenant ID, subdomain, plan, connection string |

### Connection String Protection

`ConnectionStringProtector` uses AES-256-GCM to encrypt tenant connection strings stored in the Catalog DB.

- **Storage format**: `ENC:` + Base64(nonce[12] + tag[16] + ciphertext)
- **Key source**: `Security:ConnectionStringKey` (base64-encoded 32-byte key)
- **Development**: If no key is configured, plaintext passthrough is allowed
- **Production**: Key is **required** -- startup throws if missing
- **Backward compatible**: Strings without the `ENC:` prefix are returned as-is on decrypt

### Tenant Resolution Flow

1. `TenantMiddleware` extracts subdomain from `Host` header
2. Handles compound TLDs (`.com.au`, `.co.uk`, `.co.nz`)
3. Localhost and IP addresses fall back to `Tenancy:DevTenantSubdomain` config
4. Reserved subdomains (`www`, `api`, `admin`, `mail`, `ftp`) are skipped
5. Bypass paths (`/health`, `/swagger`, `/api/platform`) skip resolution entirely
6. `TenantResolver` queries Catalog DB with a `ConcurrentDictionary` cache (5-min TTL)
7. Decrypts connection string via `IConnectionStringProtector`
8. Injects `TenantContext` into `HttpContext.Items`

### DI Registration Flow (Program.cs)

```
AddMultiTenancy(configuration)
  -> ICatalogDb (singleton) from CatalogConnection
  -> IConnectionStringProtector (singleton)
  -> ITenantResolver (singleton)
  -> TenantContext? (scoped, nullable, from HttpContext)
  -> ITenantDb (scoped, from TenantContext or DefaultConnection fallback)

Feature Managers (all scoped):
  -> IAuthManager, IClientManager, IServiceManager
  -> IBookingManager, IScheduleManager, IEmployeeManager
  -> IAiChatManager, IAiToolExecutor, IKnowledgeManager
  -> IEmailService (DryRun or SMTP based on config)
  -> IReportManager, IMasterProductManager, IProductManager
  -> IProvisioningPipeline (singleton, stub)

Dev-only:
  -> IDevSeedService (singleton, only in Development)
```

### Middleware Pipeline Order

```csharp
app.UseCors();
app.UseTenantResolution();  // Must be after CORS, before auth
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
```

### Rate Limiting

| Policy | Limit | Window |
|--------|-------|--------|
| `auth-login` | 5 requests | 1 minute |
| `ai-chat` | 10 requests | 1 minute |

---

## 3. Module Map (11 Feature Modules + Products)

### Authentication (`Features/Auth/`)

- JWT with configurable issuer/audience (defaults: `zenpharm` / `zenpharm-clients`)
- Access tokens (60 min) + refresh tokens (7 days, 30 days with remember-me)
- Roles: `SuperAdmin`, `Admin`, `Manager`, `Staff`
- Account lockout after failed attempts
- BCrypt password hashing

### Clients (`Features/Clients/`)

- Patient record CRUD
- Standard fields: FirstName, LastName, Email, Phone, Notes
- Pharmacy fields: DateOfBirth, Allergies, MedicationNotes, Tags
- Stored in tenant DB

### Services (`Features/Services/`)

- Service/consultation catalogue (e.g. vaccinations, health checks)
- Fields: Name, Description, Category, Price, DurationMinutes, IsActive
- Category-based filtering

### Bookings (`Features/Bookings/`)

- Appointment management with status tracking
- Statuses: `pending`, `confirmed`, `cancelled`, `completed`, `no_show`
- JOINs to Clients, Services, and Employees
- Available slot calculation

### Schedules (`Features/Schedules/`)

- Employee roster management
- Per-employee, per-day entries with start/end times and location
- Auto-generate Monday-to-Friday schedules

### Employees (`Features/Employees/`)

- Staff CRUD with role-based filtering
- Pharmacy roles (enforced by DB CHECK constraint):
  `pharmacist`, `dispense_technician`, `pharmacy_assistant`, `cashier`, `manager`, `staff`

### AI Chat (`Features/AiChat/`)

- Claude API integration with SSE (Server-Sent Events) streaming
- Tool Use framework with registered tools
- Session persistence (AiChatSessions + AiChatMessages tables)
- Configurable model, max tokens, system prompt
- DryRun mode for development

### AI Tools (`Features/AiChat/Tools/`)

Currently registered tools (3):

| Tool | Description |
|------|-------------|
| `search_knowledge` | Searches the knowledge base for articles |
| `list_services` | Lists available services with pricing |
| `check_availability` | Checks available appointment slots |

Tool definitions follow the Claude API tool_use schema. The `AiToolExecutor` resolves managers via `IServiceProvider.CreateScope()`.

### Knowledge Base (`Features/Knowledge/`)

- CRUD for knowledge articles (Title, Content, Category, Tags)
- SQL LIKE-based search with relevance scoring
- Used by the AI chat `search_knowledge` tool

### Notifications (`Features/Notifications/`)

- **Email**: `IEmailService` with two implementations:
  - `DryRunEmailService` (logs only, used in development)
  - `SmtpEmailService` (real SMTP delivery)
- **SMS**: SMS Broadcast integration with `NormalisePhone` utility
- Both support DryRun mode (controlled by config flags)
- Startup warnings if DryRun is true in non-Development environments

### Reports (`Features/Reports/`)

- Dashboard summary endpoint with date range filtering
- Metrics: TotalClients, TotalBookings, TotalEmployees, Revenue
- Pharmacy metrics: TotalProducts, LowStockCount, ExpiringCount (30-day window)
- Daily stats breakdown (booking count + revenue per day)

### Master Products (`Features/MasterProducts/`)

- Platform-wide product catalogue stored in **Catalog DB** (via `ICatalogDb`)
- Pharmacy-specific fields: GenericName, Brand, Barcode, ScheduleClass, PackSize, ActiveIngredients, Warnings, PbsItemCode, ImageUrl
- Schedule classes: `Unscheduled`, `S2`, `S3`, `S4` (Australian TGA classification)
- SKU-based uniqueness

### Tenant Products (`Features/Products/`)

- Per-tenant inventory stored in **Tenant DB** (via `ITenantDb`)
- Import from master catalogue (denormalised master data + local overrides)
- Stock management: quantity, reorder level, expiry date, custom pricing
- Stock movements: `stock_in`, `stock_out`, `adjustment`, `expired`, `return`
- Low stock alerts (quantity <= reorder level)
- Expiry alerts (configurable days-ahead window)

### Platform (`Features/Platform/`)

- Tenant management endpoints (`/api/platform/*`)
- `ProvisioningPipeline` -- **currently a stub** that logs and returns a fake tenant ID
- `StripeWebhookEndpoints` -- Stripe webhook receiver for subscription lifecycle
- Bypasses tenant resolution (no subdomain required)

---

## 4. Database Schema

### Catalog DB (`CatalogMigration.cs`) -- 6 Migrations

All migrations are idempotent (`IF NOT EXISTS` guards). The catalog migration includes a 3-retry loop with exponential backoff to handle Azure SQL Basic tier cold starts.

| Migration | Table | Key Columns |
|-----------|-------|-------------|
| 001 | `Tenants` | Id (GUID), Subdomain (unique), DisplayName, ConnectionString, PrimaryColour, Status, ContactEmail/Phone |
| 002 | `Plans` | Id (GUID), Name (unique), PriceMonthly/Yearly, Features (JSON), MaxUsers, MaxProducts |
| 003 | `Subscriptions` | Id (GUID), TenantId (FK), PlanId (FK), PlanName, Status, BillingPeriod, Stripe IDs |
| 004 | `MasterProducts` | Id (GUID), Sku (unique), Name, Category, Description, UnitPrice, Unit, Metadata |
| 005 | MasterProducts + pharmacy columns | GenericName, Brand, Barcode, ScheduleClass (CHECK: Unscheduled/S2/S3/S4), PackSize, ActiveIngredients, Warnings, PbsItemCode, ImageUrl |
| 006 | Tenants + pharmacy columns | Abn, AddressLine1/2, Suburb, State, Postcode, BusinessHoursJson |

### Tenant DB (`TenantMigration.cs`) -- 14 Migrations

Tenant migrations run against individual tenant connection strings. They are triggered by `DevSeedService` (dev) and by the provisioning pipeline (production, when implemented).

| Migration | Table | Key Columns |
|-----------|-------|-------------|
| 001 | `AdminUsers` | Id (GUID), Username/Email (unique), PasswordHash, FullName, Role (CHECK: SuperAdmin/Admin/Manager/Staff), FailedLoginAttempts, LockoutEnd |
| 002 | `RefreshTokens` | Id (GUID), UserId (FK cascade), TokenHash, ExpiresAt, IsRevoked |
| 003 | `Clients` | Id (GUID), FirstName, LastName, Email, Phone, Notes |
| 004 | `Services` | Id (GUID), Name, Description, Category, Price, DurationMinutes, IsActive |
| 005 | `Employees` | Id (GUID), FirstName, LastName, Email, Phone, Role, IsActive |
| 006 | `Bookings` | Id (GUID), ClientId/ServiceId/EmployeeId (FKs), StartTime/EndTime, Status (CHECK: 5 statuses) |
| 007 | `Schedules` | Id (GUID), EmployeeId (FK), Date, StartTime/EndTime (TIME), Location, Notes |
| 008 | `KnowledgeEntries` | Id (GUID), Title, Content, Category, Tags |
| 009 | `AiChatSessions` | Id (BIGINT IDENTITY), SessionToken (unique), ClientIp, MessageCount |
| 010 | `AiChatMessages` | Id (BIGINT IDENTITY), SessionId (FK cascade), Role, Content, ToolsCalled, DurationMs |
| 011 | Clients + pharmacy columns | DateOfBirth, Allergies, MedicationNotes, Tags |
| 012 | Employees pharmacy roles | Drops `CK_Employees_Role`, adds `CK_Employees_PharmacyRole` (pharmacist/dispense_technician/pharmacy_assistant/cashier/manager/staff) |
| 013 | `TenantProducts` | Id (GUID), MasterProductId (unique), denormalised master data, CustomName/Price, StockQuantity, ReorderLevel, ExpiryDate, IsVisible, IsFeatured, SortOrder |
| 014 | `StockMovements` | Id (GUID), TenantProductId (FK), MovementType (CHECK: stock_in/stock_out/adjustment/expired/return), Quantity, Reference, CreatedBy |

---

## 5. Current State

### Completed Work

| Phase | PR | Tests | Description |
|-------|-----|-------|-------------|
| Phase 1 (zentech-biz) | PR #1, #2 on zentech-biz | 144 | Full generic SaaS template with all 11 modules |
| Phase 2 Basic (zenpharm) | PR #2 | 174 | Pharmacy customisation: MasterProducts, TenantProducts, patient fields, pharmacy roles, admin Products page, ZenPharm branding |
| Dev Seed + Security | PR #3 | 194 | DevSeedService, ConnectionStringProtector, migration retry, deployment fixes |

### Git History (Key Commits)

```
0b0a242 fix: deployment issues -- migration retry, API base URL, batch DDL
4d85ebe feat: add deployment security -- connection string encryption, production config
bca0466 feat: add dev seed data for E2E testing
8d09d06 feat: rebrand to ZenPharm with pharmacy marketing homepage
7173347 feat: add admin Products page with catalogue import and inventory management
74e1078 feat: add MasterProducts catalogue and TenantProducts inventory modules
17ab426 feat: add pharmacy-specific schema migrations
f634abe chore: initialise zenpharm repo (forked from zentech-biz template)
```

### Azure Deployment

| Resource | URL |
|----------|-----|
| API | https://zenpharm-api-au.azurewebsites.net |
| Admin | https://kind-tree-093309e00.4.azurestaticapps.net |
| Public | https://zealous-flower-0e43e0200.1.azurestaticapps.net |

**Infrastructure**: Azure Australia East (SQL + App Service), East Asia (Static Web Apps -- SWA not available in AU East).

See `.claude/deployment.md` for full deployment procedures and Azure resource details.

### Test Suite

194 tests across 19 test files:

| Test File | Area |
|-----------|------|
| `Auth/AuthManagerTests.cs` | JWT auth, login, lockout |
| `Clients/ClientContractTests.cs` | Client DTO validation |
| `Services/ServiceContractTests.cs` | Service contracts |
| `Bookings/BookingContractTests.cs` | Booking contracts |
| `Schedules/ScheduleContractTests.cs` | Schedule contracts |
| `Employees/EmployeeContractTests.cs` | Employee contracts |
| `AiChat/AiChatTests.cs` | AI chat, tool execution |
| `Knowledge/KnowledgeContractTests.cs` | Knowledge base |
| `Notifications/EmailTests.cs` | Email service |
| `Notifications/SmsTests.cs` | SMS broadcast |
| `Reports/ReportContractTests.cs` | Report DTOs |
| `MasterProducts/MasterProductContractTests.cs` | Master product contracts |
| `Products/ProductContractTests.cs` | Tenant product contracts |
| `Security/ConnectionStringProtectorTests.cs` | AES-256-GCM encrypt/decrypt |
| `Seeding/DevSeedServiceTests.cs` | Dev seed idempotency |
| `Tenancy/SubdomainExtractionTests.cs` | Subdomain parsing, compound TLDs |
| `Tenancy/TenantResolverTests.cs` | Tenant resolution, caching |
| `Platform/ProvisioningContractTests.cs` | Provisioning pipeline |

---

## 6. Key Patterns and Gotchas

### Patterns

- **Primary constructors (C# 12)**: All managers and services use primary constructor syntax for DI injection
- **InternalsVisibleTo**: `api.csproj` exposes internals to both `api.tests` and `DynamicProxyGenAssembly2` (required for NSubstitute to proxy internal interfaces)
- **GUID primary keys**: All entity tables use `UNIQUEIDENTIFIER DEFAULT NEWSEQUENTIALID()` -- never use `SCOPE_IDENTITY()` with GUIDs; use `DECLARE @InsertedId TABLE(Id UNIQUEIDENTIFIER)` + `OUTPUT INSERTED.Id INTO @InsertedId` pattern
- **Immutable records**: `TenantContext`, `ToolExecutionResult`, `ProvisionRequest/Result` are all `sealed record` types
- **Feature-based organisation**: Code is organised by feature (`Features/Auth/`, `Features/Clients/`, etc.), not by layer
- **Idempotent migrations**: All DDL uses `IF NOT EXISTS` guards so migrations are safe to re-run

### Gotchas

| Issue | Explanation |
|-------|-------------|
| **Dapper + NSubstitute** | Cannot mock `IDbConnection` with NSubstitute for Dapper async operations -- Dapper internally requires `DbConnection`. Test DB-unavailable scenarios by throwing from `ICatalogDb.CreateAsync()` instead |
| **Nullable DI** | Registering `TenantContext?` in DI triggers CS8634 -- suppressed with `#pragma warning disable CS8634` in `TenancyServiceExtensions.cs` |
| **SQL batch DDL** | Migration 005 uses `EXEC()` wrappers for constraints/indexes after `ALTER TABLE ADD` because SQL Server cannot mix certain DDL statements in the same batch |
| **Azure SQL cold start** | Basic tier databases (5 DTU) can be slow after idle periods -- `CatalogMigration` includes a 3-retry loop with exponential backoff (5s, 10s, 15s) |
| **ZIP deployment paths** | Linux App Service requires forward-slash paths in ZIP archives -- **never** use PowerShell `Compress-Archive` (creates backslashes); use Python `zipfile` instead |
| **React 19 useRef** | Requires explicit initial value: `useRef<T>(null)` not `useRef<T>()` |
| **CS1626 async iterators** | Cannot yield inside try-catch in async iterators -- extract parsing to a helper method, yield outside try-catch |
| **CORS in production** | Frontends are on separate SWA domains -- CORS `AllowedOrigins` must include both admin and public URLs |
| **DryRun warnings** | Startup logs warnings if Email/SMS DryRun is true in non-Development environments |

### British English

All code, comments, UI text, and documentation **must** use British English spelling. Key reminders:

| Wrong (American) | Correct (British) |
|---|---|
| color | colour |
| catalog | catalogue |
| initialize | initialise |
| optimize | optimise |
| customize | customise |
| organize | organise |
| center | centre |
| license (noun) | licence |
| canceled | cancelled |

**Exception**: Third-party API names, CSS keywords, and language keywords retain their original spelling.

---

## 7. Development Setup

### Prerequisites

- .NET 8 SDK
- Node.js 20+
- SQL Server (local or Azure SQL)

### User Secrets

The project uses `dotnet user-secrets` with ID `zenpharm-api`. Set the following secrets:

```bash
cd api
dotnet user-secrets init  # Only if not already initialised

# Required
dotnet user-secrets set "Jwt:SecretKey" "dev-only-secret-key-replace-in-production-min-32-chars!"
dotnet user-secrets set "ConnectionStrings:CatalogConnection" "Server=localhost;Database=ZenPharmCatalog;Trusted_Connection=True;TrustServerCertificate=True"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Database=ZenPharmTenant;Trusted_Connection=True;TrustServerCertificate=True"

# Optional (for AI chat -- leave empty for DryRun mode)
dotnet user-secrets set "AiChat:ApiKey" "sk-ant-..."

# Optional (for connection string encryption -- not needed in dev)
dotnet user-secrets set "Security:ConnectionStringKey" "<base64-encoded-32-bytes>"
```

### Running Locally

```bash
# API (port 51003)
cd api && dotnet run

# Admin panel (port 51001)
cd admin && npm run dev

# Public site (port 51000)
cd public && npm run dev
```

### Running Tests

```bash
dotnet test api/api.tests          # Run all 194 tests
dotnet test api/api.tests -v n     # Verbose output
```

### Dev Seed Data

When running in Development mode, `DevSeedService` automatically seeds on startup:
- A "Basic" plan ($79/month)
- A dev tenant (subdomain from `Tenancy:DevTenantSubdomain`, defaults to "dev")
- A subscription linking tenant to plan
- Tenant DB migrations
- An admin user (`admin` / `admin123`, role: `SuperAdmin`)
- 30 pharmacy master products across 11 categories (4 schedule classes)

All seeding is idempotent (`IF NOT EXISTS` guards).

### Configuration Hierarchy

`.NET config priority` (highest to lowest):
1. Environment variables (e.g. `ConnectionStrings__CatalogConnection`)
2. User secrets (`dotnet user-secrets`)
3. `appsettings.{Environment}.json`
4. `appsettings.json`

### Configuration Files

| File | Purpose |
|------|---------|
| `appsettings.json` | Base config (all values empty/default) |
| `appsettings.Development.json` | Dev overrides: Debug logging, DryRun=true for AiChat/SMS, CORS localhost, DevTenantSubdomain |
| `appsettings.Production.json` | Production overrides: Warning-only logging, DryRun=false for Email/SMS/AiChat |

---

## 8. Next Phase: Phase 2.5 Premium Features

The following features are planned for the Premium tier. None have been started.

### Product Template Packs
- Pre-filled catalogue of 500+ common pharmacy products
- "Community Pharmacy Starter" pack
- One-click import into tenant inventory

### Schedule Class Compliance Logic
- S2 (Pharmacy Medicine): Free sale from pharmacy, no pharmacist required
- S3 (Pharmacist Only Medicine): Pharmacist review and approval workflow
- S4 (Prescription Only Medicine): Display/reference only, not for OTC sale
- Enforcement in checkout/dispensing flows

### PBS Public API Integration
- Australian Pharmaceutical Benefits Scheme government API
- Import PBS-listed items with subsidised pricing
- Auto-update catalogue with PBS schedule changes

### AI Pharmacy Tools (extend `AiToolExecutor`)
- Drug interaction queries (cross-reference active ingredients)
- Inventory recommendations (reorder suggestions based on sales velocity)
- Marketing copy generation (product descriptions, promotional material)
- Patient medication history lookup

### AI Knowledge Base Content
- Pre-loaded drug information articles
- Health advice content packs
- Automatic knowledge base population from master product data

### Online Shop (Click-and-Collect)
- Public-facing product catalogue
- Shopping cart + checkout flow
- Order management in admin panel
- Collection notification (SMS/email)

### SMS Notifications
- Prescription ready for collection
- Appointment reminders (24h before)
- Low stock alerts to pharmacy manager
- Expiry date warnings

### Advanced Reports
- Stock turnover analysis (velocity, days-on-hand)
- Sales trends (daily/weekly/monthly)
- Expiry waste tracking (expired stock value)
- Revenue by category/schedule class

---

## 9. Git Workflow

### Branch Protection (CRITICAL)

**NEVER push directly to `main`** -- this triggers automatic production deployment. All changes must go through:

1. Create a feature branch: `feat/xxx`, `fix/xxx`, `refactor/xxx`
2. Push branch to remote
3. Create a Pull Request
4. Wait for CI to pass and PR approval
5. Merge via PR

### Commit Message Format

```
<type>: <description>

<optional body>
```

Types: `feat`, `fix`, `refactor`, `docs`, `test`, `chore`, `perf`, `ci`

### CI/CD

**CI** (`.github/workflows/ci.yml`): Runs on push to `main` and `feat/*`, and on PRs to `main`:
- API: .NET restore, build (Release), test
- Admin: npm ci, build (includes TypeScript type check)
- Public: npm ci, build

**Deploy** (`.github/workflows/deploy.yml`): Manual trigger (`workflow_dispatch`) with environment choice (staging/production):
- API: dotnet publish, deploy to Azure App Service via publish profile
- Admin: npm build, deploy to Azure Static Web Apps
- Public: npm build, deploy to Azure Static Web Apps

---

## 10. File Structure

```
zenpharm/
|
+-- api/                                # .NET 8 API backend
|   +-- Program.cs                      # Application entry point, DI, middleware, endpoints
|   +-- api.csproj                      # Project file (UserSecretsId: zenpharm-api)
|   +-- appsettings.json                # Base configuration
|   +-- appsettings.Development.json    # Dev overrides (DryRun, CORS, logging)
|   +-- appsettings.Production.json     # Production overrides (DryRun=false)
|   |
|   +-- Common/                         # Shared infrastructure
|   |   +-- Db.cs                       # ICatalogDb, ITenantDb, SqlConnectionFactory, TenantSqlConnectionFactory
|   |   +-- Migrations/
|   |   |   +-- ICatalogMigration.cs    # Interface
|   |   |   +-- CatalogMigration.cs     # 6 DDL migrations (Tenants, Plans, Subscriptions, MasterProducts, pharmacy columns)
|   |   |   +-- ITenantMigration.cs     # Interface (accepts connection string)
|   |   |   +-- TenantMigration.cs      # 14 DDL migrations (all tenant tables)
|   |   +-- Security/
|   |   |   +-- IConnectionStringProtector.cs
|   |   |   +-- ConnectionStringProtector.cs  # AES-256-GCM encryption
|   |   +-- Seeding/
|   |   |   +-- IDevSeedService.cs
|   |   |   +-- DevSeedService.cs       # Dev-only: seeds plan, tenant, admin, 30 products
|   |   |   +-- PharmacyMasterProductData.cs  # 30 seed products (4 schedule classes, 11 categories)
|   |   +-- Tenancy/
|   |       +-- TenantContext.cs         # Immutable record (TenantId, Subdomain, Plan, ConnectionString...)
|   |       +-- TenantEntity.cs          # DB entity mapped from Catalog query
|   |       +-- TenantMiddleware.cs      # Subdomain extraction, validation, resolution
|   |       +-- TenantResolver.cs        # Catalog DB query + ConcurrentDictionary cache (5-min TTL)
|   |       +-- ITenantResolver.cs       # Interface
|   |       +-- TenancyServiceExtensions.cs  # AddMultiTenancy() + UseTenantResolution()
|   |       +-- TenantHttpContextExtensions.cs  # GetTenantContext() / RequireTenantContext()
|   |
|   +-- Features/                       # Feature modules (one folder per domain)
|   |   +-- Auth/                       # JWT login, refresh, roles
|   |   |   +-- IAuthManager.cs
|   |   |   +-- AuthManager.cs
|   |   |   +-- AuthContracts.cs        # DTOs (LoginRequest, TokenResponse, etc.)
|   |   |   +-- AuthEndpoints.cs        # Minimal API route mappings
|   |   +-- Clients/                    # Patient records
|   |   |   +-- IClientManager.cs
|   |   |   +-- ClientManager.cs
|   |   |   +-- ClientContracts.cs
|   |   |   +-- ClientEndpoints.cs
|   |   +-- MasterProducts/            # Platform-wide catalogue (ICatalogDb)
|   |   |   +-- IMasterProductManager.cs
|   |   |   +-- MasterProductManager.cs
|   |   |   +-- MasterProductContracts.cs
|   |   |   +-- MasterProductEndpoints.cs
|   |   +-- Products/                   # Tenant inventory (ITenantDb)
|   |   |   +-- IProductManager.cs
|   |   |   +-- ProductManager.cs
|   |   |   +-- ProductContracts.cs
|   |   |   +-- ProductEndpoints.cs
|   |   +-- Services/                   # Service catalogue
|   |   +-- Bookings/                   # Appointments
|   |   +-- Schedules/                  # Employee rosters
|   |   +-- Employees/                  # Staff management
|   |   +-- AiChat/                     # AI consultant
|   |   |   +-- IAiChatManager.cs
|   |   |   +-- AiChatManager.cs
|   |   |   +-- AiChatContracts.cs
|   |   |   +-- AiChatEndpoints.cs
|   |   |   +-- Tools/
|   |   |       +-- IAiToolExecutor.cs  # Interface + ToolExecutionResult record
|   |   |       +-- AiToolExecutor.cs   # 3 tools: search_knowledge, list_services, check_availability
|   |   +-- Knowledge/                  # AI knowledge base
|   |   +-- Notifications/              # Email (DryRun/SMTP) + SMS Broadcast
|   |   +-- Reports/                    # Dashboard summary + inventory stats
|   |   +-- Platform/                   # Tenant provisioning + Stripe webhooks
|   |       +-- IProvisioningPipeline.cs
|   |       +-- ProvisioningPipeline.cs # STUB -- logs only, returns fake ID
|   |       +-- StripeContracts.cs
|   |       +-- StripeWebhookEndpoints.cs
|   |       +-- TenantManagementEndpoints.cs
|   |
|   +-- api.tests/                      # xUnit test project
|       +-- api.tests.csproj            # References: xUnit 2.9.3, NSubstitute 5.3.0
|       +-- Auth/AuthManagerTests.cs
|       +-- Clients/ClientContractTests.cs
|       +-- Services/ServiceContractTests.cs
|       +-- Bookings/BookingContractTests.cs
|       +-- Schedules/ScheduleContractTests.cs
|       +-- Employees/EmployeeContractTests.cs
|       +-- AiChat/AiChatTests.cs
|       +-- Knowledge/KnowledgeContractTests.cs
|       +-- Notifications/EmailTests.cs
|       +-- Notifications/SmsTests.cs
|       +-- Reports/ReportContractTests.cs
|       +-- MasterProducts/MasterProductContractTests.cs
|       +-- Products/ProductContractTests.cs
|       +-- Security/ConnectionStringProtectorTests.cs
|       +-- Seeding/DevSeedServiceTests.cs
|       +-- Tenancy/SubdomainExtractionTests.cs
|       +-- Tenancy/TenantResolverTests.cs
|       +-- Platform/ProvisioningContractTests.cs
|       +-- SampleTest.cs
|
+-- admin/                              # React 18 + Ant Design admin panel
|   +-- src/
|       +-- App.tsx                     # Router setup
|       +-- main.tsx                    # Entry point
|       +-- components/
|       |   +-- AdminLayout.tsx         # Sidebar + layout
|       |   +-- RequireAuth.tsx         # Auth guard
|       +-- pages/
|           +-- LoginPage.tsx
|           +-- DashboardPage.tsx       # Summary cards + inventory stats
|           +-- ClientsPage.tsx
|           +-- ServicesPage.tsx
|           +-- BookingsPage.tsx
|           +-- SchedulesPage.tsx
|           +-- EmployeesPage.tsx
|           +-- KnowledgePage.tsx
|           +-- ProductsPage.tsx        # Tabs: My Products / Import from Catalogue
|
+-- public/                             # React 19 + Tailwind marketing site
|   +-- src/
|       +-- App.tsx                     # Router setup
|       +-- main.tsx                    # Entry point
|       +-- components/
|       |   +-- Layout.tsx              # Header + footer
|       +-- pages/
|       |   +-- HomePage.tsx            # ZenPharm landing page
|       |   +-- AboutPage.tsx
|       |   +-- ServicesPage.tsx        # API-driven service listing
|       |   +-- PricingPage.tsx         # $79/$199/Enterprise tiers
|       |   +-- ContactPage.tsx
|       +-- features/
|           +-- ai-chat/
|               +-- AiConsultant.tsx    # SSE streaming chat widget
|
+-- .claude/
|   +-- tasks.md                        # Cross-session task tracking
|   +-- settings.json                   # Claude Code permissions
|   +-- deployment.md                   # Full Azure deployment guide
|   +-- rules/                          # Project rules (British English, coding style, etc.)
|   +-- agents/                         # Agent configurations
|   +-- commands/                       # Slash commands
|   +-- skills/                         # Skill definitions
|
+-- .github/
|   +-- workflows/
|       +-- ci.yml                      # CI: build + test (API, admin, public)
|       +-- deploy.yml                  # Deploy: manual trigger, staging/production
|
+-- CLAUDE.md                           # Project instructions for AI agents
+-- README.md
```

---

## 11. API Endpoints Summary

All feature endpoints are mapped in `Program.cs` via extension methods:

| Module | Base Path | Auth Required |
|--------|-----------|---------------|
| Health | `GET /health` | No |
| Auth | `/api/auth/*` | No (rate limited) |
| Clients | `/api/clients/*` | Yes |
| Services | `/api/services/*` | Yes (list is public) |
| Bookings | `/api/bookings/*` | Yes |
| Schedules | `/api/schedules/*` | Yes |
| Employees | `/api/employees/*` | Yes |
| AI Chat | `/api/ai-chat/*` | No (rate limited) |
| Knowledge | `/api/knowledge/*` | Yes |
| Reports | `/api/reports/*` | Yes |
| Master Products | `/api/master-products/*` | Yes |
| Products | `/api/products/*` | Yes |
| Platform | `/api/platform/*` | Yes (SuperAdmin) |
| Stripe Webhooks | `/api/webhooks/stripe` | No (Stripe signature verification) |

---

## 12. Security Considerations

### Current Security Measures

- JWT authentication with configurable expiry and role-based authorisation
- BCrypt password hashing (cost factor default)
- Account lockout after failed login attempts
- Rate limiting on auth and AI chat endpoints
- AES-256-GCM encryption for stored connection strings
- CORS with explicit origin allowlisting
- Parameterised SQL queries (Dapper) -- no raw string concatenation
- User secrets for sensitive configuration (never in source control)
- DryRun safety warnings in non-Development environments
- Swagger disabled in Production

### Security Gaps to Address

- `ProvisioningPipeline` is a **stub** -- no real tenant provisioning yet
- Stripe webhook signature verification is not yet implemented
- No CSRF protection (relies on JWT Bearer tokens, which are not vulnerable to CSRF)
- No input validation library (e.g. FluentValidation) -- validation is manual in endpoints
- No audit logging for admin actions
- No IP allowlisting for platform management endpoints

---

## 13. Upstream Dependency: zentech-biz

ZenPharm was forked from `zentech-biz` (located at `C:\repos\zentech-biz`). The generic template provides:

- Multi-tenancy infrastructure (TenantMiddleware, TenantResolver, Catalog/Tenant DB split)
- All 11 core modules (Auth, Clients, Services, Bookings, Schedules, Employees, AiChat, Knowledge, Notifications, Reports, Platform)
- CI/CD workflows
- Frontend scaffolds (admin + public)

ZenPharm adds pharmacy-specific customisations on top:
- 6 additional database migrations (005-006 catalog, 011-014 tenant)
- MasterProducts and TenantProducts modules
- Pharmacy roles, patient fields, schedule classes
- Dev seed data with 30 pharmacy products
- ZenPharm branding throughout frontends

**Important**: Changes to the generic template in `zentech-biz` are not automatically propagated to ZenPharm. If upstream improvements are needed, they must be manually cherry-picked or rebased.

---

## 14. Quick Reference

### Test Naming Convention
```
ClassName_Method_Outcome
```
Example: `AuthManager_Login_ReturnsTokenOnValidCredentials`

### Adding a New Feature Module

1. Create `Features/NewFeature/` directory
2. Add `INewFeatureManager.cs` (public interface)
3. Add `NewFeatureManager.cs` (internal sealed class, primary constructor with ITenantDb or ICatalogDb)
4. Add `NewFeatureContracts.cs` (DTOs as sealed records)
5. Add `NewFeatureEndpoints.cs` (static class with `MapNewFeatureEndpoints()` extension method)
6. Register in `Program.cs`: `builder.Services.AddScoped<INewFeatureManager, NewFeatureManager>()`
7. Map endpoints in `Program.cs`: `app.MapNewFeatureEndpoints()`
8. Add tests in `api.tests/NewFeature/`
9. Add migration if new tables are needed (append to `TenantDdl` or `CatalogDdl` array)

### Adding a New AI Tool

1. Add tool definition to `AiToolExecutor.GetToolDefinitions()` (follows Claude tool_use schema)
2. Add case to the `switch` in `AiToolExecutor.ExecuteAsync()`
3. Implement the `ExecuteXxxAsync()` private method (resolve manager via `IServiceProvider.CreateScope()`)
4. Add tests in `api.tests/AiChat/AiChatTests.cs`

### Adding a New Database Migration

Append a new entry to the `CatalogDdl` or `TenantDdl` array in the respective migration class. Always:
- Use `IF NOT EXISTS` guards for idempotency
- Use `EXEC()` for constraints/indexes after `ALTER TABLE ADD` in the same batch
- Use sequential numbering (e.g. `015_NewTable`)
- Never modify existing migrations -- only append new ones
