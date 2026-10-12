
using System.Net;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Notificator;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.

        var TelegramToken = "";

        // Добавляем HttpClient для echo сервиса
        builder.Services.AddHttpClient();

        // Этот сервис отправляет сам себе http запрос, это нужно что-бы сервис не засыпал
        // По умолчанию хостинг уводит сервис в сон каждые 15 минут
        builder.Services.AddHostedService<EchoService>();

        // Регистрируем Телеграм как синглтон, так как вызываться он
        // будет не только из сервиса рассылки, но и из сервиса отслеживающего сообщения в чате
        builder.Services.AddSingleton(_ => new TelegramBotClient(TelegramToken));
        builder.Services.AddSingleton<ITelegramBotClient>(sp => sp.GetRequiredService<TelegramBotClient>());

        // Добавляем db context в качестве базы будем юзать SQLite
        builder.Services.AddDbContext<NotificatorDbContext>(options =>
        {
            options.UseSqlite("Data source=/Users/stepansalov/Desktop/CSharp/Notificator/Notificator/Notificator.db");
        });

        // Добавляем в service collection основную логику реализованую в MessageHandler
        builder.Services.AddScoped<IMessageHandler, MessageHandler>();

        // Регистрируем сервис слежения за чатом
        builder.Services.AddHostedService<TelegramService>();

        // Регистрируем сервис уведомлений
        builder.Services.AddHostedService<NotificationService>();

        // Указывам IHost что сервисы будут запускаться и останавливаться одновременно
        builder.Services.Configure<HostOptions>(options =>
        {
            options.ServicesStartConcurrently = true;
            options.ServicesStopConcurrently = true;
        });

        builder.Services.AddControllers();

        var app = builder.Build();

        app.UseAuthorization();

        app.MapControllers();

        app.Run();
    }
}

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

        await executionService.HandleCallbackAsync(client, update.CallbackQuery, cancellationToken);
    }

    private Task OnError(Exception exception, HandleErrorSource source)
    {
        logger.LogError(exception.Message);
        return Task.CompletedTask;
    }
}

internal class EchoService(HttpClient httpClient, ILogger<EchoService> logger) : BackgroundService
{
    private readonly TimeSpan period = TimeSpan.FromMinutes(14);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(period);

        try
        {
            while(await timer.WaitForNextTickAsync(stoppingToken))
            {
                var echo = Guid.NewGuid().ToString("N")[..10];

                var response = await httpClient.GetAsync($"https://autosourse.onrender.com/api/echo?echo={echo}", stoppingToken);

                if (response.StatusCode != HttpStatusCode.OK)
                {
                    logger.LogError($"Servier error, status code: {response.StatusCode}");
                    return;
                }

                var body = response.Content.ReadAsStringAsync(stoppingToken);
            }
        }
        catch(Exception ex)
        {
            logger.LogError(ex.Message);
        }
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

internal class NotificatorDbContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<Subscriber> Subscribers { get; set; }
    public DbSet<Apartment> Apartments { get; set; }
    public DbSet<Schedule> Schedules { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var subscriber = modelBuilder.Entity<Subscriber>();
        subscriber.ToTable("subscribers");
        subscriber.HasKey(u => u.Id);
        subscriber.Property(u => u.Id).HasColumnName("id").UseAutoincrement();
        subscriber.Property(u => u.Username).HasColumnName("username");
        subscriber.Property(u => u.CreatedAt).HasColumnName("created_at");
        subscriber.Property(u => u.Deleted).HasColumnName("deleted").HasDefaultValue(false);
        subscriber.Property(u => u.IsActive).HasColumnName("is_active").HasDefaultValue(false);
        subscriber.Property(u => u.ApartmentId).HasColumnName("apartment_id");
        subscriber.HasOne(u => u.Apartment).WithMany(u => u.Subscribers).HasForeignKey(u => u.ApartmentId);

        var apartment = modelBuilder.Entity<Apartment>();
        apartment.HasKey(u => u.Id);
        apartment.Property(u => u.Id).HasColumnName("id").UseAutoincrement();
        apartment.Property(u => u.Name).HasColumnName("name").HasMaxLength(400); // 400 символов для адреса должно хватить
        apartment.HasMany(u => u.Subscribers).WithOne(u => u.Apartment);

        var schedule = modelBuilder.Entity<Schedule>();
        schedule.HasKey(u => u.Id);
        schedule.Property(u => u.Id).HasColumnName("id").UseAutoincrement();
        schedule.Property(u => u.Description).HasColumnName("description").HasMaxLength(400);

        base.OnModelCreating(modelBuilder);
    }
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
