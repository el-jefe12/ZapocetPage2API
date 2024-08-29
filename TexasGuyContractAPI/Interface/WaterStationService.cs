using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using TexasGuyContractIdentity.Models;
using TexasGuyContractIdentity.Data;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Identity.UI.Services;
using TexasGuyContractAPI.Interface;

public class WaterStationService : IWaterStationService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<WaterStationService> _logger;
    private readonly IEmailSender _emailSender;

    private readonly string _errorNotificationEmail = "admin@texasguycontract.com";
    private readonly string _droughtWarningEmail = "drought-alerts@texasguycontract.com";
    private readonly string _floodWarningEmail = "flood-alerts@texasguycontract.com";

    public WaterStationService(ApplicationDbContext context, ILogger<WaterStationService> logger, IEmailSender emailSender)
    {
        _context = context;
        _logger = logger;
        _emailSender = emailSender;
    }

    public async Task CheckAndLogWaterStationsAsync(int stationId)
    {
        _logger.LogInformation($"Starting CheckAndLogWaterStationsAsync for station ID: {stationId}");

        try
        {
            var station = await _context.StationsEntries.FindAsync(stationId);
            if (station == null || !station.isEnabled)
            {
                _logger.LogWarning($"Station ID: {stationId} is either disabled or does not exist.");
                return;
            }

            _logger.LogInformation($"Processing station ID: {stationId}");

            if (ShouldLogEntry(station.Minutes, stationId))
            {
                var waterHeight = new Random().Next(0, 101);
                var historyEntry = new History
                {
                    Timestamp = DateTime.UtcNow,
                    StationID = stationId,
                    Value = waterHeight
                };

                _logger.LogInformation($"Creating history entry for station ID: {stationId} with value: {waterHeight}");

                _context.HistoryEntries.Add(historyEntry);

                await CheckAndSendWarningsAsync(station, waterHeight);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Saved changes to the database.");
            }
            else
            {
                _logger.LogInformation($"No entry created for station ID: {stationId}.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"An error occurred while processing station ID: {stationId}");
            await _emailSender.SendEmailAsync(
                _errorNotificationEmail,
                "Error in Water Station Service",
                $"An error occurred in WaterStationService for station ID: {stationId}: {ex.Message}\n\nStack Trace: {ex.StackTrace}"
            );
            throw;
        }
    }

    private bool ShouldLogEntry(int minutes, int stationId)
    {
        var lastEntry = _context.HistoryEntries
            .Where(h => h.StationID == stationId)
            .OrderByDescending(h => h.Timestamp)
            .FirstOrDefault();

        var shouldLog = lastEntry == null || (DateTime.UtcNow - lastEntry.Timestamp).TotalMinutes >= minutes;

        _logger.LogInformation($"Station ID: {stationId}, ShouldLogEntry: {shouldLog}");
        return shouldLog;
    }

    private async Task CheckAndSendWarningsAsync(Stations station, int waterHeight)
    {
        if (waterHeight < 30)
        {
            await _emailSender.SendEmailAsync(
                station.Email,
                "Drought Warning",
                $"Drought alert for station ID: {station.ID} ({station.StationName}). Water height is critically low at {waterHeight}."
            );
            _logger.LogInformation($"Drought warning sent for station ID: {station.ID}.");
        }
        else if (waterHeight > 70)
        {
            await _emailSender.SendEmailAsync(
                station.Email,
                "Flood Warning",
                $"Flood alert for station ID: {station.ID} ({station.StationName}). Water height is dangerously high at {waterHeight}."
            );
            _logger.LogInformation($"Flood warning sent for station ID: {station.ID}.");
        }
    }
}
