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
                        Console.WriteLine("Uso: dotnet run --project tools/CryptoTrading.RagTool -- optimize-input \"<seu pedido>\" [--profile antigravity|copilot|code-review|integration]");
                        return 1;
                    }
                    await RunOptimizeInputAsync(args[1], GetOption(args, "--profile") ?? "antigravity");
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
        Console.WriteLine("  optimize-input \"<pedido>\" [--profile <perfil>] Gera prompt estruturado para agentes.");
        Console.WriteLine("  context-pack \"<pedido>\"   Retorna apenas o contexto agrupado.");
        Console.WriteLine("\nPerfis optimize-input:");
        Console.WriteLine("  antigravity | copilot | code-review | integration");
        Console.WriteLine("============================================================\n");
    }

    private static string? GetOption(string[] args, string optionName)
    {
        for (var i = 0; i < args.Length - 1; i++)
        {
            if (args[i].Equals(optionName, StringComparison.OrdinalIgnoreCase))
            {
                return args[i + 1];
            }
        }

        return null;
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

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("\n>>> OBJETIVO <<<");
        Console.ResetColor();
        Console.WriteLine(rawInput);

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

        var probableFiles = ExtractProbableFiles(collectionsDict);
        var sources = ExtractSources(collectionsDict);

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("\n>>> FONTES RECUPERADAS <<<");
        Console.ResetColor();
        Console.WriteLine(sources.Count == 0 ? "- Nenhuma fonte encontrada." : string.Join("\n", sources.Select(s => $"- {s}")));

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("\n>>> ARQUIVOS PROVÁVEIS <<<");
        Console.ResetColor();
        Console.WriteLine(probableFiles.Count == 0 ? "- Consultar docs/planos relacionados antes de editar." : string.Join("\n", probableFiles.Select(f => $"- {f}")));

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("\n>>> RISCOS <<<");
        Console.ResetColor();
        Console.WriteLine("- Não bypassar RiskEngine/RiskDecision/DecisionAudit em fluxos operacionais.");
        Console.WriteLine("- Não mascarar dados simulados como reais; explicitar Simulation, Paper, TestnetDryRun ou TestnetReal.");
        Console.WriteLine("- Não versionar secrets nem emitir API keys em logs, auditorias ou prompts.");

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("\n>>> TESTES SUGERIDOS <<<");
        Console.ResetColor();
        Console.WriteLine("- dotnet test -c Release");
        Console.WriteLine("- git diff --check");
        Console.WriteLine("- cd dashboard && npm run build, quando houver mudança no dashboard.");

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("\n>>> CRITÉRIOS <<<");
        Console.ResetColor();
        Console.WriteLine("- Implementar a menor entrega validável.");
        Console.WriteLine("- Atualizar docs/checklists quando mudar comportamento ou maturidade.");
        Console.WriteLine("- Manter heavy gates como opt-in quando dependerem de Docker, exchange real ou browsers.");

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("\n>>> DEPENDÊNCIAS <<<");
        Console.ResetColor();
        Console.WriteLine("- Qdrant local e embeddings precisam estar atualizados para máxima precisão.");
        Console.WriteLine("- Rode refresh após mudanças grandes em plans/ ou src/.");
    }

    private static async Task RunOptimizeInputAsync(string rawInput, string profile)
    {
        var collectionsDict = await BuildContextCollectionsPackAsync(rawInput);

        var docsContext = FormatCollectionContext(collectionsDict.GetValueOrDefault("cryptotrading_docs"));
        var decisionsContext = FormatCollectionContext(collectionsDict.GetValueOrDefault("cryptotrading_decisions"));
        var tasksContext = FormatCollectionContext(collectionsDict.GetValueOrDefault("cryptotrading_tasks"));
        var codeContext = FormatCollectionContext(collectionsDict.GetValueOrDefault("cryptotrading_code"));

        var probableFiles = ExtractProbableFiles(collectionsDict);

        var filesStr = probableFiles.Count > 0
            ? string.Join("\n- ", probableFiles.Select(f => $"[{Path.GetFileName(f)}](file://{f})"))
            : "[Nenhum arquivo identificado de forma especifica; consulte a colecao de codigo]";
        var profileSpec = BuildProfileSpec(profile);

        var promptTemplate = $@"Você é um agente de desenvolvimento especialista trabalhando no repositório `ernanesa/CryptoTrading_05.2026_2`.

## OBJETIVO DO PEDIDO
{rawInput}

## PERFIL SELECIONADO
{profileSpec.Name}

## POSTURA DO AGENTE
{profileSpec.Guidance}

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

## ENTREGÁVEIS ESPERADOS
{profileSpec.Deliverables}

## PLANO MÍNIMO ANTES DE EDITAR
1. Liste os arquivos que pretende alterar.
2. Explique os testes que serão executados.
3. Implemente a menor entrega de valor.
4. Rode validações e atualize documentação quando aplicável.
";

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("\n=== PROMPT OTIMIZADO COMPLETO (RAG-ENRICHED) ===");
        Console.ResetColor();
        Console.WriteLine(promptTemplate);
        Console.WriteLine("\n================================================\n");
    }

    private static HashSet<string> ExtractProbableFiles(Dictionary<string, List<Qdrant.Client.Grpc.ScoredPoint>> collectionsDict)
    {
        var probableFiles = new HashSet<string>();
        if (!collectionsDict.TryGetValue("cryptotrading_code", out var codePoints))
        {
            return probableFiles;
        }

        foreach (var cp in codePoints)
        {
            if (cp.Payload.TryGetValue("source", out var sVal))
            {
                probableFiles.Add(sVal.StringValue);
            }
        }

        return probableFiles;
    }

    private static HashSet<string> ExtractSources(Dictionary<string, List<Qdrant.Client.Grpc.ScoredPoint>> collectionsDict)
    {
        var sources = new HashSet<string>();
        foreach (var point in collectionsDict.Values.SelectMany(v => v))
        {
            if (point.Payload.TryGetValue("source", out var sVal))
            {
                sources.Add(sVal.StringValue);
            }
        }

        return sources;
    }

    private static AgentProfileSpec BuildProfileSpec(string profile)
    {
        return profile.Trim().ToLowerInvariant() switch
        {
            "copilot" or "github-copilot" => new AgentProfileSpec(
                "GitHub Copilot / Code Assistant",
                "Foque em implementação direta, código pequeno, testes objetivos e aderência a padrões locais.",
                "- Patch compilável\n- Testes unitários ou smoke relevantes\n- Lista curta de arquivos alterados"),
            "code-review" or "review" => new AgentProfileSpec(
                "Code Review",
                "Atue como auditor: priorize bugs, riscos, regressões, segurança, secrets e lacunas de teste.",
                "- Achados ordenados por severidade\n- Referências de arquivo/linha\n- Riscos residuais e testes ausentes"),
            "integration" or "integration-agent" => new AgentProfileSpec(
                "Integration Agent",
                "Foque em conflitos, compatibilidade entre branches, validações finais e relatório de release.",
                "- Plano de integração\n- Comandos executados e resultados\n- Pendências bloqueantes e não bloqueantes"),
            _ => new AgentProfileSpec(
                "Antigravity",
                "Foque em autonomia com cautela: use RAG, preserve escopo, paralelize trilhas independentes e valide tudo que alterar.",
                "- Plano curto\n- Patch mínimo validável\n- Testes e atualização de docs/checklists")
        };
    }

    private sealed record AgentProfileSpec(string Name, string Guidance, string Deliverables);

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
