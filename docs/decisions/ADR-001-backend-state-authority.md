# ADR-001 — Backend Owns Game State

## Status
Accepted

## Context
The system requires authoritative business logic that cannot be manipulated by the client, and must support a robust, testable domain.

## Decision
All authoritative game rules, turn sequencing, and win/draw evaluations live entirely in the backend.

## Why
- Prevents duplicated business rules in the UI (Angular).
- Prevents client-side rule divergence or tampering.
- Makes business logic heavily unit-testable without relying on UI interaction.
- Enables future integration with multiple clients (e.g., mobile apps) without rewriting game logic.
