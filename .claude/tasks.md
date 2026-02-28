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

- **状态**: in_progress
- **优先级**: high
- **创建**: 2026-02-21
- **更新**: 2026-02-27
- **描述**: zentech-biz 作为通用行业 SaaS 模板，完善通用基建和核心模块。完成后可 fork 出 zenpharm 等行业垂直项目。
- **设计参考**: `zenpharm-saas-product-plan.md` (ZenPharm 方案中的通用部分回馈模板)
- **子任务**:
  1. [x] 项目骨架 — .NET 8 API + React 19 前台 + React 18 后台 *(2026-02-22, 86 files, all build green)*
  2. [x] **多租户基建** — TenantMiddleware + Catalog DB + Database-per-Tenant 架构 *(2026-02-27, 26 tests pass)*
     - Catalog DB schema (Tenants, Plans, Subscriptions, MasterProducts)
     - Tenant DB schema (AdminUsers, RefreshTokens)
     - TenantMiddleware: 子域名解析 → 注入 TenantContext (连接字符串、套餐、品牌)
     - ICatalogDb + ITenantDb 双数据库接口
     - TenantResolver: 子域名→租户, ConcurrentDictionary 缓存 5min TTL
     - CatalogMigration + TenantMigration 启动时运行
     - 所有 Feature Manager 构造函数已注入 ITenantDb
     - Platform endpoints stub (/api/platform/tenants)
     - Dev seed data (Basic/Premium plans, dev tenant, admin user)
  3. [ ] **Stripe 订阅 + 自动开通** — 注册 → 付款 → 自动创建租户 DB
     - Stripe Checkout Session → Webhook → Provisioning pipeline
     - 套餐管理 (Basic / Premium / Enterprise 框架)
  4. [ ] 认证模块 — JWT + 角色权限 (从 SMCP 抽取，适配多租户)
  5. [ ] 客户管理 — 通用 Client CRUD (SMCP Customer 通用化)
  6. [ ] 服务/产品目录 — ServiceItem 抽象 (SMCP Product/Category)
     - 通用产品目录框架，具体字段由行业 fork 扩展
  7. [ ] 预约系统 — Booking 通用化 (SMCP PickupTimeSlot)
  8. [ ] 排班系统 — Schedule 通用化 (SMCP Roster)
  9. [ ] 员工管理 — Employee CRUD (从 SMCP 抽取)
  10. [ ] AI 引擎 — Claude API + SSE + Tool Use 框架 (zentech + SMCP)
  11. [ ] AI 知识库 — 向量搜索 + 对话持久化 (zentech-website)
  12. [ ] SMS 通知 — SMS Broadcast (从 SMCP 抽取)
  13. [ ] Email 通知 — 选择服务商并集成
  14. [ ] 前台官网模板 — React 19 + Tailwind (通用营销首页 + 注册入口)
  15. [ ] 管理后台模板 — Ant Design (品牌定制、产品管理、客户、库存、员工、排班)
  16. [ ] 报表 Dashboard — 通用图表框架 (SMCP Dashboard)
  17. [ ] Azure 部署流水线 — GitHub Actions + App Service + SWA
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
