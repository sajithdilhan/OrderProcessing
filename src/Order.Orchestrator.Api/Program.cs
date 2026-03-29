using MassTransit;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Order.Orchestrator.Api.Application.Interfaces;
using Order.Orchestrator.Api.Application.Services;
using Order.Orchestrator.Api.Consumers;
using Order.Orchestrator.Api.HealthChecks;
using Order.Orchestrator.Api.Infrastructure.Clients;
using Order.Orchestrator.Api.Infrastructure.Storage;
using Polly;
using Scalar.AspNetCore;
using Shared.Contracts.Common;
using System.Text.Json;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddHttpClient<IOmsClient, OmsClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:OmsBaseUrl"]!);
})
.AddStandardResilienceHandler(options =>
{
    options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(15);
    options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(5);

    options.Retry.MaxRetryAttempts = 3;
    options.Retry.BackoffType = DelayBackoffType.Exponential;
    options.Retry.Delay = TimeSpan.FromSeconds(1);
    options.Retry.UseJitter = true;

    options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(10);
    options.CircuitBreaker.FailureRatio = 0.5;
    options.CircuitBreaker.MinimumThroughput = 4;
    options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(30);
});

builder.Services.AddHttpClient<IInventoryClient, InventoryClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:InventoryBaseUrl"]!);
})
.AddStandardResilienceHandler(options =>
{
    options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(20);
    options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(6);

    options.Retry.MaxRetryAttempts = 3;
    options.Retry.BackoffType = DelayBackoffType.Exponential;
    options.Retry.Delay = TimeSpan.FromSeconds(2);
    options.Retry.UseJitter = true;

    options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(12); 
    options.CircuitBreaker.FailureRatio = 0.5;
    options.CircuitBreaker.MinimumThroughput = 4;
    options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(30);
});

builder.Services.AddSingleton<IProcessedOrderStore, InMemoryProcessedOrderStore>();
builder.Services.AddSingleton<IDeadLetterStore, InMemoryDeadLetterStore>();

builder.Services.AddScoped<PendingOrderSyncService>();
builder.Services.AddScoped<PaymentConfirmedProcessor>();

var rabbitSettings = builder.Configuration.GetSection(RabbitMqSettings.SectionName).Get<RabbitMqSettings>()
    ?? throw new InvalidOperationException("RabbitMqSettings configuration is missing.");

builder.Services.AddSingleton(rabbitSettings);
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<PaymentConfirmedConsumer>();
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(rabbitSettings!.Host,
                 rabbitSettings.VirtualHost,
                 h =>
                 {
                     h.Username(rabbitSettings.Username);
                     h.Password(rabbitSettings.Password);
                 });

        cfg.ReceiveEndpoint("order-orchestrator-payment-confirmed", e =>
        {
            e.ConfigureConsumer<PaymentConfirmedConsumer>(context);
        });
    });
});

builder.Services.AddHealthChecks()
    .AddCheck<InventoryHealthCheck>(
        "inventory",
        failureStatus: HealthStatus.Degraded,
        tags: new[] { "ready", "dependency" })
    .AddCheck<RabbitMqHealthCheck>(
        "rabbitmq",
        failureStatus: HealthStatus.Unhealthy,
        tags: new[] { "ready", "dependency", "messaging" });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("Order Orchestrator API")
               .WithTheme(ScalarTheme.DeepSpace)
               .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient)
               .EnableDarkMode();
    });
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";

        var response = new
        {
            status = report.Status.ToString(),
            totalDuration = report.TotalDuration,
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                description = entry.Value.Description,
                duration = entry.Value.Duration,
                exception = entry.Value.Exception?.Message
            })
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
});

app.Run();
