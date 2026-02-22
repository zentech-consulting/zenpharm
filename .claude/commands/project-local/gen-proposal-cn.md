---
description: 生成中文客户提案/计划书 PDF。将讨论中的方案整理为 Zentech 品牌的专业提案文档。
---

# /gen-proposal-cn - 生成中文客户提案 (PDF)

将当前会话中讨论的方案/计划整理为专业的 Zentech 品牌中文客户提案 PDF。

## 参数

`$ARGUMENTS` — 可选，提案主题或客户名称。如果为空，从会话内容自动提取。

## 第一步: 数据采集

从以下来源收集提案内容:

1. **当前会话** → 讨论的方案、功能规划、技术架构、时间预估
2. **.claude/tasks.md** → 已规划的任务和里程碑
3. **RELEASE_NOTES.md** → 已完成的功能 (证明交付能力)
4. **项目 CLAUDE.md** → 技术栈、项目结构信息
5. **用户补充** → `$ARGUMENTS` 中的额外说明

## 第二步: 组织内容

按以下结构整理提案，确保逻辑通顺、有说服力:

### 提案结构

| 章节 | 内容指引 |
|------|----------|
| **封面** | 提案标题、客户名、日期 |
| **执行摘要** | 1-2 段概述：问题、方案、预期成果 |
| **项目背景** | 当前痛点、业务需求、市场机会 |
| **解决方案** | 方案概述 + 核心功能模块 (每个模块 3-5 句) |
| **实施计划** | 分阶段计划，每阶段的目标和交付物 |
| **交付物与时间线** | 表格形式，列出每个阶段的交付物、时间、里程碑 |
| **投资预算** | 费用明细表 (如有讨论) 或 "根据需求定制" |
| **关于我们** | Zentech 简介、技术能力、相关案例 |

## 第三步: 生成 HTML

将以下完整 HTML 写入 `docs/proposal-{YYYY-MM-DD}.html` (如 docs 目录不存在则创建):

