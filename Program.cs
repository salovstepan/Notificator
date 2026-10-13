using Notificator.Extensions;

namespace Notificator;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Подгружаем чувствительные данные из окружения
        DotNetEnv.Env.Load();

        // Добавляем чувствительные данные
        builder.Configuration.AddEnvironmentVariables().Build();

        // Регистрируем наше приложение
        builder.Services.AddApplication(builder.Configuration);

        var app = builder.Build();

        app.UseAuthorization();

        app.MapControllers();

        app.Run();
    }
}
