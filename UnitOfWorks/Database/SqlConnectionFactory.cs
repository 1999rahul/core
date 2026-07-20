using Microsoft.Data.SqlClient;
using System.Data;

namespace UnitOfWorks.Database
{
    // This class holds nothing butjust a connection string and knows how to createan SqlConnection Object
    // This has no per-request state, so one instance for entire lifecycle is safe and avoids the pointless re-allocation of memory
    public class SqlConnectionFactory : IDbConnectionFactory
    {
        private string connectionString;
        public SqlConnectionFactory(string connectionString)
        {
            this.connectionString = connectionString;
        }

        public IDbConnection CreateConnection()
        {
            return new SqlConnection(connectionString);
        }
    }
}
