# ZenPharm Azure Deployment Guide

## Azure Infrastructure

| Resource | Details |
|----------|---------|
| **Subscription** | Zentech (`21a284f6-2759-476d-9182-9c75785417a7`) |
| **Tenant ID** | `e06eaa67-a376-4150-af37-10bf3645a1de` |
| **Resource Group** | `zenpharm` (Australia East) |

### SQL Server

- **Server**: `zenpharm-sql.database.windows.net`
- **Admin user**: `zenpharmadmin`
- **Databases**:
  - `ZenPharmCatalog` — tenant registry, plans, subscriptions, master products (Basic 5 DTU)
  - `ZenPharmTenant` — per-tenant operational data (Basic 5 DTU)

### App Service (API)

- **Plan**: `zenpharm-plan` (B1 Linux)
- **Web App**: `zenpharm-api-au`
- **URL**: https://zenpharm-api-au.azurewebsites.net
- **Runtime**: .NET 8 on Linux
- **Startup command**: `dotnet api.dll`
- **Build setting**: `SCM_DO_BUILD_DURING_DEPLOYMENT=false` (pre-built deployment)

### Static Web Apps (Frontends)

| App | Name | URL | Region |
|-----|------|-----|--------|
| Admin | `zenpharm-admin` | https://kind-tree-093309e00.4.azurestaticapps.net | East Asia |
| Public | `zenpharm-public` | https://zealous-flower-0e43e0200.1.azurestaticapps.net | East Asia |

> **Note**: Static Web Apps are in East Asia because SWA is not available in Australia East.

---

## App Settings (zenpharm-api-au)

All settings are configured in the Azure Portal under Configuration > Application Settings.

| Setting | Description |
|---------|-------------|
| `ConnectionStrings__CatalogConnection` | SQL Azure connection to ZenPharmCatalog |
| `ConnectionStrings__DefaultConnection` | SQL Azure connection to ZenPharmTenant |
| `Jwt__SecretKey` | 64-character random string |
| `Jwt__Issuer` | `zenpharm` |
| `Jwt__Audience` | `zenpharm-clients` |
| `Security__ConnectionStringKey` | Base64-encoded 32-byte AES key |
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `SCM_DO_BUILD_DURING_DEPLOYMENT` | `false` |

---

## Deployment Steps

### API Deployment

**1. Build the release artefact:**

```bash
dotnet publish api -c Release -r linux-x64 --self-contained false -o api/publish
```

**2. Create the deployment ZIP (forward slashes required):**

The ZIP archive **must** use forward-slash paths. Linux App Service will fail to locate files if backslashes are used. Do **not** use PowerShell `Compress-Archive` — it creates backslash paths.

Use Python instead:

```python
import os, zipfile

with zipfile.ZipFile("api/deploy.zip", "w", zipfile.ZIP_DEFLATED) as zf:
    for root, dirs, files in os.walk("api/publish"):
        for f in files:
            full = os.path.join(root, f)
            arcname = os.path.relpath(full, "api/publish").replace(os.sep, "/")
            zf.write(full, arcname)
```

**3. Deploy to Azure:**

```bash
az webapp deployment source config-zip \
  --name zenpharm-api-au \
  --resource-group zenpharm \
  --src api/deploy.zip
```

### Admin Frontend Deployment

**1. Build:**

```bash
cd admin
VITE_API_BASE_URL=https://zenpharm-api-au.azurewebsites.net npm run build
```

**2. Add SPA navigation fallback:**

Create `admin/dist/staticwebapp.config.json`:

```json
{
  "navigationFallback": {
    "rewrite": "/index.html"
  }
}
```

**3. Get deployment token:**

```bash
TOKEN=$(az staticwebapp secrets list \
  --name zenpharm-admin \
  --resource-group zenpharm \
  --query "properties.apiKey" -o tsv)
```

**4. Deploy:**

```bash
swa deploy admin/dist --deployment-token "$TOKEN" --env production
```

### Public Frontend Deployment

Follows the same pattern as admin.

**1. Build:**

```bash
cd public
VITE_API_BASE_URL=https://zenpharm-api-au.azurewebsites.net npm run build
```

**2. Add SPA navigation fallback:**

Create `public/dist/staticwebapp.config.json` (same content as admin above).

**3. Get deployment token:**

```bash
TOKEN=$(az staticwebapp secrets list \
  --name zenpharm-public \
  --resource-group zenpharm \
  --query "properties.apiKey" -o tsv)
```

**4. Deploy:**

```bash
swa deploy public/dist --deployment-token "$TOKEN" --env production
```

---

## CORS Configuration

The API permits cross-origin requests from:

- `https://kind-tree-093309e00.4.azurestaticapps.net` (admin)
- `https://zealous-flower-0e43e0200.1.azurestaticapps.net` (public)
- `http://localhost:51000`, `http://localhost:51001` (development)

---

## Initial Production Setup

After the first deployment, the database tables are created automatically by the startup migrations, but you must seed the initial data manually.

### 1. Seed the Catalogue Database (ZenPharmCatalog)

Connect via Azure Portal Query Editor or SSMS and run:

```sql
-- Create a plan
INSERT INTO Plans (Name, PriceMonthly, PriceYearly, Features, MaxUsers, MaxProducts)
VALUES ('Basic', 79, 790, 'Core features', 5, 500);

-- Create the first tenant
INSERT INTO Tenants (Subdomain, DisplayName, ConnectionString, Status)
VALUES ('dev', 'Dev Pharmacy', '<DefaultConnection string>', 'Active');

-- Link tenant to plan via subscription
INSERT INTO Subscriptions (TenantId, PlanId, PlanName)
SELECT t.Id, p.Id, 'Basic'
FROM Tenants t, Plans p
WHERE t.Subdomain = 'dev' AND p.Name = 'Basic';
```

### 2. Seed the Tenant Database (ZenPharmTenant)

The tenant migration runs automatically when the first request for that tenant arrives. After it completes, create an admin user in the `AdminUsers` table.

> **Note**: `DevSeedService` only runs when `ASPNETCORE_ENVIRONMENT=Development`. Production data must be seeded manually via SQL.

---

## Known Issues and Gotchas

1. **Azure SQL Basic tier cold start** — The catalogue database can be slow to respond after idle periods. `CatalogMigration` includes a 3-retry loop with exponential backoff to handle this.

2. **ZIP path separators** — Deployment ZIPs **must** use forward slashes (`/`) in entry paths. The Linux App Service host will not resolve backslash paths. Always use the Python approach described above, never PowerShell `Compress-Archive`.

3. **SQL Server batch DDL** — Migration 005 uses `EXEC()` for constraints and indexes after `ALTER TABLE ADD` because SQL Server cannot mix DDL statements in certain batch contexts.

4. **Production environment differences** — In Production (`appsettings.Production.json`):
   - Swagger is disabled (only available in Development)
   - `DryRun=false` for Email, SMS, and AiChat (live integrations enabled)

---

## Security Notes

- **SQL admin password**: Stored exclusively in Azure App Settings — never committed to source control.
- **JWT secret key**: 64-character random string configured in App Settings.
- **Connection string encryption**: 32-byte AES-256-GCM key stored in App Settings as `Security__ConnectionStringKey`.
- **SQL connections**: All connection strings use `Encrypt=True; TrustServerCertificate=False`.
- **Swagger**: Disabled in Production to prevent API schema exposure.
