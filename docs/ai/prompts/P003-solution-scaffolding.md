# P003 — Solution Scaffolding and Technical Baseline

You have completed:

* P001 — Complete Specification Discovery and Repository Analysis
* P002 — Repository Baseline, Git Hygiene and Governance Verification

The repository now has a clean Git baseline.

You are now allowed to create the application solution structure.

## CRITICAL RULE

Do NOT implement Tic Tac Toe business functionality yet.

This prompt is ONLY for:

* solution/project scaffolding
* framework setup
* dependency structure
* configuration
* build setup
* test project setup
* Angular application scaffolding
* developer experience
* technical baseline verification

Do not implement:

* Game aggregate
* board rules
* move validation
* win detection
* draw detection
* undo
* Memento
* computer strategy
* Strategy implementation
* scoreboard
* GameCompleted
* REST business endpoints
* UI game behavior

Those belong to later prompts.

---

# 1. Read the Specification Again

Before modifying anything, read:

```text
README.md
docs/01-requirements.md
docs/02-prerequisites-and-environment.md
docs/03-nfr.md
docs/04-ddd-and-domain-model.md
docs/05-architecture.md
docs/06-api-contract.md
docs/07-test-strategy.md
docs/08-ai-development-governance.md
docs/09-traceability-matrix.md
docs/10-adr-template-and-initial-decisions.md
docs/11-implementation-plan.md
docs/12-root-ai-change-log.md
docs/13-assumptions.md
docs/14-panel-review.md
docs/ADR-007-game-completed-domain-event.md
docs/AI-CHANGE-AUDIT.md
docs/AI_PROMPTS.md
docs/ASSUMPTIONS.md
docs/CHANGELOG.md
docs/README.md
```

Also read the P001 analysis artifacts:

```text
docs/ai/DOCUMENT-INVENTORY.md
docs/ai/REPOSITORY-ANALYSIS.md
docs/ai/REQUIREMENT-TRACEABILITY.md
docs/ai/ARCHITECTURE-TRACEABILITY.md
docs/ai/AI-DECISIONS.md
docs/ai/IMPLEMENTATION-PLAN.md
docs/ai/IMPLEMENTATION-LOG.md
docs/ai/MANUAL-CHANGES.md
```

---

# 2. Inspect the Development Environment

Before creating projects, inspect the locally available toolchain.

Check:

```text
.NET SDK
ASP.NET Core tooling
Node.js
npm
Angular CLI
Git
```

Use the versions required by:

```text
docs/02-prerequisites-and-environment.md
```

Do not arbitrarily upgrade or downgrade tooling.

If the required tooling is missing, report it clearly.

Do not install major tooling without explicit approval.

---

# 3. Derive the Solution Structure From the Architecture

Read:

```text
docs/05-architecture.md
docs/04-ddd-and-domain-model.md
```

Determine the exact project boundaries required by the specification.

Do not invent a different architecture.

Before creating projects, report the proposed structure:

```text
Solution
│
├── Domain
├── Application
├── Infrastructure
├── API
├── Tests
└── Angular/Web
```

Use the exact names and locations specified by the architecture documentation where available.

If the documentation leaves the exact names open, use clear conventional names and document the choice.

---

# 4. Dependency Direction

Establish dependency direction consistent with the architecture.

Expected conceptual direction:

```text
Domain
  ↑
Application
  ↑
Infrastructure

API
 ↓
Application

Web
 ↓
API
```

More precisely:

```text
Domain
  ← Application
  ← Infrastructure
  ← API
```

The Domain project must not depend on:

* ASP.NET Core
* Entity Framework
* Angular
* HTTP
* infrastructure
* external storage
* UI concerns

Application must not contain:

* Angular code
* HTTP controllers
* infrastructure implementation details

Infrastructure implements application/domain abstractions where required.

API handles HTTP concerns.

Angular handles presentation and API interaction.

Do not introduce dependencies merely for convenience.

---

# 5. Create the .NET Solution

Create the .NET solution according to the architecture documentation.

The solution must contain the required backend projects.

Do not implement domain functionality.

The projects should compile with minimal placeholder configuration only.

Use nullable reference types and the language/version required by the specification.

Enable appropriate compiler/analyzer settings if required by the NFR/architecture documentation.

---

# 6. Create Backend Test Projects

Create the test projects required by:

```text
docs/07-test-strategy.md
```

Do not write business tests yet.

Only establish:

* test framework
* test project structure
* project references
* test discovery
* test execution

A single smoke test may be created only if needed to prove the test infrastructure works.

Do not create fake domain tests.

---

# 7. Create Angular Application

Create the Angular application according to:

