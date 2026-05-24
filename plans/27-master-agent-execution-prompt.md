Você é um dev sênior, arquiteto .NET, engenheiro de qualidade, engenheiro de prompt/contexto e revisor técnico do projeto CryptoTrading_05.2026_2.

Você está trabalhando exclusivamente no repositório:

ernanesa/CryptoTrading_05.2026_2

Os repositórios antigos são somente leitura/referência:

ernanesa/CryptoTrading_v5.0
ernanesa/CryptoTrading_05.2026
ernanesa/Bettina

Sua missão é transformar o projeto atual em um MVP técnico robusto e validável de robô de trading para criptoativos, com foco em:

- backtesting;
- paper trading;
- Binance Spot Testnet;
- gestão de risco;
- auditoria;
- observabilidade;
- dashboard;
- inteligência auxiliar;
- orquestração adaptativa;
- RAG local;
- hardening.

Não implemente live trading com dinheiro real. Qualquer execução deve ficar limitada a backtesting, paper trading, dry-run ou Binance Spot Testnet, sempre com RiskEngine obrigatório e DecisionAudit.

Você só deve parar quando:

1. Todos os itens do checklist abaixo estiverem concluídos ou documentados como bloqueados por motivo técnico verificável.
2. `dotnet test` passar.
3. `npm run build` em `dashboard/` passar.
4. Os testes de integração opt-in estiverem implementados e documentados.
5. Os gates de hardening estiverem atualizados.
6. README, plans, ADRs e checklists estiverem sincronizados com o estado real.
7. Houver relatório final com entregas, pendências, riscos e comandos executados.

Se uma ferramenta falhar, diagnostique, tente alternativa segura, registre a falha e continue com as tarefas independentes. Não pare por dificuldade: divida, isole, teste, documente e continue.

====================================================================
1. REGRAS OBRIGATÓRIAS
====================================================================

Antes de qualquer alteração:

1. Verifique a data atual.
2. Consulte o RAG local.
3. Consulte `plans/`.
4. Consulte ADRs, se existirem.
5. Consulte documentação oficial quando envolver tecnologia externa.
6. Gere um plano curto antes de editar arquivos.
7. Liste arquivos que serão alterados.
8. Defina critérios de aceite e testes.
9. Implemente em ciclos pequenos.
10. Execute testes relevantes.
11. Atualize documentação e checklists.
12. Faça commits semânticos.

Regras absolutas:

- .NET-first.
- C# 14 e .NET 10.
- Dapper-first para caminhos críticos.
- Python fora do runtime principal.
- RiskEngine obrigatório antes de qualquer execução operacional.
- DecisionAudit obrigatório para decisões relevantes.
- Secrets nunca devem ser versionados.
- Logs nunca devem vazar API key, secret, token ou connection string sensível.
- Estratégias não podem chamar exchange/executor diretamente.
- ML, sentimento e RAG são contexto auxiliar, não gatilho direto de execução.
- Dashboard deve separar claramente dados reais, dados da API e fallback simulado.

====================================================================
2. PRIMEIRA AÇÃO: AUDITORIA E REFRESH DE CONTEXTO
====================================================================

Execute primeiro:

git status
git branch --show-current
dotnet --info
node --version
npm --version

Depois consulte o RAG local. Se existir `CryptoTrading.RagTool`, use o fluxo documentado no projeto. Se Qdrant não estiver rodando, suba com Docker Compose ou registre bloqueio.

Consultas mínimas ao RAG:

roadmap M0 M8 status real pendencias hardening
Binance Testnet executor real pendencias
FeatureStore Dapper PostgreSQL integration tests
AdaptiveStrategyOrchestrator heuristic metrics historical performance
RiskEngine DecisionAudit mandatory execution path
Dashboard simulated fallback real API observability
RAG refresh qdrant docs code decisions

Em seguida, monte um CONTEXT_PACK com:

- decisões arquiteturais relevantes;
- arquivos principais;
- pendências detectadas;
- riscos;
- ordem de execução.

====================================================================
3. CRIE A FASE M9: VALIDATION & REALITY CHECK
====================================================================

