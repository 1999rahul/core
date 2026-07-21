using Microsoft.EntityFrameworkCore;
using UnitTesting.Entities;

namespace UnitTesting.Database
{
    public class AppDbContext: DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // Repositories
        public DbSet<Product> Products => Set<Product>();

        public DbSet<Order> Orders => Set<Order>();
    }
}
