using System.Text.Json;
using Publisher.Core.Domain.Entities;
using Publisher.Core.Domain.Interfaces;

namespace Publisher.Core.Domain.Application.Services;

public class PublishingService : IPublishingService
{
	private readonly IOutboxRepository _outboxRepository;
	private readonly IMessagePublisher _messagePublisher;

	public PublishingService(IOutboxRepository outboxRepository, IMessagePublisher messagePublisher)
	{
		_outboxRepository = outboxRepository ?? throw new ArgumentNullException(nameof(outboxRepository));
		_messagePublisher = messagePublisher ?? throw new ArgumentNullException(nameof(messagePublisher));
	}

	public async Task EnqueueMessageAsync(string topic, string key, object payload)
	{
		if (string.IsNullOrEmpty(topic))
			throw new ArgumentException("Topic cannot be null or empty", nameof(topic));

		if (string.IsNullOrEmpty(key))
			throw new ArgumentException("Key cannot be null or empty", nameof(key));

		if (payload == null)
			throw new ArgumentNullException(nameof(payload));

		var serializedPayload = JsonSerializer.Serialize(payload);
		var outboxMessage = new OutboxMessage(topic, key, serializedPayload);
            
		await _outboxRepository.SaveAsync(outboxMessage);
	}

	public async Task ProcessOutboxAsync()
	{
		var unprocessedMessages = await _outboxRepository.GetUnprocessedMessagesAsync();

		foreach (var message in unprocessedMessages)
		{
			try
			{
				Console.WriteLine(
					$"Processing outbox message: {message.Id}, Topic: {message.Topic}, Retry Count: {message.RetryCount}");

				await _messagePublisher.PublishAsync(message.Topic, message.Key, message.Payload);

				// Mark as processed only if successful
				await _outboxRepository.MarkAsProcessedAsync(message.Id);

				Console.WriteLine($"Successfully published message: {message.Id}");
			}
			catch (Exception ex)
			{
				// Explicitly update the retry count and error message
				string errorMessage = $"Failed to publish message: {ex.Message}";
				Console.WriteLine($"{errorMessage}. Message ID: {message.Id}, Retry Count: {message.RetryCount}");

				await _outboxRepository.MarkAsFailedAsync(message.Id, errorMessage);

				// Optional: Add a small delay after an error to avoid hammering the broker
				await Task.Delay(100);
			}
		}
	}
}
