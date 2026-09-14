using Microsoft.EntityFrameworkCore;
using OrderService.Models;

namespace OrderService.Data;

// OrderService's OWN database.
// Do NOT use ProductDbContext here. Orders and products stay in separate databases.
public class OrderDbContext : DbContext
{
    public OrderDbContext(DbContextOptions<OrderDbContext> options)
        : base(options)
    {
    }

    public DbSet<Order> Orders => Set<Order>();
}
