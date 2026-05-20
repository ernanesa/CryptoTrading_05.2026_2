# 25 — Plano de Desenvolvimento Detalhado e Paralelizável (CryptoTrading 05.2026 v2)

Data-base: **2026-05-20 UTC-03 / America/Maceio**
Responsável: **Antigravity AI (Gemini 3.5 Flash - High)**
Repositório Oficial: `ernanesa/CryptoTrading_05.2026_2`

---

## 1. Introdução e Análise Estratégica do Planejamento

Analisamos detalhadamente todos os **26 arquivos** contidos na pasta `plans/`. O repositório atual encontra-se completamente limpo de código-fonte, configurando um cenário ideal para estabelecer uma fundação robusta (.NET 10 + C# 14 no Backend, React + TS + Vite no Frontend) combinada com um **RAG local (Qdrant + BGE-M3)** que servirá como a "memória técnica" do projeto, auxiliando os agentes de IA (Antigravity e GitHub Copilot) na manutenção do alinhamento arquitetural.

Abaixo, apresentamos a síntese crítica dos documentos do diretório `plans/`:

1.  **Regras Operacionais e Qualidade (ADR Padrão, DoR/DoD)**:
    *   Foco em **.NET-first** (core em C#) e **Dapper-first** (SQL explícito de alto desempenho para séries temporais).
    *   Proibição estrita de Python no runtime de execução do robô (permitido apenas na pasta `tools/rag/` para ferramentas auxiliares).
    *   Toda decisão relevante passa obrigatoriamente pelo `RiskEngine` (motor de risco centralizado) e gera um registro histórico auditável (`DecisionAudit`).
2.  **Arquitetura e Componentização**:
    *   Organização em camadas limpas: Domain, Contracts, Application, Infrastructure, Api, Worker, e Dashboard.
    *   Divisão em planos lógicos claros: Control Plane (orquestrador), Execution Plane (ingestão e execução), Learning Plane (tracker de performance e Bandit) e Observability Plane (audit e dashboard).
3.  **RAG Local e MCP**:
    *   Decisão de rodar embeddings locais em CPU com o modelo **BGE-M3** (BAAI/bge-m3) por suportar múltiplos idiomas, recuperação densa/esparsa/multi-vetorial e contexto longo de 8192 tokens.
    *   Qdrant como banco vetorial com coleções dedicadas (`cryptotrading_docs`, `cryptotrading_code`, `cryptotrading_decisions`, `cryptotrading_prompts`, `cryptotrading_tasks`).
    *   Conexão dos agentes (Antigravity/Copilot) via Model Context Protocol (MCP) restrito ao escopo do workspace.

---

## 2. Estratégia de Paralelização de Atividades (Trilhas Paralelas)

Para maximizar a eficiência energética e temporal do Xeon E5-2650 v4 (24 threads) e dos 32 GB RAM do usuário, dividiremos a largada do desenvolvimento em **duas trilhas paralelas principais**, seguidas por etapas de integração e evolução.

```text
TRILHA 1: INFRAESTRUTURA DE IA & RAG (Python)
- Baixar e rodar Qdrant Localmente (Static Binary)
- Criar a pasta tools/rag/ com ambiente virtual Python
- Criar os scripts do RAG (qdrant_schema, chunking, ingest, query, prompt_optimizer)
- Executar a indexação inicial de toda a pasta plans/
- Configurar o MCP em .vscode/mcp.json e .github/copilot-instructions.md

TRILHA 2: FUNDAÇÃO .NET 10 (M0)
- Criar a Solution .NET 10
- Criar a estrutura completa de projetos (Domain, Contracts, Application, Infrastructure, Worker, Api)
- Configurar as dependências e arquivos globais (Directory.Build.props, .editorconfig)
- Configurar log básico com Serilog e criar um teste unitário de verificação inicial
- Configurar o .gitignore e preparar o workflow de build (dotnet build)
```

---

## 3. Passo a Passo Detalhado para Execução

### Fase 1: Trilha 1 — Instalação, Configuração e Carga do RAG Local
Como não temos `docker` nem `podman` prontamente instalados e disponíveis na máquina (conforme verificações de comandos), utilizaremos a abordagem **Static Binary Execution** para o Qdrant Server ou o modo **Serverless (Local Path)** na própria biblioteca do Python, garantindo 100% de viabilidade imediata. 

#### **Passo 1.1: Download e Inicialização do Qdrant Server Local**
Baixaremos o executável nativo do Qdrant para Linux x86_64, criando um diretório dedicado para ferramentas de suporte no workspace.

1. Criar pasta de ferramentas: `mkdir -p tools/rag/bin`
2. Baixar o binário estático e extrair:
   ```bash
   wget https://github.com/qdrant/qdrant/releases/download/v1.12.0/qdrant-x86_64-unknown-linux-gnu.tar.gz
   tar -xzf qdrant-x86_64-unknown-linux-gnu.tar.gz -C tools/rag/bin/
   chmod +x tools/rag/bin/qdrant
   ```
3. Executar o Qdrant em segundo plano escutando localmente:
   ```bash
   mkdir -p tools/rag/qdrant_storage
   ./tools/rag/bin/qdrant --uri "http://localhost:6333" --storage-path "tools/rag/qdrant_storage" > tools/rag/qdrant.log 2>&1 &
   ```

#### **Passo 1.2: Configuração do Ambiente Virtual do tools/rag**
1. Criar o arquivo `tools/rag/requirements.txt`:
   ```text
   qdrant-client
   sentence-transformers
   torch --index-url https://download.pytorch.org/whl/cpu
   transformers
   fastapi
   uvicorn
   pydantic
   python-dotenv
   rich
   tiktoken
   markdown-it-py
   ```
2. Inicializar o ambiente virtual:
   ```bash
   python3 -m venv tools/rag/.venv
   source tools/rag/.venv/bin/activate
   pip install --upgrade pip
   pip install -r tools/rag/requirements.txt
   ```

#### **Passo 1.3: Criação dos Scripts do Pipeline RAG**
Criar os arquivos em `tools/rag/`:
- `qdrant_schema.py`: Define o schema e cria as coleções (`cryptotrading_docs`, etc.) usando `BAAI/bge-m3` com FastEmbed ou SentenceTransformers local (dimensão correta: 1024).
- `chunking.py`: Parser inteligente de Markdown (chunk por heading + metadados de arquivo/seção).
- `ingest.py`: Lê a pasta `plans/`, executa o chunking, gera embeddings locais via CPU e persiste no Qdrant.
- `query.py` / `prompt_optimizer.py`: Interface de busca e estruturação de prompts formatados para IA.

#### **Passo 1.4: Configurações de Integração de Agentes (VS Code & Copilot)**
1. Criar `.github/copilot-instructions.md` com as regras estritas da stack do projeto (C# 14, .NET 10, Dapper-first, sem Python no runtime, etc.).
2. Criar `.vscode/mcp.json` para conectar os agentes ao RAG e ao filesystem local.

---

### Fase 2: Trilha 2 — Stage 00: Foundation (.NET 10)
Enquanto os pacotes Python de IA estão sendo baixados e instalados, inicializaremos de forma assíncrona a estrutura da Solution C#.

#### **Passo 2.1: Inicialização da Solution**
1. No diretório raiz do workspace:
   ```bash
   dotnet new sln -n CryptoTrading
   ```

#### **Passo 2.2: Criação dos Projetos (Arquitetura Limpa)**
Criar pastas e projetos focados na arquitetura pretendida:
```bash
# Domain (Core de Regras e Modelos - Classe de Biblioteca)
dotnet new classlib -n CryptoTrading.Domain -o src/Domain --framework net10.0
# Contracts (Interfaces e DTOs)
dotnet new classlib -n CryptoTrading.Contracts -o src/Contracts --framework net10.0
# Application (Casos de Uso)
dotnet new classlib -n CryptoTrading.Application -o src/Application --framework net10.0
# Infrastructure (Acesso a Banco com Dapper, Conectores)
dotnet new classlib -n CryptoTrading.Infrastructure -o src/Infrastructure --framework net10.0
# Worker (Ingestão de Dados e Execução de Estratégias)
dotnet new worker -n CryptoTrading.Worker -o src/Worker --framework net10.0
# API / BFF (ASP.NET Core / SignalR para Dashboard)
dotnet new webapi -n CryptoTrading.Api -o src/Api --framework net10.0

# Adicionar projetos à solution
dotnet sln add src/Domain src/Contracts src/Application src/Infrastructure src/Worker src/Api
```

#### **Passo 2.3: Configuração de Dependências Internas**
```bash
dotnet add src/Contracts/CryptoTrading.Contracts.csproj reference src/Domain/CryptoTrading.Domain.csproj
dotnet add src/Application/CryptoTrading.Application.csproj reference src/Contracts/CryptoTrading.Contracts.csproj
dotnet add src/Infrastructure/CryptoTrading.Infrastructure.csproj reference src/Application/CryptoTrading.Application.csproj
dotnet add src/Worker/CryptoTrading.Worker.csproj reference src/Infrastructure/CryptoTrading.Infrastructure.csproj
dotnet add src/Api/CryptoTrading.Api.csproj reference src/Infrastructure/CryptoTrading.Infrastructure.csproj
```

#### **Passo 2.4: Configurações Globais e Padronização**
1. **`Directory.Build.props`**: No raiz da solution, para garantir avisos tratados como erro e C# 14 implícito.
2. **`.editorconfig`**: Estilo de código e formatação automatizada.
3. **Logs & Testes**: Criação de projeto xUnit para testes unitários (`tests/CryptoTrading.UnitTests`) e configuração básica de `Serilog` no Worker e na API.

---

## 4. Cronograma de Entregas e Fases Posteriores (Pós-Foundation)

Após concluirmos a indexação do RAG e o esqueleto estrutural da Solution .NET (M0), prosseguiremos de acordo com o roadmap macro:

| Fase | Prazo Estimado | Componentes Principais | Critério de Aceite |
| :--- | :--- | :--- | :--- |
| **M1: Market Data & Feature Store** | 4-5 dias | `BinanceMarketDataAdapter`, `FeatureStore`, PostgreSQL schemas + Dapper migrations | Candles salvos no Postgres; Features calculadas usando a lib `Skender.Stock.Indicators` |
| **M2: Backtesting & Strategy Lab** | 5-6 dias | `BacktestEngine`, `SlippageModel`, `FeeModel`, Relatórios de performance | Mínimo de 3 estratégias rodando reprodutivelmente em dados passados com métricas estruturadas |
| **M3: Paper Trading & Risco** | 5-6 dias | `RiskEngine` completo, `VirtualWallet`, `DecisionAudit` persistidos | Todo trade simulado passa obrigatoriamente pelas validações de limites de risco do motor central |
| **M4: Binance Spot Testnet** | 3-4 dias | `TestnetExecutor`, WebSocket listeners em tempo real | Ordens criadas e gerenciadas via sandbox oficial da Binance |
| **M5: Dashboard & Observabilidade** | 5 dias | React (Vite 8) + SignalR BFF + OpenTelemetry tracing | Painel mostrando posições ativas, logs unificados e scores operacionais |
| **M6: Inteligência e ML** | 4 dias | `ML.Service` (ML.NET/ONNX) de Anomalias/Regimes, integrações adicionais do RAG | Previsões/Snapshots alimentados no Orquestrador de forma assíncrona e resiliente |
| **M7: Orquestração Adaptativa** | 7 dias | `AdaptiveStrategyOrchestrator`, Multi-Armed Bandit Allocator, Scores | Alocação de ativos e estratégias mudando dinamicamente com as transições de regimes do mercado |
| **M8: Hardening** | 3 dias | BenchmarkDotNet, Chaos tests, fechamento dos checklists | Estabilidade testada sob estresse extremo, performance sob Native AOT validada |

---

## 5. Próximos Passos Imediatos (Para execução paralela)

Iniciamos a **Trilha 1** (instalação do Qdrant e setup do ambiente RAG) e a **Trilha 2** (estrutura do projeto C#) imediatamente em paralelo no sistema.
