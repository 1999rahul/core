using UnitOfWorks.Helper;

namespace UnitOfWorks.Entities
{

    [TableName("Products")]
    public class Product
    {
        public int Id { get; set; }

        public int Stock { get; set; }
    }
}
