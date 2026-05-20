# 08 — Stage 04: Binance Spot Testnet

## Objetivo

Validar integração com Binance Spot Testnet em sandbox.

## Entrega de valor

Fluxo realista de validação de ordens testnet com filtros da exchange, auditoria e sincronização.

## Componentes

- ExchangeInfoProvider;
- ExchangeRuleValidator;
- BinanceTestnetExecutor;
- OrderStatusSynchronizer;
- BalanceSnapshotService;
- TestnetAuditLog.

## Validações

- símbolo existe;
- tick size;
- step size;
- min/max quantity;
- min notional;
- precisão de preço;
- precisão de quantidade;
- client order id único;
- secrets fora de logs.

## Critérios de aceite

- [ ] payload inválido bloqueado localmente;
- [ ] Testnet pode ser desligada por configuração;
- [ ] secrets não aparecem em logs;
- [ ] falhas de API tratadas;
- [ ] status sincronizado;
- [ ] auditoria criada.
