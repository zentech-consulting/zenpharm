# CLAUDE.md

## Project Overview

ZenPharm is an Australian pharmacy management SaaS platform. Forked from zentech-biz (generic SaaS template), customised for independent community pharmacies.

**Positioning**: Cloud-based pharmacy management — shared product catalogue, inventory, patient records, scheduling, and AI assistant.

| Component | Stack | Purpose | Port |
|-----------|-------|---------|------|
| api | .NET 8 + Dapper + SQL Server | REST API backend | :51003 |
| public | React 19 + TypeScript + Vite + Tailwind | Marketing site + AI Consultant | :51000 |
| admin | React 18 + TypeScript + Vite + Ant Design | Pharmacy admin panel | :51001 |

## Task Management

### Session tasks (TodoWrite)
1. **New task** → Add to Todo List immediately
2. **Starting work** → Mark as `in_progress`
3. **Done** → Mark as `completed`

### Long-term tasks (.claude/tasks.md)
Cross-session tasks tracked in `.claude/tasks.md`.

## Core Modules

| Module | Description |
|--------|-------------|
| Auth (JWT + roles) | User login, role-based access |
| Clients | Patient records with allergies, medications, DOB |
| MasterProducts | Shared product catalogue (PBS, schedule class, barcode) |
| Products | Per-tenant inventory (import, stock, expiry, pricing) |
| Services | Service items (consultations, vaccinations) |
| Bookings | Appointment system |
| Schedules | Employee roster management |
| Employees | Staff CRUD (pharmacist, technician, assistant, cashier, manager) |
| AI Chat + Tool Use | Claude API + SSE streaming + Tool registry |
| Knowledge Base | SQL LIKE search + conversation persistence |
| SMS / Email | Notifications |
| Reports | Dashboard with inventory stats |

## Pharmacy-Specific Features

- **Schedule classes**: Unscheduled, S2, S3, S4 (Australian TGA classification)
- **PBS item codes**: Pharmaceutical Benefits Scheme integration-ready
- **Stock movements**: stock_in, stock_out, adjustment, expired, return
- **Low stock alerts**: Configurable reorder levels per product
- **Expiry tracking**: 30-day expiry alerts on dashboard
- **Patient fields**: Date of birth, allergies, medication notes, tags

## Quick Commands

```bash
# API
cd api && dotnet run                    # :51003

# Public site
cd public && npm run dev                # :51000

# Admin panel
cd admin && npm run dev                 # :51001
```

## Project Structure

```
zenpharm/
├── api/                    # .NET 8 API
│   ├── Features/
│   │   ├── Auth/           # JWT authentication
│   │   ├── Clients/        # Patient management
│   │   ├── MasterProducts/ # Shared catalogue (ICatalogDb)
│   │   ├── Products/       # Tenant inventory (ITenantDb)
│   │   ├── Services/       # Service items
│   │   ├── Bookings/       # Appointments
│   │   ├── Schedules/      # Employee rosters
│   │   ├── Employees/      # Staff management
│   │   ├── AiChat/         # AI consultant + Tool Use
│   │   ├── Knowledge/      # AI knowledge base
│   │   ├── Notifications/  # SMS + Email
│   │   └── Reports/        # Dashboard + inventory stats
│   └── api.tests/          # xUnit tests
├── public/                 # React 19 marketing site
├── admin/                  # React 18 admin panel
├── .claude/
│   ├── tasks.md            # Task tracking
│   └── settings.json       # Permissions
└── .github/
    └── workflows/          # CI/CD
```

## Key Files

| File | Purpose |
|------|---------|
| `api/appsettings.json` | API configuration |
| `api/Features/MasterProducts/` | Shared product catalogue |
| `api/Features/Products/` | Tenant inventory management |
| `api/Features/AiChat/` | AI consultant core |
| `.claude/tasks.md` | Task tracking |

## Important Rules

### Never connect to production database
Before starting the API, **always** verify the connection string points to dev/QAT.
`.NET config priority`: Environment variables > launchSettings > appsettings.{env}.json > appsettings.json

### API key security
Never write API keys, passwords, or secrets into code or config files. Use `dotnet user-secrets`.

### Git rules
- Feature branches, PR merges
- Never push directly to main/master

## Documentation Rules

- Documentation updates go in `.claude/` directory
- Keep CLAUDE.md concise, detailed content in `.claude/` files
- Do not create CLAUDE-*.md files in the root directory
