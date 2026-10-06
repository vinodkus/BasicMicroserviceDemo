using System.Net;
using OrderService.Models;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace OrderService.Services;

// This class talks to the OTHER microservice: ProductService.
// This is the most important Microservices idea in this demo:
// OrderService does not store products. It asks ProductService over HTTP.
//
// Timeout + retry are configured on this HttpClient in Program.cs.
// This method still makes one GET call. The resilience handler may repeat
// that GET automatically if ProductService has a temporary failure.
public class ProductApiClient
{
    private readonly HttpClient _httpClient;

    // IHttpClientFactory creates this HttpClient using the "ProductService" configuration
    // from Program.cs (BaseAddress = http://localhost:5001).
    public ProductApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<ProductLookupResult> GetProductByIdAsync(int productId)
    {
        try
        {
            Console.WriteLine("[ProductService] Request started");

            // Example: GET http://localhost:5001/api/products/1
            var response = await _httpClient.GetAsync($"/api/products/{productId}");

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return ProductLookupResult.NotFound();
            }

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                var message = string.IsNullOrWhiteSpace(body)
                    ? "ProductService is temporarily unavailable. Order cannot be created right now."
                    : body;

                return ProductLookupResult.Unavailable(message);
            }

            var product = await response.Content.ReadFromJsonAsync<ProductDto>();

            if (product is null)
            {
                return ProductLookupResult.Unavailable("ProductService returned an empty body.");
            }

            return ProductLookupResult.Found(product);
        }
        catch (Exception ex) when (ex is HttpRequestException or TimeoutRejectedException or TaskCanceledException or BrokenCircuitException)
        {
            // Safety net if an exception still escapes the Fallback policy.
            Console.WriteLine("[OrderService] ProductService unavailable.");
            Console.WriteLine("[OrderService] Request failed after retries.");
            return ProductLookupResult.Unavailable(
                "ProductService is temporarily unavailable. Order cannot be created right now.");
        }
    }
}

// Small result object so the controller can return 404 vs 503 without extra complexity.
public class ProductLookupResult
{
    public bool IsFound { get; private set; }
    public bool IsNotFound { get; private set; }
    public ProductDto? Product { get; private set; }
    public string? ErrorMessage { get; private set; }

    public static ProductLookupResult Found(ProductDto product)
    {
        return new ProductLookupResult { IsFound = true, Product = product };
    }

    public static ProductLookupResult NotFound()
    {
        return new ProductLookupResult { IsNotFound = true };
    }

    public static ProductLookupResult Unavailable(string message)
    {
        return new ProductLookupResult { ErrorMessage = message };
    }
}
