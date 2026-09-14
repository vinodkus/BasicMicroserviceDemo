using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using OrderService.Models;
using OrderService.Services;

namespace OrderService.Controllers;

[ApiController]
[Route("api/orders")]
public class OrdersController : ControllerBase
{
    private readonly OrderDbContext _db;
    private readonly ProductApiClient _productApiClient;

    public OrdersController(OrderDbContext db, ProductApiClient productApiClient)
    {
        _db = db;
        _productApiClient = productApiClient;
    }

    // GET /api/orders
    [HttpGet]
    public async Task<ActionResult<List<Order>>> GetAll()
    {
        var orders = await _db.Orders.ToListAsync();
        return Ok(orders);
    }

    // GET /api/orders/1
    [HttpGet("{id:int}")]
    public async Task<ActionResult<Order>> GetById(int id)
    {
        var order = await _db.Orders.FindAsync(id);

        if (order is null)
        {
            return NotFound(new { message = $"Order with id {id} was not found." });
        }

        return Ok(order);
    }

    // POST /api/orders
    // This is the Microservices communication flow:
    // Client -> OrderService -> HTTP GET ProductService -> OrderService saves order in its OWN database
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
        // 3. Get the product information (price lives in ProductService, not in this database).
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

        // 5. Save the order into OrderService's own database.
        var order = new Order
        {
            ProductId = request.ProductId,
            Quantity = request.Quantity,
            TotalAmount = totalAmount
        };

        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
    }
}
