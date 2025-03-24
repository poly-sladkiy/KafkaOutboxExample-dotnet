namespace Publisher.Core.Domain.Entities;

public class OutboxMessage
{
	public Guid Id { get; private set; }
	public string Topic { get; private set; }
	public string Key { get; private set; }
	public string Payload { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime? ProcessedAt { get; private set; }
	public string? Error { get; private set; }
	public int RetryCount { get; private set; }

	private OutboxMessage() { }
	
	public OutboxMessage(string topic, string key, string payload)
	{
		Id = Guid.NewGuid();
		Topic = topic;
		Key = key;
		Payload = payload;
		CreatedAt = DateTime.UtcNow;
		ProcessedAt = null;
		Error = null;
		RetryCount = 0;
	}

	public void MarkAsProcessed()
	{
		ProcessedAt = DateTime.UtcNow;
	}

	public void MarkAsFailed(string error)
	{
		Error = error;
		RetryCount++;
	}
}
