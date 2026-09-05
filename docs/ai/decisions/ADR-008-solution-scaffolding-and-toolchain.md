# ADR-008 — Solution Scaffolding, Toolchain & Project Structure

## Status
Accepted

## Context
Prompt P003 requires establishing the clean solution scaffolding, dependency structure, and technical baseline for the TicTacToe application without implementing domain business functionality.

The development environment provides:
- .NET SDK (supports `net8.0` LTS)
- Node.js v22.20.0 (LTS) & npm 10.9.3
- Angular CLI 21.2.23 (latest modern release)

## Decision
1. **.NET Solution Structure**:
   - `TicTacToe.Domain` (`net8.0`, classlib): Pure domain logic, no external or framework dependencies.
   - `TicTacToe.Application` (`net8.0`, classlib): Orchestrates use cases and DTOs; references `TicTacToe.Domain`.
   - `TicTacToe.Infrastructure` (`net8.0`, classlib): In-memory repositories; references `TicTacToe.Application` and `TicTacToe.Domain`.
   - `TicTacToe.Api` (`net8.0`, ASP.NET Core Web API): Controllers, ProblemDetails, CORS; references `TicTacToe.Application` and `TicTacToe.Infrastructure`.
   - `TicTacToe.Tests` (`net8.0`, xUnit): Unit & integration tests (`Microsoft.AspNetCore.Mvc.Testing`); references all projects.

2. **Angular Architecture**:
   - Client-only SPA (`ssr: false`, standalone components, Vitest test runner).
   - Strict dev port `http://localhost:4200`.
   - Dedicated environment configuration (`src/environments/environment.ts`) pointing to `http://localhost:5000/api`.

3. **CORS Policy**:
   - Restricted explicitly to `http://localhost:4200` via ASP.NET Core named policy (`FrontendPolicy`).

4. **Technical Health Check**:
   - Standard `/health` endpoint established on the API backend returning HTTP 200 OK.

## Consequences
### Positive
- Strict compliance with clean architecture and unidirectional dependency flow.
- Zero business logic leakage into scaffolding.
- Verified test discovery and execution across both .NET (xUnit) and Angular (Vitest).

### Negative
- None. Meets all NFR and architectural requirements.
