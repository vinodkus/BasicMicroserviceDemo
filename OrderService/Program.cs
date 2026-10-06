using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Http.Resilience;
using OrderService.Data;
using OrderService.Services;
using Polly;
using Polly.CircuitBreaker;
using Polly.Fallback;
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
    // Fallback first so it runs only AFTER Retry, Circuit Breaker, and Timeout finish.
    // Request → Fallback → Retry → Circuit Breaker → Timeout
    pipeline.AddFallback(new FallbackStrategyOptions<HttpResponseMessage>
    {
        ShouldHandle = args =>
        {
            if (args.Outcome.Exception is BrokenCircuitException)
            {
                return PredicateResult.True();
            }

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
        FallbackAction = _ =>
        {
            Console.WriteLine("[ProductService] Fallback executed - ProductService unavailable.");

            var fallbackResponse = new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
            {
                Content = new StringContent("ProductService is temporarily unavailable. Order cannot be created right now.")
            };

            return Outcome.FromResultAsValueTask(fallbackResponse);
        }
    });

    // Retry stays outer relative to Circuit Breaker and Timeout.
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
            Console.WriteLine($"[ProductService] Retry {retryNumber}");
            return default;
        }
    });

    // Circuit Breaker sits between Retry (outer) and Timeout (inner):
    // Request → Retry → Circuit Breaker → Timeout
    // It counts only transient failures/timeouts, not HTTP 200 and not 404.
    pipeline.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
    {
        FailureRatio = 0.5,
        MinimumThroughput = 4,
        SamplingDuration = TimeSpan.FromSeconds(10),
        BreakDuration = TimeSpan.FromSeconds(15),
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
        OnOpened = args =>
        {
            Console.WriteLine($"[ProductService] CIRCUIT OPEN - calls blocked for {args.BreakDuration.TotalSeconds:0} seconds");
            return default;
        },
        OnClosed = _ =>
        {
            Console.WriteLine("[ProductService] Circuit closed");
            return default;
        },
        OnHalfOpened = _ =>
        {
            Console.WriteLine("[ProductService] Circuit half-open");
            return default;
        }
    });

    // Timeout last = inner strategy. Each GET attempt waits at most TimeoutSeconds.
    pipeline.AddTimeout(new HttpTimeoutStrategyOptions
    {
        Timeout = TimeSpan.FromSeconds(timeoutSeconds),
        OnTimeout = _ =>
        {
            Console.WriteLine("[ProductService] Timeout");
            return default;
        }
    });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

Console.WriteLine("[OrderService] Starting...");
Console.WriteLine("[OrderService] Listening on http://localhost:5002");
Console.WriteLine("[OrderService] Swagger: http://localhost:5002/swagger");
app.Run();
