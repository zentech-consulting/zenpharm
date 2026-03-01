# CLAUDE.md

## Project Overview

zentech-biz 是 Zentech Consulting 的通用行业管理平台模板。从 SMCP (药房管理系统) 抽取通用能力，快速生成不同行业的垂直管理方案。

**定位**: 内部脚手架，对外以"行业专属 AI 管理方案"交付，每个客户独立实例部署。

| 组件 | 技术栈 | 用途 | 端口 |
|------|--------|------|------|
| api | .NET 8 + Dapper + SQL Server | REST API 后端 | :51003 |
| public | React 19 + TypeScript + Vite + Tailwind | 前台客户网站 + AI 顾问 | :51000 |
| admin | React 18 + TypeScript + Vite + Ant Design | 后台管理面板 | :51001 |

**代码来源**: 从 `C:\repos\smcp` 和 `C:\repos\zentech-website` 抽取通用模块。

## Task Management (重要)

### 会话级任务 (TodoWrite)
当前会话中的任务用 TodoWrite 工具管理：
1. **新任务** → 立即添加到 Todo List
2. **开始工作** → 标记为 `in_progress`
3. **完成任务** → 立即标记为 `completed`

### 长期任务 (.claude/tasks.md)
跨会话的长期任务记录在 `.claude/tasks.md` 文件中。

**必须遵守**:
1. 会话开始时 → 检查 `.claude/tasks.md`
2. 制定计划后 → **立即**写入 `.claude/tasks.md`（不要等到最后！）
3. 任务完成时 → 更新状态为 completed

## 核心模块

从 SMCP + zentech-website 抽取以下 11 个模块：

| 模块 | 来源 | 说明 |
|------|------|------|
| 认证 (JWT + 角色) | SMCP | 用户登录、角色权限 |
| 客户管理 | SMCP | 通用 Client CRUD |
| 服务/产品目录 | SMCP | ServiceItem 抽象 |
| 预约系统 | SMCP | Booking 通用化 |
| 排班系统 | SMCP | Schedule 通用化 |
| 员工管理 | SMCP | 员工 CRUD + 角色 |
| AI 顾问 + Tool Use | zentech + SMCP | Claude API + SSE 流式 + Tool 注册框架 |
| AI 知识库 | zentech-website | 向量搜索 + 对话持久化 |
| SMS 通知 | SMCP | SMS Broadcast |
| Email 通知 | 待定 | 探索替代方案 |
| 报表 Dashboard | SMCP | 通用图表框架 |

## 行业配置包

每个新垂直通过填写配置包实现定制，不改核心代码：

- **实体模型** — 行业管什么 (学生、教室、课程...)
- **服务目录** — 卖什么 (钢琴课 $60/半小时...)
- **预约规则** — 怎么约 (按教室、按老师、时长)
- **员工角色** — 谁干什么 (老师、前台、店长)
- **AI 知识库** — AI 懂什么 (乐器推荐、课程介绍)
- **AI 工具** — AI 能做什么 (查空位、推荐课程、下预约)
- **品牌配置** — 长什么样 (Logo、主色、slogan)
- **通知模板** — 发什么通知 (预约确认、课程提醒)

## Quick Commands

```bash
# API
cd api && dotnet run                    # :51003

# 前台
cd public && npm run dev                # :51000

# 后台
cd admin && npm run dev                 # :51001
```

## Project Structure

```
zentech-biz/
├── api/                    # .NET 8 API
│   ├── Features/           # 按功能模块组织
│   │   ├── Auth/           # JWT 认证
│   │   ├── Clients/        # 客户管理
│   │   ├── Services/       # 服务/产品目录
│   │   ├── Bookings/       # 预约系统
│   │   ├── Schedules/      # 排班系统
│   │   ├── Employees/      # 员工管理
│   │   ├── AiChat/         # AI 顾问 + Tool Use
│   │   ├── Knowledge/      # AI 知识库
│   │   ├── Notifications/  # SMS + Email
│   │   └── Reports/        # 报表
│   └── api.tests/          # xUnit 测试
├── public/                 # React 19 前台
│   └── src/
│       ├── pages/          # 页面组件
│       ├── components/     # 通用组件
│       └── features/       # AI Chat 等功能模块
├── admin/                  # React 18 后台
│   └── src/
│       ├── pages/          # 管理页面
│       ├── components/     # 通用组件
│       └── features/       # 各管理模块
├── .claude/
│   ├── tasks.md            # 长期任务
│   ├── settings.json       # 权限配置
│   └── commands/           # 斜杠命令
└── .github/
    └── workflows/          # CI/CD
```

## Key Files

| 文件 | 用途 |
|------|------|
| `api/appsettings.json` | API 配置 |
| `api/Features/AiChat/` | AI 顾问核心 |
| `api/Features/AiChat/Tools/` | 行业 Tool 注册表 |
| `.claude/tasks.md` | 任务追踪 |

## 重要规则

### 不连接生产数据库
启动 API 前**必须**检查连接字符串，确认指向开发/QAT 数据库。
`.NET 配置优先级`: 环境变量 > launchSettings > appsettings.{env}.json > appsettings.json

### API 密钥安全
绝对不能将 API Key、密码写入代码或配置文件。使用 `dotnet user-secrets`。

### Git 规则
- 功能分支开发，PR 合并
- 禁止自动推送到 main/master

## Documentation Rules

- 文档更新在 `.claude/` 目录
- CLAUDE.md 保持简洁，详细内容拆分到 `.claude/` 外部文件
- 根目录禁止创建 CLAUDE-*.md
