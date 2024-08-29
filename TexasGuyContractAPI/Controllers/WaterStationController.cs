using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using TexasGuyContractAPI.Interface;
using TexasGuyContractIdentity.Data;

namespace TexasGuyContractAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WaterStationController : ControllerBase
    {
        private readonly IWaterStationService _waterStationService;
        private readonly ApplicationDbContext _context;

        public WaterStationController(IWaterStationService waterStationService, ApplicationDbContext context)
        {
            _waterStationService = waterStationService;
            _context = context;
        }

        // Endpoint to log water stations based on a specific station ID
        [HttpPost("log-water-stations/{stationId}")]
        public async Task<IActionResult> LogWaterStations(int stationId)
        {
            try
            {
                await _waterStationService.CheckAndLogWaterStationsAsync(stationId);
                return Ok($"Water station ID {stationId} processed successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                return StatusCode(500, "Internal server error");
            }
        }

        // Endpoint to test database connection
        [HttpGet("test-database-connection")]
        public async Task<IActionResult> TestDatabaseConnection()
        {
            try
            {
                var count = await _context.StationsEntries.CountAsync();
                return Ok($"Database connection successful. Number of stations: {count}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Database connection failed: {ex.Message}");
            }
        }
    }
}
