# Final Prompt Pack

This document archives the final prompts used for the parallel execution agents to finish the MVP.
They enforce the use of local RAG tools.

## Prompt A: Ponte REST Testnet com RiskDecision
**Role:** Dev sênior .NET especializado em segurança de execução Testnet.
**Objective:** Conectar a rota REST de submissão Testnet ao RiskEngine/RiskDecision, sem criar bypass para o BinanceTestnetExecutor.
**RAG Commands:**
- `dotnet run --project tools/CryptoTrading.RagTool -- context-pack "Testnet REST bridge RiskDecision RiskEngine BinanceTestnetExecutor"`
- `dotnet run --project tools/CryptoTrading.RagTool -- optimize-input "Criar ponte REST Testnet com RiskDecision aprovado" --profile antigravity`

## Prompt B: Dashboard RuntimeMode
**Role:** Dev sênior frontend/backend focado em clareza operacional.
**Objective:** Fazer o dashboard consumir `/api/runtime/status` como fonte canônica do modo operacional.
**RAG Commands:**
- `dotnet run --project tools/CryptoTrading.RagTool -- context-pack "RuntimeMode dashboard /api/runtime/status Simulation Paper TestnetDryRun TestnetReal"`
- `dotnet run --project tools/CryptoTrading.RagTool -- optimize-input "Integrar RuntimeMode canonico no dashboard" --profile copilot`

## Prompt C: Paper Trading state machine
**Role:** Dev sênior de motor de simulação e execução.
**Objective:** Completar Paper Trading com state machine, eventos, PnL não realizado e reconciliação.
**RAG Commands:**
- `dotnet run --project tools/CryptoTrading.RagTool -- context-pack "PaperOrder OrderStatus PaperTradeExecutor state machine PnL reconciliation DecisionAudit"`
- `dotnet run --project tools/CryptoTrading.RagTool -- optimize-input "Completar Paper Trading state machine" --profile antigravity`

## Prompt D: Backtesting report validation
**Role:** Quant dev .NET especializado em backtesting.
**Objective:** Validar e fechar métricas avançadas, persistência e relatórios de backtesting.
**RAG Commands:**
- `dotnet run --project tools/CryptoTrading.RagTool -- context-pack "BacktestReport Sortino Calmar Exposure HoldingTime RegimeBreakdown Markdown report"`
- `dotnet run --project tools/CryptoTrading.RagTool -- optimize-input "Validar métricas avançadas e relatórios de backtesting" --profile antigravity`

## Prompt E: Adaptive feedback persistence
**Role:** Quant engineer e arquiteto de orquestração adaptativa.
**Objective:** Fazer o AdaptiveStrategyOrchestrator usar feedback persistido real de backtests, paper trading e auditorias.
**RAG Commands:**
- `dotnet run --project tools/CryptoTrading.RagTool -- context-pack "AdaptiveStrategyOrchestrator StrategyPerformanceTracker MultiArmedBandit strategy_metrics paper backtest feedback"`
- `dotnet run --project tools/CryptoTrading.RagTool -- optimize-input "Persistir feedback loop da orquestração adaptativa" --profile antigravity`

## Prompt F: Intelligence shadow mode
**Role:** Dev sênior .NET/ML e arquiteto de inteligência auxiliar.
**Objective:** Evoluir Intelligence Layer para ModelRegistry persistente e shadow mode, mantendo ML como contexto auxiliar.
**RAG Commands:**
- `dotnet run --project tools/CryptoTrading.RagTool -- context-pack "IntelligenceSnapshot ModelRegistry ShadowModelRunner FeatureSchema DriftMonitor ML.NET ONNX"`
- `dotnet run --project tools/CryptoTrading.RagTool -- optimize-input "Criar shadow mode para Intelligence Layer" --profile copilot`

## Prompt G: Final Readiness + Opt-ins
**Role:** Engenheiro DevOps, segurança e qualidade.
**Objective:** Validar gates finais e documentar resultados opt-in possíveis.
**RAG Commands:**
- `dotnet run --project tools/CryptoTrading.RagTool -- context-pack "final readiness opt-in Playwright Testcontainers FeatureStore Native AOT secrets"`
- `dotnet run --project tools/CryptoTrading.RagTool -- optimize-input "Validar readiness final e gates opt-in" --profile integration`

## Prompt H: Final MVP Report
**Role:** Release engineer e arquiteto revisor.
**Objective:** Criar o relatório final do MVP e registrar percentuais finais.
**RAG Commands:**
- `dotnet run --project tools/CryptoTrading.RagTool -- context-pack "final MVP report Testnet RiskDecision RuntimeMode Paper Adaptive Backtesting Intelligence Readiness"`
- `dotnet run --project tools/CryptoTrading.RagTool -- optimize-input "Gerar relatório final do MVP" --profile integration`
