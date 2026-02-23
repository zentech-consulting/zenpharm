using System.Data;
using Microsoft.Data.SqlClient;

namespace Api.Common;

public interface IDbConnectionFactory
{
    Task<IDbConnection> CreateAsync();
}

internal sealed class SqlConnectionFactory(string connectionString) : IDbConnectionFactory
{
    public async Task<IDbConnection> CreateAsync()
    {
        var conn = new SqlConnection(connectionString);
        await conn.OpenAsync();
        return conn;
    }
}
