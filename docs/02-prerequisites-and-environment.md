# Prerequisites and Environment

## 1. Required Development Environment

Install and verify:

- Git.
- GitHub account/repository access.
- .NET SDK compatible with the selected supported release.
- Node.js LTS.
- npm.
- Angular CLI compatible with the selected Angular version.
- A modern browser.
- IDE/editor.
- REST client such as browser DevTools, curl, Postman or equivalent.

## 2. AI IDE

The implementation may be performed with:

- OpenAI Codex.
- Google Antigravity.
- Claude.
- GitHub Copilot.
- Another approved AI-assisted coding workflow.

The tool is secondary to the engineering process.

The repository must preserve enough evidence to explain:

1. Requirement interpretation.
2. Prompts used.
3. Generated output.
4. Manual changes.
5. Review performed.
6. Tests added.
7. Assumptions.
8. Trade-offs.
9. Bugs discovered.
10. Decisions accepted/rejected.

## 3. Repository Setup

Recommended structure:

```text
tictactoe/
├── frontend/
├── backend/
├── tests/
├── docs/
│   ├── requirements/
│   ├── architecture/
│   ├── adr/
│   ├── api/
│   ├── testing/
│   └── ai/
├── README.md
├── CHANGELOG.md
├── AI_CHANGELOG.md
├── AI_PROMPTS.md
├── ASSUMPTIONS.md
└── .gitignore
```

## 4. Backend Prerequisites

Recommended solution layout:

```text
backend/
├── TicTacToe.Api/
├── TicTacToe.Application/
├── TicTacToe.Domain/
├── TicTacToe.Infrastructure/
└── TicTacToe.Tests/
```

For this assignment, Infrastructure can remain very small because in-memory storage is explicitly acceptable.

Suggested dependencies:

- ASP.NET Core Web API.
- Built-in dependency injection.
- OpenAPI/Swagger.
- xUnit/NUnit/MSTest.
- FluentAssertions if desired.
- WebApplicationFactory for integration tests if desired.

Avoid unnecessary packages.

## 5. Frontend Prerequisites

Recommended structure:

```text
frontend/
├── src/
│   ├── app/
│   │   ├── core/
│   │   ├── game/
│   │   ├── scoreboard/
│   │   └── shared/
│   └── assets/
└── package.json
```

Recommended frontend responsibilities:

- Components render backend state.
- API service owns HTTP communication.
- State service/facade coordinates UI state.
- Types/interfaces represent API DTOs.
- No duplicated authoritative game rules.

## 6. Storage

The assignment permits:

- In-memory storage.
- SQLite.

Recommended choice: **in-memory storage**.

Reason:

- Assignment is local.
- No persistence requirement.
- Faster setup.
- Lower operational complexity.
- Easier panel review.
- Keeps focus on architecture and business rules.

Document that production deployment would replace this with a durable/shared store depending on availability and concurrency requirements.

## 7. Local Ports

Choose stable documented ports, for example:

```text
Frontend: http://localhost:4200
Backend:  https://localhost:7001
```

Do not assume these exact ports are mandatory. Document the actual values used.

## 8. CORS

During local development:

- Allow only the configured frontend origin.
- Do not use unrestricted `AllowAnyOrigin()` as the final quality solution.

Example:

```text
http://localhost:4200
```

## 9. Environment Configuration

Do not hard-code environment-specific values throughout the code.

Use configuration for:

- Backend URL.
- Logging level.
- Allowed origins.
- Optional feature switches.

Never commit secrets.

## 10. Pre-Implementation Checks

Before writing business code:

- Build an empty backend.
- Build an empty Angular app.
- Verify API can start.
- Verify Angular app can start.
- Verify frontend can call a simple health endpoint.
- Verify Git repository.
- Verify tests execute.
- Verify Swagger/OpenAPI works.

## 11. Definition of Ready

Implementation should not start until:

- Requirements are mapped.
- Architecture decision is documented.
- Domain model is identified.
- API contract is drafted.
- Error strategy is defined.
- Undo policy is selected.
- Computer strategy is defined.
- Test matrix exists.
- AI audit process is configured.

## 12. Definition of Done

A feature is done only when:

- Code exists.
- Unit tests exist.
- Relevant integration test exists where applicable.
- Documentation is updated.
- Requirement traceability is updated.
- AI/manual change is recorded.
- Review is completed.
- No known regression remains.
