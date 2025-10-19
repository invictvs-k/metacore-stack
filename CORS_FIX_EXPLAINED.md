# CORS Fix - RoomOperator Connection Issue

## Problem Statement

The RoomOperator connection was failing in both:
1. Settings page - "Test Connections" button showing "Failed to fetch"
2. Overview page - Health check showing "Connection failed"

Even though the configuration was correct (using the exact repository settings).

## Root Cause Analysis

### The CORS Issue

**What is CORS?**
Cross-Origin Resource Sharing (CORS) is a browser security feature that restricts web pages from making requests to a different domain than the one serving the web page.

**In our case:**
- Dashboard runs on: `http://localhost:5173` (Vite dev server)
- Integration API runs on: `http://localhost:40901`
- RoomServer runs on: `http://localhost:40801`
- RoomOperator runs on: `http://localhost:40802`

When the dashboard tried to directly fetch from RoomOperator:
```javascript
fetch('http://127.0.0.1:40802/health')
```

The browser blocked it with:
```
Access to fetch at 'http://127.0.0.1:40802/health' from origin 'http://localhost:5173' 
has been blocked by CORS policy: No 'Access-Control-Allow-Origin' header is present 
on the requested resource.
```

### Why MCP Status Worked

The MCP status endpoint was already working because it was proxied through the Integration API:
```javascript
fetch('/api/mcp/status')  // This goes to Integration API, not directly to RoomServer
```

The Integration API then makes a server-side request to RoomServer, which has no CORS restrictions.

## Solution: Health Check Proxy Endpoints

### Implementation

Created new proxy endpoints in Integration API (`tools/integration-api/src/routes/health.ts`):

1. **GET /api/health/roomserver**
   - Proxies health check to RoomServer
   - Tries `/health` endpoint first, falls back to base URL
   - Returns structured response with status

2. **GET /api/health/roomoperator**
   - Proxies health check to RoomOperator
   - Same fallback logic as RoomServer
   - Returns structured response with status

3. **GET /api/health/all**
   - Checks all services (RoomServer, RoomOperator, MCP) in one request
   - Returns overall health status
   - Useful for dashboard overview

### Response Format

**Success (200):**
```json
{
  "status": "healthy",
  "service": "roomoperator",
  "endpoint": "http://127.0.0.1:40802",
  "data": { ... }
}
```

**Error (503):**
```json
{
  "status": "error",
  "service": "roomoperator",
  "error": "Connection timeout",
  "endpoint": "http://127.0.0.1:40802"
}
```

### Frontend Updates

**Settings Page (Before):**
```javascript
// Direct fetch - BLOCKED BY CORS
const roResponse = await fetch(`${parsedConfig.roomOperator.baseUrl}/health`, {
  method: 'GET',
  signal: AbortSignal.timeout(5000)
});
```

**Settings Page (After):**
```javascript
// Proxied through Integration API - NO CORS ISSUES
const roResponse = await fetch('/api/health/roomoperator', {
  signal: AbortSignal.timeout(5000)
});
```

**Overview Page** - Same pattern applied

## Request Flow Comparison

### Before (CORS Error)
```
Browser (localhost:5173)
    |
    | Direct fetch to localhost:40802
    v
❌ BLOCKED BY CORS POLICY
    |
RoomOperator (localhost:40802)
```

### After (Working)
```
Browser (localhost:5173)
    |
    | fetch('/api/health/roomoperator')
    v
Integration API (localhost:40901)
    |
    | Server-side fetch to localhost:40802
    v
✅ NO CORS RESTRICTIONS (server-to-server)
    |
RoomOperator (localhost:40802)
```

## Benefits of This Approach

1. **No CORS Issues** - Server-side requests don't have CORS restrictions
2. **Consistent Pattern** - Same approach as MCP status endpoint
3. **Better Error Handling** - Centralized error handling in Integration API
4. **Timeout Management** - Consistent 5-second timeout across all checks
5. **Fallback Logic** - Tries /health first, then base URL
6. **Single Source of Truth** - Configuration only in Integration API
7. **Future-Proof** - Easy to add authentication, rate limiting, etc.

