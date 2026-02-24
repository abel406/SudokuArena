using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using SudokuArena.Application.Abstractions.Repositories;
using SudokuArena.Sync.Worker.Configuration;

namespace SudokuArena.Sync.Worker.Workers;

public sealed class OutboxSyncWorker(
    ILogger<OutboxSyncWorker> logger,
    IOutboxRepository outboxRepository,
    IHttpClientFactory httpClientFactory,
    IOptions<CloudSyncOptions> optionsAccessor) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var options = optionsAccessor.Value;
        if (string.IsNullOrWhiteSpace(options.BaseUrl))
        {
            logger.LogWarning("CloudSync.BaseUrl is empty. Worker will stay idle.");
            return;
        }

        var client = httpClientFactory.CreateClient("cloud-sync");
        client.BaseAddress = new Uri(options.BaseUrl);
        if (!string.IsNullOrWhiteSpace(options.ApiToken))
        {
            client.DefaultRequestHeaders.Authorization = new("Bearer", options.ApiToken);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var pending = await outboxRepository.DequeuePendingAsync(50, stoppingToken);
                foreach (var evt in pending)
                {
                    var response = await client.PostAsJsonAsync(
                        "/api/sync/events",
                        new { evt.Id, evt.EventType, evt.Payload, evt.CreatedUtc },
                        stoppingToken);

                    if (response.IsSuccessStatusCode)
                    {
                        await outboxRepository.MarkAsSyncedAsync(evt.Id, stoppingToken);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error while sending outbox events to cloud.");
            }

            await Task.Delay(TimeSpan.FromSeconds(Math.Max(2, options.PollSeconds)), stoppingToken);
        }
    }
}
