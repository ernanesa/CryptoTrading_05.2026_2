using CryptoTrading.Domain.Entities;

namespace CryptoTrading.Application.Services;

public class ExchangeRuleValidator
{
    public (bool IsValid, string Message) ValidateOrder(TestnetOrder order, ExchangeFilterInfo filters)
    {
        if (!order.Symbol.Equals(filters.Symbol, StringComparison.OrdinalIgnoreCase))
        {
            return (false, $"Símbolo da ordem ({order.Symbol}) não confere com o filtro ({filters.Symbol}).");
        }

        // 1. Validar quantidade mínima e máxima
        if (order.Quantity < filters.MinQty)
        {
            return (false, $"Quantidade {order.Quantity:F8} menor do que a quantidade mínima permitida de {filters.MinQty:F8}.");
        }
        if (order.Quantity > filters.MaxQty)
        {
            return (false, $"Quantidade {order.Quantity:F8} maior do que a quantidade máxima permitida de {filters.MaxQty:F8}.");
        }

        // 2. Validar precisão e Step Size da quantidade
        if (filters.StepSize > 0m)
        {
            var remainder = order.Quantity % filters.StepSize;
            // Tolerância muito pequena para precisão decimal
            if (remainder > 0.00000001m && (filters.StepSize - remainder) > 0.00000001m)
            {
                return (false, $"Quantidade {order.Quantity:F8} não está em conformidade com o Step Size de {filters.StepSize:F8}.");
            }
        }

        // Para ordens LIMIT, validar filtros de preço e notional mínimo
        if (order.Type.Equals("LIMIT", StringComparison.OrdinalIgnoreCase))
        {
            if (order.Price <= 0m)
            {
                return (false, "Preço deve ser maior que zero para ordens LIMIT.");
            }

            // Validar tick size
            if (filters.TickSize > 0m)
            {
                var remainder = order.Price % filters.TickSize;
                if (remainder > 0.00000001m && (filters.TickSize - remainder) > 0.00000001m)
                {
                    return (false, $"Preço {order.Price:F8} não está em conformidade com o Tick Size de {filters.TickSize:F8}.");
                }
            }

            // Validar Notional Mínimo (Preço * Quantidade)
            var notional = order.Price * order.Quantity;
            if (notional < filters.MinNotional)
            {
                return (false, $"Notional total da ordem (${notional:F2}) é menor do que o mínimo exigido pela exchange de ${filters.MinNotional:F2}.");
            }
        }

        return (true, "Ordem validada com sucesso localmente.");
    }
}
