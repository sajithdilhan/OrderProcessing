using Order.Orchestrator.Api.Application.Interfaces;
using Order.Orchestrator.Api.Application.Services;
using Order.Orchestrator.Api.BackgroundServices;
using Order.Orchestrator.Api.Infrastructure.Clients;
using Order.Orchestrator.Api.Infrastructure.Queue;
using Order.Orchestrator.Api.Infrastructure.Storage;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddHttpClient<IOmsClient, OmsClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:OmsBaseUrl"]!);
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddHttpClient<IInventoryClient, InventoryClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:InventoryBaseUrl"]!);
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddSingleton<IOrderQueue, ChannelOrderQueue>();
builder.Services.AddSingleton<IProcessedOrderStore, InMemoryProcessedOrderStore>();
builder.Services.AddSingleton<IDeadLetterStore, InMemoryDeadLetterStore>();

builder.Services.AddScoped<PendingOrderSyncService>();
builder.Services.AddScoped<PaymentConfirmedProcessor>();

builder.Services.AddHostedService<PaymentQueueConsumerService>();

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

app.Run();
