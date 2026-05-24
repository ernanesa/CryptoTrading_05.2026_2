using System;
using System.Linq;
using System.Reflection;

class Program {
    static void Main() {
        var a1 = Assembly.Load("Binance.Net");
        var a2 = Assembly.Load("CryptoExchange.Net");
        foreach(var t in a1.GetTypes().Concat(a2.GetTypes())) {
            if (t.BaseType != null && t.BaseType.Name.Contains("ApiCredentials")) Console.WriteLine("Base: " + t.FullName);
            if (t.Name.Contains("Credentials")) Console.WriteLine("Name: " + t.FullName);
        }
    }
}
