using System.Data;
using Microsoft.Data.SqlClient;

namespace AspiraHub.Data
{
    // Reads the same "DefaultConnection" connection string every other
    // part of an ASP.NET Core + SQL Server app normally uses
    // (appsettings.json -> ConnectionStrings:DefaultConnection). Change
    // the key below if your project names it differently.
    public class SqlConnectionFactory : IDbConnectionFactory
    {
        private readonly string _connectionString;

        public SqlConnectionFactory(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Missing ConnectionStrings:DefaultConnection in appsettings.json");
        }

        public IDbConnection CreateConnection() => new SqlConnection(_connectionString);
    }
}
