using Microsoft.EntityFrameworkCore;
using Moq;
using UnitTesting.Database;
using UnitTesting.Entities;
using UnitTesting.Services;

namespace UnitTesting.Tests
{
    [TestFixture]
    public class OrderServiceTests
    {
        // Create mocks for all the dependencies
        private Mock<IEmailService>? mockEmailService = null;
        private AppDbContext? dbContext = null;

        private OrderService? sut = null;   // sut -> System under test

        // PENDING: What should we write in this method. Read more about OneTimeSetup
        // Runs once before all tests
        [OneTimeSetUp]
        public void OneTimeSetup()
        {

        }

        // Runs once before every test
        [SetUp]
        public void Setup()
        {
            mockEmailService = new Mock<IEmailService>();

            var dbContextOptions = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
            dbContext = new AppDbContext(dbContextOptions);

            sut = new OrderService(dbContext, mockEmailService.Object);
        }

        // Small helper to reduce repetition when seeding test data.
        private Product SeedProduct(int id = 1, decimal price = 10m, int stock = 5)
        {
            var product = new Product { Id = id, Name = "Test Widget", Price = price, StockQuantity = stock };
            dbContext?.Products.Add(product);
            dbContext?.SaveChanges();
            return product;
        }

        // Valid Request
        // Invalid Request

        [Test]
        public async Task CreateOrder_ValidRequest_CreatesAnOrder()
        {
            // Arrange
            var product = SeedProduct();
            mockEmailService?.Setup(emailService => emailService.SendOrderConformationEmailAsync(It.IsAny<string>(), It.IsAny<int>())).Returns(Task.CompletedTask);

            // Act
            var createdOrder = await sut?.CreateOrder(1, 2, "1999rahul.py@gmail.com");

            // Assert
            Assert.That(createdOrder.Id > 0);
        }

        [Test]
        public async Task CreateOrder_ValidRequest_DecresesStock()
        {
            // Arrange
            var product = SeedProduct();
            mockEmailService?.Setup(emailService => emailService.SendOrderConformationEmailAsync(It.IsAny<string>(), It.IsAny<int>())).Returns(Task.CompletedTask);

            // Act
            var createdOrder = await sut?.CreateOrder(1, 2, "1999rahul.py@gmail.com");

            // Assert
            var updatedProduct = await dbContext.Products.FindAsync(product.Id);

            Assert.That(updatedProduct.StockQuantity == 3);
        }

        // Runs after every test
        [TearDown]
        public void TearDown()
        {
            dbContext?.Dispose();
        }

        // PENDING: What should we write in this method. Read more about OneTimeTearDown
        [OneTimeTearDown]
        public void OneTimeTearDown()
        {

        }
    }
}
