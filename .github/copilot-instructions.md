# GitHub Copilot Instructions — CryptoTrading 05.2026 v2

## Repositório oficial

Trabalhe apenas no repositório:

```text
ernanesa/CryptoTrading_05.2026_2
```

Os repositórios anteriores são somente leitura/referência.

## Regras obrigatórias

Antes de implementar:

1. Verifique a data atual.
2. Consulte `plans/`.
3. Consulte RAG/MCP quando houver contexto técnico relevante.
4. Gere um plano curto antes de alterar arquivos.
5. Informe arquivos que serão alterados.
6. Defina testes e critérios de aceite.

## Decisões do projeto

- Backend: .NET 10 + C# 14.
- Frontend: React + TypeScript + Vite.
- Persistência: Dapper-first + PostgreSQL.
- Python fora do runtime principal.
- ML.NET pode ser serviço separado.
- AOT é seletivo por serviço.
- RiskEngine é obrigatório nos fluxos de decisão.
- DecisionAudit deve registrar decisões relevantes.
- Estratégias não devem acessar executor diretamente.
- Orquestração adaptativa é parte central do projeto.

## Fluxo de entrega

Para cada tarefa:

- implementar a menor entrega de valor possível;
- manter código testável;
- atualizar documentação/checklists quando necessário;
- não adicionar dependências sem justificativa;
- não versionar secrets.

## Prompt interno recomendado

```text
Consulte plans/ e o RAG antes de responder.
Transforme o pedido em plano, riscos, arquivos, testes e critérios de aceite.
Depois implemente a menor entrega de valor possível.
Ao final, atualize checklists e documentação.
```
