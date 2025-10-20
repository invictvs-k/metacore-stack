#!/usr/bin/env node

/**
 * MCP Integration Test Runner
 * 
 * Executes MCP-specific integration test scenarios and generates reports.
 */

import fs from 'fs';
import path from 'path';
import { fileURLToPath } from 'url';
import config from '../config.mcp.js';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

// Simple logger
class Logger {
  constructor(verbose = false) {
    this.verbose = verbose;
  }

  info(message, ...args) {
    console.log(`[INFO] ${message}`, ...args);
  }

  success(message, ...args) {
    console.log(`[SUCCESS] ${message}`, ...args);
  }

  warn(message, ...args) {
    console.warn(`[WARN] ${message}`, ...args);
  }

  error(message, ...args) {
    console.error(`[ERROR] ${message}`, ...args);
  }

  step(message) {
    console.log(`\n[STEP] ${message}`);
  }

  section(title) {
    console.log(`\n${'='.repeat(80)}`);
    console.log(`  ${title}`);
    console.log(`${'='.repeat(80)}\n`);
  }
}

const logger = new Logger(config.execution.verbose);

// Load scenarios
async function loadScenario(scenarioName) {
  const scenarioPath = path.join(__dirname, `${scenarioName}.js`);
  try {
    const module = await import(scenarioPath);
    return module.default;
  } catch (error) {
    logger.error(`Failed to load scenario ${scenarioName}:`, error.message);
    return null;
  }
}

// Run all scenarios
async function runScenarios() {
  logger.section('MCP Integration Test Suite');
  logger.info('Configuration:', {
    roomServer: config.roomServer.baseUrl,
    operator: config.operator.baseUrl,
    scenarios: config.scenarios,
  });

  const results = [];
  const startTime = Date.now();

  for (const scenarioName of config.scenarios) {
    logger.section(`Running Scenario: ${scenarioName}`);
    
    const scenario = await loadScenario(scenarioName);
    if (!scenario) {
      results.push({
        scenario: scenarioName,
        status: 'FAILED',
        error: 'Failed to load scenario',
        duration: 0,
      });
      
      if (config.execution.failFast) {
        logger.error('Fail-fast enabled, stopping test execution');
        break;
      }
      
      continue;
    }

    try {
      const context = {
        config,
        logger,
      };
      
      const result = await scenario(context);
      results.push(result);
      
      if (result.status === 'FAILED' && config.execution.failFast) {
        logger.error('Fail-fast enabled, stopping test execution');
        break;
      }
    } catch (error) {
      logger.error(`Scenario ${scenarioName} threw an exception:`, error);
      results.push({
        scenario: scenarioName,
        status: 'FAILED',
        error: error.message,
        stack: error.stack,
        duration: 0,
      });
      
      if (config.execution.failFast) {
        logger.error('Fail-fast enabled, stopping test execution');
        break;
      }
    }

    // Wait between scenarios
    if (config.timeouts.betweenScenariosMs > 0) {
      await new Promise(resolve => setTimeout(resolve, config.timeouts.betweenScenariosMs));
    }
  }

  const totalDuration = Date.now() - startTime;

  // Generate summary
  logger.section('Test Summary');
  const passed = results.filter(r => r.status === 'PASSED').length;
  const failed = results.filter(r => r.status === 'FAILED').length;
  const total = results.length;

  logger.info(`Total scenarios: ${total}`);
  logger.info(`Passed: ${passed}`);
  logger.info(`Failed: ${failed}`);
  logger.info(`Total duration: ${totalDuration}ms`);

  results.forEach(result => {
    const icon = result.status === 'PASSED' ? '✓' : '✗';
    logger.info(`  ${icon} ${result.scenario}: ${result.status} (${result.duration}ms)`);
  });

  // Save results
  const report = {
    timestamp: new Date().toISOString(),
    config: {
      roomServer: config.roomServer.baseUrl,
      operator: config.operator.baseUrl,
      scenarios: config.scenarios,
    },
    summary: {
      total,
      passed,
      failed,
      duration: totalDuration,
    },
    results,
  };

  // Create output directory
  const artifactsDir = process.env.ARTIFACTS_DIR || config.output.logsDir;
  const resultsDir = path.join(artifactsDir, 'results');
  fs.mkdirSync(resultsDir, { recursive: true });

  // Save JSON report
  const reportPath = path.join(artifactsDir, config.output.resultsFile);
  fs.writeFileSync(reportPath, JSON.stringify(report, null, 2));
  logger.success(`Report saved to: ${reportPath}`);

  // Generate JUnit XML
  const junitXml = generateJUnitXML(report);
  const junitPath = path.join(artifactsDir, config.output.junitFile);
  fs.writeFileSync(junitPath, junitXml);
  logger.success(`JUnit report saved to: ${junitPath}`);

  // Exit with appropriate code
  const exitCode = failed > 0 ? 1 : 0;
  logger.section(`Test Suite ${exitCode === 0 ? 'PASSED' : 'FAILED'}`);
  process.exit(exitCode);
}

// Generate JUnit XML
function generateJUnitXML(report) {
  const escapeXML = (str) => {
    if (!str) return '';
    return str
      .replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/>/g, '&gt;')
      .replace(/"/g, '&quot;')
      .replace(/'/g, '&apos;');
  };

  const testcases = report.results.map(result => {
    const status = result.status === 'PASSED' ? '' : `<failure message="${escapeXML(result.error || 'Test failed')}"></failure>`;
    return `    <testcase name="${escapeXML(result.scenario)}" time="${result.duration / 1000}">${status}</testcase>`;
  }).join('\n');

  return `<?xml version="1.0" encoding="UTF-8"?>
<testsuites>
  <testsuite name="MCP Integration Tests" tests="${report.summary.total}" failures="${report.summary.failed}" time="${report.summary.duration / 1000}">
${testcases}
  </testsuite>
</testsuites>`;
}

// Run the test suite
runScenarios().catch(error => {
  logger.error('Fatal error:', error);
  process.exit(1);
});
