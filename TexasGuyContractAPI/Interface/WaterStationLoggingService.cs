using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TexasGuyContractAPI.Interface;
using TexasGuyContractIdentity.Data;
using Microsoft.EntityFrameworkCore;

public class WaterStationLoggingService : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<WaterStationLoggingService> _logger;
    private readonly Dictionary<int, DateTime> _nextExecutionTimes = new(); // Tracks next execution time for each station

    public WaterStationLoggingService(IServiceScopeFactory serviceScopeFactory, ILogger<WaterStationLoggingService> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("WaterStationLoggingService is running...");
            await ProcessStationsAsync(stoppingToken);
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken); // Check every 10 seconds or as needed
        }
    }

    private async Task ProcessStationsAsync(CancellationToken stoppingToken)
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
                    var now = DateTime.UtcNow;
                    var nextExecutionTime = _nextExecutionTimes.GetValueOrDefault(station.ID, now);

                    if (now >= nextExecutionTime)
                    {
                        _logger.LogInformation($"Processing station ID: {station.ID}");

                        var waterStationService = scope.ServiceProvider.GetRequiredService<IWaterStationService>();
                        await waterStationService.CheckAndLogWaterStationsAsync(station.ID); // Pass the station ID

                        // Calculate next execution time
                        _nextExecutionTimes[station.ID] = now.AddMinutes(station.Minutes);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while processing water stations");
        }
    }
}
