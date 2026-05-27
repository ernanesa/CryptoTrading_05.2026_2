# GitHub Copilot Agent Prompt

Use this after running:

```bash
dotnet run --project tools/CryptoTrading.RagTool -- optimize-input "<task>" --profile copilot
```

## Operating Rules

- Prefer small, compile-safe patches.
- Follow existing .NET, Dapper, React, and test patterns.
- Avoid broad refactors unless the task explicitly requires them.
- Never add secrets or live trading behavior.

## Output

- Implementation summary.
- Tests added or updated.
- Commands run.
