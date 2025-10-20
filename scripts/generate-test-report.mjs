#!/usr/bin/env node
/**
 * Generate test report from test execution results
 * Aggregates results from smoke, contract, and schema tests
 */

import { exec } from 'child_process';
import { promisify } from 'util';
import { mkdir, writeFile } from 'fs/promises';
import { join } from 'path';

const execAsync = promisify(exec);

async function ensureDir(dir) {
  await mkdir(dir, { recursive: true });
}

async function runTest(command, name) {
  console.log(`🧪 Running ${name}...`);
  const startTime = Date.now();
  
  try {
    const { stdout, stderr } = await execAsync(command, {
      cwd: process.cwd(),
      maxBuffer: 10 * 1024 * 1024
    });
    
    const duration = Date.now() - startTime;
    
    return {
      name,
      status: 'passed',
      duration,
      output: stdout,
      error: null
    };
  } catch (error) {
    const duration = Date.now() - startTime;
    
    return {
      name,
      status: 'failed',
      duration,
      output: error.stdout || '',
      error: error.message
    };
  }
}

async function generateReport(results) {
  const timestamp = new Date().toISOString();
  
  let report = `# Test Report\n\n`;
  report += `**Generated**: ${timestamp}\n\n`;
  
  // Summary
  const passed = results.filter(r => r.status === 'passed').length;
  const failed = results.filter(r => r.status === 'failed').length;
  const total = results.length;
  
  report += `## Summary\n\n`;
  report += `- **Total Tests**: ${total}\n`;
  report += `- **Passed**: ${passed} ✅\n`;
  report += `- **Failed**: ${failed} ${failed > 0 ? '❌' : ''}\n`;
  report += `- **Success Rate**: ${((passed / total) * 100).toFixed(1)}%\n\n`;
  
  // Total duration
  const totalDuration = results.reduce((sum, r) => sum + r.duration, 0);
  report += `- **Total Duration**: ${(totalDuration / 1000).toFixed(2)}s\n\n`;
  
  // Individual test results
  report += `## Test Results\n\n`;
  
  for (const result of results) {
    const icon = result.status === 'passed' ? '✅' : '❌';
    report += `### ${icon} ${result.name}\n\n`;
    report += `- **Status**: ${result.status}\n`;
    report += `- **Duration**: ${(result.duration / 1000).toFixed(2)}s\n\n`;
    
    if (result.status === 'failed' && result.error) {
      report += `**Error**:\n\`\`\`\n${result.error}\n\`\`\`\n\n`;
    }
    
    if (result.output) {
      report += `<details>\n<summary>Output</summary>\n\n\`\`\`\n${result.output}\n\`\`\`\n</details>\n\n`;
    }
  }
  
  // Coverage section (placeholder)
  report += `## Code Coverage\n\n`;
  report += `_Note: Coverage reporting not yet implemented._\n\n`;
  report += `Target coverage: 60%\n\n`;
  
  // Recommendations
  if (failed > 0) {
    report += `## Recommendations\n\n`;
    report += `- Review failed test outputs above\n`;
    report += `- Fix failing tests before merging\n`;
    report += `- Ensure all dependencies are installed\n`;
    report += `- Check for environment-specific issues\n\n`;
  }
  
  return report;
}

async function main() {
  console.log('📊 Generating test report...\n');
  
  const artifactsDir = join(process.cwd(), '.artifacts', 'test');
  await ensureDir(artifactsDir);
  
  // Run all test suites
  const results = [];
  
  results.push(await runTest('npm run test:schemas', 'Schema Validation'));
  results.push(await runTest('npm run test:contracts', 'Contract Validation'));
  results.push(await runTest('npm run test:smoke', 'Smoke Tests'));
  
  // Generate report
  const report = await generateReport(results);
  
  // Save report
  const reportPath = join(artifactsDir, 'test-report.md');
  await writeFile(reportPath, report);
  
  // Also save to ci directory for visibility
  const ciReportPath = join(process.cwd(), 'ci', 'test-report.md');
  await writeFile(ciReportPath, report);
  
  console.log('\n✅ Test report generated');
  console.log(`📄 Artifacts: ${artifactsDir}`);
  console.log(`📄 CI Report: ${ciReportPath}`);
  
  // Exit with error if tests failed
  const failed = results.filter(r => r.status === 'failed').length;
  if (failed > 0) {
    console.warn(`\n⚠️  ${failed} test suite(s) failed`);
    process.exit(1);
  }
}

main().catch(error => {
  console.error('❌ Test report generation failed:', error);
  process.exit(1);
});
