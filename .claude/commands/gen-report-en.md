---
description: Generate an English task completion report PDF. Collects data from the current session and project info, produces a professional Zentech-branded report.
---

# /gen-report-en - Generate Task Completion Report (PDF)

Generate a professional Zentech-branded English report PDF based on work completed in the current session.

## Parameters

`$ARGUMENTS` — Optional. Report topic or additional notes. If empty, auto-extract from session content.

## Step 1: Data Collection

Gather report content from:

1. **Current session** → What was completed, key decisions, technical approach
2. **git log --oneline -20** → Related commits from this session
3. **.claude/tasks.md** → Related task status changes
4. **RELEASE_NOTES.md** → Current version number (if exists)
5. **User input** → Additional notes from `$ARGUMENTS`

## Step 2: Organise Content

Structure the report as follows — each section should have substantive content:

### Report Structure

| Section | Content Guide |
|---------|---------------|
| **Cover** | Report title, project name, date, version |
| **Project Overview** | Brief project intro, current phase, scope of this work |
| **Completed Work** | List each feature/fix/improvement with 2-3 sentence description |
| **Key Outcomes** | Quantified results (e.g., X% performance gain, X bugs fixed, X features added) |
| **Technical Notes** | Important technical decisions and implementation details (concise) |
| **Recommendations** | Next steps, outstanding issues, risk flags |

## Step 3: Generate HTML

Write the following complete HTML to `docs/report-{YYYY-MM-DD}.html` (create docs/ directory if it doesn't exist):

```html
<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="UTF-8">
<title>Task Report — {Project Name}</title>
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
    font-family: 'Segoe UI', sans-serif;
    font-size: 11pt;
    line-height: 1.7;
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
  <h1>{Report Title}</h1>
  <div class="subtitle">{Project Name}</div>
  <div class="meta">
    Date: {Month DD, YYYY}<br>
    Version: {vX.Y.Z or "N/A"}<br>
    Prepared by: Zentech Consulting
  </div>
  <div class="company">Zentech Consulting Pty Ltd</div>
</div>

<div class="content">

  <h2>Project Overview</h2>
  <p>{Project introduction and scope of this work}</p>

  <h2>Completed Work</h2>
  <!-- Each item as h3 + description paragraph -->
  <h3>1. {Feature/Fix Title}</h3>
  <p>{2-3 sentence description}</p>

  <h2>Key Outcomes</h2>
  <div class="highlight-box">
    <ul>
      <li>{Quantified outcome 1}</li>
      <li>{Quantified outcome 2}</li>
    </ul>
  </div>

  <h2>Technical Notes</h2>
  <p>{Key technical decisions}</p>

  <h2>Recommendations</h2>
  <table>
    <tr><th>Recommendation</th><th>Priority</th><th>Details</th></tr>
    <tr><td>{Item 1}</td><td>High</td><td>{Details}</td></tr>
  </table>

  <div class="footer-note">
    Confidential — Zentech Consulting Pty Ltd, 2026
  </div>
</div>

</body>
</html>
```

**Note**: The HTML above is a template framework. Replace all `{placeholder}` content with actual collected data. Add or remove sections, table rows, and list items as needed.

## Step 4: Generate PDF

Run the following command:

```powershell
& "C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe" --headless --disable-gpu --print-to-pdf="docs/report-{YYYY-MM-DD}.pdf" "file:///{project-absolute-path}/docs/report-{YYYY-MM-DD}.html"
```

**Replacements**:
- `{YYYY-MM-DD}` → Current date, e.g., `2026-02-17`
- `{project-absolute-path}` → Get from `pwd`, use forward slashes, e.g., `C:/repos/smcp`

## Step 5: Clean Up and Confirm

1. Verify PDF was generated: `ls docs/report-*.pdf`
2. Delete temporary HTML: `rm docs/report-{YYYY-MM-DD}.html`
3. Report to user: file path, page count (if available)

## Notes

- Content should target both **technical and management** readers
- Avoid excessive technical detail (no full code blocks) — use summaries + key snippets
- If session doesn't have enough info for a section, omit it, but at minimum include: Cover, Completed Work, Recommendations
