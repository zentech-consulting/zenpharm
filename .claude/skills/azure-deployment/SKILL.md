---
name: azure-deployment
description: Azure 部署工作流技能。涵盖从代码提交到生产验证的完整 CI/CD 流水线，包括分支策略、GitHub Actions 自动部署、SQL 迁移、APIM 管理、环境隔离和安全检查清单。基于 SMCP 项目的成熟实战经验提炼。
---

# Azure Deployment Skill

GitHub + Azure 全自动部署工作流。经 SMCP 项目生产环境验证的成熟模式。

## 核心流水线

```
Code Ready → Build & Test → Push to Staging → Verify Staging →
Create PR → SQL Migration (if needed) → Merge PR →
APIM Import (if new endpoints) → Verify Production
```

## 分支策略

```
feature/xxx  →  staging  →  main/master
     ↓            ↓           ↓
   Local       Staging      Production
```

| 分支 | 用途 | 自动部署 |
|------|------|----------|
| `feature/*` | 功能开发 | 无 (仅本地) |
| staging 分支 (如 `qat`) | 测试验证 | 推送即部署到 Staging |
| 主分支 (如 `master`/`main`) | 生产环境 | PR 合并即部署 |

**关键规则**:
- **禁止直接推送主分支** — 必须通过 PR + Approval
- 所有变更必须先在 Staging 测试通过
- Claude 禁止合并 PR 到主分支，用户必须手动合并

---

## Phase 1: Pre-Deployment Checks

### 1.1 Build All Projects

```bash
# .NET API
cd <api-project-path> && dotnet build

# React/Vite Frontend
cd <frontend-path> && npm run build

# Admin (if exists)
cd <admin-path> && npm run build
```

**要求**: 全部 **0 errors** 编译通过。Warnings 可接受。

### 1.2 Run Tests

```bash
# .NET tests
cd <test-project-path> && dotnet test

# Frontend tests
cd <frontend-path> && npm test
```

记录已知的预期失败 (如 KeyVault 集成测试在本地会失败)，其余必须全部通过。

### 1.3 Code Review Checklist

- [ ] 无硬编码的密钥 (API keys, 密码, 连接字符串)
- [ ] 无遗留的 `console.log` 调试语句
- [ ] SQL 查询使用参数化输入 (防 SQL 注入)
- [ ] 用户输入有服务端验证
- [ ] CORS 配置正确匹配目标环境
- [ ] 无测试数据混入生产代码
- [ ] 敏感文件已加入 `.gitignore`

### 1.4 Review Changes

```bash
# 查看 staging 与 production 的差异
git diff <main-branch>...<staging-branch> --stat
git diff <main-branch>...<staging-branch>

# 查看提交历史
git log <main-branch>..<staging-branch> --oneline
```

---

## Phase 2: Staging Deployment

### 2.1 Push to Staging

```bash
git checkout <staging-branch>
git merge feature/your-feature
git push origin <staging-branch>
```

推送触发 **GitHub Actions** 自动部署。

### 2.2 GitHub Actions 工作流配置

每个服务需要独立的工作流文件:

| 服务类型 | 工作流触发 | 部署目标 |
|----------|-----------|----------|
| .NET API | `<staging-branch>` + `api/**` 变更 | Azure App Service |
| React Frontend | `<staging-branch>` + `frontend/**` 变更 | Azure Static Web App |
| Admin Frontend | `<staging-branch>` + `admin/**` 变更 | Azure Static Web App |

### 2.3 Verify Staging

```bash
# 检查 GitHub Actions 状态
gh run list --branch <staging-branch> --limit 5

# 检查 API 健康 (App Insights)
az monitor app-insights query \
  --app <staging-insights-name> \
  --resource-group <resource-group> \
  --analytics-query "requests | where timestamp > ago(1h) | summarize total=count(), failed=countif(resultCode != '200') by bin(timestamp, 5m) | order by timestamp desc"

# 检查异常
az monitor app-insights query \
  --app <staging-insights-name> \
  --resource-group <resource-group> \
  --analytics-query "exceptions | where timestamp > ago(1h) | project timestamp, type, outerMessage | order by timestamp desc | take 10"

# 健康检查 endpoint
curl https://<staging-api-url>/api/health
```

---

## Phase 3: SQL Migration (If Needed)

### 3.1 迁移脚本规范

- **命名格式**: `YYYY-MM-DD_Description.sql`
- **存储位置**: `<api-project>/SQL/Migrations/`
- **幂等性**: 始终使用 `IF NOT EXISTS` / `IF COL_LENGTH(...) IS NULL` 模式
- **执行顺序**: Staging 先行，验证后再 Production

