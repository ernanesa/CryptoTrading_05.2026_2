using System;
using System.Linq;
using System.Reflection;
using CryptoExchange.Net.Authentication;

public class Test {
    public static void Main() {
        var assembly = Assembly.Load("Binance.Net");
        var types = assembly.GetTypes().Where(t => typeof(ApiCredentials).IsAssignableFrom(t) || t.Name.Contains("Credentials")).Select(t => t.FullName);
        foreach (var type in types) Console.WriteLine(type);
    }
}
