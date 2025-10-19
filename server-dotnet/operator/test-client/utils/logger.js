/**
 * Logger utility with timestamps and color coding
 */

const COLORS = {
  reset: '\x1b[0m',
  bright: '\x1b[1m',
  dim: '\x1b[2m',
  red: '\x1b[31m',
  green: '\x1b[32m',
  yellow: '\x1b[33m',
  blue: '\x1b[34m',
  magenta: '\x1b[35m',
  cyan: '\x1b[36m',
};

class Logger {
  constructor(verbose = false) {
    this.verbose = verbose;
    this.startTime = Date.now();
  }

  _timestamp() {
    const elapsed = Date.now() - this.startTime;
    const seconds = (elapsed / 1000).toFixed(3);
    return `[+${seconds}s]`;
  }

  _format(level, color, message, data = null) {
    const timestamp = this._timestamp();
    const levelStr = level.padEnd(7);
    let output = `${COLORS.dim}${timestamp}${COLORS.reset} ${color}${levelStr}${COLORS.reset} ${message}`;
    
    if (data !== null && data !== undefined) {
      if (typeof data === 'object') {
        output += `\n${COLORS.dim}${JSON.stringify(data, null, 2)}${COLORS.reset}`;
      } else {
        output += ` ${COLORS.dim}${data}${COLORS.reset}`;
      }
    }
    
    return output;
  }

  info(message, data = null) {
    console.log(this._format('INFO', COLORS.blue, message, data));
  }

  success(message, data = null) {
    console.log(this._format('SUCCESS', COLORS.green, message, data));
  }

  warn(message, data = null) {
    console.warn(this._format('WARN', COLORS.yellow, message, data));
  }

  error(message, data = null) {
    console.error(this._format('ERROR', COLORS.red, message, data));
  }

  debug(message, data = null) {
    if (this.verbose) {
      console.log(this._format('DEBUG', COLORS.magenta, message, data));
    }
  }

  step(stepNumber, message) {
    console.log(`\n${COLORS.bright}${COLORS.cyan}═══ Step ${stepNumber}: ${message} ═══${COLORS.reset}`);
  }

  section(title) {
    console.log(`\n${COLORS.bright}╔════════════════════════════════════════════════╗${COLORS.reset}`);
    console.log(`${COLORS.bright}║  ${title.padEnd(44)}║${COLORS.reset}`);
    console.log(`${COLORS.bright}╚════════════════════════════════════════════════╝${COLORS.reset}\n`);
  }

  separator() {
    console.log(`${COLORS.dim}${'─'.repeat(60)}${COLORS.reset}`);
  }

  summary(passed, failed, skipped = 0) {
    this.separator();
    console.log(`\n${COLORS.bright}Test Summary:${COLORS.reset}`);
    console.log(`  ${COLORS.green}✓ Passed: ${passed}${COLORS.reset}`);
    console.log(`  ${COLORS.red}✗ Failed: ${failed}${COLORS.reset}`);
    if (skipped > 0) {
      console.log(`  ${COLORS.yellow}⊘ Skipped: ${skipped}${COLORS.reset}`);
    }
    
    const total = passed + failed + skipped;
    const successRate = total > 0 ? ((passed / total) * 100).toFixed(1) : 0;
    console.log(`  ${COLORS.bright}Success Rate: ${successRate}%${COLORS.reset}\n`);
    
    return failed === 0;
  }
}

export default Logger;
