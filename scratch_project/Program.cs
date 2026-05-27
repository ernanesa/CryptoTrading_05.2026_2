using System.IO;
var files = new[] { "../tests/UnitTests/PaperTradingTests.cs", "../tests/UnitTests/BinanceTestnetTests.cs", "../tests/UnitTests/PaperTradingScenarioTests.cs" };
foreach (var file in files) {
    if (File.Exists(file)) {
        var c = File.ReadAllText(file);
        c = c.Replace("public Task ClearPaperTradingDataAsync()", "public Task SaveStrategyPerformanceMetricAsync(CryptoTrading.Domain.Entities.StrategyPerformanceMetric metric) => Task.CompletedTask; public Task<CryptoTrading.Domain.Entities.StrategyPerformanceMetric?> GetStrategyPerformanceMetricAsync(string strategyName, string symbol, string timeframe, string regime) => Task.FromResult<CryptoTrading.Domain.Entities.StrategyPerformanceMetric?>(null); public Task SaveStrategyStateAsync(CryptoTrading.Domain.Entities.StrategyState state) => Task.CompletedTask; public Task<CryptoTrading.Domain.Entities.StrategyState?> GetStrategyStateAsync(string strategyName, string symbol) => Task.FromResult<CryptoTrading.Domain.Entities.StrategyState?>(null); public Task ClearPaperTradingDataAsync()");
        File.WriteAllText(file, c);
    }
}
