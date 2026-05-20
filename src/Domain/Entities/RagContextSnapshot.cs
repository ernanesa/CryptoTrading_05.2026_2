namespace CryptoTrading.Domain.Entities;

public class RagContextSnapshot
{
    public string ProviderVersion { get; set; } = "rag-context-provider-m6-v1";
    public string Source { get; set; } = "local-plans-rag";
    public string Query { get; set; } = string.Empty;
    public List<string> ContextItems { get; set; } = new();
}
