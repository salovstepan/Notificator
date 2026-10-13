using Microsoft.EntityFrameworkCore;
using Notificator.DataSources;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace Notificator.Services;

internal class NotificationService(TelegramBotClient client, ILogger<TelegramService> logger, IServiceScopeFactory scopeFactory) : BackgroundService
{
    private readonly TimeSpan period = TimeSpan.FromMinutes(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(period);

        using var scope = scopeFactory.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<NotificatorDbContext>()
            ?? throw new Exception("Try to get the dbContext is failure");

        try
        {
            while(await timer.WaitForNextTickAsync(stoppingToken))
            {
                continue;

                var now = DateTime.UtcNow.AddHours(2);

                if (now.Hour > 9) continue;

                var apartments = await dbContext.Apartments.AsNoTracking().Where(u => !u.Deleted).ToListAsync(stoppingToken);

                foreach (var apartment in apartments)
                {
                    var schedule = await dbContext.Schedules
                        .FirstOrDefaultAsync(u => u.ApartmentId == apartment.Id && u.Day == now.DayOfWeek, stoppingToken);

                    if (schedule is null) continue;

                    if (schedule.LastNotification.Date == now.Date) continue;

                    var users = await dbContext.Subscribers
                        .OrderBy(u => u.Order)
                        .Where(u => !u.Deleted && u.IsActive).ToListAsync(stoppingToken);

                    if (users is null) continue;

                    var user = users.FirstOrDefault(u => u.IsCurrent);

                    if (user is null)
                    {
                        user = users.FirstOrDefault();
                    }

                    if (user is null) continue;

                    var message = $"📋 Задание на сегодня ({now:hh.dd.yyyy})\n" +
                                    $"👤 Ответственный: {user.Name} (@{user.Username})\n" +
                                    $"🎯 Задача: {schedule.Description}";
                    
                    await client.SendMessage(-apartment.Id, message, parseMode: ParseMode.Markdown, cancellationToken: stoppingToken);

                    var next = users.FirstOrDefault(u => u.Order > user.Order);

                    if (next is null)
                    {
                        next = users.First();
                    }

                    schedule.LastNotification = now;
                    user.IsCurrent = false;
                    next.IsCurrent = true;

                    await dbContext.SaveChangesAsync(stoppingToken);
                }
            }
        }
        catch(Exception ex)
        {
            logger.LogError(ex.Message);
        }
    }
}
