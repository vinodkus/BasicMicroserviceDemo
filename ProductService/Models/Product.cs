namespace ProductService.Models;

// This is the Product data that ProductService owns.
// In Microservices, each service has its own model. We do NOT share this class with OrderService.
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
}
