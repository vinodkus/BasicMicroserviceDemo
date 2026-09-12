using Microsoft.AspNetCore.Mvc;
using OrderService.Models;
using OrderService.Services;

namespace OrderService.Controllers;

[ApiController]
[Route("api/orders")]
public class OrdersController : ControllerBase
{
    private readonly OrderStore _orderStore;
    private readonly ProductApiClient _productApiClient;

    public OrdersController(OrderStore orderStore, ProductApiClient productApiClient)
    {
        _orderStore = orderStore;
        _productApiClient = productApiClient;
    }

    // GET /api/orders
    [HttpGet]
    public ActionResult<List<Order>> GetAll()
    {
        return Ok(_orderStore.GetAll());
    }

    // GET /api/orders/1
    [HttpGet("{id:int}")]
    public ActionResult<Order> GetById(int id)
    {
        var order = _orderStore.GetById(id);

        if (order is null)
        {
            return NotFound(new { message = $"Order with id {id} was not found." });
        }

        return Ok(order);
    }

    // POST /api/orders
    // This is the Microservices communication flow:
    // Client -> OrderService -> HTTP GET ProductService -> OrderService creates order
    [HttpPost]
    public async Task<ActionResult<Order>> Create([FromBody] CreateOrderRequest request)
    {
        if (request.ProductId <= 0)
        {
            return BadRequest(new { message = "productId must be greater than 0." });
        }

        if (request.Quantity <= 0)
        {
            return BadRequest(new { message = "quantity must be greater than 0." });
        }

        // 1. Receive the request (already done above).
        // 2. Call ProductService using HttpClient.
        // 3. Get the product information.
        var lookup = await _productApiClient.GetProductByIdAsync(request.ProductId);

        if (lookup.IsNotFound)
        {
            return NotFound(new { message = $"Product with id {request.ProductId} was not found in ProductService." });
        }

        if (!lookup.IsFound || lookup.Product is null)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                message = lookup.ErrorMessage ?? "ProductService is currently unavailable."
            });
        }

        var product = lookup.Product;

        // 4. Calculate TotalAmount = Product.Price * Quantity
        var totalAmount = product.Price * request.Quantity;

        // 5. Create the order.
        // 6. Store it in the in-memory order list.
        var order = _orderStore.Add(request.ProductId, request.Quantity, totalAmount);

        // 7. Return the created order.
        return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
    }
}
