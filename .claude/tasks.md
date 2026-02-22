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

### Phase 1: 从 SMCP 抽取通用模板
- **状态**: in_progress
- **优先级**: high
- **创建**: 2026-02-21
- **更新**: 2026-02-21
- **描述**: 从 SMCP + zentech-website 抽取通用代码，建立模板项目骨架
- **子任务**:
  1. [x] 项目骨架 — .NET 8 API + React 19 前台 + React 18 后台 *(2026-02-22, 86 files, all build green)*
  2. [ ] 认证模块 — JWT + 角色权限 (从 SMCP 抽取)
  3. [ ] 客户管理 — 通用 Client CRUD (SMCP Customer 通用化)
  4. [ ] 服务/产品目录 — ServiceItem 抽象 (SMCP Product/Category)
  5. [ ] 预约系统 — Booking 通用化 (SMCP PickupTimeSlot)
  6. [ ] 排班系统 — Schedule 通用化 (SMCP Roster)
  7. [ ] 员工管理 — Employee CRUD (从 SMCP 抽取)
  8. [ ] AI 引擎 — Claude API + SSE + Tool Use 框架 (zentech + SMCP)
  9. [ ] AI 知识库 — 向量搜索 + 对话持久化 (zentech-website)
  10. [ ] SMS 通知 — SMS Broadcast (从 SMCP 抽取)
  11. [ ] Email 通知 — 选择服务商并集成
  12. [ ] 前台官网模板 — React 19 + Tailwind (zentech-website 结构)
  13. [ ] 报表 Dashboard — 通用图表框架 (SMCP Dashboard)
  14. [ ] Azure 部署流水线 — GitHub Actions + App Service + SWA
  - **参考源码**:
  - SMCP API: `C:\repos\smcp\smcp.api\smcp.api\Features\`
  - SMCP Admin: `C:\repos\smcp\smcp.admin\`
  - SMCP Public: `C:\repos\smcp\smcp.public\`
  - Zentech AI: `C:\repos\zentech-website\api\ZentechWebsite.Api\Features\AiConsultant\`
  - Zentech Frontend: `C:\repos\zentech-website\frontend\`

---

## Pending (待处理)

### Phase 2: 首个垂直 — 乐器行
- **状态**: pending
- **优先级**: high
- **创建**: 2026-02-21
- **更新**: 2026-02-21
- **描述**: 在模板基础上定制乐器行垂直方案
- **功能**: 教室预约、学生/家长管理、老师排班、乐器销售、AI 课程顾问
- **前置**: Phase 1 完成

---

## Completed (已完成)

### Phase 0: 模板架构设计
- **状态**: completed
- **优先级**: high
- **创建**: 2026-02-21
- **更新**: 2026-02-21
- **描述**: 确定模板架构、模块清单、运行模式、技术栈、首个垂直行业
