using Confluent.Kafka;
using Subscriber.Core.Application.Messaging;

namespace Subscriber.Infrastructure.Messaging;

public class KafkaConsumerFactory : IKafkaConsumerFactory
{
	private readonly ConsumerConfig _consumerConfig;

	public KafkaConsumerFactory(string bootstrapServers, string groupId)
	{
		if (string.IsNullOrEmpty(bootstrapServers))
			throw new ArgumentException("Bootstrap servers cannot be null or empty", nameof(bootstrapServers));

		if (string.IsNullOrEmpty(groupId))
			throw new ArgumentException("Group ID cannot be null or empty", nameof(groupId));

		_consumerConfig = new ConsumerConfig
		{
			BootstrapServers = bootstrapServers,
			GroupId = groupId,
			AutoOffsetReset = AutoOffsetReset.Earliest,
			EnableAutoCommit = false
		};
	}

	public IConsumer<string, string> CreateConsumer(IEnumerable<string> topics)
	{
		var consumer = new ConsumerBuilder<string, string>(_consumerConfig).Build();
            
		foreach (var topic in topics)
		{
			consumer.Subscribe(topic);
		}
            
		return consumer;
	}
}
