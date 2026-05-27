using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace CryptoTrading.Api.Services;

public class AuthMiddleware
{
    private readonly RequestDelegate _next;

    public AuthMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, TokenService tokenService)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        // 1. Permitir endpoints públicos sem autenticação
        if (path.Equals("/health", StringComparison.OrdinalIgnoreCase) ||
            path.Equals("/api/auth/login", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/openapi/", StringComparison.OrdinalIgnoreCase) ||
            context.Request.Method.Equals("OPTIONS", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        // 2. Extrair o token do cabeçalho Authorization ou do parâmetro SignalR Query String
        var token = string.Empty;
        var authHeader = context.Request.Headers.Authorization.ToString();
        
        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            token = authHeader["Bearer ".Length..].Trim();
        }
        else if (context.Request.Query.TryGetValue("access_token", out var queryToken))
        {
            token = queryToken.ToString();
        }

        // 3. Validar token
        if (tokenService.ValidateToken(token, out var payload) && payload != null)
        {
            // Criar ClaimsPrincipal básico para compatibilidade opcional com autorizações nativas
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, payload.Username),
                new Claim(ClaimTypes.Role, payload.Role)
            };
            var identity = new ClaimsIdentity(claims, "SecureTokenAuth");
            context.User = new ClaimsPrincipal(identity);
            
            // Armazenar payload nos itens do HttpContext para facilidade de uso
            context.Items["UserPayload"] = payload;

            await _next(context);
        }
        else
        {
            // Retornar 401 caso o token não seja fornecido ou seja inválido
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync("{\"Message\": \"Acesso não autorizado: Token JWT inválido, expirado ou ausente.\"}");
        }
    }
}
