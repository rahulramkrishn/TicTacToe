# Tic-Tac-Toe Enterprise Solution

This repository contains an enterprise-grade implementation of Tic-Tac-Toe, featuring a **.NET 8 Backend** (Domain-Driven Design, Onion Architecture) and an **Angular 21 Frontend** (Standalone Components, Signals, WCAG 2.1 AA).

## 🚀 How to Run the Solution

Follow these simple steps to get both the backend and frontend running locally.

### Prerequisites
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js (v20+) & npm](https://nodejs.org/)

### Step 1: Start the Backend (REST API)
Open a terminal in the root directory of the repository and run:
```bash
dotnet run --project backend/TicTacToe.Api --launch-profile http
```
The API will start and listen at **`http://localhost:5000`**. 
*(Optional: You can explore the interactive API documentation at `http://localhost:5000/swagger`)*

### Step 2: Start the Frontend (Angular UI)
Open a **second, separate terminal** in the root directory and run:
```bash
npm start --prefix frontend
```
The application will compile and start. Once ready, open your browser and navigate to **`http://localhost:4200`** to play the game!

---

## 🧪 How to Review the Solution

The project is built with strict architectural boundaries, 100% test coverage, and a focus on maintainability.

### 1. Run Automated Tests
You can verify the integrity of the system by running its 238 automated tests:

- **Run Backend Tests (xUnit):**
  ```bash
  dotnet test backend/TicTacToe.sln
  ```
- **Run Frontend Tests (Vitest/Angular):**
  ```bash
  npm test --prefix frontend -- --watch=false
  ```

### 2. Review Key Documentation
To understand the engineering decisions behind the code, we highly recommend reviewing the documentation:
- 📖 **[Architecture](docs/architecture.md)** — *Start here! The authoritative guide to the architecture, domain, and flow.*
- 📖 [Requirements](docs/requirements.md)
- 📖 [Domain Model & DDD](docs/domain-model.md)
- 📖 [API Contract](docs/api-contract.md)
- 📖 [Architecture Decisions](docs/decisions/)

---
*Note: The objective of this repository is not merely to build a simple game, but to demonstrate disciplined, test-driven, enterprise software engineering.*
