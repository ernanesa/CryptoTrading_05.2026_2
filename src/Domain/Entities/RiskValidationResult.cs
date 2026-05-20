using CryptoTrading.Domain.Enums;

namespace CryptoTrading.Domain.Entities;

public class RiskValidationResult
{
    public bool IsApproved { get; set; }
    public string Reason { get; set; } = string.Empty;
    public RiskStatus NewStatus { get; set; } = RiskStatus.Normal;

    public static RiskValidationResult Approve(RiskStatus status = RiskStatus.Normal) => 
        new() { IsApproved = true, Reason = "Aprovado pelas regras de risco.", NewStatus = status };

    public static RiskValidationResult Reject(string reason, RiskStatus status = RiskStatus.Normal) => 
        new() { IsApproved = false, Reason = reason, NewStatus = status };
}
