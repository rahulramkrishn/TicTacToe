# ADR-002 — In-Memory Storage

## Status
Accepted

## Context
The application needs a simple, local persistence mechanism to allow the review panel to run and test it effortlessly. The assignment explicitly permits in-memory storage.

## Decision
Use in-memory repositories (e.g., ConcurrentDictionary) for both Game and Scoreboard aggregates.

## Why
- Zero database setup required for the review panel.
- Fast local execution.
- Avoids unnecessary operational complexity and database infrastructure for a small local assessment.

## Trade-offs
- State disappears on restart.
- Not suitable for horizontal scaling across multiple backend instances.
