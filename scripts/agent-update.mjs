#!/usr/bin/env node
/**
 * Agent backlog update script
 * Scans briefs and reports to maintain the agent/backlog.md file
 */

import { readdir, readFile, writeFile, mkdir, access } from 'fs/promises';
import { join } from 'path';

const BRIEFS_DIR = join(process.cwd(), 'docs', 'agent', 'briefs');
const REPORTS_DIR = join(process.cwd(), 'agent', 'reports');
const BACKLOG_FILE = join(process.cwd(), 'agent', 'backlog.md');

async function ensureDir(dir) {
  try {
    await access(dir);
  } catch {
    await mkdir(dir, { recursive: true });
  }
}

async function getBriefs() {
  try {
    const files = await readdir(BRIEFS_DIR);
    return files
      .filter(f => f.endsWith('.md') && f !== 'README.md')
      .map(f => join(BRIEFS_DIR, f));
  } catch (error) {
    console.warn('No briefs directory found');
    return [];
  }
}

async function getReports() {
  try {
    const files = await readdir(REPORTS_DIR);
    return files
      .filter(f => f.endsWith('.md'))
      .map(f => join(REPORTS_DIR, f));
  } catch (error) {
    return [];
  }
}

async function parseBrief(briefPath) {
  const content = await readFile(briefPath, 'utf-8');
  const filename = briefPath.split('/').pop();
  
  // Extract title from markdown
  const titleMatch = content.match(/^#\s+(.+)$/m);
  const title = titleMatch ? titleMatch[1] : filename;
  
  return {
    path: briefPath.replace(process.cwd() + '/', ''),
    filename,
    title,
    content
  };
}

async function parseReport(reportPath) {
  const content = await readFile(reportPath, 'utf-8');
  const filename = reportPath.split('/').pop();
  
  // Extract metadata from report
  const briefMatch = content.match(/\*\*Brief\*\*:\s*(.+)/);
  const statusMatch = content.match(/\*\*Status\*\*:\s*(.+)/);
  const dateMatch = content.match(/\*\*Date\*\*:\s*(.+)/);
  
  return {
    path: reportPath.replace(process.cwd() + '/', ''),
    filename,
    brief: briefMatch ? briefMatch[1].trim() : null,
    status: statusMatch ? statusMatch[1].trim() : null,
    date: dateMatch ? dateMatch[1].trim() : null
  };
}

async function generateBacklog() {
  console.log('🔍 Scanning briefs and reports...');
  
  const briefs = await getBriefs();
  const reports = await getReports();
  
  console.log(`  Found ${briefs.length} briefs`);
  console.log(`  Found ${reports.length} reports`);
  
  // Parse all briefs
  const briefsData = await Promise.all(briefs.map(parseBrief));
  
  // Parse all reports
  const reportsData = await Promise.all(reports.map(parseReport));
  
  // Match reports to briefs
  const completedBriefs = new Set();
  const briefToReport = new Map();
  
  for (const report of reportsData) {
    if (report.brief && report.status?.includes('✅')) {
      completedBriefs.add(report.brief);
      briefToReport.set(report.brief, report);
    }
  }
  
  // Generate backlog content
  let backlog = '# Agent Backlog\n\n';
  backlog += '_Last updated: ' + new Date().toISOString() + '_\n\n';
  backlog += 'This file tracks agent tasks and their execution status.\n\n';
  
  // Pending tasks
  const pending = briefsData.filter(b => !completedBriefs.has(b.path));
  if (pending.length > 0) {
    backlog += '## Pending\n\n';
    for (const brief of pending) {
      backlog += `- [ ] **${brief.title}**\n`;
      backlog += `  - Brief: \`${brief.path}\`\n`;
      backlog += `  - Status: Not started\n\n`;
    }
  }
  
  // Completed tasks
  const completed = briefsData.filter(b => completedBriefs.has(b.path));
  if (completed.length > 0) {
    backlog += '## Completed\n\n';
    for (const brief of completed) {
      const report = briefToReport.get(brief.path);
      backlog += `- [x] **${brief.title}**\n`;
      backlog += `  - Brief: \`${brief.path}\`\n`;
      backlog += `  - Status: ${report.status}\n`;
      if (report.date) {
        backlog += `  - Completed: ${report.date}\n`;
      }
      backlog += `  - Report: \`${report.path}\`\n\n`;
    }
  }
  
  // Reports without matching briefs
  const orphanedReports = reportsData.filter(r => 
    r.brief && !briefsData.some(b => b.path === r.brief)
  );
  
  if (orphanedReports.length > 0) {
    backlog += '## Archived\n\n';
    backlog += '_Tasks completed without a corresponding brief file._\n\n';
    for (const report of orphanedReports) {
      backlog += `- [x] ${report.brief}\n`;
      backlog += `  - Status: ${report.status}\n`;
      if (report.date) {
        backlog += `  - Completed: ${report.date}\n`;
      }
      backlog += `  - Report: \`${report.path}\`\n\n`;
    }
  }
  
  backlog += '## How to Use\n\n';
  backlog += '### Adding a Task\n\n';
  backlog += '1. Create a brief in `docs/agent/briefs/your-task.md`\n';
  backlog += '2. Run `npm run agent:update` to add it to the backlog\n';
  backlog += '3. Agent will pick it up automatically or via workflow dispatch\n\n';
  
  backlog += '### Completing a Task\n\n';
  backlog += '1. Agent executes the brief\n';
  backlog += '2. Agent creates report in `agent/reports/`\n';
  backlog += '3. Run `npm run agent:update` to mark as completed\n\n';
  
  backlog += '### Viewing Reports\n\n';
  backlog += '```bash\n';
  backlog += '# List all reports\n';
  backlog += 'ls -lt agent/reports/\n\n';
  backlog += '# View a specific report\n';
  backlog += 'cat agent/reports/YYYY-MM-DD-HH-MM-task-name.md\n';
  backlog += '```\n';
  
  return backlog;
}

async function main() {
  console.log('🤖 Agent Backlog Update\n');
  
  // Ensure directories exist
  await ensureDir(join(process.cwd(), 'agent'));
  await ensureDir(REPORTS_DIR);
  
  // Generate backlog
  const backlog = await generateBacklog();
  
  // Write backlog file
  await writeFile(BACKLOG_FILE, backlog);
  
  console.log('\n✅ Backlog updated successfully');
  console.log(`📄 View at: ${BACKLOG_FILE}`);
}

main().catch(error => {
  console.error('❌ Error updating backlog:', error);
  process.exit(1);
});
