using Notificator.Extensions;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace Notificator;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Регистрируем наше приложение
        builder.Services.AddApplication(builder.Configuration);

        var app = builder.Build();

        app.UseAuthorization();

        app.MapControllers();

        app.Run();
    }
}

internal class MessageHandler : IMessageHandler
{
    public Task HandleCallbackAsync(TelegramBotClient client, CallbackQuery? callback, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task HandleMessageAsync(TelegramBotClient client, Message message, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}

internal interface IMessageHandler
{
    public Task HandleMessageAsync(TelegramBotClient client, Message message, CancellationToken cancellationToken);

    public Task HandleCallbackAsync(TelegramBotClient client, CallbackQuery? callback, CancellationToken cancellationToken);
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
    public int Id { get; set; }
    public required string Name { get; set; }
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
    public int ApartmentId { get; set; }
}
