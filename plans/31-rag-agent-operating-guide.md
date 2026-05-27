# RAG Agent Operating Guide

This guide describes how to operate the CryptoTrading local RAG Tool (`CryptoTrading.RagTool`) to assist AI coding assistants, human developers, and subagents.

## Core Purpose

The RAG Tool queries locally persisted embeddings in Qdrant to supply context on plans, decisions, tasks, and project codebases. It is crucial to use it before starting complex tasks or spawning parallel branches.

## Operating Commands

### 1. Ingest Documentation & Plans
Index all planning files (`plans/*.md`) and the project root `README.md` into Qdrant.
```bash
dotnet run --project tools/CryptoTrading.RagTool -- ingest
```

### 2. Clean Refresh
Recreate collections and execute a full clean ingestion of all documentation and source code files.
```bash
dotnet run --project tools/CryptoTrading.RagTool -- refresh
```

### 3. Semantic Query
Search across all collections for matching terms or questions.
```bash
dotnet run --project tools/CryptoTrading.RagTool -- query "How does RiskEngine validate signal size?"
```

### 4. Semantic Context Pack
Retrieve aggregated semantically relevant blocks across the four core collections.
```bash
dotnet run --project tools/CryptoTrading.RagTool -- context-pack "Implement paper trading state machine"
```

### 5. Input Optimization (Prompt Generator)
Generate highly structured prompts enriched with local code locations, architecture guidelines, potential risks, and behavioral profile instructions (e.g., Antigravity, GitHub Copilot).
```bash
dotnet run --project tools/CryptoTrading.RagTool -- optimize-input "Strict Binance Spot Testnet validation"
```

Profiles are supported:
```bash
dotnet run --project tools/CryptoTrading.RagTool -- optimize-input "Strict Binance Spot Testnet validation" --profile antigravity
dotnet run --project tools/CryptoTrading.RagTool -- optimize-input "Paper order state machine" --profile copilot
dotnet run --project tools/CryptoTrading.RagTool -- optimize-input "Review testnet lifecycle diff" --profile code-review
dotnet run --project tools/CryptoTrading.RagTool -- optimize-input "Final MVP integration" --profile integration
```

Saved prompt wrappers live in `plans/prompts/`:

- `antigravity-agent.md`
- `copilot-agent.md`
- `code-review-agent.md`
- `integration-agent.md`

## Collection Structure

The tool splits files into four discrete Qdrant collections:
1. `cryptotrading_docs`: For general guides, stage definitions, and stage checklists.
2. `cryptotrading_decisions`: For architectural decisions and compliance limits.
3. `cryptotrading_tasks`: For TODO checklists and historical work packages.
4. `cryptotrading_code`: For indexable source files (`.cs`, `.ts`, `.tsx`, `.css`, `.json`).

## Strict Rules
- Always redact sensitive API keys or connection strings using the `SecretRedactor` before indexing code comments.
- Do not index temporary `obj/`, `bin/`, or `.gemini/` directories.
- Before complex implementation, run both `context-pack` and `optimize-input`.
- After large changes in `plans/` or `src/`, run `refresh` so agent context does not drift.
