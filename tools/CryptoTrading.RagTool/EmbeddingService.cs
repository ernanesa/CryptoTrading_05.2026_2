using System;
using SmartComponents.LocalEmbeddings;

namespace CryptoTrading.RagTool;

public class EmbeddingService : IDisposable
{
    private readonly LocalEmbedder _embedder;

    public EmbeddingService()
    {
        // Inicializa o embedder local. O modelo all-MiniLM-L6-v2 (384 dimensões)
        // é embutido no assembly e carregado na primeira execução na CPU.
        _embedder = new LocalEmbedder();
    }

    public float[] GenerateEmbedding(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("O texto para gerar embedding não pode ser vazio.", nameof(text));

        // SmartComponents.LocalEmbeddings retorna um objeto do tipo Embedding
        // que pode ser convertido implicitamente ou explicitamente em ReadOnlySpan<float> ou float[]
        var embedding = _embedder.Embed(text);
        return embedding.Values.ToArray();
    }

    public void Dispose()
    {
        _embedder.Dispose();
        GC.SuppressFinalize(this);
    }
}
