namespace CryptoTrading.Domain.Entities;

public class ExplanationSnapshot
{
    public string ModelVersion { get; set; } = "explanation-heuristic-m6-v1";
    public string Summary { get; set; } = string.Empty;
    public List<string> Factors { get; set; } = new();
}
