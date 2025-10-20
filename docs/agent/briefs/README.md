# Agent Briefs

## What is a Brief?

A **brief** is a focused instruction document for AI coding agents. It describes a single, well-scoped task with clear acceptance criteria and context.

## When to Use a Brief

Use briefs for:
- Implementing a new feature
- Fixing a specific bug
- Refactoring a component
- Adding tests for existing functionality
- Creating documentation

## How to Write an Effective Brief

### 1. Be Specific

❌ Bad: "Improve the dashboard"  
✅ Good: "Add error boundary to the RoomList component to handle API failures gracefully"

### 2. Provide Context

Include:
- Why this task is needed
- What problem it solves
- Links to relevant code/docs/issues

### 3. Define Acceptance Criteria

Be explicit about what "done" means:
- ✓ Component renders error message when API fails
- ✓ Error is logged to console with context
- ✓ Unit tests cover error scenarios
- ✓ Error UI matches design system

### 4. Specify Inputs and Outputs

**Inputs:**
- Existing code files to modify
- API contracts to follow
- Design specs or mockups

**Outputs:**
- Modified/new files
- Updated tests
- Documentation updates

### 5. Set Boundaries

Clarify what is **out of scope** to prevent scope creep:
- "Do not modify the authentication logic"
- "Focus only on the UI; backend changes will be separate"

## Brief Template

Use the template at `../templates/brief-template.md` to create new briefs.

## Example Briefs

```markdown
# Brief: Add Health Check Endpoint to Integration API

## Context
The Integration API needs a health check endpoint for monitoring and load balancing.

## Component
`tools/integration-api`

## Acceptance Criteria
- [ ] GET /health returns 200 OK when healthy
- [ ] Response includes service version and timestamp
- [ ] Response includes dependency status (database, external APIs)
- [ ] Unit tests verify endpoint behavior
- [ ] Documentation updated

## Inputs
- Current API structure in `tools/integration-api/src/`
- Express.js framework patterns

## Outputs
- New file: `src/routes/health.ts`
- Updated: `src/app.ts` (register route)
- Updated: `README.md` (document endpoint)
- New file: `src/routes/__tests__/health.test.ts`

## Out of Scope
- Adding new dependencies
- Modifying existing endpoints
- Infrastructure changes
```

## Tips

1. **One task per brief**: Don't combine multiple unrelated changes
2. **Reference existing patterns**: Point to similar code as examples
3. **Include error scenarios**: Specify how to handle edge cases
4. **Link to standards**: Reference coding conventions, style guides, ADRs
5. **Be testable**: Ensure acceptance criteria can be objectively verified

## Review Checklist

Before submitting a brief to an agent:

- [ ] Task is clearly scoped and achievable
- [ ] Context explains the "why"
- [ ] Acceptance criteria are measurable
- [ ] Inputs and outputs are specified
- [ ] Boundaries are explicit
- [ ] Examples or references are provided
- [ ] No ambiguous language ("improve", "better", "nice to have")
