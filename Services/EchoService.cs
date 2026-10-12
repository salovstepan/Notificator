
using System.Net;

namespace Notificator.Services;

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
