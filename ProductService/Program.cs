using ProductService.Services;

var builder = WebApplication.CreateBuilder(args);

// This microservice MUST run on port 5001.
// OrderService is configured to call http://localhost:5001
builder.WebHost.UseUrls("http://localhost:5001");

builder.Services.AddControllers();

// Swagger UI so you can test APIs in the browser: http://localhost:5001/swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Singleton = one shared ProductStore for the whole application.
// This keeps the in-memory product list alive across all HTTP requests.
builder.Services.AddSingleton<ProductStore>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

app.Run();
