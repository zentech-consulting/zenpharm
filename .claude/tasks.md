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

### Phase 2: 首个垂直 — ZenPharm 药房 SaaS

- **状态**: pending
- **优先级**: high
- **创建**: 2026-02-21
- **更新**: 2026-02-27
- **描述**: 从 zentech-biz 模板 fork 出 zenpharm 项目，定制药房行业垂直方案
- **详细方案**: `zenpharm-saas-product-plan.md`
- **前置**: Phase 1 模板完成
- **药房专属功能**:
  - 共享产品库 (master_products 药品字段: schedule_class, PBS code, active_ingredients)
  - 产品模板包 ("社区药房标配" 500+ 产品)
  - 药品合规逻辑 (S2 自由销售, S3 药剂师审核, S4 仅展示)
  - PBS Public API 对接 (政府补贴药品目录)
  - AI 药房助手 Tools (药物交互查询、库存推荐、营销文案)
  - AI 知识库内容 (药品信息、健康建议)
  - zenpharm.com.au 品牌营销内容
  - 在线商店 (Premium 功能, Phase 2+)

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
