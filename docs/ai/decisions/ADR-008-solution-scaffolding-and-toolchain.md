# ADR-008 — Solution Scaffolding, Toolchain & Project Structure

## Status
Accepted

## Context
Prompt P003 establishes the solution scaffolding, project boundaries, and technical baseline for the TicTacToe application without implementing domain business functionality.

The development environment provides:
- .NET SDK (supports `net8.0` LTS runtime)
- Node.js v22.20.0 (LTS) & npm 10.9.3
- Angular CLI 21.2.23 (latest modern release)

## Decision
1. **.NET Solution Structure & Dependency Inversion**:
   - `TicTacToe.Domain` (`net8.0`, classlib): Pure domain logic, entities, value objects, domain services, domain events, repository interfaces. Zero external or framework dependencies.
   - `TicTacToe.Application` (`net8.0`, classlib): Orchestrates use cases, command/query handlers, DTO mapping, and in-process domain event handlers. References `TicTacToe.Domain` only.
   - `TicTacToe.Infrastructure` (`net8.0`, classlib): Implements domain repository interfaces (`IGameRepository`, `IScoreboardRepository`) using thread-safe in-memory stores. References `TicTacToe.Domain` only (decoupled from `TicTacToe.Application`).
   - `TicTacToe.Api` (`net8.0`, ASP.NET Core Web API): HTTP controllers, ProblemDetails middleware, CORS policy, health endpoints, DI composition root. References `TicTacToe.Application` and `TicTacToe.Infrastructure`.
   - `TicTacToe.Tests` (`net8.0`, xUnit): Unit tests (Domain, Application) and integration tests (`WebApplicationFactory`). References all solution projects.

2. **Angular Architecture**:
   - Client-only SPA (`ssr: false`, standalone components, Vitest test runner).
   - Strict dev port `http://localhost:4200`.
   - Dedicated environment configuration (`src/environments/environment.ts`) pointing to `http://localhost:5000/api`.

3. **CORS Policy**:
   - Restricted explicitly to `http://localhost:4200` via ASP.NET Core named policy (`FrontendPolicy`).

4. **Technical Health Check**:
   - Standard `/health` endpoint established on the API backend returning HTTP 200 OK.

## Alternatives Considered & Rejected
- **Coupling Infrastructure to Application (`Infrastructure → Application`)**: Rejected during P003.1 audit. In-memory repositories implement domain repository interfaces; coupling Infrastructure to Application is unnecessary and violates clean layer isolation.
- **Server-Side Rendering (SSR) for Angular**: Rejected. The assessment specifies a client-side SPA communicating with a .NET REST API. SSR introduces unnecessary Node server complexity.
- **Multiple Backend Test Projects**: Rejected. As specified in `docs/02-prerequisites-and-environment.md`, a single unified `TicTacToe.Tests` project houses both unit tests and `WebApplicationFactory` API integration tests cleanly.

## Consequences
### Positive
- Strict compliance with clean architecture and inward dependency flow toward the Domain.
- Zero business logic leakage into scaffolding.
- Verified test discovery and execution across both .NET (xUnit) and Angular (Vitest).
- 100% reproducible clean builds via `dotnet restore` and `npm ci`.

### Negative
- None. Meets all NFR and architectural requirements.
