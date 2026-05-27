# Antigravity Agent Prompt

Use this after running:

```bash
dotnet run --project tools/CryptoTrading.RagTool -- optimize-input "<task>" --profile antigravity
```

## Operating Rules

- Work only in `ernanesa/CryptoTrading_05.2026_2`.
- Use the RAG context pack before editing.
- Keep branches and file ownership narrow.
- Do not implement live trading with real money.
- Route operational trading flows through `RiskEngine`, `RiskDecision`, and audit records.
- Run `dotnet test -c Release` and `git diff --check` before finishing.

## Output

- Short plan.
- Files changed.
- Validations.
- Remaining risks.
