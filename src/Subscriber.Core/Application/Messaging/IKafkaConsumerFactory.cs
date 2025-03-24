using Confluent.Kafka;

namespace Subscriber.Core.Application.Messaging;

public interface IKafkaConsumerFactory
{
	IConsumer<string, string> CreateConsumer(IEnumerable<string> topics);
}
