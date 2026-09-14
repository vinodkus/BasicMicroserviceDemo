using Microsoft.EntityFrameworkCore;
using ProductService.Models;

namespace ProductService.Data;

// ProductService's OWN database.
// OrderService cannot use this DbContext or this database.
public class ProductDbContext : DbContext
{
    public ProductDbContext(DbContextOptions<ProductDbContext> options)
        : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Starter data so GET /api/products is not empty after the first migration.
        modelBuilder.Entity<Product>().HasData(
            new Product { Id = 1, Name = "Laptop", Price = 50000 },
            new Product { Id = 2, Name = "Mouse", Price = 1000 },
            new Product { Id = 3, Name = "Keyboard", Price = 2000 }
        );
    }
}