```html
<!DOCTYPE html>
<html lang="zh-CN">
<head>
<meta charset="UTF-8">
<title>项目提案 — {项目名}</title>
<style>
  @page {
    size: A4;
    margin: 25mm;
    @bottom-center {
      content: "Confidential — Zentech Consulting Pty Ltd";
      font-size: 8pt;
      color: #999;
    }
  }
  @media print {
    .cover-page { page-break-after: always; }
    .section { page-break-inside: avoid; }
    h2 { page-break-after: avoid; }
    table { page-break-inside: avoid; }
  }
  * { margin: 0; padding: 0; box-sizing: border-box; }
  body {
    font-family: 'Segoe UI', 'Microsoft YaHei', sans-serif;
    font-size: 11pt;
    line-height: 1.8;
    color: #1a1a1a;
  }
  .cover-page {
    display: flex;
    flex-direction: column;
    justify-content: center;
    align-items: center;
    min-height: 100vh;
    background: linear-gradient(135deg, #008170, #006658);
    color: white;
    text-align: center;
    padding: 60px 40px;
  }
  .cover-page h1 {
    font-size: 30pt;
    font-weight: 700;
    margin-bottom: 12px;
    letter-spacing: 1px;
  }
  .cover-page .subtitle {
    font-size: 16pt;
    opacity: 0.9;
    margin-bottom: 8px;
  }
  .cover-page .client {
    font-size: 14pt;
    opacity: 0.85;
    margin-bottom: 40px;
    font-style: italic;
  }
  .cover-page .meta {
    font-size: 11pt;
    opacity: 0.8;
    line-height: 2;
  }
  .cover-page .company {
    margin-top: 60px;
    font-size: 12pt;
    font-weight: 600;
    letter-spacing: 2px;
    text-transform: uppercase;
    opacity: 0.7;
  }
  .content { padding: 0 10px; }
  h2 {
    font-size: 16pt;
    color: #008170;
    margin-top: 36px;
    margin-bottom: 14px;
    padding-bottom: 6px;
    border-bottom: 2px solid #008170;
  }
  h3 {
    font-size: 13pt;
    color: #006658;
    margin-top: 22px;
    margin-bottom: 8px;
  }
  p { margin-bottom: 12px; }
  ul, ol { margin: 10px 0 10px 24px; }
  li { margin-bottom: 6px; }
  table {
    border-collapse: collapse;
    width: 100%;
    margin: 16px 0;
    font-size: 10pt;
  }
  th {
    background-color: #008170;
    color: white;
    padding: 10px 14px;
    text-align: left;
    font-weight: 600;
  }
  td {
    padding: 8px 14px;
    border-bottom: 1px solid #e0e0e0;
  }
  tr:nth-child(even) { background-color: #f0faf8; }
  .highlight-box {
    background: #f0faf8;
    border-left: 4px solid #008170;
    padding: 14px 18px;
    margin: 16px 0;
    border-radius: 0 4px 4px 0;
  }
  .phase-card {
    border: 1px solid #ddd;
    border-radius: 6px;
    padding: 16px 20px;
    margin: 12px 0;
    background: #fafffe;
  }
  .phase-card h3 {
    margin-top: 0;
    color: #008170;
  }
  .phase-card .timeline {
    display: inline-block;
    background: #008170;
    color: white;
    padding: 2px 10px;
    border-radius: 12px;
    font-size: 9pt;
    font-weight: 600;
  }
  .about-section {
    background: #f8f9fa;
    padding: 24px;
    border-radius: 6px;
    margin-top: 24px;
  }
  code {
    font-family: 'Cascadia Code', 'Consolas', monospace;
    font-size: 9pt;
    background: #f4f4f4;
    padding: 2px 6px;
    border-radius: 3px;
  }
  .footer-note {
    margin-top: 48px;
    padding-top: 16px;
    border-top: 1px solid #ddd;
    font-size: 9pt;
    color: #999;
    text-align: center;
  }
</style>
</head>
<body>

<div class="cover-page">
  <h1>{提案标题}</h1>
  <div class="subtitle">项目提案书</div>
  <div class="client">呈交: {客户名称}</div>
  <div class="meta">
    日期: {YYYY年MM月DD日}<br>
    文档编号: PROP-{YYYYMMDD}-001
  </div>
  <div class="company">Zentech Consulting Pty Ltd</div>
</div>

<div class="content">

  <h2>执行摘要</h2>
  <div class="highlight-box">
    <p>{1-2 段概述：客户面临的挑战、我们的解决方案、预期成果}</p>
  </div>

  <h2>项目背景</h2>
  <h3>现状与挑战</h3>
  <p>{当前痛点描述}</p>
  <h3>业务需求</h3>
  <ul>
    <li>{需求 1}</li>
    <li>{需求 2}</li>
  </ul>

  <h2>解决方案</h2>
  <p>{方案整体概述}</p>
  <h3>{模块 1 名称}</h3>
  <p>{模块描述，3-5 句}</p>
  <h3>{模块 2 名称}</h3>
  <p>{模块描述，3-5 句}</p>

  <h2>实施计划</h2>
  <div class="phase-card">
    <h3>第一阶段: {阶段名} <span class="timeline">{时长}</span></h3>
    <ul>
      <li>{交付物 1}</li>
      <li>{交付物 2}</li>
    </ul>
  </div>
  <div class="phase-card">
    <h3>第二阶段: {阶段名} <span class="timeline">{时长}</span></h3>
    <ul>
      <li>{交付物 1}</li>
      <li>{交付物 2}</li>
    </ul>
  </div>

  <h2>交付物与时间线</h2>
  <table>
    <tr><th>阶段</th><th>交付物</th><th>时间</th><th>里程碑</th></tr>
    <tr><td>第一阶段</td><td>{交付物}</td><td>{时间}</td><td>{里程碑}</td></tr>
    <tr><td>第二阶段</td><td>{交付物}</td><td>{时间}</td><td>{里程碑}</td></tr>
  </table>

  <h2>投资预算</h2>
  <table>
    <tr><th>项目</th><th>说明</th><th>费用</th></tr>
    <tr><td>{项目1}</td><td>{说明}</td><td>{费用}</td></tr>
    <tr><td colspan="2"><strong>合计</strong></td><td><strong>{总额}</strong></td></tr>
  </table>
  <p><em>* 以上报价有效期 30 天，具体费用根据需求评审后确定。</em></p>

  <h2>关于我们</h2>
  <div class="about-section">
    <h3>Zentech Consulting Pty Ltd</h3>
    <p>Zentech Consulting 是一家专注于数字化转型的技术咨询公司，在医疗健康、金融服务、零售等行业拥有丰富的项目经验。我们采用 AI 驱动的开发流程，提供高效、高质量的软件交付。</p>
    <h3>核心能力</h3>
    <ul>
      <li>全栈 Web 应用开发 (.NET / React / Node.js)</li>
      <li>AI 与机器学习集成 (Claude API / OpenAI)</li>
      <li>云架构与 DevOps (Azure / AWS)</li>
      <li>移动应用与跨平台解决方案</li>
    </ul>
  </div>

  <div class="footer-note">
    Confidential — Zentech Consulting Pty Ltd, 2026
  </div>
</div>

</body>
</html>
```

**注意**: 以上 HTML 是模板框架。你需要用实际采集到的数据替换所有 `{占位符}` 内容。可以根据实际情况增减阶段、模块、表格行。

## 第四步: 生成 PDF

运行以下命令:

```powershell
& "C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe" --headless --disable-gpu --print-to-pdf="docs/proposal-{YYYY-MM-DD}.pdf" "file:///{当前项目绝对路径}/docs/proposal-{YYYY-MM-DD}.html"
```

**替换说明**:
- `{YYYY-MM-DD}` → 当前日期，如 `2026-02-17`
- `{当前项目绝对路径}` → 用 `pwd` 获取，正斜杠格式，如 `C:/repos/smcp`

## 第五步: 清理并确认

1. 确认 PDF 文件已生成: `ls docs/proposal-*.pdf`
2. 删除临时 HTML: `rm docs/proposal-{YYYY-MM-DD}.html`
3. 向用户报告: 文件路径、页数（如可获取）

## 注意事项

- 提案内容面向**客户/决策者**，使用商务语言，避免过度技术术语
- 强调**价值和成果**而非技术实现细节
- 预算章节: 如果会话中没讨论具体费用，写"根据详细需求评审后提供正式报价"
- "关于我们"章节可根据项目特点调整突出的能力
- 中文排版使用全角标点
- 如果提案主题不明确，先向用户询问
