using Common.Models;
using ConsumerApp.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<InMemoryStore>();
// Add services to the container.
// Register the background consumer service. We use a lambda to pass the store's Add method.
builder.Services.AddHostedService<RabbitMqConsumerService>(serviceProvider =>
{
    var store = serviceProvider.GetRequiredService<InMemoryStore>();
    return new RabbitMqConsumerService(message =>
    {
        store.Add(message);
        Console.WriteLine($"Stored message with ID: {message.Id} (Total stored: {store.Messages.Count})");
    });
});

builder.Services.AddControllers();
// Add Swagger/OpenAPI support (optional)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
