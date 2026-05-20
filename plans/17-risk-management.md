# 17 — Risk Management

## Objetivo

Centralizar risco como camada obrigatória do robô.

## Regras mínimas

- max drawdown;
- perda máxima diária simulada;
- exposição máxima por ativo;
- exposição total;
- máximo de ordens abertas;
- máximo de sinais por janela;
- spread máximo;
- liquidez mínima;
- volatility guard;
- correlation guard;
- cooldown;
- halted mode;
- circuit breaker.

## RiskDecision

Toda decisão deve conter:

- status: approved/rejected/reduced;
- reasons;
- risk multiplier;
- timestamp;
- symbol;
- strategy;
- regime;
- correlation id.

## Regra

Nenhum fluxo de backtest, paper trading, testnet ou fase avançada pode ignorar o RiskEngine.

## Integração com orquestrador

O AdaptiveStrategyOrchestrator pode sugerir estratégia, ativo, timeframe, peso e tamanho, mas o RiskEngine pode:

- aprovar;
- rejeitar;
- reduzir exposição;
- exigir cooldown;
- acionar halted mode;
- registrar motivo.

## Critérios de aceite

- [ ] Toda decisão relevante gera RiskDecision.
- [ ] Toda rejeição tem motivo legível.
- [ ] RiskEngine é chamado antes de qualquer executor.
- [ ] Limites são configuráveis.
- [ ] Dashboard exibe estado de risco.
