# 任务管理

> 此文件用于跨会话的长期任务追踪。Claude Code 在会话开始时应读取此文件。

## 格式说明

```markdown
### 任务标题
- **状态**: pending | in_progress | completed | blocked
- **优先级**: high | medium | low
- **创建**: YYYY-MM-DD
- **更新**: YYYY-MM-DD
- **描述**: 任务详情
```

---

## In Progress (进行中)

### Phase 1: 通用 SaaS 模板 — 基建 + 核心模块

- **状态**: completed
- **优先级**: high
- **创建**: 2026-02-21
- **更新**: 2026-03-01
- **描述**: zentech-biz 作为通用行业 SaaS 模板，完善通用基建和核心模块。完成后可 fork 出 zenpharm 等行业垂直项目。
- **设计参考**: `zenpharm-saas-product-plan.md` (ZenPharm 方案中的通用部分回馈模板)
- **子任务**:
  1. [x] 项目骨架 — .NET 8 API + React 19 前台 + React 18 后台 *(2026-02-22, 86 files, all build green)*
  2. [x] **多租户基建** — TenantMiddleware + Catalog DB + Database-per-Tenant 架构 *(2026-02-27, 26 tests pass)*
  3. [x] **Stripe 订阅 + 自动开通** (stub) — Webhook receiver + ProvisioningPipeline stub *(2026-03-01)*
  4. [x] **认证模块** — JWT + 角色权限 (从 SMCP 抽取，适配多租户) *(2026-03-01, 16 tests)*
  5. [x] 客户管理 — Client CRUD with Dapper *(2026-03-01, 6 tests)*
  6. [x] 服务/产品目录 — Service CRUD with category filter *(2026-03-01, 6 tests)*
  7. [x] 预约系统 — Booking CRUD with JOINs, availability slots *(2026-03-01, 10 tests)*
  8. [x] 排班系统 — Schedule CRUD with auto-generate Mon-Fri *(2026-03-01, 8 tests)*
  9. [x] 员工管理 — Employee CRUD with role filter *(2026-03-01, 6 tests)*
  10. [x] AI 引擎 — Claude API + SSE streaming + Tool Use (3 tools) *(2026-03-01, 22 tests)*
  11. [x] AI 知识库 — SQL LIKE search + CRUD *(2026-03-01, 6 tests)*
  12. [x] SMS 通知 — SMS Broadcast with NormalisePhone *(2026-03-01, 8 tests)*
  13. [x] Email 通知 — DryRun + SMTP services *(2026-03-01, 4 tests)*
  14. [x] 前台官网 — Services page (API), About page, AI Consultant widget with SSE *(2026-03-01)*
  15. [x] 管理后台 — 7 CRUD pages (Clients, Services, Bookings, Schedules, Employees, Knowledge, Dashboard) *(2026-03-01)*
  16. [x] 报表 Dashboard — COUNT + SUM queries + daily stats *(2026-03-01, 6 tests)*
  17. [x] Azure 部署 — CI/CD workflows + Dockerfile *(2026-03-01)*
