using CryptoTrading.Domain.Entities;

namespace CryptoTrading.Contracts.Interfaces;

/// <summary>
/// Contrato para o armazenamento persistente de candles e suas respectivas features calculadas.
/// </summary>
public interface IFeatureStore
{
    /// <summary>
    /// Garante que o schema relacional (tabelas e índices) esteja criado no banco de dados.
    /// </summary>

    /// <summary>
    /// Insere ou atualiza candles históricos em lote de forma altamente eficiente.
    /// </summary>
    Task SaveCandlesAsync(IEnumerable<Candle> candles);

    /// <summary>
    /// Grava as features calculadas e associadas aos candles de forma eficiente.
    /// </summary>
    Task SaveFeaturesAsync(IEnumerable<CandleFeature> features);

    /// <summary>
    /// Resgata o último timestamp de candle salvo para um determinado par e intervalo.
    /// Útil para evitar re-ingestão e calcular deltas de dados faltantes.
    /// </summary>
    Task<DateTime?> GetLastCandleTimeAsync(string symbol, string interval);

    /// <summary>
    /// Resgata os MarketDataPoints (candles + features) salvos no banco de dados para um determinado par e intervalo temporal.
    /// </summary>
    Task<IEnumerable<MarketDataPoint>> GetMarketDataPointsAsync(string symbol, string interval, DateTime startTime, DateTime endTime);

    /// <summary>
    /// Salva ou atualiza o saldo de um ativo na carteira virtual.
    /// </summary>
    Task SaveWalletBalanceAsync(WalletBalance balance);

    /// <summary>
    /// Resgata os saldos de todos os ativos da carteira virtual.
    /// </summary>
    Task<IEnumerable<WalletBalance>> GetWalletBalancesAsync();

    /// <summary>
    /// Registra um novo trade executado na simulação (Paper Trading).
    /// </summary>
    Task SavePaperTradeAsync(PaperTrade trade);

    /// <summary>
    /// Resgata os trades executados na simulação de forma ordenada.
    /// </summary>
    Task<IEnumerable<PaperTrade>> GetPaperTradesAsync(string symbol, int limit = 100);

    /// <summary>
    /// Salva ou atualiza uma posição ativa no Paper Trading.
    /// </summary>
    Task SavePaperPositionAsync(Position position);

    /// <summary>
    /// Resgata a posição ativa para o Paper Trading de um par.
    /// </summary>
    Task<Position?> GetActivePaperPositionAsync(string symbol);

    /// <summary>
    /// Grava uma auditoria de decisão de trading (sinal aceito ou rejeitado e por qual regra).
    /// </summary>
    Task SavePaperOrderAsync(PaperOrder order);
    Task<IEnumerable<PaperOrder>> GetActivePaperOrdersAsync(string symbol);

    Task SaveDecisionAuditAsync(DecisionAudit audit);

    /// <summary>
    /// Resgata o histórico de auditoria de decisões do RiskEngine e estratégias.
    /// </summary>
    Task<IEnumerable<DecisionAudit>> GetDecisionAuditsAsync(int limit = 100);

    /// <summary>
    /// Limpa os dados de simulação (Paper Trading) para reinicialização.
    /// </summary>
    Task ClearPaperTradingDataAsync();

    /// <summary>
    /// Salva ou atualiza as regras de filtro da exchange para um par.
    /// </summary>
    Task SaveExchangeFilterInfoAsync(ExchangeFilterInfo filter);

    /// <summary>
    /// Resgata as regras de filtro da exchange salvas para um par.
    /// </summary>
    Task<ExchangeFilterInfo?> GetExchangeFilterInfoAsync(string symbol);

    /// <summary>
    /// Salva ou atualiza o status de uma ordem da Testnet.
    /// </summary>
    Task SaveTestnetOrderAsync(TestnetOrder order);

    /// <summary>
    /// Resgata uma ordem da Testnet através do seu clientOrderId.
    /// </summary>
    Task<TestnetOrder?> GetTestnetOrderAsync(string clientOrderId);

    /// <summary>
    /// Resgata ordens ativas da Testnet para fins de sincronização de status.
    /// </summary>
    Task<IEnumerable<TestnetOrder>> GetActiveTestnetOrdersAsync();

    /// <summary>
    /// Registra uma auditoria de transação da Testnet.
    /// </summary>
    Task SaveTestnetAuditLogAsync(TestnetAuditLog log);

    /// <summary>
    /// Resgata logs de auditoria da Testnet de forma ordenada.
    /// </summary>
    Task<IEnumerable<TestnetAuditLog>> GetTestnetAuditLogsAsync(int limit = 100);

    Task SaveStrategyPerformanceMetricAsync(StrategyPerformanceMetric metric);
    Task<StrategyPerformanceMetric?> GetStrategyPerformanceMetricAsync(string strategyName, string symbol, string timeframe, string regime);
    Task SaveStrategyStateAsync(StrategyState state);
    Task<StrategyState?> GetStrategyStateAsync(string strategyName, string symbol);
}
