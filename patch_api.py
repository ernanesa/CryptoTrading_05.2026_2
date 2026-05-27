import re

with open('src/Api/Program.cs', 'r') as f:
    content = f.read()

endpoints = """
// 5. Listar reports
app.MapGet("/api/backtest/reports", async (IBacktestRepository backtestRepo, int limit = 50) =>
{
    var reports = await backtestRepo.GetReportsAsync(limit);
    return Results.Ok(reports);
})
.WithName("GetBacktestReports")
.WithOpenApi();

app.MapGet("/api/backtest/reports/latest", async (IBacktestRepository backtestRepo, string strategy, string symbol) =>
{
    var report = await backtestRepo.GetLatestReportAsync(strategy, symbol);
    return report != null ? Results.Ok(report) : Results.NotFound();
})
.WithName("GetLatestBacktestReport")
.WithOpenApi();
"""

content = content.replace("app.MapGet(\"/api/paper/wallet\", async (IFeatureStore store) =>", endpoints + "\napp.MapGet(\"/api/paper/wallet\", async (IFeatureStore store) =>")

with open('src/Api/Program.cs', 'w') as f:
    f.write(content)
