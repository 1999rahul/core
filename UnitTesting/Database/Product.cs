namespace UnitTesting.Entities
{
    public class Product
    {
        public int Id { get; set; }

        public decimal Price { get; set; }

        public string Name { get; set; } = string.Empty;

        public int StockQuantity { get; set; }

    }
}
