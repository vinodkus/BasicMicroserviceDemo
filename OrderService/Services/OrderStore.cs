using OrderService.Models;

namespace OrderService.Services;

// Simple in-memory store. Data lives only while OrderService is running.
// Registered as Singleton so all requests share the same order list.
public class OrderStore
{
    private readonly List<Order> _orders = [];
    private int _nextId = 1;

    public List<Order> GetAll()
    {
        return _orders;
    }

    public Order? GetById(int id)
    {
        return _orders.FirstOrDefault(order => order.Id == id);
    }

    public Order Add(int productId, int quantity, decimal totalAmount)
    {
        var order = new Order
        {
            Id = _nextId++,
            ProductId = productId,
            Quantity = quantity,
            TotalAmount = totalAmount
        };

        _orders.Add(order);
        return order;
    }
}