```text
docs/02-prerequisites-and-environment.md
docs/05-architecture.md
```

Do not implement the game UI.

The Angular application should only establish:

* project structure
* build configuration
* development configuration
* linting if required
* test configuration if required
* environment configuration
* basic application bootstrap

Do not create:

* game board
* game service
* scoreboard UI
* move history UI
* undo UI

Those belong to later prompts.

---

# 8. Configure Local Development

Establish the local development configuration specified by the documentation.

Where applicable configure:

```text
Backend port
Frontend port
CORS
API base URL
Development environment
```

CORS must be restricted to the actual local frontend origin rather than using unrestricted:

```text
AllowAnyOrigin
```

unless the specification explicitly requires otherwise.

Do not add production infrastructure.

---

# 9. Configuration and Secrets

Do not place secrets in source control.

Create configuration templates if required.

Use:

```text
appsettings.json
appsettings.Development.json
environment configuration
```

only as appropriate to the architecture.

Do not create fake secrets.

Ensure `.gitignore` protects local secret/configuration files where appropriate.

---

# 10. Health / Technical Smoke Verification

If the architecture or NFR documentation requires health checks, establish only the technical health-check infrastructure.

Do not implement business endpoints.

A technical health endpoint such as:

```text
/health
```

may be created if required by the specification.

Do not create:

```text
/api/games
/api/scoreboard
```

yet.

Those belong to the API implementation phase.

---

# 11. Build Verification

Run the backend build.

Run the backend tests.

Run the Angular build.

Run Angular tests if configured.

Verify that:

```text
.NET solution builds successfully
Backend tests execute
Angular application builds successfully
Frontend tests execute if configured
```

Record exact commands and results.

---

# 12. Dependency Verification

Inspect project references.

Confirm:

* Domain has no infrastructure dependency.
* Domain has no API dependency.
* Domain has no Angular dependency.
* Application does not depend on API.
* API depends on Application as designed.
* Infrastructure dependencies follow the documented architecture.
* Tests reference only the projects they need.

If the architecture documentation defines a different valid dependency structure, follow the documentation and record it.

---

# 13. No Business Logic

Before completing P003, search the repository for accidental business implementation.

There must be no implementation of:

```text
win
draw
undo
computer move
scoreboard
GameCompleted
move validation
turn switching
board rules
```

unless those terms exist only in comments/placeholders generated by the scaffolding.

---

# 14. Documentation Updates

Update:

```text
docs/ai/IMPLEMENTATION-LOG.md
docs/ai/REQUIREMENT-TRACEABILITY.md
```

Record:

* Prompt ID: P003
* scaffolding created
* projects created
* framework versions
* configuration created
* build results
* test results
* manual changes, if any
* review status

If P003 requires a new architectural decision that is not already covered, create an ADR under:

```text
docs/ai/decisions/
```

Do not silently change existing ADRs.

---

# 15. Create P003 Review Artifact

Create:

```text
docs/ai/reviews/P003-review.md
```

Use:

```markdown
# P003 Review

## Objective

Solution scaffolding and technical baseline.

## Specification Used

List the specification files consulted.

## Projects Created

List all projects.

## Dependency Direction

Document the dependency graph.

## Tool Versions

Document .NET, Node, npm, Angular and other relevant versions.

## Build Verification

Document commands and results.

## Test Verification

Document commands and results.

## Frontend Verification

Document Angular build/test results.

## Human Review Required

List anything that needs manual review.

## Frozen Decisions Confirmed

- cellIndex 0..8
- Reset reuses gameId
- Memento
- Strategy
- GameCompleted

## Business Functionality Implemented

Explicitly state:

None.

## Status

Ready / Not Ready for P004.
```

---

# 16. Git Commit

Before committing:

```bash
git status
git diff
git diff --cached
```

Review all generated files.

Then commit:

```text
feat: scaffold application solution and technical baseline
```

Do not combine future business implementation into this commit.

---

# 17. Final Report

Return:

## Repository Structure

Show the complete relevant structure.

## Projects

List every backend, frontend and test project.

## Dependency Graph

Show project dependencies.

## Toolchain

Show actual versions.

## Configuration

List configuration introduced.

## Build

Show build results.

## Tests

Show test results.

## Angular

Show Angular build/test results.

## Requirements Covered

List only requirements genuinely addressed by scaffolding.

## Business Functionality

Confirm:

```text
No Tic Tac Toe business functionality implemented.
```

## Git

Show:

* commit hash
* commit message
* working-tree status

## Issues

List any unresolved issues.

## Recommendation

If everything passes:

```text
P003 COMPLETE — READY FOR P004
```

Otherwise:

```text
P003 BLOCKED
```

and explain why.

STOP after P003.
