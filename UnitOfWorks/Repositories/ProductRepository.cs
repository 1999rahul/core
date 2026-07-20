using System.Data;
using UnitOfWorks.Entities;

namespace UnitOfWorks.Repositories
{
    public class ProductRepository : Repository<Product>, IProductRepository
    {
        public ProductRepository(IDbConnection connection, IDbTransaction? transaction, string tableName) : base(connection, transaction, tableName) { }

        public Task<IEnumerable<Product>> SearchByName(string keyword)
        {
            throw new NotImplementedException();
        }
    }
}
