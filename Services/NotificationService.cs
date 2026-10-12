using Notificator.DataSources;
using Telegram.Bot;

namespace Notificator.Services;

internal class NotificationService(TelegramBotClient client, ILogger<TelegramService> logger, IServiceScopeFactory scopeFactory) : BackgroundService
{
    private readonly TimeSpan period = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(period);

        using var scope = scopeFactory.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<NotificatorDbContext>()
            ?? throw new Exception("Try to get the dbContext is failure");

        var subscriberId = 1000;
        var text = "";

        try
        {
            while(await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    await client.SendMessage(subscriberId, text, cancellationToken: stoppingToken);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to send reminder to chat {ChatId}", subscriberId);
                    continue;
                }
            }
        }
        catch(Exception ex)
        {
            logger.LogError(ex.Message);
        }
    }
}
