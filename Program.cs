using Notificator.Extensions;

namespace Notificator;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Подгружаем и добавляем чувствительные данные из окружения
        DotNetEnv.Env.Load();

        builder.Configuration.AddEnvironmentVariables().Build();

        // Регистрируем наше приложение
        builder.Services.AddApplication(builder.Configuration);

        var app = builder.Build();

        app.UseAuthentication(); 
        app.UseAuthorization();

        app.MapControllers();

        app.Run();
    }
}