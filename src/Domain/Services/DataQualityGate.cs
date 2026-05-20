using CryptoTrading.Domain.Entities;

namespace CryptoTrading.Domain.Services;

/// <summary>
/// Barreira de qualidade de dados para filtrar e bloquear candles inconsistentes ou com anomalias de mercado.
/// </summary>
public class DataQualityGate
{
    /// <summary>
    /// Valida um candle contra regras estritas de integridade financeira e de dados.
    /// </summary>
    public bool Validate(Candle candle, out List<string> errors)
    {
        errors = new List<string>();

        if (string.IsNullOrWhiteSpace(candle.Symbol))
        {
            errors.Add("Símbolo do ativo está vazio ou inválido.");
        }

        if (string.IsNullOrWhiteSpace(candle.Interval))
        {
            errors.Add("Intervalo temporal do candle está vazio.");
        }

        if (candle.OpenTime >= candle.CloseTime)
        {
            errors.Add($"Timestamp inconsistente: Horário de fechamento ({candle.CloseTime}) deve ser posterior ao de abertura ({candle.OpenTime}).");
        }

        if (candle.Open <= 0 || candle.High <= 0 || candle.Low <= 0 || candle.Close <= 0)
        {
            errors.Add($"Preço inválido encontrado. Valores devem ser maiores que zero. Open: {candle.Open}, High: {candle.High}, Low: {candle.Low}, Close: {candle.Close}");
        }

        if (candle.Volume < 0)
        {
            errors.Add($"Volume de negociação negativo encontrado: {candle.Volume}");
        }

        if (candle.High < candle.Open)
        {
            errors.Add($"Máxima inconsistente: High ({candle.High}) é menor que Open ({candle.Open}).");
        }

        if (candle.High < candle.Close)
        {
            errors.Add($"Máxima inconsistente: High ({candle.High}) é menor que Close ({candle.Close}).");
        }

        if (candle.Low > candle.Open)
        {
            errors.Add($"Mínima inconsistente: Low ({candle.Low}) é maior que Open ({candle.Open}).");
        }

        if (candle.Low > candle.Close)
        {
            errors.Add($"Mínima inconsistente: Low ({candle.Low}) é maior que Close ({candle.Close}).");
        }

        if (candle.High < candle.Low)
        {
            errors.Add($"Inconsistência crítica: High ({candle.High}) é menor que Low ({candle.Low}).");
        }

        return errors.Count == 0;
    }
}
