namespace Subscriber.Core.Domain.Entities;

public class Message
{
	public string Topic { get; }
	public string Key { get; }
	public string Payload { get; }
	public DateTime ReceivedAt { get; }

	public Message(string topic, string key, string payload)
	{
		Topic = topic;
		Key = key;
		Payload = payload;
		ReceivedAt = DateTime.UtcNow;
	}
}