- **最终统计**: 144 tests pass, 0 warnings, 0 errors. All 3 projects build successfully.
- **参考源码**:
  - SMCP API: `C:\repos\smcp\smcp.api\smcp.api\Features\`
  - SMCP Admin: `C:\repos\smcp\smcp.admin\`
  - SMCP Public: `C:\repos\smcp\smcp.public\`
  - Zentech AI: `C:\repos\zentech-website\api\ZentechWebsite.Api\Features\AiConsultant\`
  - Zentech Frontend: `C:\repos\zentech-website\frontend\`

---

## Pending (待处理)

### Phase 2: ZenPharm MVP — Phase 1 Basic Tier

- **状态**: completed
- **优先级**: high
- **创建**: 2026-02-21
- **更新**: 2026-03-02
- **描述**: Pharmacy-specific customisation of zentech-biz template (Basic tier features)
- **PR**: #2 merged
- **子任务**:
  1. [x] Database schema — 6 pharmacy migrations (MasterProducts cols, Tenants cols, Clients cols, Employee roles, TenantProducts, StockMovements)
  2. [x] MasterProducts module (ICatalogDb) — platform-wide catalogue with PBS/schedule/barcode
  3. [x] TenantProducts module (ITenantDb) — import, stock movements, low-stock/expiry alerts
  4. [x] Clients pharmacy fields — DateOfBirth, Allergies, MedicationNotes, Tags
  5. [x] Employee pharmacy roles — pharmacist, dispense_technician, pharmacy_assistant, cashier, manager, staff
  6. [x] Reports enhancement — TotalProducts, LowStockCount, ExpiringCount
  7. [x] Admin Products page — tabs (My Products / Import from Catalogue), stock modals, schedule class colour tags
  8. [x] Dashboard inventory cards — Total Products, Low Stock, Expiring Soon
  9. [x] ZenPharm branding — public homepage, About, Pricing ($79/$199/Enterprise), admin sidebar
- **统计**: 174 tests pass, 0 warnings, 0 errors

### Dev Seed Data — E2E Testing Support

- **状态**: completed
- **优先级**: high
- **创建**: 2026-03-02
- **更新**: 2026-03-02
- **描述**: Auto-seed dev tenant, admin user, plan, and 30 pharmacy products on startup (dev only)
- **PR**: #3 (6 commits, pending merge)
- **子任务**:
  1. [x] IDevSeedService + DevSeedService (idempotent, dev-only)
  2. [x] PharmacyMasterProductData (30 products, 4 schedule classes)
  3. [x] Program.cs integration (double IsDevelopment guard)
  4. [x] 10 tests (184 total passing)
  5. [x] Code review fixes (internal interface, UserSecretsId, deprecate SQL seeds)

### Pre-Deployment Blockers

- **状态**: completed
- **优先级**: critical
- **创建**: 2026-03-02
- **更新**: 2026-03-02
- **描述**: Issues surfaced by security review, resolved in PR #3
- **子任务**:
  1. [x] **ConnectionString encryption** — AES-256-GCM via ConnectionStringProtector; key from `Security:ConnectionStringKey` config; required in production, dev passthrough; backward compatible
  2. [x] **appsettings.Production.json** — DryRun=false for Email/SMS/AiChat; startup warning if DryRun=true in non-Development
  3. [x] **User-secrets migration** — UserSecretsId changed to zenpharm-api; devs must re-init secrets

### Azure Deployment — First Production Deploy

- **状态**: completed
- **优先级**: critical
- **创建**: 2026-03-02
- **更新**: 2026-03-02
- **描述**: Deploy ZenPharm to Azure under Zentech tenant
- **PR**: #3 (includes deployment fixes)
- **子任务**:
  1. [x] Resource Group: zenpharm (australiaeast)
  2. [x] SQL Server: zenpharm-sql.database.windows.net (Basic 5 DTU x2)
  3. [x] App Service: zenpharm-api-au (B1 Linux, .NET 8)
  4. [x] Static Web Apps: zenpharm-admin + zenpharm-public (eastasia)
  5. [x] Migration retry logic (Azure SQL cold start)
  6. [x] API base URL env var for production frontends
  7. [x] CORS configuration
  8. [x] Deployment documentation (.claude/deployment.md)
  9. [x] Project handoff document (.claude/handoff.md)
- **URLs**:
  - API: https://zenpharm-api-au.azurewebsites.net
  - Admin: https://kind-tree-093309e00.4.azurestaticapps.net
  - Public: https://zealous-flower-0e43e0200.1.azurestaticapps.net

### Phase 2.5: ZenPharm Premium Features

- **状态**: pending
- **优先级**: high
- **创建**: 2026-03-02
- **更新**: 2026-03-02
- **描述**: Phase 2 Premium tier features for ZenPharm
- **前置**: Phase 2 Basic completed
- **功能清单**:
  - 产品模板包 ("社区药房标配" 500+ 产品预填充)
  - 药品合规逻辑 (S2 自由销售, S3 药剂师审核, S4 仅展示)
  - PBS Public API 对接 (政府补贴药品目录)
  - AI 药房助手 Tools (药物交互查询、库存推荐、营销文案)
  - AI 知识库内容 (药品信息、健康建议)
  - 在线商店 (click-and-collect)
  - SMS 通知 (prescription ready, appointment reminders)
  - 高级报表 (stock turnover, sales trends, expiry waste)

### Phase 3: 更多垂直行业

- **状态**: pending
- **优先级**: medium
- **创建**: 2026-02-27
- **更新**: 2026-02-27
- **描述**: 基于同一模板 fork 更多行业项目
- **候选**: 乐器行、美容院、健身房、诊所等
- **前置**: Phase 1 完成 + Phase 2 验证模板可行性

---

## Completed (已完成)

### Phase 0: 模板架构设计

- **状态**: completed
- **优先级**: high
- **创建**: 2026-02-21
- **更新**: 2026-02-21
- **描述**: 确定模板架构、模块清单、运行模式、技术栈、首个垂直行业
