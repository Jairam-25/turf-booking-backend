using Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure.Services;

/// <summary>
/// Hosted background service that:
/// 1. On startup  — generates 7 days of slots for every existing turf (idempotent backfill).
/// 2. Every 24 h  — adds the next day's slots so the rolling window is always 7 days ahead.
/// </summary>
public sealed class DailySlotGeneratorService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DailySlotGeneratorService> _logger;

    // How many future days to keep filled with slots
    private const int RollingWindowDays = 7;

    // How long to wait between daily runs (24 h)
    private static readonly TimeSpan RunInterval = TimeSpan.FromHours(24);

    public DailySlotGeneratorService(
        IServiceScopeFactory scopeFactory,
        ILogger<DailySlotGeneratorService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "[DailySlotGenerator] Service starting. Will backfill {Days}-day window on startup, " +
            "then refresh every {Hours}h.", RollingWindowDays, RunInterval.TotalHours);

        // --- Startup backfill (covers all existing turfs that have no slots) ---
        await RunGenerationAsync(stoppingToken);

        // --- Daily loop ---
        using var timer = new PeriodicTimer(RunInterval);

        while (!stoppingToken.IsCancellationRequested &&
               await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RunGenerationAsync(stoppingToken);
        }
    }

    private async Task RunGenerationAsync(CancellationToken ct)
    {
        try
        {
            // ISlotService is Scoped, so we must create a new scope
            await using var scope = _scopeFactory.CreateAsyncScope();
            var slotService = scope.ServiceProvider.GetRequiredService<ISlotService>();

            _logger.LogInformation(
                "[DailySlotGenerator] Generating slots for next {Days} days ...",
                RollingWindowDays);

            await slotService.GenerateSlotsForAllTurfsAsync(RollingWindowDays, ct);

            _logger.LogInformation("[DailySlotGenerator] Slot generation run complete.");
        }
        catch (OperationCanceledException)
        {
            // App is shutting down — expected, don't log as error
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[DailySlotGenerator] Slot generation run failed.");
        }
    }
}
