using System.Data;

namespace UnitOfWorks.Repositories
{
    public class Repository<T> : IRepository<T> where T : class
    {
        private readonly IDbConnection connection;

        private readonly IDbTransaction? transaction;

        private readonly string tableName;

        public Repository(IDbConnection connection, IDbTransaction? transaction, string tableName)
        {
            this.connection = connection;
            this.transaction = transaction;
            this.tableName = tableName;
        }

        public Task<int> AddAsync(T entity)
        {
            throw new NotImplementedException();
        }

        public Task<int> DeleteAsync(int id)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<T>> GetAllAsync()
        {
            throw new NotImplementedException();
        }

        public Task<T> GetByIdAsync(int id)
        {
            throw new NotImplementedException();
        }

        public Task<int> UpdateAsync(T entity)
        {
            throw new NotImplementedException();
        }
    }
}
