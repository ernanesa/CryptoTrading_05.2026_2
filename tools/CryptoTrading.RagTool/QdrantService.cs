using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Qdrant.Client;
using Qdrant.Client.Grpc;

namespace CryptoTrading.RagTool;

public class QdrantService : IDisposable
{
    private const ulong EmbeddingSize = 384;
    private static readonly string[] Collections =
    {
        "cryptotrading_docs",
        "cryptotrading_code",
        "cryptotrading_decisions",
        "cryptotrading_tasks"
    };

    private static readonly string[] IndexedCollections =
    {
        "cryptotrading_docs",
        "cryptotrading_code",
        "cryptotrading_decisions",
        "cryptotrading_tasks"
    };

    private readonly QdrantClient _client;

    public QdrantService(string host = "localhost", int port = 6334)
    {
        _client = new QdrantClient(host, port);
    }

    public async Task InitializeCollectionsAsync()
    {
        Console.WriteLine("Verificando/Criando coleções no Qdrant...");

        foreach (var col in Collections)
        {
            try
            {
                if (await EnsureCollectionAsync(col))
                {
                    Console.WriteLine($"Coleção já existe: {col}");
                    var info = await _client.GetCollectionInfoAsync(col);
                    Console.WriteLine($"  -> Info: {info}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao gerenciar coleção '{col}': {ex.Message}");
            }
        }
    }

    public async Task RefreshIndexedCollectionsAsync()
    {
        Console.WriteLine("Recriando coleções indexadas no Qdrant...");

        foreach (var col in IndexedCollections)
        {
            try
            {
                if (await _client.CollectionExistsAsync(col))
                {
                    Console.WriteLine($"Removendo coleção indexada: {col}");
                    await _client.DeleteCollectionAsync(col);
                }

                await EnsureCollectionAsync(col);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao recriar coleção '{col}': {ex.Message}");
            }
        }
    }

    private async Task<bool> EnsureCollectionAsync(string collectionName)
    {
        var exists = await _client.CollectionExistsAsync(collectionName);
        if (exists)
        {
            return true;
        }

        Console.WriteLine($"Criando coleção: {collectionName} (Dimensão: {EmbeddingSize}, Distância: Cosine)...");
        await _client.CreateCollectionAsync(
            collectionName: collectionName,
            vectorsConfig: new VectorParams
            {
                Size = EmbeddingSize,
                Distance = Distance.Cosine
            }
        );

        return false;
    }

    public async Task UpsertChunksAsync(string collectionName, List<(MarkdownChunk Chunk, float[] Vector)> data)
    {
        if (data.Count == 0) return;

        Console.WriteLine($"Enviando {data.Count} pontos para a coleção '{collectionName}'...");

        var points = new List<PointStruct>();
        ulong idCounter = 1;

        foreach (var item in data)
        {
#pragma warning disable CS0612, CS0618
            var vector = new Vector();
            vector.Data.AddRange(item.Vector);

            var point = new PointStruct
            {
                Id = idCounter++,
                Vectors = new Vectors { Vector = vector },
                Payload =
                {
                    ["content"] = item.Chunk.Content,
                    ["source"] = item.Chunk.SourceFile,
                    ["title"] = item.Chunk.Title,
                    ["section"] = item.Chunk.Section,
                    ["source_type"] = item.Chunk.SourceType,
                    ["created_at"] = item.Chunk.CreatedAt,
                    ["indexed_at"] = item.Chunk.IndexedAt
                }
            };
#pragma warning restore CS0612, CS0618
            points.Add(point);
        }

        try
        {
            await _client.UpsertAsync(collectionName, points);
            Console.WriteLine("Pontos inseridos com sucesso!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao inserir pontos no Qdrant: {ex.Message}");
        }
    }
    public async Task<IReadOnlyList<ScoredPoint>> SearchAsync(string collectionName, float[] queryVector, int limit = 3)
    {
        try
        {
            var results = await _client.SearchAsync(
                collectionName: collectionName,
                vector: queryVector,
                limit: (uint)limit
            );
            return results;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao buscar no Qdrant: {ex.Message}");
            return Array.Empty<ScoredPoint>();
        }
    }

    public void Dispose()
    {
        _client.Dispose();
        GC.SuppressFinalize(this);
    }
}
