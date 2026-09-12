using OrderService.Services;

var builder = WebApplication.CreateBuilder(args);

// This microservice MUST run on port 5002.
builder.WebHost.UseUrls("http://localhost:5002");

builder.Services.AddControllers();

// Swagger UI: http://localhost:5002/swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Singleton = one shared OrderStore for the whole application.
builder.Services.AddSingleton<OrderStore>();

// IHttpClientFactory + typed HttpClient.
// BaseAddress is ProductService: http://localhost:5001
var productServiceUrl = builder.Configuration["ProductService:BaseUrl"] ?? "http://localhost:5001";

builder.Services.AddHttpClient<ProductApiClient>(client =>
{
    client.BaseAddress = new Uri(productServiceUrl);
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

app.Run();
