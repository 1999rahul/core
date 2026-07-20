using UnitOfWorks.Repositories;

namespace UnitOfWorks.Database
{
    public interface IUnitOfWork: IDisposable
    {
        IProductRepository Products { get; }

        IRepository<T> Repository<T>() where T : class;

        void BeginTransaction();

        void Commit();

        void Rollback();
    }
}
