namespace CryptoTrading.Domain.Entities;

public class RegisteredModelInfo
{
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
    public bool IsShadowMode { get; set; } = true;
    public bool IsActive { get; set; } = true;
}
