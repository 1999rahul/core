using Microsoft.EntityFrameworkCore;
using UnitTesting.Database;
using UnitTesting.Entities;

namespace UnitTesting.Services
{
    public class OrderService : IOrderService
    {
        private AppDbContext dbContext;

        private IEmailService emailService;

        private readonly int UNIT_PRICE = 10;

        public OrderService(AppDbContext dbContext, IEmailService emailService)
        {
            this.dbContext = dbContext;
            this.emailService = emailService;
        }

        public async Task<Order> CreateOrder(int productId, int quantity, string customerEmail)
        {

            var product = await dbContext.Products.FindAsync(productId);

            if (product == null)
                throw new InvalidOperationException($"Product with id {productId} not found.");

            product.StockQuantity = product.StockQuantity - quantity;


            int totalPrice = quantity * UNIT_PRICE;

            Order order = new Order()
            {
                ProductId = productId,
                Quantity = quantity,
                TotalPrice = totalPrice,
                CustomerEmail = customerEmail
            };

            dbContext.Add(order);


            await dbContext.SaveChangesAsync();

            await emailService.SendOrderConformationEmailAsync(customerEmail, order.Id);

            return order;
        }
    }
}
