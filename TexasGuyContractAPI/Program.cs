using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using TexasGuyContractAPI.Interface;
using TexasGuyContractIdentity.Data;
using TexasGuyContractIdentity.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.SetMinimumLevel(LogLevel.Information); // Ensure all information level logs are captured

// Configure services
builder.Services.AddControllers();
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
        b => b.MigrationsAssembly("TexasGuyContractAPI"))); // Ensure the correct migrations assembly

builder.Services.AddDefaultIdentity<IdentityUser>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddScoped<IWaterStationService, WaterStationService>(); // Register IWaterStationService
builder.Services.AddScoped<IEmailSender, EmailSender>(); // Register EmailSender

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register the background service
builder.Services.AddHostedService<WaterStationLoggingService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
