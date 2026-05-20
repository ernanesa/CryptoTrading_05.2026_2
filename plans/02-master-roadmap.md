# 02 — Master Roadmap

Data-base: **2026-05-20 UTC-03 / America/Maceio**.

## Roadmap macro

| Marco | Fase | Entrega de valor |
|---|---|---|
| M0 | Foundation | Base compilável, limpa e documentada |
| M1 | Market Data + Feature Store | Dados Binance e features confiáveis |
| M2 | Backtesting + Strategy Lab | Estratégias comparáveis com métricas |
| M3 | Paper Trading + Risk | Simulação, PnL, risco e auditoria |
| M4 | Binance Spot Testnet | Fluxos validados em sandbox da exchange |
| M5 | Dashboard + Observability | Painel, logs, métricas e tracing |
| M6 | Intelligence Layer | ML/sentimento/RAG como contexto auxiliar |
| M7 | Adaptive Strategy Orchestration | Seleção dinâmica de estratégia, ativo, timeframe e alocação |
| M8 | Hardening | Testes, segurança, performance e preparo de fases avançadas |

## Sequência

```text
M0 → M1 → M2 → M3 → M4 → M5 → M6 → M7 → M8
```

## Métricas norteadoras

- retorno líquido;
- profit factor;
- max drawdown;
- expectancy;
- Sharpe/Sortino;
- estabilidade por regime;
- custo de execução;
- slippage;
- taxa de rejeição pelo RiskEngine;
- performance fixa versus adaptativa.

## Critério de avanço

Uma fase só avança quando:

- [ ] entrega de valor foi demonstrada;
- [ ] build passa;
- [ ] testes relevantes passam;
- [ ] checklist atualizado;
- [ ] documentação atualizada;
- [ ] riscos pendentes registrados.

## Referências read-only

- `CryptoTrading_v5.0`: padrões de engenharia, observabilidade, serviços adaptativos e estratégias.
- `CryptoTrading_05.2026`: planejamento e arquitetura inicial.
- `Bettina`: pesquisa histórica inicial.
