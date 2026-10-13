using Microsoft.AspNetCore.Authentication;

namespace Notificator.Services.Authentication;

public class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions
{
    public const string DefaultScheme = "ApiKey";
    public string HeaderName { get; set; } = "API-Key";
}