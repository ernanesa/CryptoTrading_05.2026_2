# Exemplos para o Antigravity no CryptoTrading 05.2026_2

Como Agente Antigravity, você pode interagir com o RAG Local para maximizar sua performance.

## 1. Atualizar e Ingerir os Documentos
Quando houver novas ADRs ou mudanças massivas de código:
`dotnet run --project tools/CryptoTrading.RagTool -- refresh`

## 2. Buscar Contexto Direto
Se o plano pede para você implementar algo, tire dúvidas com:
`dotnet run --project tools/CryptoTrading.RagTool -- query "Como o RiskEngine funciona?"`

## 3. Preparar um Prompt de Implementação
Antes de iniciar uma tarefa pesada, gere o prompt consolidado que vai guiar a sua sessão:
`dotnet run --project tools/CryptoTrading.RagTool -- optimize-input "Implementar validação M9"`

Este comando vai lhe retornar o objetivo, o contexto injetado, riscos e critérios de aceite que você deverá usar para balizar seus passos.
