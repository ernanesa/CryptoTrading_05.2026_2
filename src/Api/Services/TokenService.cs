using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace CryptoTrading.Api.Services;

public class TokenService
{
    private readonly string _jwtSecret;
    private readonly string _adminUsername;
    private readonly string _adminPasswordHash; // Armazenado como hash SHA256 para máxima segurança

    public TokenService(IConfiguration configuration)
    {
        // Secret para assinatura criptográfica HMAC-SHA256
        _jwtSecret = configuration["Security:JwtSecret"] ?? "CryptoTrading_Secret_Key_2026_Secure_HMAC_SHA256_Signature";
        _adminUsername = configuration["Security:Username"] ?? "admin";
        
        // Carrega a senha do appsettings ou usa um fallback seguro
        var plainPassword = configuration["Security:Password"] ?? "CryptoAdmin2026!";
        _adminPasswordHash = HashPassword(plainPassword);
    }

    public string? Authenticate(string username, string password)
    {
        if (!string.Equals(username, _adminUsername, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var inputHash = HashPassword(password);
        if (!string.Equals(inputHash, _adminPasswordHash, StringComparison.Ordinal))
        {
            return null;
        }

        // Gerar token assinado
        var expiration = DateTime.UtcNow.AddHours(8); // Expiração em 8h
        var payload = new TokenPayload
        {
            Username = username,
            Role = "Admin",
            ExpiresAt = expiration
        };

        var json = JsonSerializer.Serialize(payload);
        var payloadBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
        var signature = GenerateSignature(payloadBase64);

        return $"{payloadBase64}.{signature}";
    }

    public bool ValidateToken(string? token, out TokenPayload? payload)
    {
        payload = null;
        if (string.IsNullOrWhiteSpace(token)) return false;

        var parts = token.Split('.');
        if (parts.Length != 2) return false;

        var payloadBase64 = parts[0];
        var signature = parts[1];

        // Verificar assinatura criptográfica
        var expectedSignature = GenerateSignature(payloadBase64);
        if (!string.Equals(signature, expectedSignature, StringComparison.Ordinal))
        {
            return false;
        }

        try
        {
            var jsonBytes = Convert.FromBase64String(payloadBase64);
            var json = Encoding.UTF8.GetString(jsonBytes);
            payload = JsonSerializer.Deserialize<TokenPayload>(json);

            if (payload == null || payload.ExpiresAt < DateTime.UtcNow)
            {
                return false;
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    private string GenerateSignature(string base64Payload)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_jwtSecret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(base64Payload));
        return Convert.ToBase64String(hash)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('='); // Base64Url-like
    }

    public static string HashPassword(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        var builder = new StringBuilder();
        foreach (var b in bytes)
        {
            builder.Append(b.ToString("x2"));
        }
        return builder.ToString();
    }
}

public class TokenPayload
{
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}