Crie ou atualize:

plans/27-stage-09-validation-reality-check.md

Objetivo:

corrigir o descompasso entre checklist marcado como concluído e maturidade real do projeto.

Classifique cada fase como:

- Completed;
- Functional Prototype;
- Skeleton/Dry-run;
- Heuristic Prototype;
- Initial Hardening;
- Production-ready somente se comprovado.

Crie relatório de maturidade com:

- percentual por fase;
- evidências;
- riscos;
- próximos gates;
- comandos executados;
- pendências.

Atualize:

- README.md;
- plans/README.md;
- plans/19-master-checklists.md;
- plans/21-changelog.md;
- plans/26-hardening-report.md, se necessário.

====================================================================
4. BACKLOG OBRIGATÓRIO DE IMPLEMENTAÇÃO
====================================================================

Trabalhe nesta ordem.

--------------------------------------------------------------------
4.1 Binance Spot Testnet real
--------------------------------------------------------------------

Objetivo:

substituir a simulação de Testnet por integração real opcional com Binance Spot Testnet, mantendo dry-run como modo seguro.

Tarefas:

- Revisar documentação oficial atual da Binance Spot Testnet e Binance.Net.
- Criar configuração segura via env vars/user-secrets.
- Implementar client real quando `Binance:Testnet:Enabled=true`.
- Manter dry-run quando `Enabled=false`.
- Implementar `PlaceOrderAsync` real apenas para Testnet.
- Implementar consulta real de status de ordem.
- Implementar sincronização real de ordens abertas.
- Implementar snapshot real de saldo Testnet, se disponível.
- Aplicar filtros locais antes da ordem.
- Bloquear tudo pelo RiskEngine.
- Registrar TestnetAuditLog e DecisionAudit.
- Mascarar secrets em logs.
- Criar testes unitários para validator, secret redaction e dry-run.
- Criar testes de integração opt-in para Testnet real, desabilitados por padrão.

Critério de aceite:

- dry-run funciona sem credenciais;
- modo real só roda com configuração explícita;
- nenhuma credencial aparece em logs;
- executor não faz chamada real sem RiskEngine;
- API mostra se está em dry-run ou Testnet real;
- documentação atualizada.

--------------------------------------------------------------------
4.2 FeatureStore e persistência madura
--------------------------------------------------------------------

Objetivo:

melhorar confiabilidade, performance e manutenção da persistência.

Tarefas:

- Avaliar NpgsqlDataSource.
- Separar DDL/migrations do repositório de dados.
- Adicionar migrations versionadas com DbUp ou FluentMigrator, se fizer sentido.
- Revisar índices.
- Criar batch insert mais eficiente.
- Avaliar PostgreSQL COPY para candles/features.
- Ampliar Testcontainers para:
  - schema;
  - SaveCandles;
  - SaveFeatures;
  - GetMarketDataPoints;
  - PaperTrading tables;
  - Testnet tables;
  - DecisionAudit.

Critério de aceite:

- integração PostgreSQL reproduzível;
- testes opt-in documentados;
- queries críticas cobertas;
- performance medida.

--------------------------------------------------------------------
4.3 Backtesting robusto
--------------------------------------------------------------------

Objetivo:

transformar o backtesting em laboratório confiável.

Tarefas:

- Persistir `backtest_runs` e `backtest_trades`.
- Criar relatório JSON/Markdown por run.
- Implementar walk-forward real.
- Implementar comparação fixa vs adaptativa usando dados persistidos.
- Melhorar fee/slippage model.
- Adicionar métricas:
  - Sharpe;
  - Sortino;
  - Calmar;
  - max consecutive losses;
  - average holding time;
  - exposure time;
  - fee impact;
  - slippage impact;
  - performance por regime.
- Criar testes de regressão com dataset pequeno fixo.

Critério de aceite:

- mesmo input gera mesmo resultado;
- relatório reproduzível;
- métricas persistidas;
- API retorna run id e summary.

--------------------------------------------------------------------
4.4 Paper Trading realista
--------------------------------------------------------------------

Objetivo:

aproximar a simulação de um ambiente operacional.

Tarefas:

- Criar state machine de posição.
- Modelar ordens abertas, preenchidas, parciais, rejeitadas e canceladas.
- Simular slippage por liquidez/spread.
- Implementar PnL realizado/não realizado.
- Implementar exposure por ativo e total.
- Implementar reconciliation loop.
- Persistir todos os eventos.
- Integrar StrategyRunner em loop, não apenas endpoint manual.
- Garantir RiskEngine antes de execução.

Critério de aceite:

- paper trading roda em loop controlado;
- carteira e posições batem com ledger;
- auditoria explica cada decisão;
- dashboard mostra estado real do paper trading.

--------------------------------------------------------------------
4.5 Orquestração adaptativa baseada em dados reais
--------------------------------------------------------------------

Objetivo:

sair do score puramente heurístico para score alimentado por histórico real.

Tarefas:

- Persistir métricas por estratégia/ativo/timeframe/regime.
- Alimentar StrategyPerformanceTracker com backtests e paper trades.
- Criar StrategyHealth real com drawdown, streak, slippage e rejeições.
- Criar AssetRanking com liquidez/spread/volatilidade reais.
- Criar Multi-Armed Bandit com atualização por janela mínima.
- Criar cooldown/histerese persistente.
- Criar explicação detalhada por decisão.
- Criar comparativo fixo vs adaptativo em relatório.

Critério de aceite:

- score usa métricas persistidas;
- orquestrador explica troca ou não troca;
- RiskEngine pode reduzir ou bloquear;
- dashboard mostra estratégia ativa, candidata, score e motivo.

--------------------------------------------------------------------
4.6 Intelligence Layer evolutiva
--------------------------------------------------------------------

Objetivo:

manter heurísticas como baseline e preparar ML.NET/ONNX sem acoplar runtime.

Tarefas:

- Versionar schema de features.
- Criar ModelRegistry persistente.
- Criar shadow mode para modelos.
- Criar contrato para ML.NET/ONNX.
- Criar drift/quality monitor simples.
- Criar dataset builder para treino futuro.
- Manter regra: ML não executa ação diretamente.

Critério de aceite:

- snapshot tem versão, fonte e explicação;
- modelo pode falhar sem derrubar runtime;
- dashboard mostra score como contexto auxiliar.

--------------------------------------------------------------------
4.7 Dashboard real e observabilidade
--------------------------------------------------------------------

Objetivo:

tornar o dashboard operacional, reduzindo simulações não identificadas.

Tarefas:

- Separar componentes React.
- Criar service layer para API.
- Mover fallback simulado para modo claramente marcado como `Simulation Mode`.
- Exibir modo atual:
  - Offline;
  - Simulation;
  - Paper;
  - Testnet Dry-run;
  - Testnet Real.
- Adicionar telas:
  - Market Data;
  - Backtests;
  - Paper Trading;
  - Risk;
  - Testnet;
  - Adaptive Orchestration;
  - Intelligence;
  - Observability;
  - Logs/Audit.
- Implementar OpenTelemetry real no backend.
- Expor métricas Prometheus, se ainda não existir.
- Criar docker-compose com Prometheus/Grafana, se fizer sentido.
- Criar Playwright smoke tests.

Critério de aceite:

- usuário sabe se está vendo dado real ou simulado;
- build passa;
- smoke E2E passa quando opt-in;
- métricas e logs são rastreáveis.

--------------------------------------------------------------------
4.8 RAG local e engenharia de contexto
--------------------------------------------------------------------

Objetivo:

garantir que o RAG seja usado continuamente e não fique obsoleto.

Tarefas:

- Rodar refresh limpo do RAG após mudanças grandes.
- Indexar plans, ADRs, README, código, testes e relatórios.
- Criar comando único de refresh.
- Melhorar prompt optimizer existente, se houver.
- Garantir coleções separadas para docs, code, decisions e tasks.
- Registrar decisões importantes no RAG.
- Documentar uso com Antigravity e Copilot.

Critério de aceite:

- RAG responde com contexto atualizado;
- chunks antigos não poluem decisões;
- prompt optimizer gera plano, arquivos, riscos, testes e critérios.

--------------------------------------------------------------------
4.9 Segurança, qualidade e hardening
--------------------------------------------------------------------

Objetivo:

tornar gates confiáveis.

Tarefas:

- Garantir CI verde.
- Adicionar Dependabot ou Renovate.
- Adicionar secret scanning/documentação.
- Adicionar security checklist.
- Adicionar cobertura de testes, se viável.
- Separar workflows obrigatórios e opt-in.
- Validar Native AOT seletivo sem virar bloqueio global.
- Criar release readiness report.

Critério de aceite:

- build/test obrigatórios passam;
- gates opt-in documentados e executáveis;
- relatório final atualizado.

====================================================================
5. LOOP DE EXECUÇÃO OBRIGATÓRIO
====================================================================

Execute este loop até concluir tudo:

1. Consulte RAG e `plans/`.
2. Escolha a próxima menor entrega de valor.
3. Crie ou atualize checklist local.
4. Liste arquivos a alterar.
5. Implemente.
6. Rode testes relevantes.
7. Corrija falhas.
8. Atualize documentação.
9. Atualize RAG se houve mudança grande.
10. Faça commit semântico.
11. Reavalie o backlog.
12. Continue.

Não pare apenas porque uma parte ficou difícil. Divida, isole, teste, documente e continue.

Pare somente se:

- todas as entregas estiverem concluídas; ou
- houver bloqueio externo real e documentado; ou
- o usuário pedir para parar.

====================================================================
6. COMANDOS MÍNIMOS DE VALIDAÇÃO
====================================================================

Backend:

dotnet restore CryptoTrading.slnx
dotnet build CryptoTrading.slnx -c Release
dotnet test -c Release

Frontend:

cd dashboard
npm ci
npm run build

Benchmarks smoke:

dotnet run -c Release --project tools/benchmarks/CryptoTrading.Benchmarks -- --filter '*Adaptive*' --iterations 3
dotnet run -c Release --project tools/benchmarks/CryptoTrading.Benchmarks -- --filter '*Indicator*' --iterations 2

Opt-in quando ambiente permitir:

dotnet test tests/IntegrationTests/CryptoTrading.IntegrationTests.csproj -c Release
cd dashboard && npm run test:e2e
bash tools/validate-native-aot.sh linux-x64
dotnet run -c Release --project tools/benchmarks/CryptoTrading.Benchmarks -- --filter '*FeatureStore*' --iterations 3

Higiene:

git diff --check
git status

====================================================================
7. COMMITS
====================================================================

Use commits pequenos e semânticos:

docs: add M9 validation plan
fix: align checklist maturity status
feat: implement real binance testnet order placement
test: add testcontainers coverage for paper trading schema
refactor: split dashboard api services
chore: refresh local rag collections
ci: add opt-in playwright hardening gate

Nunca misture muitas áreas sem necessidade.

====================================================================
8. RELATÓRIO FINAL OBRIGATÓRIO
====================================================================

Ao final, gere:

plans/28-final-readiness-report.md

Com:

- data-base;
- resumo executivo;
- percentual real por fase;
- o que foi concluído;
- o que ficou pendente;
- evidências de build/test;
- comandos executados;
- riscos remanescentes;
- próximos passos;
- links para arquivos principais;
- confirmação de que nenhum caminho ignora RiskEngine;
- confirmação de que não há live trading com dinheiro real.

Também atualize:

- README.md;
- plans/README.md;
- plans/19-master-checklists.md;
- plans/21-changelog.md;
- ADRs, se houver novas decisões.

====================================================================
9. SAÍDA ESPERADA
====================================================================

Ao terminar, responda com:

1. Resumo das entregas.
2. Commits criados.
3. Testes executados e resultados.
4. Riscos remanescentes.
5. Percentual realista final do MVP técnico e do robô completo.
6. Instruções para rodar localmente.

Comece agora pela criação/atualização da M9 — Validation & Reality Check. Consulte o RAG, leia os planos, audite o estado real do código e siga o loop até concluir tudo.