## Configuration Requirements

No configuration changes needed! The solution works with existing config:
```json
{
  "roomOperator": {
    "baseUrl": "http://127.0.0.1:40802",
    "events": {
      "type": "sse",
      "path": "/events"
    }
  }
}
```

## Testing the Fix

### Prerequisites
1. Integration API must be running on port 40901
2. RoomOperator should be running on port 40802 (optional for error testing)

### Test Steps

1. **Settings Page:**
   ```bash
   # Navigate to http://localhost:5173/settings
   # Click "Test Connections"
   # Expected: RoomOperator shows "Connected" (green) if service is running
   # Expected: RoomOperator shows "Connection timeout" (red) if service is down
   ```

2. **Overview Page:**
   ```bash
   # Navigate to http://localhost:5173/
   # Observe RoomOperator status
   # Expected: Shows "healthy" (green check) if service is running
   # Expected: Shows "error" (red X) with error message if service is down
   ```

3. **Direct API Test:**
   ```bash
   # Test the proxy endpoint directly
   curl http://localhost:40901/api/health/roomoperator
   
   # Expected if RoomOperator is running:
   # {"status":"healthy","service":"roomoperator","endpoint":"http://127.0.0.1:40802","data":{...}}
   
   # Expected if RoomOperator is down:
   # {"status":"error","service":"roomoperator","error":"Connection failed","endpoint":"http://127.0.0.1:40802"}
   ```

## Alternative Solutions Considered

### 1. Add CORS Headers to RoomOperator
**Pros:** Direct connection, no proxy needed
**Cons:** 
- Requires modifying RoomOperator code
- Security risk (exposing service to browser)
- Not our codebase to modify

### 2. Use Vite Proxy
**Pros:** Built into Vite
**Cons:**
- Only works in development
- Requires rebuilding for different environments
- Harder to test production builds

### 3. Same-Origin Deployment
**Pros:** No CORS at all
**Cons:**
- Requires complex deployment setup
- Not suitable for development
- Limits architecture flexibility

**✅ Chosen Solution: Integration API Proxy**
- Works in both dev and production
- Consistent with existing MCP pattern
- Server-side = more control and security
- Easy to test and maintain

## Related Files

### Backend
- `tools/integration-api/src/routes/health.ts` - New proxy endpoints
- `tools/integration-api/src/index.ts` - Router registration
- `tools/integration-api/README.md` - Documentation

### Frontend
- `apps/operator-dashboard/src/pages/Settings.tsx` - Uses proxy in connection test
- `apps/operator-dashboard/src/pages/Overview.tsx` - Uses proxy in health checks

## Troubleshooting

### RoomOperator still shows as failed

1. **Check if Integration API is running:**
   ```bash
   curl http://localhost:40901/health
   # Should return: {"status":"ok","timestamp":"..."}
   ```

2. **Check if RoomOperator is running:**
   ```bash
   curl http://localhost:40802/health
   # Or: curl http://localhost:40802
   ```

3. **Check Integration API logs:**
   ```bash
   # Look for errors when calling /api/health/roomoperator
   ```

4. **Verify configuration:**
   ```bash
   # Check configs/dashboard.settings.json
   # roomOperator.baseUrl should be "http://127.0.0.1:40802"
   ```

### CORS errors still appearing

If you still see CORS errors, verify:
1. You're using the proxy endpoints (`/api/health/*`) not direct URLs
2. Integration API is running and accessible
3. Browser cache is cleared
4. You're not mixing old and new code

## Summary

The fix resolves the RoomOperator connection issue by:
1. Creating proxy endpoints in Integration API for health checks
2. Updating frontend to use these proxies instead of direct connections
3. Eliminating CORS restrictions since requests are server-to-server
4. Maintaining consistent patterns with existing MCP status endpoint

**Result:** RoomOperator status now works correctly in both Settings and Overview pages! ✅
