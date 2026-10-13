using Microsoft.EntityFrameworkCore;
using Notificator.DataSources;

namespace Notificator.Extensions;

public static class MigrationExtensions
{
    public static async Task MigrationsAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        
        var context = scope.ServiceProvider.GetRequiredService<NotificatorDbContext>();

        var migrations = await context.Database.GetPendingMigrationsAsync();

        if (migrations.Any())
        {
            await context.Database.MigrateAsync();
        }
    }
}