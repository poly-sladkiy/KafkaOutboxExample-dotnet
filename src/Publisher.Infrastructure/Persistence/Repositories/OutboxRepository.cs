using Dapper;
using Publisher.Core.Domain.Entities;
using Publisher.Core.Domain.Interfaces;

namespace Publisher.Infrastructure.Persistence.Repositories;

public class OutboxRepository : IOutboxRepository
{
	private readonly DbConnectionFactory _connectionFactory;

	public OutboxRepository(DbConnectionFactory connectionFactory)
	{
		_connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
	}

	public async Task<int> SaveAsync(OutboxMessage message)
	{
		const string sql = @"
                INSERT INTO outbox_messages (id, topic, key, payload, created_at, processed_at, error, retry_count)
                VALUES (@Id, @Topic, @Key, @Payload, @CreatedAt, @ProcessedAt, @Error, @RetryCount)";

		using var connection = _connectionFactory.CreateConnection();
		return await connection.ExecuteAsync(sql, message);
	}

	public async Task<IEnumerable<OutboxMessage>> GetUnprocessedMessagesAsync(int batchSize = 10)
	{
		const string sql = @"
                SELECT id, topic, key, payload, created_at, processed_at, error, retry_count
                FROM outbox_messages
                WHERE processed_at IS NULL
                ORDER BY created_at
                LIMIT @BatchSize";

		using var connection = _connectionFactory.CreateConnection();
		return await connection.QueryAsync<OutboxMessage>(sql, new { BatchSize = batchSize });
	}

	public async Task MarkAsProcessedAsync(Guid id)
	{
		const string sql = @"
                UPDATE outbox_messages
                SET processed_at = @ProcessedAt
                WHERE id = @Id";

		using var connection = _connectionFactory.CreateConnection();
		await connection.ExecuteAsync(sql, new { Id = id, ProcessedAt = DateTime.UtcNow });
	}

	public async Task MarkAsFailedAsync(Guid id, string error)
	{
		const string sql = @"
                UPDATE outbox_messages
                SET error = @Error, retry_count = retry_count + 1
                WHERE id = @Id";

		using var connection = _connectionFactory.CreateConnection();
		connection.Open();
            
		var result = await connection.ExecuteAsync(sql, new { Id = id, Error = error });
            
		if (result == 0)
		{
			throw new InvalidOperationException($"Failed to update message with ID {id}. Message not found.");
		}
	}
}