### 3.2 使用 PowerShell + AAD Token 执行迁移

```powershell
# 获取 AAD Token
$token = az account get-access-token --resource https://database.windows.net/ --query accessToken --output tsv

# 连接数据库
$conn = New-Object System.Data.SqlClient.SqlConnection
$conn.ConnectionString = 'Server=tcp:<db-server>.database.windows.net,1433;Initial Catalog=<database-name>;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;'
$conn.AccessToken = $token
$conn.Open()

# 执行迁移
$script = Get-Content -Path "path/to/migration.sql" -Raw
$cmd = $conn.CreateCommand()
$cmd.CommandText = $script
$result = $cmd.ExecuteNonQuery()
Write-Host "Rows affected: $result"

$conn.Close()
```

> **⚠️ 不要使用 sqlcmd**: 旧版 sqlcmd (v15) 无法处理长 AAD token。PowerShell SqlClient + AccessToken 是推荐方式。

### 3.3 迁移安全规则

| 规则 | 说明 |
|------|------|
| 先 Staging 后 Production | 永远先在 Staging 执行并验证 |
| 幂等脚本 | 重复执行不应报错或产生副作用 |
| 备份确认 | Production 迁移前确认有备份 |
| 回滚计划 | 准备对应的回滚 SQL |

---

## Phase 4: Production Deployment

### 4.1 Create PR

```bash
gh pr create --base <main-branch> --head <staging-branch> \
  --title "Release vX.Y.Z: Brief description" \
  --body "$(cat <<'EOF'
## Summary
- Feature 1
- Feature 2
- Bug fix 1

## Database Migration
- [ ] Migration executed on Staging
- [ ] Migration executed on Production

## Config Changes
- List any Azure config changes needed
- CORS origins, App Settings, etc.

## Test Plan
- [ ] Feature 1 tested on Staging
- [ ] Feature 2 tested on Staging
- [ ] Mobile responsiveness checked
- [ ] API health verified

🤖 Generated with [Claude Code](https://claude.com/claude-code)
EOF
)"
```

### 4.2 Merge PR (USER ACTION)

> **Claude 禁止合并 PR 到主分支。** 用户必须在 GitHub 上手动合并。

合并后 GitHub Actions 自动部署:

| 服务类型 | 部署方式 |
|----------|----------|
| .NET API | App Service (Kudu zip deploy) |
| React Frontend | Azure Static Web App |
| Admin Frontend | Azure Static Web App |

### 4.3 APIM Import (If New API Endpoints) ⚠️

> **如果项目使用 Azure API Management (APIM):**
> 不做这步，新 endpoint 在生产环境会返回 404！
> Staging 直连 App Service 不受影响，但 Production 通过 APIM 路由。

**操作步骤:**
1. Azure Portal → API Management → `<apim-name>`
2. APIs → 选择对应 API
3. Import OpenAPI: `https://<prod-api>.azurewebsites.net/swagger/v1/swagger.json`
4. 或手动添加 Operation (Method + URL pattern)

**需要导入的场景:**
- 新增整个模块/Controller → 所有 endpoint 都需要导入
- 已有模块下新增 endpoint → 只需导入新的 operation
- 仅修改已有 endpoint 逻辑 → 不需要更新 APIM

### 4.4 Verify Production

```bash
# 检查 GitHub Actions
gh run list --branch <main-branch> --limit 5

# 检查 API 健康
az monitor app-insights query \
  --app <prod-insights-name> \
  --resource-group <resource-group> \
  --analytics-query "requests | where timestamp > ago(30m) | summarize total=count(), failed=countif(resultCode !in ('200','201','204')) | project total, failed"

# 检查异常
az monitor app-insights query \
  --app <prod-insights-name> \
  --resource-group <resource-group> \
  --analytics-query "exceptions | where timestamp > ago(30m) | project timestamp, type, outerMessage | order by timestamp desc | take 10"
```

---

## Phase 5: Post-Deployment

### 5.1 CORS Verification

```bash
# 查看 App Service 的 CORS 配置
az webapp config appsettings list \
  --name <api-app-service> \
  --resource-group <resource-group> \
  --query "[?contains(name, 'Cors')]"
```

**CORS 隔离原则**: 每个环境的 API 只接受同环境前端的请求。

| 环境 | 允许的 Origins |
|------|---------------|
| Development | `http://localhost:<frontend-port>`, `http://localhost:<admin-port>` |
| Staging | Staging 前端 URL |
| Production | Production 前端 URL |

