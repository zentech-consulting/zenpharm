---
description: Review recent work and update tasks.md to reflect actual progress. Marks completed tasks, adds newly discovered work.
---

# /update-tasks - 更新任务状态

检查近期工作，将实际进度同步到 `.claude/tasks.md`。**解决 AI 做完工作后不自动更新任务文件的问题。**

## 数据采集

并行读取:
1. **.claude/tasks.md** — 当前任务列表
2. **git log --oneline -15** — 最近 15 条提交
3. **RELEASE_NOTES.md** — 最近发布记录 (如果存在)

## 分析逻辑

### 1. 已完成但未标记的任务

对比 tasks.md 中的 pending/in_progress 任务与 git log:
- 如果 commit 消息明确涉及某任务 → 标记为建议完成
- 如果 RELEASE_NOTES 已记录某功能 → 标记为建议完成

### 2. 新工作未记录为任务

检查 git log 中是否有未被任何任务覆盖的工作:
- 大功能开发 (feat: commits)
- Bug 修复 (fix: commits)
- 重构工作 (refactor: commits)

### 3. 过期任务

- in_progress 超过 30 天无相关 commit → 建议确认是否仍然相关
- blocked 任务 → 检查阻塞原因是否已解除

## 输出格式

```
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
  任务更新建议  |  {项目名}  |  {日期}
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

## 建议标记为完成
- "任务标题" → 依据: commit abc1234 "feat: ..."

## 建议新增任务
- "新功能描述" → 来源: commit def5678 "feat: ..."

## 需要确认
- "任务标题" → 30 天无进展，是否仍然相关?

## 无需变更
其余 X 个任务状态正确。

━━━ 确认后将写入 .claude/tasks.md ━━━
```

## 关键规则

- **写入前必须向用户确认** — 列出所有变更建议后等待确认
- 不要凭空猜测任务完成状态，必须有 commit/release 证据
- 更新后保持 tasks.md 原有格式不变
- 新增任务使用与现有任务一致的格式

## 注意

- 如果 .claude/tasks.md 不存在，提示创建并提供模板
- 如果没有任何变更建议，告知用户任务状态已是最新
