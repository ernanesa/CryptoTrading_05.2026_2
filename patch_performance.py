import re

with open('src/Application/Services/PerformanceAnalyzer.cs', 'r') as f:
    content = f.read()

content = content.replace(
"""                    // Sortino ratio
                    // Not formally tracked in report yet, but can be added if BacktestReport has the property
                    // report.SortinoRatio = ...""",
"""                    // Sortino ratio
                    report.SortinoRatio = (meanReturn / downsideDev) * (decimal)Math.Sqrt((double)returns.Count);"""
)

content = content.replace(
"""        // Calmar Ratio
        // Typically Annualized Return / Max Drawdown
        if (report.MaxDrawdownPercent > 0)
        {
            // report.CalmarRatio = report.TotalPnLPercent / report.MaxDrawdownPercent;
        }""",
"""        // Calmar Ratio
        if (report.MaxDrawdownPercent > 0)
        {
            report.CalmarRatio = report.TotalPnLPercent / report.MaxDrawdownPercent;
        }"""
)

with open('src/Application/Services/PerformanceAnalyzer.cs', 'w') as f:
    f.write(content)

