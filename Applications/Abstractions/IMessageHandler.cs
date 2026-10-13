using Telegram.Bot;
using Telegram.Bot.Types;

namespace Notificator.Applications.Abstractions;

internal interface IMessageHandler
{
    public Task HandleMessageAsync(TelegramBotClient client, Message message, CancellationToken cancellationToken);

    public Task HandleCallbackAsync(TelegramBotClient client, Update update, CancellationToken cancellationToken);
}
