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
                case "ingest":
                    await RunIngestionAsync(repoRoot);
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

                case "optimize":
                    if (args.Length < 2)
                    {
                        Console.WriteLine("Erro: Defina o objetivo/pedido.");
                        Console.WriteLine("Uso: dotnet run --project tools/CryptoTrading.RagTool -- optimize \"<seu pedido>\"");
                        return 1;
                    }
                    await RunOptimizeAsync(args[1]);
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
        Console.WriteLine("  query \"<pergunta>\"      Realiza busca semântica por documentos relevantes.");
        Console.WriteLine("  optimize \"<pedido>\"     Gera um prompt completo e estruturado para agentes de IA.");
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
        
        // Fallback para o diretório de execução do Program.cs caso esteja sendo executado de forma direta
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

    private static async Task RunIngestionAsync(string repoRoot)
    {
        Console.WriteLine("\n=== INICIANDO INGESTÃO DE PLANOS & DOCUMENTOS ===");

        // 1. Inicializa Serviços
        using var qdrant = new QdrantService();
        using var embedder = new EmbeddingService();

        // Inicializa coleções
        await qdrant.InitializeCollectionsAsync();

        // 2. Coleta arquivos
        var plansDir = Path.Combine(repoRoot, "plans");
        var markdownFiles = Directory.GetFiles(plansDir, "*.md", SearchOption.AllDirectories).ToList();
        
        // Adiciona também o README.md da raiz do workspace
        var rootReadme = Path.Combine(repoRoot, "README.md");
        if (File.Exists(rootReadme))
        {
            markdownFiles.Add(rootReadme);
        }

        Console.WriteLine($"Encontrados {markdownFiles.Count} arquivos markdown para indexação.");

        var allChunks = new List<MarkdownChunk>();
        foreach (var file in markdownFiles)
        {
            Console.WriteLine($"Processando: {Path.GetFileName(file)}...");
            var fileChunks = MarkdownChunker.ParseFile(file, repoRoot);
            allChunks.AddRange(fileChunks);
        }

        Console.WriteLine($"Total de chunks de texto gerados: {allChunks.Count}");

        // 3. Gera Embeddings e envia em lote
        Console.WriteLine("Gerando embeddings locais via CPU (ONNX MiniLM)...");
        var pointsToInsert = new List<(MarkdownChunk Chunk, float[] Vector)>();

        for (int i = 0; i < allChunks.Count; i++)
        {
            var chunk = allChunks[i];

            try
            {
                var vector = embedder.GenerateEmbedding(chunk.Content);
                if (i < 5)
                {
                    Console.WriteLine($"Chunk {i + 1}: tamanho do vetor = {vector.Length}");
                }
                pointsToInsert.Add((chunk, vector));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n[AVISO] Erro ao gerar embedding para chunk {i}: {ex.Message}");
            }
        }
        Console.WriteLine("\nEmbeddings gerados com sucesso!");

        // Envia para o Qdrant
        await qdrant.UpsertChunksAsync("cryptotrading_docs", pointsToInsert);

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("\n=== INGESTÃO CONCLUÍDA COM SUCESSO! ===");
        Console.ResetColor();
    }

    private static async Task RunQueryAsync(string queryText)
    {
        Console.WriteLine($"\n=== REALIZANDO BUSCA SEMÂNTICA ===");
        
        using var qdrant = new QdrantService();
        using var embedder = new EmbeddingService();

        Console.WriteLine("Gerando embedding para a pergunta...");
        var queryVector = embedder.GenerateEmbedding(queryText);

        Console.WriteLine("Buscando no Qdrant...");
        var results = await qdrant.SearchAsync("cryptotrading_docs", queryVector, limit: 3);

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"\nResultados encontrados: {results.Count}\n");
        Console.ResetColor();

        for (int i = 0; i < results.Count; i++)
        {
            var r = results[i];
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

    private static async Task RunOptimizeAsync(string rawInput)
    {
        using var qdrant = new QdrantService();
        using var embedder = new EmbeddingService();

        var queryVector = embedder.GenerateEmbedding(rawInput);
        var results = await qdrant.SearchAsync("cryptotrading_docs", queryVector, limit: 3);

        var contextPack = new System.Text.StringBuilder();
        foreach (var r in results)
        {
            var payload = r.Payload;
            var source = payload.TryGetValue("source", out var s) ? s.StringValue : "Desconhecido";
            var section = payload.TryGetValue("section", out var sec) ? sec.StringValue : "Geral";
            var content = payload.TryGetValue("content", out var c) ? c.StringValue : "";

            contextPack.AppendLine($"\n--- Fonte: {source} > {section} ---");
            contextPack.AppendLine(content);
        }

        var optimizedPrompt = $@"Você está trabalhando no repositório ernanesa/CryptoTrading_05.2026_2.

Objetivo:
{rawInput}

Contexto recuperado do RAG:
{contextPack}

Regras obrigatórias:
- escrever somente neste repositório;
- consultar planos relevantes;
- seguir .NET-first;
- seguir Dapper-first;
- não bypassar RiskEngine;
- atualizar checklists ao final.

Antes de codar, gere um plano curto e diga quais arquivos serão alterados.";

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("\n=== PROMPT OTIMIZADO PARA AGENTE DE IA ===");
        Console.ResetColor();
        Console.WriteLine(optimizedPrompt);
        Console.WriteLine("\n==========================================\n");
    }
}
