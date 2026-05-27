using System;
using System.Collections.Generic;
using CryptoTrading.Domain.Entities;

namespace CryptoTrading.Application.Services;

public class FeatureSchemaRegistry
{
    private static readonly FeatureSchemaVersion DefaultSchema = new();

    public FeatureSchemaVersion GetSchema(string schemaName)
    {
        if (schemaName.Equals("intelligence-snapshot/v1", StringComparison.OrdinalIgnoreCase))
        {
            return DefaultSchema;
        }

        return new FeatureSchemaVersion
        {
            Version = schemaName,
            Source = "Unknown",
            Fields = new List<string>()
        };
    }
}
