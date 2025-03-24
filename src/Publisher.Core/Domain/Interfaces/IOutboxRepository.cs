using Publisher.Core.Domain.Entities;

namespace Publisher.Core.Domain.Interfaces;

public interface IOutboxRepository
{
	Task<int> SaveAsync(OutboxMessage message);
	Task<IEnumerable<OutboxMessage>> GetUnprocessedMessagesAsync(int batchSize = 10);
	Task MarkAsProcessedAsync(Guid id);
	Task MarkAsFailedAsync(Guid id, string error);
}