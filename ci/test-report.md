# Test Report

**Generated**: 2025-10-20T16:36:44.512Z

## Summary

- **Total Tests**: 3
- **Passed**: 3 ✅
- **Failed**: 0
- **Success Rate**: 100.0%

- **Total Duration**: 3.12s

## Test Results

### ✅ Schema Validation

- **Status**: passed
- **Duration**: 0.51s

<details>
<summary>Output</summary>

```

> metacore-stack-dashboard@1.0.0 test:schemas
> cd schemas && npm run validate


> validate
> node ./validate.mjs

✅ examples/room-min.json ok
✅ examples/entity-human.json ok
✅ examples/message-command.json ok
✅ examples/artifact-sample.json ok
✅ invalid example rejected: examples/invalid/artifact-bad-hash.json
✅ invalid example rejected: examples/invalid/message-missing-fields.json

All schema validations passed.

```

</details>

### ✅ Contract Validation

- **Status**: passed
- **Duration**: 0.25s

<details>
<summary>Output</summary>

```

> metacore-stack-dashboard@1.0.0 test:contracts
> node scripts/validate-contracts.mjs

🔍 Validating contracts...

✅ SSE Event 'log' valid
✅ SSE Event 'heartbeat' valid
✅ SSE Event 'done' valid
✅ Commands Catalog valid

==================================================
✅ All contract validations PASSED

```

</details>

### ✅ Smoke Tests

- **Status**: passed
- **Duration**: 2.35s

<details>
<summary>Output</summary>

```

> metacore-stack-dashboard@1.0.0 test:smoke
> node tests/smoke/smoke.test.mjs

🚀 Running smoke tests...

🔨 Testing builds...
  - Building integration-api...
    ✅ integration-api builds successfully
  - Validating schemas...
    ✅ Schemas valid

🌐 Testing endpoints (if services are running)...
  ⚠️  Integration API Health:  (service may not be running)
  ⚠️  SSE Heartbeat:  (service may not be running)

✅ Smoke tests passed

```

</details>

## Code Coverage

_Note: Coverage reporting not yet implemented._

Target coverage: 60%
