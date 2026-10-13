using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Notificator.Applications;
using Notificator.Applications.Abstractions;
using Notificator.DataSources;
using Notificator.Services;
using Telegram.Bot;

namespace Notificator.Extensions;

public static class ServiceExtention
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        // Не забудь перенести чувствительные данные из appsettings.json в .env мой друг
        var TelegramToken = configuration.GetTelegramToken("Default")
            ?? throw new Exception("TelegramTokens:Default is not defined!");
            
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new Exception("ConnectionStrings:Default is not defined!");

        // Добавляем db context в качестве базы будем юзать SQLite
        services.AddDbContext<NotificatorDbContext>(options =>
        {
            options.UseSqlite(connectionString);
        });

        // Добавляем HttpClient для echo сервиса
        services.AddHttpClient();

        // Этот сервис отправляет сам себе http запрос, это нужно что-бы сервис не засыпал
        // По умолчанию хостинг уводит сервис в сон каждые 15 минут
        services.AddHostedService<EchoService>();

        // Регистрируем Телеграм как синглтон, так как вызываться он
        // будет не только из сервиса рассылки, но и из сервиса отслеживающего сообщения в чате
        services.AddSingleton(_ => new TelegramBotClient(TelegramToken));
        services.AddSingleton<ITelegramBotClient>(sp => sp.GetRequiredService<TelegramBotClient>());

        // Добавляем в service collection основную логику реализованую в MessageHandler
        services.AddScoped<IMessageHandler, MessageHandler>();

        // Регистрируем сервис слежения за чатом
        services.AddHostedService<TelegramService>();

        // Регистрируем сервис уведомлений
        services.AddHostedService<NotificationService>();

        // Указывам IHost что сервисы будут запускаться и останавливаться одновременно
        services.Configure<HostOptions>(options =>
        {
            options.ServicesStartConcurrently = true;
            options.ServicesStopConcurrently = true;
        });

        services.AddControllers().AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            });
        
        return services;
    }
}