using Microsoft.EntityFrameworkCore;
using ProductService.Data;

var builder = WebApplication.CreateBuilder(args);

// This microservice MUST run on port 5001.
// OrderService is configured to call http://localhost:5001
builder.WebHost.UseUrls("http://localhost:5001");

builder.Services.AddControllers();

// Swagger UI so you can test APIs in the browser: http://localhost:5001/swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ProductService uses its OWN SQLite database.
// Connection string comes from appsettings.json (not hard-coded here).
builder.Services.AddDbContext<ProductDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("ProductDatabase")));

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

app.Run();
