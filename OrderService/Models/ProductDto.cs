namespace OrderService.Models;

// This is NOT shared from ProductService.
// OrderService has its own small copy of Product, used only to read the HTTP response.
// In real Microservices, each service owns its own models.
public class ProductDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
}
