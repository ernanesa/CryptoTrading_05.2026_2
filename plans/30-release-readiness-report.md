# Release Readiness Report

## Status
- [x] Readiness report consolidated.
- [x] Security and Secrets Checklist established.
- [x] Redaction validation integrated in CI.
- [x] Hardening gates separated (mandatory vs opt-in).
- [x] Dependabot activated for dependencies update.

## Security Checklist
1. **Secrets Avoidance:** Never commit passwords, connection strings, or JWT tokens to the repository.
2. **Environment Variables:** All application secrets must be loaded via environment variables at runtime.
3. **Secret Redaction:** `SecretRedactor` service is responsible for masking any sensitive information before logging.
4. **Secret Scanning:** Rely on GitHub Advanced Security or external tools to block leaked secrets on PRs.
5. **No Hardcoded Keys:** Validate that unit tests enforce secret redaction and dry-run execution safely.

## Secrets Documentation
- `DATABASE_CONNECTION_STRING`: Connection string for PostgreSQL database. Should be supplied dynamically.
- `BINANCE_API_KEY` / `BINANCE_API_SECRET`: Required for execution and market data requests. Do not store in plain text.
- `JWT_SECRET`: For internal API auth (if applicable).

## Gates Structure
- **Mandatory Gates (`ci.yml`)**: Includes dotnet build, xUnit unit tests, dashboard npm build, and git diff checks. Runs on all pushes to `main` and `develop`.
- **Opt-In Gates (`hardening-gates.yml`)**: Runs via workflow dispatch. Includes Playwright, Testcontainers for integration tests, FeatureStore Benchmark, and Native AOT validation.
- **Dependabot (`dependabot.yml`)**: Weekly updates are enabled for NuGet, dashboard npm packages, and GitHub Actions.
- **Live Trading Boundary**: The MVP readiness scope does not authorize real live trading. Testnet execution remains guarded by `RiskDecision`, and real exchange credentials must stay environment-provided and redacted.
- **Redaction Gate**: Secret masking is covered by the mandatory .NET unit test suite through `SecretRedactor`; external secret scanning remains a repository/security setting outside local CI.

## Actionable Next Steps
- Periodically review Dependabot PRs.
- Enable integration tests in mandatory CI when Docker runners are standardized.
