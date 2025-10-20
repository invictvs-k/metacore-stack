#!/usr/bin/env node
/**
 * Security audit script - runs npm audit and dotnet list package --vulnerable
 * Generates reports in .artifacts/security/
 */

import { exec } from 'child_process';
import { promisify } from 'util';
import { mkdir, writeFile } from 'fs/promises';
import { join } from 'path';

const execAsync = promisify(exec);

async function ensureDir(dir) {
  await mkdir(dir, { recursive: true });
}

async function runNpmAudit() {
  console.log('🔍 Running npm audit...');
  try {
    const { stdout } = await execAsync('npm audit --json', { 
      cwd: process.cwd(),
      maxBuffer: 10 * 1024 * 1024 
    });
    return JSON.parse(stdout);
  } catch (error) {
    // npm audit exits with non-zero if vulnerabilities found
    if (error.stdout) {
      return JSON.parse(error.stdout);
    }
    throw error;
  }
}

async function runDotnetAudit() {
  console.log('🔍 Running dotnet package vulnerability check...');
  try {
    const { stdout } = await execAsync(
      'dotnet list package --vulnerable',
      { cwd: join(process.cwd(), 'server-dotnet') }
    );
    return stdout;
  } catch (error) {
    return error.stdout || 'Error running dotnet audit';
  }
}

async function generateReport(npmAudit, dotnetAudit) {
  const timestamp = new Date().toISOString();
  
  let report = `# Security Audit Report\n\n`;
  report += `**Generated**: ${timestamp}\n\n`;
  
  // NPM Audit Summary
  report += `## NPM Dependencies\n\n`;
  const metadata = npmAudit.metadata || { vulnerabilities: {} };
  const vulns = metadata.vulnerabilities || {};
  
  report += `- **Total**: ${vulns.total || 0} vulnerabilities\n`;
  report += `- **Critical**: ${vulns.critical || 0}\n`;
  report += `- **High**: ${vulns.high || 0}\n`;
  report += `- **Moderate**: ${vulns.moderate || 0}\n`;
  report += `- **Low**: ${vulns.low || 0}\n\n`;
  
  if (vulns.total > 0) {
    report += `### Recommendations\n\n`;
    report += `Run \`npm audit fix\` to automatically fix compatible vulnerabilities.\n\n`;
  }
  
  // .NET Audit Summary
  report += `## .NET NuGet Packages\n\n`;
  if (dotnetAudit.includes('no vulnerable packages')) {
    report += `✅ No vulnerable packages found\n\n`;
  } else {
    report += `\`\`\`\n${dotnetAudit}\n\`\`\`\n\n`;
  }
  
  return report;
}

async function main() {
  const artifactsDir = join(process.cwd(), '.artifacts', 'security');
  await ensureDir(artifactsDir);
  
  const npmAudit = await runNpmAudit();
  const dotnetAudit = await runDotnetAudit();
  
  // Save raw audit data
  await writeFile(
    join(artifactsDir, 'npm-audit.json'),
    JSON.stringify(npmAudit, null, 2)
  );
  
  await writeFile(
    join(artifactsDir, 'nuget-audit.txt'),
    dotnetAudit
  );
  
  // Generate markdown report
  const report = await generateReport(npmAudit, dotnetAudit);
  await writeFile(
    join(artifactsDir, 'security-report.md'),
    report
  );
  
  console.log('✅ Security audit complete');
  console.log(`📊 Reports saved to ${artifactsDir}`);
  
  // Exit with error if critical or high vulnerabilities found
  const metadata = npmAudit.metadata || { vulnerabilities: {} };
  const vulns = metadata.vulnerabilities || {};
  if ((vulns.critical || 0) > 0 || (vulns.high || 0) > 0) {
    console.warn('⚠️  Critical or high severity vulnerabilities found!');
    process.exit(1);
  }
}

main().catch(error => {
  console.error('❌ Security audit failed:', error);
  process.exit(1);
});
