namespace CryptoTrading.Domain.Enums;

public enum TradeSignalType
{
    Buy,   // Entrada em Long / Compra
    Sell,  // Entrada em Short / Venda
    Exit,  // Fechar posição aberta
    Hold   // Sem ação
}
