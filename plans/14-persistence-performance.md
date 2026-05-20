# 14 — Persistence and Performance

## Decisão

Usar **Dapper-first** para dados críticos.

## Por quê

Caminhos de alto volume precisam de controle explícito de SQL, menos overhead e previsibilidade.

## Usar Dapper em

- candles;
- features;
- backtest reads;
- trade ledger;
- decision audit;
- strategy metrics;
- relatórios.

## EF Core

Opcional apenas para:

- migrations se compensar;
- telas administrativas simples;
- CRUD não crítico.

Alternativa: Dapper + FluentMigrator.

## Otimizações previstas

- NpgsqlDataSource;
- batch insert;
- PostgreSQL COPY;
- índices corretos;
- queries projetadas;
- streaming reads;
- particionamento/time-series futuramente.

## Tabelas iniciais

- candles;
- features;
- backtest_runs;
- backtest_trades;
- paper_orders;
- paper_positions;
- risk_decisions;
- decision_audit;
- strategy_metrics;
- system_events.

## Benchmarks obrigatórios

- EF Core vs Dapper insert;
- Dapper batch vs COPY;
- leitura de candles;
- leitura de features;
- loop de backtest;
- cálculo de indicadores.
