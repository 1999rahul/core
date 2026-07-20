using System.Data;
using UnitOfWorks.Entities;
using UnitOfWorks.Helper;
using UnitOfWorks.Repositories;

namespace UnitOfWorks.Database
{
    // UnitOfWork opens exactly ONE physical connection in its constructor and keeps it open for the life of that instance. Getting this lifetime right matters:

    // Transient would open a brand-new connection every time something injects IUnitOfWork, defeating the "share one connection/transaction" purpose entirely.
    // Singleton would keep ONE connection open for the whole application's lifetime and share it (and any transaction on it) across concurrent, unrelated requests - which is unsafe and will corrupt data under load.
    // Scoped matches "one unit of work per request," which is exactly the semantic we want: everything that happens during this request shares one connection,
    // and can share one transaction if BeginTransaction() is called.

    public class UnitOfWork : IUnitOfWork
    {
        private readonly IDbConnection connection;

        private IDbTransaction? transaction;

        private bool disposed;

        private readonly Dictionary<Type, object> repositories = new();

        private IProductRepository? productRepository;

        public UnitOfWork(IDbConnectionFactory connectionFactory)
        {
            this.connection = connectionFactory.CreateConnection();
            connection.Open();
        }

        public IProductRepository Products => productRepository ?? new ProductRepository(connection, transaction, TableNameHelper.GetTableName<Product>());


        public IRepository<T> Repository<T>() where T : class
        {
            Type type = typeof(T);

            if (repositories.TryGetValue(type, out var repository))
            {
                return (IRepository<T>)repository;
            }

            var tableName = TableNameHelper.GetTableName<T>();

            repositories[type] = new Repository<T>(connection, transaction, tableName);

            return (IRepository<T>)repositories[type];
        }

        public void BeginTransaction()
        {
            transaction = connection.BeginTransaction();

            ClearRepositories();
        }

        public void Commit()
        {
            try
            {
                transaction?.Commit();
            }
            catch
            {
                Rollback();
                throw;
            }
            finally
            {
                transaction?.Dispose();
                transaction = null;
                ClearRepositories();
            }
        }

        public void Rollback()
        {
            transaction?.Rollback();
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            // Dispose the DB Connection and transaction
            transaction?.Dispose();

            if (connection.State != ConnectionState.Closed)
            {
                connection.Close();
            }

            connection.Dispose();
            disposed = true;
        }

        private void ClearRepositories()
        {
            // Clear all the Repositories
            repositories.Clear();

            productRepository = null;
        }
    }
}
