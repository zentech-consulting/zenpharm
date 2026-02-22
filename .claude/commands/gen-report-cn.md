---
description: 生成中文工作完成报告 PDF。从当前会话和项目信息中收集数据，生成 Zentech 品牌的专业报告。
---

# /gen-report-cn - 生成中文工作完成报告 (PDF)

根据当前会话中完成的工作，生成一份专业的 Zentech 品牌中文报告 PDF。

## 参数

`$ARGUMENTS` — 可选，报告主题或补充说明。如果为空，从会话内容自动提取。

## 第一步: 数据采集

从以下来源收集报告内容:

1. **当前会话** → 完成了什么工作、关键决策、技术方案
2. **git log --oneline -20** → 本次会话相关的提交记录
3. **.claude/tasks.md** → 关联的任务状态变化
4. **RELEASE_NOTES.md** → 当前版本号 (如存在)
5. **用户补充** → `$ARGUMENTS` 中的额外说明

## 第二步: 组织内容

按以下结构整理报告，每个章节都要有实质内容:

### 报告结构

| 章节 | 内容指引 |
|------|----------|
| **封面** | 报告标题、项目名、日期、版本号 |
| **项目概况** | 项目简介、当前阶段、本次工作范围 |
| **完成工作** | 逐项列出完成的功能/修复/改进，每项 2-3 句说明 |
| **关键成果** | 量化成果 (如: 性能提升 X%、修复 X 个 bug、新增 X 个功能) |
| **技术要点** | 重要的技术决策和实现方式 (面向技术读者，但保持简洁) |
| **后续建议** | 下一步工作建议、待解决的问题、风险提示 |

## 第三步: 生成 HTML

将以下完整 HTML 写入 `docs/report-{YYYY-MM-DD}.html` (如 docs 目录不存在则创建):

```html
<!DOCTYPE html>
<html lang="zh-CN">
<head>
<meta charset="UTF-8">
<title>工作报告 — {项目名}</title>
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
    h2 { page-break-after: avoid; }
    table, pre { page-break-inside: avoid; }
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
    font-size: 32pt;
    font-weight: 700;
    margin-bottom: 16px;
    letter-spacing: 1px;
  }
  .cover-page .subtitle {
    font-size: 16pt;
    opacity: 0.9;
    margin-bottom: 40px;
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
    margin-top: 32px;
    margin-bottom: 12px;
    padding-bottom: 6px;
    border-bottom: 2px solid #008170;
  }
  h3 {
    font-size: 13pt;
    color: #006658;
    margin-top: 20px;
    margin-bottom: 8px;
  }
  p { margin-bottom: 10px; }
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
  code {
    font-family: 'Cascadia Code', 'Consolas', monospace;
    font-size: 9pt;
    background: #f4f4f4;
    padding: 2px 6px;
    border-radius: 3px;
  }
  pre {
    background: #f4f4f4;
    padding: 16px;
    border-radius: 4px;
    border: 1px solid #ddd;
    overflow-x: auto;
    font-size: 9pt;
    margin: 12px 0;
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
  <h1>{报告标题}</h1>
  <div class="subtitle">{项目名}</div>
  <div class="meta">
    日期: {YYYY年MM月DD日}<br>
    版本: {vX.Y.Z 或 "N/A"}<br>
    编制: Zentech Consulting
  </div>
  <div class="company">Zentech Consulting Pty Ltd</div>
</div>

<div class="content">

  <h2>项目概况</h2>
  <p>{项目简介和本次工作范围}</p>

  <h2>完成工作</h2>
  <!-- 每项工作用 h3 + 描述段落 -->
  <h3>1. {功能/修复标题}</h3>
  <p>{2-3 句说明}</p>

  <h2>关键成果</h2>
  <div class="highlight-box">
    <ul>
      <li>{量化成果 1}</li>
      <li>{量化成果 2}</li>
    </ul>
  </div>

  <h2>技术要点</h2>
  <p>{重要技术决策}</p>

  <h2>后续建议</h2>
  <table>
    <tr><th>建议事项</th><th>优先级</th><th>说明</th></tr>
    <tr><td>{建议1}</td><td>高</td><td>{说明}</td></tr>
  </table>

  <div class="footer-note">
    Confidential — Zentech Consulting Pty Ltd, 2026
  </div>
</div>

</body>
</html>
```

**注意**: 以上 HTML 是模板框架。你需要用实际采集到的数据替换所有 `{占位符}` 内容。可以根据实际情况增减章节、表格行、列表项。

## 第四步: 生成 PDF

运行以下命令:

```powershell
& "C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe" --headless --disable-gpu --print-to-pdf="docs/report-{YYYY-MM-DD}.pdf" "file:///{当前项目绝对路径}/docs/report-{YYYY-MM-DD}.html"
```

**替换说明**:
- `{YYYY-MM-DD}` → 当前日期，如 `2026-02-17`
- `{当前项目绝对路径}` → 用 `pwd` 获取，正斜杠格式，如 `C:/repos/smcp`

## 第五步: 清理并确认

1. 确认 PDF 文件已生成: `ls docs/report-*.pdf`
2. 删除临时 HTML: `rm docs/report-{YYYY-MM-DD}.html`
3. 向用户报告: 文件路径、页数（如可获取）

## 注意事项

- 报告内容应面向**技术和管理**读者，兼顾可读性和专业性
- 避免过度技术细节 (如完整代码块)，用概述 + 关键代码片段代替
- 中文排版使用全角标点
- 如果会话中没有足够信息填充某章节，可以省略该章节，但至少包含: 封面、完成工作、后续建议
