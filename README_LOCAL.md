# Guia de Execução Local — CryptoTrading 05.2026 v2

Este guia fornece as instruções passo a passo para executar localmente a infraestrutura de dados, a **Memória Técnica (RAG Local)** e a **Solution .NET 10** do robô de trading.

---

## 1. Pré-requisitos
Certifique-se de ter instalado em sua máquina:
*   **.NET Core SDK 10.0** ou superior
*   **Docker e Docker Compose** (para PostgreSQL e Qdrant)
*   **NPM / Node.js** (para o Dashboard React)

---

## 2. Inicializando a Infraestrutura e Memória Técnica (RAG Local)

O ecossistema utiliza **PostgreSQL** para o Feature Store / base operacional e **Qdrant** como banco vetorial de alta performance. Toda a orquestração do RAG local é feita nativamente em C# (.NET 10).

### Passo 2.1: Subir os Contêineres de Banco de Dados
Na raiz do repositório, inicie os serviços do PostgreSQL e Qdrant em segundo plano:
```bash
docker compose up -d
```

Para verificar os contêineres ativos:
```bash
docker ps
```
O Qdrant Dashboard visual estará acessível no navegador em: [http://localhost:6333/dashboard](http://localhost:6333/dashboard).

### Passo 2.2: Indexação Total da Documentação e Código (Ingest)
O robô possui uma ferramenta nativa em C# (`CryptoTrading.RagTool`) que analisa recursivamente os planos arquiteturais em `plans/`, o `README.md` e todo o código-fonte do projeto (`.cs`, `.ts`, `.tsx`, `.css`, `.json`), gerando embeddings em CPU localmente via ONNX Runtime (MiniLM) e salvando-os no Qdrant:
```bash
# Executar a indexação/ingestão completa no Qdrant
dotnet run --project tools/CryptoTrading.RagTool -- ingest
```

### Passo 2.3: Busca Semântica na Memória Técnica (Query)
Para realizar pesquisas semânticas sobre o planejamento e a arquitetura do projeto:
```bash
dotnet run --project tools/CryptoTrading.RagTool -- query "regras de risco do RiskEngine"
```

### Passo 2.4: Otimização de Prompts para Agentes de IA (Optimize)
Para gerar prompts altamente contextualizados e instruídos com as regras técnicas e documentos do projeto, facilitando a programação por agentes de IA:
```bash
dotnet run --project tools/CryptoTrading.RagTool -- optimize "implementar novo indicador de volume na feature store"
```

---

## 3. Compilação e Execução do Backend (.NET 10)

A partir da raiz do repositório:

### 3.1 Restaurar dependências
```bash
dotnet restore
```

### 3.2 Compilar a solução em modo estrito
```bash
dotnet build
```

### 3.3 Rodar os Testes Unitários
```bash
dotnet test
```

### 3.4 Executar o Worker Principal (Ingestão de Dados e Loop de Trading)
```bash
dotnet run --project src/Worker/CryptoTrading.Worker.csproj
```

### 3.5 Executar a API Web (SignalR BFF)
```bash
dotnet run --project src/Api/CryptoTrading.Api.csproj
```
A API ficará disponível em `http://localhost:5000` (ou porta configurada em `appsettings.json` / exibida no console).

---

## 4. Execução do Dashboard Frontend (Vite-React)

A partir da raiz do repositório:

```bash
cd dashboard

# Instalar dependências do Node
npm install

# Rodar o servidor de desenvolvimento local do Dashboard
npm run dev
```
O Dashboard estará acessível no navegador (geralmente em `http://localhost:5173`).
