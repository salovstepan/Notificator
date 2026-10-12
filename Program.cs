using Microsoft.EntityFrameworkCore;
using Notificator.DataSources;
using Notificator.Extensions;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Notificator;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        DotNetEnv.Env.Load();

        builder.Configuration.AddEnvironmentVariables().Build();

        // Регистрируем наше приложение
        builder.Services.AddApplication(builder.Configuration);

        var app = builder.Build();

        app.UseAuthorization();

        app.MapControllers();

        app.Run();
    }
}

internal class MessageHandler(NotificatorDbContext dbContext, ILogger<MessageHandler> logger) : IMessageHandler
{
    public async Task HandleMessageAsync(TelegramBotClient client, Message message, CancellationToken cancellationToken)
    {

        if (message.Text is null)
        {
            return;
        }

        if (message.Text.StartsWith("/add"))
        {
            var substr = message.Text.Split(' ');

            if (substr.Length < 2) return;

            var name = substr[1];
        }

        var replay = "nothing";

        try
        {
            await client.SendMessage(message.Chat.Id, replay, cancellationToken: cancellationToken);
        }
        catch(Exception ex)
        {
            logger.LogError(ex.Message);
        }
    }

    public async Task HandleCallbackAsync(TelegramBotClient client, Update update, CancellationToken cancellationToken)
    {
        var bot = await client.GetMe(cancellationToken);

        if (update.MyChatMember is not null)
        {   
            var chatId = update.MyChatMember.Chat.Id;

            // безпонятия, всегда ли chat.id имеет отрицательное число
            if (chatId < 0)
            {
                chatId = -update.MyChatMember.Chat.Id;
            }

            // а этот параметр заполнен даже если я удаляю бота из чата я хз что тут происходит
            var added = update.MyChatMember.NewChatMember.User.Id == bot.Id;
            // этот параметр всегда заполнен, даже если я добавляю а не удаляю бота из чата
            // думаю его пока не учитывать, может баг, может я чего-то не понимаю
            // var deleted = update.MyChatMember.OldChatMember.User.Id == bot.Id;
            // var deleted = update.MyChatMember.OldChatMember.Status == ChatMemberStatus.Left;
            // разобрался, лишние комментарии не забыть бы удалить
            var deleted = update.MyChatMember.NewChatMember.Status == ChatMemberStatus.Left;;

            if (added || deleted)
            {
                var apartment = await dbContext.Apartments.FirstOrDefaultAsync(u => u.Id == chatId, cancellationToken);

                if (apartment is null)
                {
                    apartment = new Apartment
                    {
                        Id = chatId,
                        Name = update.MyChatMember.Chat.Title ?? "new chat",
                    };
                    await dbContext.Apartments.AddAsync(apartment, cancellationToken);
                }

                if (deleted)
                {
                    apartment.Deleted = deleted;
                }

                await dbContext.SaveChangesAsync(cancellationToken);
            }
        }

        if (update.CallbackQuery is not null)
        {
            await client.AnswerCallbackQuery(update.CallbackQuery.Id, cancellationToken: cancellationToken);
        }
    }
}

internal interface IMessageHandler
{
    public Task HandleMessageAsync(TelegramBotClient client, Message message, CancellationToken cancellationToken);

    public Task HandleCallbackAsync(TelegramBotClient client, Update update, CancellationToken cancellationToken);
}

// Entity для хранения расписания
public class Schedule
{
    public int Id { get; set; }
    public required string Description { get; set; } // тут будет хранится описания того, что нужно делать постояльцу
}

// Entity для хранения квартир
public class Apartment
{
    public long Id { get; set; }
    public required string Name { get; set; }
    public bool Deleted { get; set; }
    public ICollection<Subscriber> Subscribers { get; set; } = []; // инициализируем для удобвства
}

// Entity для хранения пользователей
public class Subscriber
{
    public long Id { get; set; }
    public required string Username { get; set; }
    public DateTime CreatedAt { get; } = DateTime.UtcNow;
    public bool Deleted { get; set; } // Удален, когад пользователь выселился
    public bool IsActive { get; set; } // не активен когда пользователь временно отсутствует
    public Apartment? Apartment { get; set; }
    public long ApartmentId { get; set; }
}
