using CryptoTrading.Contracts.Interfaces;
using CryptoTrading.Domain.Entities;

namespace CryptoTrading.Application.Services;

public class RagContextProvider : IRagContextProvider
{
    public RagContextSnapshot BuildContext(string symbol, string interval, string regime)
    {
        return new RagContextSnapshot
        {
            Query = $"M6 intelligence context {symbol.ToUpperInvariant()} {interval} {regime}",
            ContextItems = new List<string>
            {
                "ML, sentimento e eventos sao contexto auxiliar e nao executam acoes.",
                "RiskEngine continua sendo o gate obrigatorio para decisoes relevantes.",
                "Scores e modelos devem manter versao e fonte registradas."
            }
        };
    }
}
