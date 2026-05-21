# ADR-002 — Dapper-First Persistence

*   **Status**: Aprovado e Implementado
*   **Data**: 2026-05-21
*   **Data-base**: **2026-05-21 UTC-03 / America/Maceio**

## Contexto

A persistência de séries temporais de candles, features geradas, logs operacionais de ordens e auditoria contínua de decisões gera um alto fluxo de gravações e leituras assíncronas de banco de dados. Mapeadores objeto-relacionais tradicionais (como o Entity Framework Core) trazem uma sobrecarga computacional de rastreamento de estado (change tracking), tradução de queries complexas e overhead de memória que podem se tornar gargalos em cenários de alta frequência (HFT) e backtests em lote.

## Decisão

Adotar o padrão **Dapper-first** para todas as persistências críticas de dados da aplicação. O Entity Framework Core está excluído das camadas críticas do robô.
*   **Banco de Dados**: PostgreSQL 16 Alpine rodando via Docker Compose.
*   **Acesso a Dados**: Dapper + Npgsql executando queries SQL puras e explícitas otimizadas para leitura e escrita em lote.
*   **Tabelas de Série Temporal**: A tabela `candles` e `candle_features` usam indexação adequada por ativo e carimbo de data/hora (timestamp) para garantir buscas sub-milissegundo para o Feature Store e simulações.

## Consequências

*   **Desempenho de Bare-Metal**: Consultas SQL de alto desempenho com o menor consumo de memória possível.
*   **Controle Absoluto**: O desenvolvedor tem visibilidade direta e total controle do plano de execução das queries escritas, eliminando surpresas com SQL ineficiente gerado automaticamente por ORMs.
*   **Escrita em Lote (Bulk Copy)**: Permite implementar inserções ultra-rápidas para backtesting que lidam com milhões de registros.
*   **Menor Abstração**: Exige que a equipe crie scripts SQL de criação de tabelas manuais ou scripts de migração controlados por código limpo, aumentando a responsabilidade com o esquema físico do banco.
