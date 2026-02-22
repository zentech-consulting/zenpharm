---
description: Report current project status and recommend prioritized next actions. Combines status overview with actionable suggestions. Single-project version.
---

# /whats-next - 项目状态 & 下一步建议

扫描**当前项目**状态，然后输出优先级排序的行动建议。

## 数据采集

依次读取:

1. **RELEASE_NOTES.md** → 最新版本号 + 最近 3 条更新
2. **.claude/tasks.md** → 所有任务详情和统计
3. **git log --oneline -10** → 最近 10 条提交

## 活跃度判定

| 状态 | 条件 |
|------|------|
| 活跃 | 7 天内有 commit 或有 in_progress 任务 |
| 就绪 | 有 pending 任务但 7 天内无 commit |
| 休眠 | 无 pending 任务且 14 天无 commit |
| 阻塞 | 有 blocked 任务 |

## 优先级排序

| 优先级 | 类型 | 说明 |
|--------|------|------|
| P0 | in_progress | 已开始未完成 → 继续做! |
| P1 | blocked 可解除 | 阻塞原因已消除 |
| P2 | pending + high | 高优先级待办 |
| P3 | pending + medium | 中优先级待办 |
| P4 | pending + low | 低优先级待办 |

## 输出格式

```
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
  {项目名}  |  {日期}
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

## 版本: vX.Y.Z (日期) | 活跃度: 活跃/就绪/休眠/阻塞

## 最近更新
- ...
- ...

## 任务统计
- Completed: X | In Progress: X | Pending: X (high: X, medium: X, low: X) | Blocked: X

## 最近提交
- abc1234 (Xd ago) commit message
- ...

━━━ 下一步建议 ━━━

## 立即继续 (in_progress)
1. 任务标题
   → 上次进行到: 具体进度描述
   → 下一步: 建议的具体操作

## 推荐启动 (high priority)
2. 任务标题
   → 原因: 为什么建议现在做

## 可以考虑 (medium priority)
3. 任务标题

## 阻塞中 (需要介入)
- 任务标题 → 原因: 阻塞描述

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
```

## 关键规则

- "立即继续" 类别必须写清**上次进行到哪里**和**下一步具体操作**
- 参考 git log 最近提交来推断 in_progress 任务的进度
- 如果 RELEASE_NOTES.md 不存在，跳过版本信息
- 如果 .claude/tasks.md 不存在，提示 "无任务文件，建议创建"
- 如果没有任何任务，提示创建 .claude/tasks.md 或联系 Grace 运行 `/handout`

## 注意

- 这是**单项目**版本，只分析当前项目
- 不读 blueprint.md 或其他项目的任务
- 跨项目全局建议请在 Grace 中运行 `/whats-next`
