using UnitOfWorks.Database;
using UnitOfWorks.Entities;
using UnitOfWorks.Repositories;

namespace UnitOfWorks.Services
{
    public class ProductService
    {
        private IUnitOfWork unitOfWorks;

        private IRepository<Product> productsRepository;

        public ProductService(IUnitOfWork unitOfWorks)
        {
            this.unitOfWorks = unitOfWorks;
            this.productsRepository = unitOfWorks.Repository<Product>();
        }

        // using UOW Without transaction
        public async Task<List<Product>> GetAllProducts(int id)
        {
            var allProducts = await productsRepository.GetAllAsync();
            return allProducts.ToList();
        }

        // Using UOW with Transaction
        public async Task UpdateProductStock(int to, int from, int quantity)
        {
            unitOfWorks.BeginTransaction();

            try
            {
               var toProduct = await productsRepository.GetByIdAsync(to);

               var fromProduct = await productsRepository.GetByIdAsync(from);

                toProduct.Stock -= quantity;
                fromProduct.Stock += quantity;

                await productsRepository.UpdateAsync(toProduct);
                await productsRepository.UpdateAsync(fromProduct);

                unitOfWorks.Commit();
            }
            catch
            {
                unitOfWorks.Rollback();
                throw;
            }
        }
    }
}
