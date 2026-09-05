# Panel Review Preparation

## 1. Five-Minute Architecture Story

> I started by treating the backend as the authoritative source of truth because the problem statement explicitly requires it. I modeled Game as the aggregate root so that board, turn, move history and completion invariants cannot be changed independently. The API layer is intentionally thin, the application layer orchestrates use cases, and the domain contains game rules and computer strategy. I chose in-memory repositories because persistence is not required and this keeps the exercise easy to run. I explicitly avoided microservices because the domain is too small to justify distributed-system complexity.

## 2. DDD Questions

### Why is Game an aggregate?

Because game invariants must change together.

### What is the aggregate invariant?

A move can only occur when:

- Game is InProgress.
- Position is valid and empty.
- Player equals current player.

### Why not expose setters?

They allow invalid state transitions.

## 3. API Questions

### Why REST?

The assignment explicitly asks for REST.

### Why return complete state?

It reduces frontend synchronization problems and keeps the backend authoritative.

### Why 409?

Business-state conflicts such as occupied cell, wrong turn and completed game are conceptually conflicts rather than malformed JSON.

## 4. Computer Strategy Questions

### Why isolate strategy?

Because it is a policy that can be independently tested and replaced.

### Why deterministic?

Predictable behavior improves reproducibility and testability.

## 5. Undo Questions

### Why disable after completion?

The assignment permits it and it prevents scoreboard rollback complexity.

### What if Option B were required?

The completed result would need to be represented as a reversible contribution and scoreboard updates would need transactional consistency.

## 6. Scalability Questions

### Can this run on multiple instances?

Not safely with process-local memory.

### What changes?

Use shared durable storage and optimistic concurrency/versioning.

### Would you use microservices?

Only if there were a business/organizational/scaling reason.

## 7. Industrial/ABB Context

Tie the exercise to the role without pretending the assignment is an industrial system:

- Backend source of truth maps to control-system state authority.
- Deterministic rules map to predictable control logic.
- Clear boundaries help evolve toward integration adapters.
- Structured observability matters in mission-critical environments.
- Fault handling and validation prevent unsafe state transitions.
- AI-assisted development needs governance in safety/mission-critical environments.

## 8. AI-Assisted Development Questions

Be ready to answer:

- Which prompts did you use?
- Why did you use AI for this part?
- What did the AI get wrong?
- What did you change manually?
- How did you validate generated code?
- How did you prevent hallucinated requirements?
- How do you know AI-generated tests are correct?
- Which architectural decisions did you make yourself?

## 9. Strong AI Answer

> I did not ask AI to invent the product. I supplied a requirement specification and acceptance criteria, then used AI in small bounded tasks. I reviewed every significant output and required tests for business rules. I also maintained an audit trail linking prompts, requirements, code changes and Git commits.

## 10. Likely Deep-Dive Questions

1. Why is the backend the source of truth?
2. How do you guarantee scoreboard updates only once?
3. What happens if two moves arrive concurrently?
4. Why not WebSockets?
5. Why not microservices?
6. Why in-memory instead of SQLite?
7. How would you deploy this to Kubernetes?
8. How would you add authentication?
9. How would you add multiple games per user?
10. How would you persist game history?
11. How would you replace the computer strategy with an LLM?
12. Why would an LLM be a bad choice for deterministic game rules?
13. How would you monitor the system?
14. How would you handle a failed scoreboard update?
15. How would you make undo transactional?
16. How would you migrate from in-memory to PostgreSQL?
17. How would you version the API?
18. How would you handle backward compatibility?
19. What OWASP risks exist?
20. How would AI-assisted development fit into a regulated/mission-critical SDLC?
