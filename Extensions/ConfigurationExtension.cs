namespace Notificator.Extensions;

public static class ConfigurationExtension
{
    public static string? GetTelegramToken(this IConfiguration configuration, string name)
        => configuration.GetSection("TelegramTokens")[name];
}