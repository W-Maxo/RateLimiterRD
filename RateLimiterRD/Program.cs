using Microsoft.Extensions.DependencyInjection;
using RateLimiterRD;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
string devStr = builder.Environment.IsDevelopment() ? "Dev" : "";
string redisConnectionString = builder.Configuration.GetValue<string>($"RedisConnectionStrings:Connection{devStr}");

var multiplexer = ConnectionMultiplexer.Connect(redisConnectionString);
builder.Services.AddSingleton<IConnectionMultiplexer>(multiplexer);
builder.Services.AddSingleton<IRateLimiterSingleton, RateLimiterSingleton>();

builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
builder.Services.AddMemoryCache();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
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
