# Copilot Instructions — CryptoTrading 05.2026 v2

- Trabalhar apenas no repositório ernanesa/CryptoTrading_05.2026_2.
- Consultar plans/ antes de implementar.
- Seguir .NET 10, C# 14, Dapper-first e .NET-first.
- Não introduzir Python no runtime principal do robô.
- Estratégias de trading não devem bypassar o RiskEngine sob nenhuma circunstância.
- Toda decisão relevante deve gerar registros audíveis no banco (DecisionAudit).
- Ao final de cada atividade, atualizar checklists e a documentação respectiva.

## Uso do RAG Local
Antes de criar propostas complexas, use o RAG:
1. Abra um terminal.
2. Rode `dotnet run --project tools/CryptoTrading.RagTool -- query "Sua pergunta de contexto"`
3. Ou obtenha um contexto pronto via `dotnet run --project tools/CryptoTrading.RagTool -- context-pack "Objetivo"` e copie para o seu prompt.
