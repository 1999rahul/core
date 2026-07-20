using System.Data;

namespace UnitOfWorks.Database
{
    public interface IDbConnectionFactory
    {
        IDbConnection CreateConnection();
    }
}
