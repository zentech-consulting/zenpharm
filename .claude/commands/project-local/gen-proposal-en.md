---
description: Generate an English client proposal PDF. Organises discussed plans into a professional Zentech-branded proposal document.
---

# /gen-proposal-en - Generate Client Proposal (PDF)

Compile the plans and solutions discussed in the current session into a professional Zentech-branded English client proposal PDF.

## Parameters

`$ARGUMENTS` — Optional. Proposal topic or client name. If empty, auto-extract from session content.

## Step 1: Data Collection

Gather proposal content from:

1. **Current session** → Discussed solutions, feature plans, architecture, time estimates
2. **.claude/tasks.md** → Planned tasks and milestones
3. **RELEASE_NOTES.md** → Completed features (demonstrates delivery capability)
4. **Project CLAUDE.md** → Tech stack, project structure
5. **User input** → Additional notes from `$ARGUMENTS`

## Step 2: Organise Content

Structure the proposal as follows — ensure logical flow and persuasive narrative:

### Proposal Structure

| Section | Content Guide |
|---------|---------------|
| **Cover** | Proposal title, client name, date |
| **Executive Summary** | 1-2 paragraph overview: problem, solution, expected outcomes |
| **Background** | Current pain points, business needs, market opportunity |
| **Solution** | Solution overview + core feature modules (3-5 sentences each) |
| **Implementation Plan** | Phased plan with objectives and deliverables per phase |
| **Deliverables & Timeline** | Table format: deliverables, timeline, milestones per phase |
| **Investment** | Cost breakdown (if discussed) or "Custom quote upon requirements review" |
| **About Us** | Zentech intro, capabilities, relevant case studies |

## Step 3: Generate HTML

Write the following complete HTML to `docs/proposal-{YYYY-MM-DD}.html` (create docs/ directory if it doesn't exist):

```html
<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="UTF-8">
<title>Proposal — {Project Name}</title>
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
  <h1>{Proposal Title}</h1>
  <div class="subtitle">Project Proposal</div>
  <div class="client">Prepared for: {Client Name}</div>
  <div class="meta">
    Date: {Month DD, YYYY}<br>
    Reference: PROP-{YYYYMMDD}-001
  </div>
  <div class="company">Zentech Consulting Pty Ltd</div>
</div>

<div class="content">

  <h2>Executive Summary</h2>
  <div class="highlight-box">
    <p>{1-2 paragraph overview: the challenge, our solution, expected outcomes}</p>
  </div>

  <h2>Background</h2>
  <h3>Current Challenges</h3>
  <p>{Pain point description}</p>
  <h3>Business Requirements</h3>
  <ul>
    <li>{Requirement 1}</li>
    <li>{Requirement 2}</li>
  </ul>

  <h2>Proposed Solution</h2>
  <p>{Solution overview}</p>
  <h3>{Module 1 Name}</h3>
  <p>{Module description, 3-5 sentences}</p>
  <h3>{Module 2 Name}</h3>
  <p>{Module description, 3-5 sentences}</p>

  <h2>Implementation Plan</h2>
  <div class="phase-card">
    <h3>Phase 1: {Phase Name} <span class="timeline">{Duration}</span></h3>
    <ul>
      <li>{Deliverable 1}</li>
      <li>{Deliverable 2}</li>
    </ul>
  </div>
  <div class="phase-card">
    <h3>Phase 2: {Phase Name} <span class="timeline">{Duration}</span></h3>
    <ul>
      <li>{Deliverable 1}</li>
      <li>{Deliverable 2}</li>
    </ul>
  </div>

  <h2>Deliverables & Timeline</h2>
  <table>
    <tr><th>Phase</th><th>Deliverables</th><th>Timeline</th><th>Milestone</th></tr>
    <tr><td>Phase 1</td><td>{Deliverables}</td><td>{Timeline}</td><td>{Milestone}</td></tr>
    <tr><td>Phase 2</td><td>{Deliverables}</td><td>{Timeline}</td><td>{Milestone}</td></tr>
  </table>

  <h2>Investment</h2>
  <table>
    <tr><th>Item</th><th>Description</th><th>Cost</th></tr>
    <tr><td>{Item 1}</td><td>{Description}</td><td>{Cost}</td></tr>
    <tr><td colspan="2"><strong>Total</strong></td><td><strong>{Total}</strong></td></tr>
  </table>
  <p><em>* This quotation is valid for 30 days. Final pricing subject to detailed requirements review.</em></p>

  <h2>About Us</h2>
  <div class="about-section">
    <h3>Zentech Consulting Pty Ltd</h3>
    <p>Zentech Consulting is a technology consultancy specialising in digital transformation across healthcare, financial services, and retail sectors. We leverage AI-driven development workflows to deliver high-quality software efficiently.</p>
    <h3>Core Capabilities</h3>
    <ul>
      <li>Full-stack web application development (.NET / React / Node.js)</li>
      <li>AI and machine learning integration (Claude API / OpenAI)</li>
      <li>Cloud architecture and DevOps (Azure / AWS)</li>
      <li>Mobile and cross-platform solutions</li>
    </ul>
  </div>

  <div class="footer-note">
    Confidential — Zentech Consulting Pty Ltd, 2026
  </div>
</div>

</body>
</html>
```

**Note**: The HTML above is a template framework. Replace all `{placeholder}` content with actual collected data. Add or remove phases, modules, and table rows as needed.

## Step 4: Generate PDF

Run the following command:

```powershell
& "C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe" --headless --disable-gpu --print-to-pdf="docs/proposal-{YYYY-MM-DD}.pdf" "file:///{project-absolute-path}/docs/proposal-{YYYY-MM-DD}.html"
```

**Replacements**:
- `{YYYY-MM-DD}` → Current date, e.g., `2026-02-17`
- `{project-absolute-path}` → Get from `pwd`, use forward slashes, e.g., `C:/repos/smcp`

## Step 5: Clean Up and Confirm

1. Verify PDF was generated: `ls docs/proposal-*.pdf`
2. Delete temporary HTML: `rm docs/proposal-{YYYY-MM-DD}.html`
3. Report to user: file path, page count (if available)

## Notes

- Content targets **clients and decision-makers** — use business language, minimise technical jargon
- Emphasise **value and outcomes** over technical implementation details
- Investment section: if no pricing was discussed, write "A formal quote will be provided upon detailed requirements review"
- Adjust the "About Us" section to highlight capabilities relevant to the specific project
- If the proposal topic is unclear, ask the user before proceeding
