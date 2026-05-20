# 10 — Stage 06: Intelligence Layer

## Objetivo

Adicionar inteligência auxiliar: ML, sentimento, eventos e RAG como contexto para decisão.

## Decisão

ML não executa ação diretamente. ML gera score/contexto. RiskEngine e Orchestrator decidem dentro das regras.

## Componentes

- [x] IntelligenceSnapshot;
- [x] FeatureExtractor;
- [x] AnomalyDetectionService;
- [x] RegimeDetectionService;
- [x] VolatilityForecastService;
- [x] MetaLabelingService;
- [x] SentimentRiskService;
- [x] EventRiskClassifier;
- [x] ModelRegistry;
- [x] RagContextProvider;
- [x] ExplanationService.

## ML.NET como serviço

Se ML.NET trouxer dependências pesadas ou conflito com AOT, criar `ML.Service` separado.

## Sentimento

Usar como filtro de risco/contexto, não como gatilho direto.

Fontes iniciais:

- Binance Announcements;
- RSS de notícias;
- calendário de eventos;
- índice de medo/euforia apenas se fonte/licença forem adequadas.

## Critérios de aceite

- [x] IntelligenceSnapshot versionado;
- [x] modelo/score tem versão;
- [x] fonte do score registrada;
- [x] insights aparecem no dashboard;
- [x] nenhum modelo bypassa RiskEngine.
