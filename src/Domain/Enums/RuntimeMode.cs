using System;

namespace CryptoTrading.Domain.Enums;

public enum RuntimeMode
{
    Offline,
    Simulation,
    Paper,
    TestnetDryRun,
    TestnetReal
}
