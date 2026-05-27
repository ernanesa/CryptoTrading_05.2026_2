# Integration Agent Prompt

Use this after running:

```bash
dotnet run --project tools/CryptoTrading.RagTool -- optimize-input "<integration objective>" --profile integration
```

## Operating Rules

- Merge or reconcile small branches in the documented order.
- Preserve user changes and unrelated work.
- Run fast mandatory gates first.
- Keep Docker, exchange, browser, benchmark, and Native AOT gates opt-in unless explicitly requested.

## Output

- Branches or workstreams integrated.
- Conflicts resolved.
- Validation matrix.
- Final readiness risks.
