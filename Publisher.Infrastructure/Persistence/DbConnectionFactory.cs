using System.Data;
using Npgsql;

namespace Publisher.Infrastructure.Persistence;

public class DbConnectionFactory
{
	private readonly string _connectionString;

	public DbConnectionFactory(string connectionString)
	{
		_connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
	}

	public IDbConnection CreateConnection()
	{
		return new NpgsqlConnection(_connectionString);
	}
}
