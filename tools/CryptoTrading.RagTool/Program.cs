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

    private static async Task<string> BuildContextPackAsync(string rawInput)
    {
        using var qdrant = new QdrantService();
        using var embedder = new EmbeddingService();

        var queryVector = embedder.GenerateEmbedding(rawInput);
        var collections = new[] { "cryptotrading_docs", "cryptotrading_decisions", "cryptotrading_tasks", "cryptotrading_code" };
        var allResults = new List<Qdrant.Client.Grpc.ScoredPoint>();

        foreach (var col in collections)
        {
            var results = await qdrant.SearchAsync(col, queryVector, limit: 2);
            allResults.AddRange(results);
        }

        allResults = allResults.OrderByDescending(r => r.Score).Take(5).ToList();

        var contextPack = new System.Text.StringBuilder();
        foreach (var r in allResults)
        {
            var payload = r.Payload;
            var source = payload.TryGetValue("source", out var s) ? s.StringValue : "Desconhecido";
            var section = payload.TryGetValue("section", out var sec) ? sec.StringValue : "Geral";
            var content = payload.TryGetValue("content", out var c) ? c.StringValue : "";

            contextPack.AppendLine($"\n--- Fonte: {source} > {section} ---");
            contextPack.AppendLine(content);
        }

        return contextPack.ToString();
    }

    private static async Task RunContextPackAsync(string rawInput)
    {
        var context = await BuildContextPackAsync(rawInput);
        Console.WriteLine(context);
    }

    private static async Task RunOptimizeInputAsync(string rawInput)
    {
        var contextPack = await BuildContextPackAsync(rawInput);

        var optimizedPrompt = $@"Você está trabalhando no repositório ernanesa/CryptoTrading_05.2026_2.

Objetivo:
{rawInput}

Contexto recuperado do RAG:
{contextPack}

Arquivos Prováveis (Com base no contexto, verifique e edite):
[Liste os arquivos principais aqui]

Riscos e Precauções:
- não bypassar RiskEngine
- seguir .NET-first e Dapper-first
- verificar testes existentes

Testes:
- defina testes de unidade ou integração
- execute validações necessárias

Critérios de Aceite:
- [Preecher critérios baseados no objetivo]

Paralelização (se aplicável):
- identifique se há algo que pode ser quebrado em sub-agentes

Regras obrigatórias:
- escrever somente neste repositório;
- consultar planos relevantes;
- atualizar checklists ao final.

Antes de codar, gere um plano curto e diga quais arquivos serão alterados.";

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("\n=== PROMPT OTIMIZADO PARA AGENTE DE IA ===");
        Console.ResetColor();
        Console.WriteLine(optimizedPrompt);
        Console.WriteLine("\n==========================================\n");
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
