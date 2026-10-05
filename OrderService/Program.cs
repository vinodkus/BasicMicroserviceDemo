using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Http.Resilience;
using OrderService.Data;
using OrderService.Services;
using Polly;
using Polly.Timeout;

var builder = WebApplication.CreateBuilder(args);

// This microservice MUST run on port 5002.
builder.WebHost.UseUrls("http://localhost:5002");

builder.Services.AddControllers();

// Swagger UI: http://localhost:5002/swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// OrderService uses its OWN SQLite database.
// This is NOT ProductService's database.
builder.Services.AddDbContext<OrderDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("OrderDatabase")));

// IHttpClientFactory + typed HttpClient.
// BaseAddress is ProductService: http://localhost:5001
var productServiceSection = builder.Configuration.GetSection("ProductService");
var productServiceUrl = productServiceSection["BaseUrl"] ?? "http://localhost:5001";
var timeoutSeconds = productServiceSection.GetValue("TimeoutSeconds", 3);
var maxRetryAttempts = productServiceSection.GetValue("Retry:MaxRetryAttempts", 2);
var retryDelayMilliseconds = productServiceSection.GetValue("Retry:DelayMilliseconds", 200);

builder.Services.AddHttpClient<ProductApiClient>(client =>
{
    client.BaseAddress = new Uri(productServiceUrl);

    // Polly timeout controls each GET attempt.
    // Infinite here so HttpClient does not cancel retries too early.
    client.Timeout = Timeout.InfiniteTimeSpan;
})
.AddResilienceHandler("product-service", pipeline =>
{
    // Polly executes the first-added strategy as the outermost wrapper.
    // Retry first = Request → Retry → Timeout (per attempt).
    // Default HttpRetryStrategyOptions retries only transient failures:
    // connection errors, timeouts, 408, 429, 500, 502, 503, 504.
    // It does NOT retry 404 Not Found.
    pipeline.AddRetry(new HttpRetryStrategyOptions
    {
        MaxRetryAttempts = maxRetryAttempts,
        Delay = TimeSpan.FromMilliseconds(retryDelayMilliseconds),
        BackoffType = DelayBackoffType.Exponential,
        UseJitter = true,
        // Default retry ignores TaskCanceledException. ProductService down + timeout
        // cancels HttpClient, so we must treat that as retryable.
        ShouldHandle = args =>
        {
            if (HttpClientResiliencePredicates.IsTransient(args.Outcome))
            {
                return PredicateResult.True();
            }

            if (args.Outcome.Exception is TimeoutRejectedException or TaskCanceledException)
            {
                return PredicateResult.True();
            }

            return PredicateResult.False();
        },
        // AttemptNumber is zero-based. +1 so the console shows Retry 1 then Retry 2.
        OnRetry = args =>
        {
            var retryNumber = args.AttemptNumber + 1;
            var reason = args.Outcome.Exception is { } ex
                ? $"{ex.GetType().Name}: {ex.Message}"
                : args.Outcome.Result is { } response
                    ? $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}"
                    : "unknown";

            Console.WriteLine($"[product-service] Retry {retryNumber}: {reason}");
            return default;
        }
    });

    // Timeout last = inner strategy. Each GET attempt waits at most TimeoutSeconds.
    pipeline.AddTimeout(TimeSpan.FromSeconds(timeoutSeconds));
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

app.Run();
