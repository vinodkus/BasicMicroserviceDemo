namespace OrderService.Models;

// JSON body that the client sends to POST /api/orders
public class CreateOrderRequest
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
}
