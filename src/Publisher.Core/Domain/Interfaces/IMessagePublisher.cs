namespace Publisher.Core.Domain.Interfaces;

public interface IMessagePublisher
{
	Task PublishAsync(string topic, string key, string payload);
}
