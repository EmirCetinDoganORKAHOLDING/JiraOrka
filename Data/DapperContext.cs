using Microsoft.Data.SqlClient;
using System.Data;

namespace Tezgah.Data;

public class DapperContext
{
    private readonly string _connectionString;

    public DapperContext(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection connection string bulunamadı.");
    }

    public IDbConnection CreateConnection()
        => new SqlConnection(_connectionString);
}
