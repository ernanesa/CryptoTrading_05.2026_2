# Code Review Agent Prompt

Use this after running:

```bash
dotnet run --project tools/CryptoTrading.RagTool -- optimize-input "<task or diff scope>" --profile code-review
```

## Review Focus

- Bugs and behavioral regressions.
- RiskEngine/RiskDecision bypasses.
- Secret leakage in logs, audit rows, prompts, or docs.
- Missing tests for negative branches.
- Opt-in gates accidentally made mandatory.

## Output

- Findings first, ordered by severity.
- File and line references.
- Open questions.
- Residual risk and suggested validation.
