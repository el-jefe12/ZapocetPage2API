using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TexasGuyContractAPI.Interface;
using TexasGuyContractIdentity.Data;
using Microsoft.EntityFrameworkCore;

public class WaterStationSchedulerService : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<WaterStationSchedulerService> _logger;
    private readonly ConcurrentDictionary<int, Timer> _timers = new(); // Track timers for each station

    public WaterStationSchedulerService(IServiceScopeFactory serviceScopeFactory, ILogger<WaterStationSchedulerService> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Load stations and initialize timers
        await LoadStationsAsync(stoppingToken);

        // Wait indefinitely
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task LoadStationsAsync(CancellationToken stoppingToken)
    {
        try
        {
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var stations = await dbContext.StationsEntries
                    .Where(ws => ws.isEnabled)
                    .ToListAsync(stoppingToken);

                foreach (var station in stations)
                {
                    if (!_timers.ContainsKey(station.ID))
                    {
                        var interval = TimeSpan.FromMinutes(station.Minutes);
                        var timer = new Timer(
                            async _ => await ProcessStationAsync(station.ID),
                            null,
                            TimeSpan.Zero,
                            interval
                        );
                        _timers[station.ID] = timer;
                        _logger.LogInformation($"Scheduled station ID: {station.ID} with interval: {interval.TotalMinutes} minutes");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while loading stations");
        }
    }

    private async Task ProcessStationAsync(int stationId)
    {
        try
        {
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var waterStationService = scope.ServiceProvider.GetRequiredService<IWaterStationService>();
                _logger.LogInformation($"Processing station ID: {stationId}");
                await waterStationService.CheckAndLogWaterStationsAsync(stationId); // Pass the stationId here
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"An error occurred while processing station ID: {stationId}");
        }
    }
}
