using Microsoft.EntityFrameworkCore;
using Notificator.DataSources;
using Notificator.Extensions;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

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
        if (message.Text is null) return;

        if (message.From is null) return;

        if (message.Text.StartsWith("/addme"))
        {
            var replay = "";

            var chatId = message.Chat.Id;

            if (chatId < 0)
            {
                chatId = -message.Chat.Id;
            }

            var apartment = await dbContext.Apartments
                .AsNoTracking()
                .Include(u => u.Subscribers)
                .FirstOrDefaultAsync(u => u.Id == chatId, cancellationToken);

            if (apartment is null) return;

            var subscriber = apartment.Subscribers.FirstOrDefault(u => u.Id == message.From.Id);

            if (subscriber is null)
            {
                subscriber = new Subscriber
                {
                    Id = message.From.Id,
                    Name = message.From.FirstName,
                    ApartmentId = apartment.Id,
                    Username = message.From.Username ?? "no name",
                };
                await dbContext.Subscribers.AddAsync(subscriber, cancellationToken);
                await dbContext.SaveChangesAsync(cancellationToken);

                replay = $"Пользователь @{subscriber.Username} успешно добавлен";
            }
            else
            {
                replay = $"Пользователь @{subscriber.Username} уже существует";
            }

            await client.SendMessage(message.Chat.Id, replay, cancellationToken: cancellationToken);
        }
    
        if (message.Text.StartsWith("/users"))
        {
            var chatId = message.Chat.Id;

            if (chatId < 0)
            {
                chatId = -message.Chat.Id;
            }

            var users = await dbContext.Subscribers
                .Where(u => !u.Deleted)
                .AsNoTracking()
                .Select(u => InlineKeyboardButton.WithCallbackData($"{u.Name} (@{u.Username})", $"user:{u.Id}"))
                .ToListAsync(cancellationToken);

            var replay = "";

            if (users.Count() > 0)
            {
                replay = "Выбирите пользователя из списка, для дальнейших действий";
            }
            else
            {
                replay = "Список пользователей пуст";
            }

            await client.SendMessage(message.Chat.Id, replay,
                replyMarkup: new InlineKeyboardMarkup(users),
                cancellationToken: cancellationToken);
        }

        logger.LogInformation("выполненна команда");
    
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

            if (update.CallbackQuery.Message is null) return;

            var chatId = update.CallbackQuery.Message.Chat.Id;

            if (chatId < 0)
            {
                chatId = -update.CallbackQuery.Message.Chat.Id;
            }

            var data = update.CallbackQuery.Data?.Trim() ?? "";

            long userId = 0;

            if (data.StartsWith("delete:") && long.TryParse(data.AsSpan(7), out userId))
            {
                var user = await dbContext.Subscribers
                    .FirstOrDefaultAsync(u => u.Id == userId && u.ApartmentId == chatId && !u.Deleted, cancellationToken);

                if (user is null)
                {
                    logger.LogInformation($"Пользователь id:{userId} не найден");
                    return;
                }

                user.Deleted = true;

                await dbContext.SaveChangesAsync(cancellationToken);

                await client.SendMessage(update.CallbackQuery.Message.Chat.Id,
                    $"Пользователь {user.Name} (@{user.Username}) удален из расписания, график сместился",
                    cancellationToken: cancellationToken);
            }

            if (data.StartsWith("active:") && long.TryParse(data.AsSpan(9), out userId))
            {
                var user = await dbContext.Subscribers
                    .FirstOrDefaultAsync(u => u.Id == userId && u.ApartmentId == chatId && !u.Deleted, cancellationToken);

                if (user is null)
                {
                    logger.LogInformation($"Пользователь id:{userId} не найден");
                    return;
                }

                user.IsActive = data.Substring(7, 1) == "1";

                await dbContext.SaveChangesAsync(cancellationToken);

                var replay = "";

                if (user.IsActive)
                {
                    replay = "Пользователь отмечен как вернувшийся, теперь он снова будет получать уведомления и вернется прежний график";
                }
                else
                {
                    replay = "Пользователь отмечен как отсутствующий, теперь он не будет получать уведомления, а график сместиться";
                }

                await client.SendMessage(update.CallbackQuery.Message.Chat.Id, replay, cancellationToken: cancellationToken);
            }

            if (data.StartsWith("user:") && long.TryParse(data.AsSpan(5), out userId))
            {
                var user = await dbContext.Subscribers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == userId && u.ApartmentId == chatId && !u.Deleted, cancellationToken);

                if (user is null)
                {
                    logger.LogInformation($"Пользователь id:{userId} не найден");
                    return;
                }

                List<InlineKeyboardButton[]> actions = [
                    [InlineKeyboardButton.WithCallbackData("Посмотреть расписание", $"schedule:{user.Id}")]                    
                ];

                if (user.IsActive)
                {
                    actions.Add([InlineKeyboardButton.WithCallbackData("Отметить как отсутствующего", $"active:0:{user.Id}")]);
                }
                else
                {
                    actions.Add([InlineKeyboardButton.WithCallbackData("Отметить как вернувшегося", $"active:1:{user.Id}")]);
                }

                actions.Add([InlineKeyboardButton.WithCallbackData("Удалить пользователя", $"delete:{user.Id}")]);

                await client.SendMessage(update.CallbackQuery.Message.Chat.Id,
                    "Выбирите действие",
                    replyMarkup: new InlineKeyboardMarkup(actions),
                    cancellationToken: cancellationToken);
            }
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
    public required string Name { get; set; }
    public required string Username { get; set; }
    public DateTime CreatedAt { get; } = DateTime.UtcNow;
    public bool Deleted { get; set; } // Удален, когад пользователь выселился
    public bool IsActive { get; set; } // не активен когда пользователь временно отсутствует
    public Apartment? Apartment { get; set; }
    public long ApartmentId { get; set; }
}
