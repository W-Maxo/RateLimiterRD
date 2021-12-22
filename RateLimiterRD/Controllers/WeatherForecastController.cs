using Microsoft.AspNetCore.Mvc;

namespace RateLimiterRD.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly IRateLimiterSingleton rateLimiterSingleton;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly ILogger<WeatherForecastController> logger;

        public WeatherForecastController(ILogger<WeatherForecastController> logger, IRateLimiterSingleton rateLimiterSingleton, IHttpContextAccessor httpContextAccessor)
        {
            this.logger = logger;
            this.httpContextAccessor = httpContextAccessor;
            this.rateLimiterSingleton = rateLimiterSingleton;
        }

        [HttpGet(Name = "GetWeatherForecast")]
        public async Task<IActionResult> Get()
        {
            string ipAddress = httpContextAccessor.HttpContext.Connection.RemoteIpAddress.ToString();

            var Exceeded = await rateLimiterSingleton.LimitExceeded(ipAddress);

            if (Exceeded)
            {
                return new ContentResult
                {
                    StatusCode = 429
                };
            }
            else
            {
                var res = Enumerable.Range(1, 5).Select(index => new WeatherForecast
                {
                    Date = DateTime.Now.AddDays(index),
                    TemperatureC = Random.Shared.Next(-20, 55),
                    Summary = Summaries[Random.Shared.Next(Summaries.Length)]
                }).ToArray();

                string json = System.Text.Json.JsonSerializer.Serialize(res);

                return new ContentResult
                {
                    Content = json,
                    ContentType = "application/json",
                    StatusCode = 200
                };
            }
        }
    }
}