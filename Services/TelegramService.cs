using Notificator.Applications.Abstractions;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Notificator.Services;

internal class TelegramService(TelegramBotClient client, ILogger<TelegramService> logger, IServiceScopeFactory scopeFactory) : BackgroundService
{
    private CancellationToken cancellationToken;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        cancellationToken = stoppingToken;

        var bot = await client.GetMe(cancellationToken: stoppingToken);

        logger.LogInformation($"I am user {bot.Id} and my name is {bot.FirstName}.");

        client.OnMessage += OnMessage;
        client.OnUpdate += OnUpdate;
        client.OnError += OnError;
        
        stoppingToken.ThrowIfCancellationRequested();
    }

    private async Task OnMessage(Message message, UpdateType type)
    {
        using var scope = scopeFactory.CreateScope();

        var executionService = scope.ServiceProvider.GetRequiredService<IMessageHandler>()
            ?? throw new Exception("Try to get the ExecutionService is failure");

        await executionService.HandleMessageAsync(client, message, cancellationToken);
    }

    private async Task OnUpdate(Update update)
    {
        using var scope = scopeFactory.CreateScope();

        var executionService = scope.ServiceProvider.GetRequiredService<IMessageHandler>()
            ?? throw new Exception("Try to get the ExecutionService is failure");

        await executionService.HandleCallbackAsync(client, update, cancellationToken);
    }

    private Task OnError(Exception exception, HandleErrorSource source)
    {
        logger.LogError(exception.Message);
        return Task.CompletedTask;
    }
}