### 5.2 Sync Branches

PR 合并后同步 staging 与主分支:

```bash
git checkout <staging-branch>
git pull origin <staging-branch>
git merge origin/<main-branch>
git push origin <staging-branch>
```

### 5.3 Update Release Notes

记录部署版本、变更内容和已知问题。

---

## GitHub Actions 设置指南

### 前置条件

1. **GitHub Secrets** - 在 repo Settings → Secrets → Actions 中配置:

| Secret 类型 | 用途 |
|-------------|------|
| `AZURE_WEBAPP_PUBLISH_PROFILE_*` | App Service 部署凭证 |
| `AZURE_STATIC_WEB_APPS_API_TOKEN_*` | Static Web App 部署 token |

2. **GitHub Variables** - 环境相关配置:

| Variable 类型 | 用途 |
|--------------|------|
| `VITE_API_BASE_URL_*` | 前端编译时 API 地址 |

### .NET API 部署工作流模板

```yaml
name: Deploy API to <environment>

on:
  push:
    branches: ['<target-branch>']
    paths: ['<api-path>/**']

jobs:
  build-and-deploy:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '<dotnet-version>'

      - name: Build
        run: dotnet publish <api-project-path> -c Release -o ./publish

      - name: Deploy to Azure App Service
        uses: azure/webapps-deploy@v3
        with:
          app-name: '<app-service-name>'
          publish-profile: ${{ secrets.AZURE_WEBAPP_PUBLISH_PROFILE }}
          package: ./publish
```

### React/Vite Static Web App 工作流模板

```yaml
name: Deploy Frontend to <environment>

on:
  push:
    branches: ['<target-branch>']
    paths: ['<frontend-path>/**']

jobs:
  build-and-deploy:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Build
        run: |
          cd <frontend-path>
          npm ci
          npm run build
        env:
          VITE_API_BASE_URL: ${{ vars.VITE_API_BASE_URL }}

      - name: Deploy to Azure Static Web App
        uses: Azure/static-web-apps-deploy@v1
        with:
          azure_static_web_apps_api_token: ${{ secrets.AZURE_STATIC_WEB_APPS_API_TOKEN }}
          repo_token: ${{ secrets.GITHUB_TOKEN }}
          action: 'upload'
          app_location: '<frontend-path>'
          output_location: 'dist'
          skip_api_build: true
```

> **⚠️ 关键**: 工作流必须在 **ubuntu-latest** 上运行！Windows 构建的产物部署到 Linux App Service 会导致 503 错误 (路径分隔符不兼容)。

---

## 环境隔离

### 三环境架构

| 层面 | Development | Staging | Production |
|------|-------------|---------|------------|
| 数据库 | Staging DB (共享) | Staging DB | Production DB |
| API 网关 | 直连 localhost | 直连 App Service | 经 APIM (如有) |
| Key Vault | N/A (user-secrets) | Staging KV | Production KV |
| CORS | localhost origins | Staging URLs | Production URLs |
| 日志级别 | Debug | Debug/Info | Warning |
| 身份认证 | Azure CLI | Managed Identity | Managed Identity |

### .NET 配置优先级 ⚠️

```
配置加载顺序（后者覆盖前者）：
1. appsettings.json                              ← 基础配置
2. appsettings.{ASPNETCORE_ENVIRONMENT}.json    ← 环境特定配置
3. launchSettings.json environmentVariables      ← 启动时环境变量
4. Azure App Service Settings                    ← 最高优先级
```

**致命陷阱**: `launchSettings.json` 中的 `ConnectionStrings__DefaultConnection` 会覆盖所有 appsettings.json 配置！确保本地开发时数据库指向 Staging DB，**绝不能指向 Production DB**。

### Vite 环境文件优先级

| 文件 | 加载条件 |
|------|----------|
| `.env.development` | `npm run dev` |
| `.env.staging` / `.env.qat` | 由 GitHub Actions 注入 |
| `.env.local` | **始终最高优先级** (不提交 Git) |

---

## 安全检查清单

### 本地开发前

- [ ] `launchSettings.json` 数据库指向 Staging (绝不能是 Production!)
- [ ] `.env.local` API 指向 `localhost`
- [ ] Azure CLI 已登录 (`az account show`)
- [ ] 本机 IP 已加入 Azure SQL 防火墙

### 部署到 Staging 前

