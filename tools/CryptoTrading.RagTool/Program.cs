using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CryptoTrading.RagTool;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        if (args.Length < 1)
        {
            ShowUsage();
            return 1;
        }

        var command = args[0].ToLowerInvariant();

        try
        {
            var repoRoot = GetRepositoryRoot();
            Console.WriteLine($"[RAG] Raiz do Repositório: {repoRoot}");

            switch (command)
            {
                case "--help":
                case "-h":
                    ShowUsage();
                    return 0;
                case "ingest":
                    await RunIngestionAsync(repoRoot, resetIndexedCollections: false);
                    break;

                case "refresh":
                    await RunIngestionAsync(repoRoot, resetIndexedCollections: true);
                    break;

                case "query":
                    if (args.Length < 2)
                    {
                        Console.WriteLine("Erro: Defina a pergunta de busca.");
                        Console.WriteLine("Uso: dotnet run --project tools/CryptoTrading.RagTool -- query \"<sua pergunta>\"");
                        return 1;
                    }
                    await RunQueryAsync(args[1]);
                    break;

                case "optimize-input":
                    if (args.Length < 2)
                    {
                        Console.WriteLine("Erro: Defina o objetivo/pedido.");
                        Console.WriteLine("Uso: dotnet run --project tools/CryptoTrading.RagTool -- optimize-input \"<seu pedido>\"");
                        return 1;
                    }
                    await RunOptimizeInputAsync(args[1]);
                    break;
                    
                case "context-pack":
                    if (args.Length < 2)
                    {
                        Console.WriteLine("Erro: Defina o objetivo/pedido.");
                        Console.WriteLine("Uso: dotnet run --project tools/CryptoTrading.RagTool -- context-pack \"<seu pedido>\"");
                        return 1;
                    }
                    await RunContextPackAsync(args[1]);
                    break;

                default:
                    Console.WriteLine($"Erro: Comando '{command}' desconhecido.");
                    ShowUsage();
                    return 1;
            }

            return 0;
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[RAG ERRO] Ocorreu uma exceção: {ex.Message}");
            Console.ResetColor();
            return 1;
        }
    }

    private static void ShowUsage()
    {
        Console.WriteLine("\n================ CryptoTrading RagTool (C#) ================");
        Console.WriteLine("Ferramenta de RAG Local offline para suporte ao desenvolvimento");
        Console.WriteLine("\nUso:");
        Console.WriteLine("  dotnet run --project tools/CryptoTrading.RagTool -- <comando> [argumentos]");
        Console.WriteLine("\nComandos:");
        Console.WriteLine("  ingest                  Lê todos os planos e indexa no Qdrant.");
        Console.WriteLine("  refresh                 Recria docs/código no Qdrant e executa ingestão limpa.");
        Console.WriteLine("  query \"<pergunta>\"      Realiza busca semântica por documentos relevantes.");
        Console.WriteLine("  optimize-input \"<pedido>\" Gera um prompt completo e estruturado para agentes de IA.");
        Console.WriteLine("  context-pack \"<pedido>\"   Retorna apenas o contexto agrupado.");
        Console.WriteLine("============================================================\n");
    }

    private static string GetRepositoryRoot()
    {
        var current = Directory.GetCurrentDirectory();
        while (current != null)
        {
            if (Directory.Exists(Path.Combine(current, "plans")) && File.Exists(Path.Combine(current, "README.md")))
            {
                return current;
            }
            current = Directory.GetParent(current)?.FullName;
        }

        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        current = baseDir;
        while (current != null)
        {
            if (Directory.Exists(Path.Combine(current, "plans")) && File.Exists(Path.Combine(current, "README.md")))
            {
                return current;
            }
            current = Directory.GetParent(current)?.FullName;
        }

        throw new InvalidOperationException("Não foi possível encontrar a raiz do repositório (com a pasta 'plans' e o 'README.md').");
    }

    private static async Task RunIngestionAsync(string repoRoot, bool resetIndexedCollections)
    {
        Console.WriteLine(resetIndexedCollections
            ? "\n=== INICIANDO REFRESH LIMPO DO RAG ==="
            : "\n=== INICIANDO INGESTÃO DE PLANOS & DOCUMENTOS ===");

        using var qdrant = new QdrantService();
        using var embedder = new EmbeddingService();

        if (resetIndexedCollections)
        {
            await qdrant.RefreshIndexedCollectionsAsync();
        }
        else
        {
            await qdrant.InitializeCollectionsAsync();
        }

        Console.WriteLine("\n=== COLETANDO ARQUIVOS DE DOCUMENTAÇÃO ===");
        var plansPath = Path.Combine(repoRoot, "plans");
        var markdownFiles = Directory.GetFiles(plansPath, "*.md", SearchOption.AllDirectories).ToList();

        var rootReadme = Path.Combine(repoRoot, "README.md");
        if (File.Exists(rootReadme)) markdownFiles.Add(rootReadme);

        Console.WriteLine($"Encontrados {markdownFiles.Count} arquivos markdown para indexação.");

        var allChunks = new List<MarkdownChunk>();
        foreach (var file in markdownFiles)
        {
            var fileChunks = MarkdownChunker.ParseFile(file, repoRoot);
            allChunks.AddRange(fileChunks);
        }

        var docsChunks = new List<(MarkdownChunk Chunk, float[] Vector)>();
        var decisionsChunks = new List<(MarkdownChunk Chunk, float[] Vector)>();
        var tasksChunks = new List<(MarkdownChunk Chunk, float[] Vector)>();

        for (int i = 0; i < allChunks.Count; i++)
        {
            var chunk = allChunks[i];
            try
            {
                var vector = embedder.GenerateEmbedding(chunk.Content);
                if (chunk.SourceType == "decision") decisionsChunks.Add((chunk, vector));
                else if (chunk.SourceType == "task") tasksChunks.Add((chunk, vector));
                else docsChunks.Add((chunk, vector));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro chunk: {ex.Message}");
            }
        }

        await qdrant.UpsertChunksAsync("cryptotrading_docs", docsChunks);
        await qdrant.UpsertChunksAsync("cryptotrading_decisions", decisionsChunks);
        await qdrant.UpsertChunksAsync("cryptotrading_tasks", tasksChunks);

        Console.WriteLine("\n=== COLETANDO ARQUIVOS DE CÓDIGO DO PROJETO ===");
        var allFiles = Directory.GetFiles(repoRoot, "*.*", SearchOption.AllDirectories);
        var codeFiles = allFiles.Where(IsCodeFile).ToList();

        Console.WriteLine($"Encontrados {codeFiles.Count} arquivos de código qualificados para indexação.");

        var allCodeChunks = new List<MarkdownChunk>();
        foreach (var file in codeFiles)
        {
            try
            {
                var fileChunks = CodeChunker.ParseFile(file, repoRoot);
                allCodeChunks.AddRange(fileChunks);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AVISO] Erro ao parsear arquivo de código {Path.GetFileName(file)}: {ex.Message}");
            }
        }

        var codePointsToInsert = new List<(MarkdownChunk Chunk, float[] Vector)>();

        for (int i = 0; i < allCodeChunks.Count; i++)
        {
            var chunk = allCodeChunks[i];
            try
            {
                var vector = embedder.GenerateEmbedding(chunk.Content);
                codePointsToInsert.Add((chunk, vector));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro chunk code: {ex.Message}");
            }
        }

        await qdrant.UpsertChunksAsync("cryptotrading_code", codePointsToInsert);

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("\n=== INGESTÃO CONCLUÍDA COM SUCESSO! ===");
        Console.ResetColor();
    }

    private static async Task RunQueryAsync(string queryText)
    {
        Console.WriteLine($"\n=== REALIZANDO BUSCA SEMÂNTICA ===");
        using var qdrant = new QdrantService();
        using var embedder = new EmbeddingService();

        var queryVector = embedder.GenerateEmbedding(queryText);

        var collections = new[] { "cryptotrading_docs", "cryptotrading_decisions", "cryptotrading_tasks", "cryptotrading_code" };
        var allResults = new List<Qdrant.Client.Grpc.ScoredPoint>();

        foreach (var col in collections)
        {
            var results = await qdrant.SearchAsync(col, queryVector, limit: 2);
            allResults.AddRange(results);
        }

        allResults = allResults.OrderByDescending(r => r.Score).Take(5).ToList();

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"\nResultados encontrados: {allResults.Count}\n");
        Console.ResetColor();

        for (int i = 0; i < allResults.Count; i++)
        {
            var r = allResults[i];
            var payload = r.Payload;
            var source = payload.TryGetValue("source", out var s) ? s.StringValue : "Desconhecido";
            var section = payload.TryGetValue("section", out var sec) ? sec.StringValue : "Geral";
            var content = payload.TryGetValue("content", out var c) ? c.StringValue : "";

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"[{i + 1}] Score: {r.Score:F4} | Fonte: {source} > {section}");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine(new string('-', 70));
            Console.ResetColor();
            Console.WriteLine(content);
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine(new string('-', 70) + "\n");
            Console.ResetColor();
        }
    }

    private static async Task<Dictionary<string, List<Qdrant.Client.Grpc.ScoredPoint>>> BuildContextCollectionsPackAsync(string rawInput)
    {
        using var qdrant = new QdrantService();
        using var embedder = new EmbeddingService();

        var queryVector = embedder.GenerateEmbedding(rawInput);
        var collections = new[] { "cryptotrading_docs", "cryptotrading_decisions", "cryptotrading_tasks", "cryptotrading_code" };
        var resultsDict = new Dictionary<string, List<Qdrant.Client.Grpc.ScoredPoint>>();

        foreach (var col in collections)
        {
            try
            {
                var results = await qdrant.SearchAsync(col, queryVector, limit: 3);
                resultsDict[col] = results.OrderByDescending(r => r.Score).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AVISO] Erro ao buscar na colecao {col}: {ex.Message}");
                resultsDict[col] = new List<Qdrant.Client.Grpc.ScoredPoint>();
            }
        }

        return resultsDict;
    }

    private static async Task RunContextPackAsync(string rawInput)
    {
        Console.WriteLine($"\n=== CONTEXT PACK PARA: \"{rawInput}\" ===");
        var collectionsDict = await BuildContextCollectionsPackAsync(rawInput);

        foreach (var pair in collectionsDict)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"\n>>> COLEÇÃO: {pair.Key} (Matches: {pair.Value.Count}) <<<");
            Console.ResetColor();

            foreach (var r in pair.Value)
            {
                var payload = r.Payload;
                var source = payload.TryGetValue("source", out var s) ? s.StringValue : "Desconhecido";
                var section = payload.TryGetValue("section", out var sec) ? sec.StringValue : "Geral";
                var content = payload.TryGetValue("content", out var c) ? c.StringValue : "";

                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"  - Score: {r.Score:F4} | Fonte: {source} > {section}");
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine(new string('-', 50));
                Console.ResetColor();
                Console.WriteLine(content.Length > 300 ? content[..300] + "..." : content);
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine(new string('-', 50));
                Console.ResetColor();
            }
        }
    }

    private static async Task RunOptimizeInputAsync(string rawInput)
    {
        var collectionsDict = await BuildContextCollectionsPackAsync(rawInput);

        var docsContext = FormatCollectionContext(collectionsDict.GetValueOrDefault("cryptotrading_docs"));
        var decisionsContext = FormatCollectionContext(collectionsDict.GetValueOrDefault("cryptotrading_decisions"));
        var tasksContext = FormatCollectionContext(collectionsDict.GetValueOrDefault("cryptotrading_tasks"));
        var codeContext = FormatCollectionContext(collectionsDict.GetValueOrDefault("cryptotrading_code"));

        var probableFiles = new HashSet<string>();
        if (collectionsDict.TryGetValue("cryptotrading_code", out var codePoints))
        {
            foreach (var cp in codePoints)
            {
                if (cp.Payload.TryGetValue("source", out var sVal))
                {
                    probableFiles.Add(sVal.StringValue);
                }
            }
        }

        var filesStr = probableFiles.Count > 0 
            ? string.Join("\n- ", probableFiles.Select(f => $"[{Path.GetFileName(f)}](file://{f})")) 
            : "[Nenhum arquivo identificado de forma especifica; consulte a colecao de codigo]";

        var promptTemplate = $@"Você é um agente de desenvolvimento especialista trabalhando no repositório `ernanesa/CryptoTrading_05.2026_2`.

## OBJETIVO DO PEDIDO
{rawInput}

## CONTEXTO E DOCUMENTAÇÃO RECUPERADA (RAG)
### Documentos e Guias de Estágios:
{docsContext}

### Decisões de Arquitetura Relevantes:
{decisionsContext}

### Checklists e Histórico de Tarefas Relacionadas:
{tasksContext}

### Referências Próximas de Código Atual:
{codeContext}

## ARQUIVOS PROVÁVEIS PARA MODIFICAÇÃO/ANÁLISE
- {filesStr}

## RISCOS E DIRETRIZES TÉCNICAS ESTRITAS
1. **Nenhum Bypass ao RiskEngine**: Qualquer fluxo de trade (Paper ou Testnet) DEVE passar rigorosamente pelo `RiskEngine`.
2. **Nenhuma Operação Real**: Todas as chaves devem ser validadas. Se estiver na Binance Testnet Real, nunca assuma preenchimento instantâneo (FILLED) sem consultar a exchange ou conferir o status retornado.
3. **Redação Absoluta de Segredos**: Nunca logue ou persista chaves de API cruas. Use o `SecretRedactor` em qualquer log ou auditoria.
4. **Princípios de Arquitetura**: Manter compatibilidade com C# 14, .NET 10, e manter queries rápidas via Dapper.

## CRITÉRIOS DE ACEITAÇÃO SUGERIDOS
- Cobertura de testes unitários ou de integração robustos.
- Build íntegro em release (`dotnet test -c Release`).
- Execução com tempo de latência de CPU reduzido e livre de Memory Leaks.

---

## PERFIS DE COMPORTAMENTO RECOMENDADOS

### PERFIL A: ANTIGRAVITY (Agente Autônomo Chefe)
> [!NOTE]
> Foco em auditoria rigorosa de todas as ramificações de risco, integração do dashboard, robustez matemática das heurísticas adaptativas e plano de liberação (Release Readiness).
> Postura: Altamente estratégico, audita as restrições e re-planeja cenários de caos.

### PERFIL B: GITHUB COPILOT / CODE-ASSISTANT (Desenvolvedor de Componentes)
> [!TIP]
> Foco em gerar implementações limpas de algoritmos específicos, queries SQL Dapper e testes unitários parametrizados.
> Postura: Foco em sintaxe perfeita, conformidade com C# 14 e geração rápida de arquivos de teste.

### PERFIL C: BRANCH WORKER (Agente Focado em Sub-tarefa)
> [!IMPORTANT]
> Foco no escopo ultra-específico da branch indicada. Não deve desviar para refatorações alheias.
> Postura: Assertividade cirúrgica, resolve estritamente os critérios de aceitação e reporta progresso ao task.md.

### PERFIL D: CODE REVIEWER (Auditor de Pull Request)
> [!CAUTION]
> Foco em conformidade regulatória interna, segurança de credenciais, cobertura de cenários negativos e legibilidade.
> Postura: Cético, busca pontos falhos, vulnerabilidades de concorrência ou bypass nos limites de alocação de capital.
";

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("\n=== PROMPT OTIMIZADO COMPLETO (RAG-ENRICHED) ===");
        Console.ResetColor();
        Console.WriteLine(promptTemplate);
        Console.WriteLine("\n================================================\n");
    }

    private static string FormatCollectionContext(List<Qdrant.Client.Grpc.ScoredPoint>? points)
    {
        if (points == null || points.Count == 0)
        {
            return "*(Nenhum item relevante localizado nesta coleção)*";
        }

        var sb = new System.Text.StringBuilder();
        foreach (var p in points)
        {
            var payload = p.Payload;
            var source = payload.TryGetValue("source", out var s) ? s.StringValue : "Desconhecido";
            var section = payload.TryGetValue("section", out var sec) ? sec.StringValue : "Geral";
            var content = payload.TryGetValue("content", out var c) ? c.StringValue : "";

            sb.AppendLine($"> **Fonte: {Path.GetFileName(source)} (Seção: {section}, Score: {p.Score:F3})**");
            sb.AppendLine($"> {content.Replace("\n", "\n> ")}");
            sb.AppendLine(">");
        }

        return sb.ToString();
    }

    private static bool IsCodeFile(string filePath)
    {
        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        if (ext != ".cs" && ext != ".ts" && ext != ".tsx" && ext != ".css" && ext != ".json")
            return false;

        var parts = filePath.Split(Path.DirectorySeparatorChar);
        foreach (var part in parts)
        {
            var p = part.ToLowerInvariant();
            if (p == "bin" || p == "obj" || p == "node_modules" || p == "dist" || p == "build" || p == ".git" || p == ".gemini" || p == "qdrant_storage" || p == ".vs" || p == ".idea")
                return false;
        }

        var fileName = Path.GetFileName(filePath).ToLowerInvariant();
        if (fileName == "package-lock.json" || fileName == "yarn.lock" || fileName == "pnpm-lock.yaml" || fileName == "mcp-state.json")
            return false;

        return true;
    }
}
