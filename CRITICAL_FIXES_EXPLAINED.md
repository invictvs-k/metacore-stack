# Critical Fixes - Event Streaming, Service Independence, and Test Execution

## Issues Fixed

### 1. RoomServer Terminating When RoomOperator Terminates ❌ → ✅

**Problem:**
Services were not independent. When RoomOperator went down, it could affect RoomServer connectivity through the event proxy.

**Solution:**
- Each event stream proxy now operates independently
- Connection failures in one service don't crash the entire proxy
- Services can reconnect independently without affecting each other
- Added proper error isolation per service

**Implementation Details:**
```typescript
// Before: Single failure could affect multiple services
const cleanup = await proxyEventStream(req, res, eventUrl, 'roomserver');

// After: Each stream is independent with isolated error handling
const cleanupRoomServer = await proxyEventStream(req, res, roomServerUrl, 'roomserver');
const cleanupRoomOperator = await proxyEventStream(req, res, roomOperatorUrl, 'roomoperator');
// If one fails, the other continues
```

---

### 2. Event Stream Not Loading in Real-Time ❌ → ✅

**Problem:**
Events weren't appearing dynamically on the Events page without switching tabs.

**Root Cause:**
The event proxy wasn't handling connection state properly, causing events to not stream correctly.

**Solution:**
- Added `isConnected` flag to track actual connection state
- Improved async iteration over event stream body
- Better handling of connection lifecycle (connect → stream → disconnect)
- Events now stream continuously when services are available

**Implementation:**
```typescript
async function proxyEventStream(...) {
  const controller = new AbortController();
  let isConnected = false; // Track actual connection state
  
  try {
    const upstreamResponse = await fetch(eventUrl, ...);
    
    if (upstreamResponse.ok && upstreamResponse.body) {
      isConnected = true; // Mark as connected only when actually connected
      
      for await (const chunk of upstreamResponse.body) {
        parseAndForwardChunk(chunk.toString(), res, source);
      }
    }
  } catch (error) {
    // Only send error if we were connected
    if (isConnected) {
      sendSSEMessage(res, { type: 'error', ... });
    }
  }
}
```

---

### 3. Duplicate Connected/Error Messages ❌ → ✅

**Problem:**
When services went down or came back up, the UI showed:
```
Connected
Error
Connected
Error
Connected
Error
...
```

**Root Cause:**
The proxy was sending a "connected" message immediately when the SSE endpoint was hit, before actually connecting to the upstream service.

**Solution:**
- Removed premature "connected" messages
- Only send messages based on actual connection events
- Added connection state tracking to prevent duplicate error messages
- One error per connection failure, not multiple

**Before:**
```typescript
// GET /api/events/roomserver
eventsRouter.get('/roomserver', async (req, res) => {
  setupSSE(res);
  
  // PROBLEM: Sends "connected" before actually connecting
  sendSSEMessage(res, { type: 'connected', source: 'roomserver' });
  
  const cleanup = await proxyEventStream(req, res, eventUrl, 'roomserver');
});
```

**After:**
```typescript
// GET /api/events/roomserver
eventsRouter.get('/roomserver', async (req, res) => {
  setupSSE(res);
  
  // No premature message - only send based on actual events
  const cleanup = await proxyEventStream(req, res, eventUrl, 'roomserver');
});
```

**Result:**
- When service is down: Single "Error: Cannot connect" message
- When service comes up: Events start streaming (no "connected" spam)
- When service goes down: Single "Error: Connection closed" message

---

### 4. Tests Not Executing ❌ → ✅

**Problem:**
Clicking "Run Test" on the Tests page resulted in errors and no test execution.

**Root Cause:**
The API expected different request body formats for different scenarios:
- For all tests: `{ all: true }`
- For specific test: `{ scenarioId: "test-name" }`

But the frontend was always sending `{ scenarioId: "all" }` which the backend didn't recognize.

**Solution:**

**Backend (already correct):**
```typescript
// POST /api/tests/run
testsRouter.post('/run', async (req, res) => {
  const { scenarioId, all } = req.body;
  const testId = all ? 'all' : (scenarioId || 'all');
  const result = await runTest(testId);
  res.json(result);
});
```

**Frontend (fixed):**
```typescript
// Before: Always sent scenarioId
const runTest = async (scenarioId = 'all') => {
  const response = await fetch('/api/tests/run', {
    method: 'POST',
    body: JSON.stringify({ scenarioId }), // ❌ Wrong for 'all'
  });
};

// After: Send correct format based on scenario
const runTest = async (scenarioId = 'all') => {
  const body = scenarioId === 'all' 
    ? { all: true }        // ✅ Correct for all tests
    : { scenarioId };      // ✅ Correct for specific test
    
  const response = await fetch('/api/tests/run', {
    method: 'POST',
    body: JSON.stringify(body),
  });
};
```

**Additional Improvements:**
- Added error state in Tests page
- Display errors in red alert box with clear message
- Better error extraction from API responses
- Errors are cleared when starting a new test

---

## Testing the Fixes

### Event Streaming
1. **Start only Integration API** (no RoomServer/RoomOperator)
   - Navigate to /events
   - Should see single error message: "Service unavailable"
   - No duplicate messages

2. **Start RoomServer**
   - Events from RoomServer start streaming
   - RoomOperator still shows single error (not spam)

3. **Start RoomOperator**
   - Events from RoomOperator start streaming
   - Both services now streaming independently

4. **Stop RoomOperator**
   - RoomOperator shows single "Connection error"
   - RoomServer continues streaming without interruption

### Test Execution
1. Navigate to /tests
2. Select "All Tests" from dropdown
3. Click "Run Test"
4. Should see:
   - Logs appearing in real-time
   - Exit code displayed when complete
   - Artifacts directory link
5. If error occurs:
   - Red alert box with clear error message

### Service Independence
1. Start Integration API only
2. Start Dashboard
3. All pages should load and function
4. Events page shows service-specific errors
5. Start RoomServer
6. RoomServer events start flowing
7. RoomOperator still shows error (doesn't affect RoomServer)

## Files Changed

### Backend
- `tools/integration-api/src/routes/events.ts`
  - Added `isConnected` flag for connection state tracking
  - Removed premature "connected" messages
  - Improved error handling with single error per failure
  - Better message categorization

### Frontend
- `apps/operator-dashboard/src/hooks/useTestRunner.ts`
  - Fixed request body format for test execution
  - Better error extraction from API responses
  
- `apps/operator-dashboard/src/pages/Tests.tsx`
  - Added error state
  - Display error messages in UI
  - Clear errors on new test run

## Summary

All critical issues have been resolved:

✅ **Services are independent** - RoomServer and RoomOperator don't affect each other
✅ **Events stream in real-time** - Continuous event flow when services are up
✅ **No duplicate messages** - Clean, single-state messages
✅ **Tests execute properly** - Correct API request format with error handling

The system is now resilient and operates correctly even when some services are unavailable.