- [ ] 代码在本地测试通过
- [ ] 无 `console.log` 或调试代码
- [ ] 无硬编码密钥
- [ ] 单元测试通过

### 部署到 Production 前

- [ ] Staging 环境测试通过
- [ ] 无测试数据混入
- [ ] 所有功能按预期工作
- [ ] 已通知相关人员
- [ ] 有回滚计划
- [ ] APIM operations 已导入 (如有新 endpoint)

### 绝对禁止事项

| 禁止 | 原因 |
|------|------|
| 本地连接 Production DB | 测试数据污染生产环境 |
| 直接推送主分支 | 未经测试的代码进入生产 |
| 代码中硬编码密钥 | 安全风险 |
| 跳过 Staging 测试 | 引入未知 bug |
| Windows 本地 zip 部署到 Linux App Service | 路径分隔符导致 503 |
| 使用 Visual Studio Publish 到 Linux App Service | 同上 |

---

## Troubleshooting

### 部署失败

```bash
# 查看失败日志
gh run view <run-id> --log-failed
```

### API 503 After Deploy

**原因**: Windows 构建的产物包含反斜杠路径，Linux App Service 无法识别。

**修复**:
1. 确保 GitHub Actions 在 `ubuntu-latest` 上运行
2. 清理 App Service wwwroot:
   ```bash
   az webapp config appsettings set --name <api-name> --resource-group <rg> \
     --settings SCM_DO_BUILD_DURING_DEPLOYMENT=true
   az webapp restart --name <api-name> --resource-group <rg>
   ```

### 新 Endpoint 返回 404 (Production)

**原因**: APIM 没有配置对应的 operation。

**修复**: 在 Azure Portal 的 APIM 中导入新 endpoint (Phase 4.3)。

### API Restart

```bash
az webapp restart --name <api-name> --resource-group <resource-group>
```

### 查看 App Service 日志

```bash
az webapp log tail --name <api-name> --resource-group <resource-group>
```

### Azure SQL 连接被拒

```bash
# 获取当前 IP
curl ifconfig.me

# 添加到防火墙
az sql server firewall-rule create \
  --resource-group <resource-group> \
  --server <sql-server-name> \
  --name "LocalDev-$(date +%Y%m%d)" \
  --start-ip-address <YOUR_IP> \
  --end-ip-address <YOUR_IP>
```

### CORS 错误

检查 API 的 CORS 配置是否包含前端的 origin:

```bash
az webapp config appsettings list --name <api-name> --resource-group <rg> \
  --query "[?contains(name, 'Cors')]"
```

---

## 项目接入模板

新项目接入此部署流程时，创建 `.claude/deploy-config.md`:

```markdown
# <Project Name> Deployment Config

## Azure Resources

| 环境 | 资源类型 | 名称 | Resource Group |
|------|----------|------|----------------|
| Staging | App Service | <name> | <rg> |
| Staging | Static Web App | <name> | <rg> |
| Staging | SQL Database | <name> | <rg> |
| Production | App Service | <name> | <rg> |
| Production | Static Web App | <name> | <rg> |
| Production | SQL Database | <name> | <rg> |
| Production | APIM (if any) | <name> | <rg> |

## Branch Mapping

| Branch | Environment | Auto Deploy |
|--------|-------------|-------------|
| <staging-branch> | Staging | Yes |
| <main-branch> | Production | Yes (on PR merge) |

## URLs

| Service | Staging | Production |
|---------|---------|------------|
| Frontend | <url> | <url> |
| Admin | <url> | <url> |
| API | <url> | <url> |

## GitHub Secrets

| Secret Name | Environment | Purpose |
|-------------|-------------|---------|
| <secret> | Staging | <purpose> |
| <secret> | Production | <purpose> |

## Build Commands

| Component | Build | Test |
|-----------|-------|------|
| API | `dotnet build` | `dotnet test` |
| Frontend | `npm run build` | `npm test` |

## Known Issues

- (list known test failures, deployment quirks, etc.)
```

---

## Quick Reference

```bash
# 部署到 Staging
git checkout <staging-branch> && git merge feature/xxx && git push

# 检查 Staging 部署状态
gh run list --branch <staging-branch> --limit 5

# 创建 Production PR
gh pr create --base <main-branch> --head <staging-branch> --title "Release: ..."

# 检查 Production 部署状态
gh run list --branch <main-branch> --limit 5

# API 健康检查
curl https://<api-url>/api/health

# 重启 API
az webapp restart --name <api-name> --resource-group <rg>

# 查看日志
az webapp log tail --name <api-name> --resource-group <rg>
```
