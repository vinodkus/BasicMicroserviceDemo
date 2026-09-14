namespace OrderService.Models;

// Order belongs to OrderService and is stored in OrderService's own database.
// Product details are NOT stored here. We only store ProductId.
// Price is fetched from ProductService over HTTP at order-create time.
public class Order
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal TotalAmount { get; set; }
}
