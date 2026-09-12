using ProductService.Models;

namespace ProductService.Services;

// Simple in-memory store. Data lives only while this service is running.
// If you restart ProductService, the list goes back to the 3 sample products.
//
// Registered as Singleton in Program.cs so ALL API requests share the SAME list.
public class ProductStore
{
    private readonly List<Product> _products =
    [
        new Product { Id = 1, Name = "Laptop", Price = 50000 },
        new Product { Id = 2, Name = "Mouse", Price = 1000 },
        new Product { Id = 3, Name = "Keyboard", Price = 2000 }
    ];

    private int _nextId = 4;

    public List<Product> GetAll()
    {
        return _products;
    }

    public Product? GetById(int id)
    {
        return _products.FirstOrDefault(product => product.Id == id);
    }

    public Product Add(string name, decimal price)
    {
        var product = new Product
        {
            Id = _nextId++,
            Name = name,
            Price = price
        };

        _products.Add(product);
        return product;
    }
}
