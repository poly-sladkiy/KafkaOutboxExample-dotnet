namespace Publisher.Core.Domain.Interfaces;

public interface IPublishingService
{
	Task EnqueueMessageAsync(string topic, string key, object payload);
	Task ProcessOutboxAsync();
}
