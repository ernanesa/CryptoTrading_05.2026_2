using System.Reflection;
using DbUp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CryptoTrading.Infrastructure.Persistence;

public static class DatabaseMigrator
{
    public static void Migrate(string connectionString, ILogger? logger = null)
    {
        var upgrader = DeployChanges.To
            .PostgresqlDatabase(connectionString)
            .WithScriptsEmbeddedInAssembly(Assembly.GetExecutingAssembly(), script => script.StartsWith("CryptoTrading.Infrastructure.Persistence.Migrations.Scripts."))
            .LogToConsole()
            .Build();

        var result = upgrader.PerformUpgrade();

        if (!result.Successful)
        {
            logger?.LogError(result.Error, "Falha na migração do banco de dados (DbUp).");
            throw new Exception("Falha na migração do banco de dados (DbUp).", result.Error);
        }
        
        logger?.LogInformation("Migrações de banco de dados aplicadas com sucesso.");
    }
}
