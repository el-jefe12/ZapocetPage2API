using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using TexasGuyContractAPI.Interface;
using TexasGuyContractIdentity.Data;
using TexasGuyContractIdentity.Services; // Make sure you have the TokenService here or use your own service

namespace TexasGuyContractAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WaterStationController : ControllerBase
    {
        private readonly IWaterStationService _waterStationService;
        private readonly ApplicationDbContext _context;
        private readonly TokenService _tokenService; // Inject your token service

        public WaterStationController(IWaterStationService waterStationService, ApplicationDbContext context, TokenService tokenService)
        {
            _waterStationService = waterStationService;
            _context = context;
            _tokenService = tokenService;
        }

        // Helper method to validate API tokens
        private async Task<bool> ValidateTokenAsync(string token)
        {
            if (string.IsNullOrEmpty(token))
                return false;

            var apiToken = await _tokenService.GetTokenAsync(token);
            return apiToken != null;
        }

        // Endpoint to log water stations based on a specific station ID
        [HttpPost("log-water-stations/{stationId}")]
        public async Task<IActionResult> LogWaterStations(int stationId)
        {
            var token = Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
            if (!await ValidateTokenAsync(token))
            {
                return Unauthorized();
            }

            try
            {
                await _waterStationService.CheckAndLogWaterStationsAsync(stationId);
                return Ok($"Water station ID {stationId} processed successfully.");
            }
            catch (Exception ex)
            {
                // Log the exception and return a 500 error response
                // Use ILogger instead of Console.WriteLine for production
                Console.WriteLine($"An error occurred: {ex.Message}");
                return StatusCode(500, "Internal server error");
            }
        }

        // Endpoint to test database connection
        [HttpGet("test-database-connection")]
        public async Task<IActionResult> TestDatabaseConnection()
        {
            var token = Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
            if (!await ValidateTokenAsync(token))
            {
                return Unauthorized();
            }

            try
            {
                var count = await _context.StationsEntries.CountAsync();
                return Ok($"Database connection successful. Number of stations: {count}");
            }
            catch (Exception ex)
            {
                // Log the exception and return a 500 error response
                return StatusCode(500, $"Database connection failed: {ex.Message}");
            }
        }
    }
}
