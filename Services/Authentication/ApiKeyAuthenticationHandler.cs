using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Notificator.Extensions;

namespace Notificator.Services.Authentication;

public class ApiKeyAuthenticationHandler(
    IOptionsMonitor<ApiKeyAuthenticationOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IConfiguration configuration) : AuthenticationHandler<ApiKeyAuthenticationOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("API-Key", out var apiKey))
        {
            return Task.FromResult(AuthenticateResult.Fail("API Key отсутствует."));
        }

        var hash = configuration.GetApiKeyHash("Default");

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(apiKey.ToString()));
        var inputHash =  Convert.ToHexString(bytes);

        if (!string.Equals(hash, inputHash))
        {
            return Task.FromResult(AuthenticateResult.Fail("Неверный или просроченный API Key."));
        }

        Claim[] claims = [
            new Claim(ClaimTypes.Name, "ApiAccess")
        ];

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
