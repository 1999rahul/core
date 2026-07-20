using UnitOfWorks.Entities;

namespace UnitOfWorks.Repositories
{
    public interface IProductRepository: IRepository<Product>
    {
        Task<IEnumerable<Product>> SearchByName(string keyword);
    }
}
