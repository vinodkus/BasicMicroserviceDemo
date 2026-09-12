namespace OrderService.Models;

// Order belongs to OrderService. Product details are NOT stored here.
// We only store ProductId. Price is fetched from ProductService at order-create time.
public class Order
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal TotalAmount { get; set; }
}
