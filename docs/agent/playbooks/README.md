# Agent Playbooks

## What is a Playbook?

A **playbook** is a multi-step workflow that coordinates multiple tasks or briefs to achieve a larger goal. It defines the sequence, dependencies, decision points, and rollback procedures.

## When to Use a Playbook

Use playbooks for:
- Complex features requiring multiple changes
- Migration or refactoring efforts
- Release workflows
- Multi-component integration
- Staged rollouts

## Playbook vs Brief

| Aspect | Brief | Playbook |
|--------|-------|----------|
| Scope | Single task | Multiple tasks |
| Duration | Minutes to hours | Hours to days |
| Dependencies | Few or none | Many, with gates |
| Decision Points | None | Multiple |
| Rollback | Simple revert | Staged rollback |

## Playbook Structure

A well-formed playbook includes:

### 1. Overview
- Goal and motivation
- Success criteria for the entire playbook
- Estimated duration

### 2. Prerequisites
- Required access/permissions
- Environment setup
- Backup procedures

### 3. Steps (Phases)
Each step includes:
- **Task**: What to do
- **Gate**: Verification before proceeding
- **Rollback**: How to undo if needed
- **Dependencies**: What must be complete first

### 4. Decision Points
- Criteria for go/no-go decisions
- Alternative paths based on outcomes
- Escalation procedures

### 5. Validation
- How to verify overall success
- Monitoring and observability
- Performance benchmarks

### 6. Rollback Plan
- Step-by-step rollback for each phase
- Data preservation procedures
- Communication plan

## Example Playbook

```markdown
# Playbook: Migrate RoomServer to PostgreSQL

## Goal
Replace in-memory storage with PostgreSQL for data persistence.

## Success Criteria
- All tests pass with PostgreSQL backend
- Zero data loss during migration
- Performance meets or exceeds current benchmarks
- Rollback tested and verified

## Prerequisites
- PostgreSQL 14+ installed locally
- Database migration tools configured
- Backup of current test data

## Phases

### Phase 1: Schema Design
**Tasks:**
1. Create PostgreSQL schema matching current data model
2. Add migration scripts

**Gate:** Schema reviewed and approved  
**Rollback:** N/A (no changes to running system)

### Phase 2: Repository Layer
**Tasks:**
1. Implement PostgreSQL repository
2. Add integration tests
3. Ensure all tests pass

**Gate:** Test coverage ≥90%, all tests green  
**Rollback:** Revert commits

### Phase 3: Configuration
**Tasks:**
1. Add connection string configuration
2. Update Docker Compose
3. Add environment variable documentation

**Gate:** Services start successfully with new config  
**Rollback:** Revert config files

### Phase 4: Feature Flag
**Tasks:**
1. Add feature flag for PostgreSQL vs. in-memory
2. Default to in-memory (safe fallback)

**Gate:** Flag toggles between backends correctly  
**Rollback:** Disable flag

### Phase 5: Parallel Run
**Tasks:**
1. Enable PostgreSQL in test environment
2. Monitor for 48 hours
3. Compare metrics with in-memory

**Decision Point:**
- If metrics acceptable → proceed
- If issues found → investigate or rollback

**Gate:** Zero critical errors, performance within 10%  
**Rollback:** Flip feature flag to in-memory

### Phase 6: Cutover
**Tasks:**
1. Set PostgreSQL as default
2. Remove in-memory code
3. Update documentation

**Gate:** All tests pass, manual verification complete  
**Rollback:** Restore in-memory code, flip flag

## Rollback Procedure

From Phase 5 or 6:
1. Set feature flag to "in-memory"
2. Restart services
3. Verify functionality
4. Communicate status

From Phase 3 or 4:
1. Revert configuration commits
2. Restart services
3. Verify tests pass

## Monitoring

During and after migration:
- Query latency (P50, P95, P99)
- Error rates
- Connection pool usage
- Database CPU/memory

## Communication

- Notify team before Phase 5
- Status updates every 4 hours during Phase 5
- Incident channel for issues
```

## Creating a Playbook

Use the template at `../templates/playbook-template.md` to create new playbooks.

## Tips

1. **Start small**: Break large goals into manageable phases
2. **Add gates**: Never proceed without verification
3. **Plan rollback**: Every phase needs a recovery path
4. **Include metrics**: Define what "working" means quantitatively
5. **Test rollback**: Practice rollback procedures before executing playbook
6. **Document decisions**: Record why choices were made at each decision point
7. **Version control**: Keep playbooks in git for tracking and reuse

## Execution Checklist

- [ ] Playbook reviewed by team lead
- [ ] All prerequisites met
- [ ] Backup/snapshot created
- [ ] Monitoring configured
- [ ] Communication channels ready
- [ ] Rollback procedure tested
- [ ] Gate criteria defined and measurable
- [ ] Stakeholders notified
