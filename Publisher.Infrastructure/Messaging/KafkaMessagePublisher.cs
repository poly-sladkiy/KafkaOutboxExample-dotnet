using Confluent.Kafka;
using Publisher.Core.Domain.Interfaces;

namespace Publisher.Infrastructure.Messaging;

public class KafkaMessagePublisher : IMessagePublisher
    {
        private readonly ProducerConfig _producerConfig;

        public KafkaMessagePublisher(string bootstrapServers)
        {
            if (string.IsNullOrEmpty(bootstrapServers))
                throw new ArgumentException("Bootstrap servers cannot be null or empty", nameof(bootstrapServers));

            // Configuration with timeout settings
            _producerConfig = new ProducerConfig
            {
                BootstrapServers = bootstrapServers,
                Acks = Acks.All,
                // Set lower timeouts to fail faster when Kafka is down
                MessageSendMaxRetries = 3,
                RetryBackoffMs = 1000,
                MessageTimeoutMs = 10000, // 10 seconds
                SocketTimeoutMs = 5000,   // 5 seconds
                RequestTimeoutMs = 5000   // 5 seconds
            };
        }

        public async Task PublishAsync(string topic, string key, string payload)
        {
            // Using a short-lived producer for each message
            using var producer = new ProducerBuilder<string, string>(_producerConfig)
                .SetErrorHandler((_, e) => 
                    Console.WriteLine($"Kafka error: {e.Reason}. Code: {e.Code}"))
                .Build();
            
            try
            {
                var message = new Message<string, string>
                {
                    Key = key,
                    Value = payload
                };

                var deliveryResult = await producer.ProduceAsync(topic, message);
                Console.WriteLine($"Message delivered to: {deliveryResult.TopicPartitionOffset}");
            }
            catch (ProduceException<string, string> ex)
            {
                Console.WriteLine($"Failed to deliver message: {ex.Message}. Error: {ex.Error.Reason}");
                throw;
            }
            catch (KafkaException ex)
            {
                Console.WriteLine($"Kafka exception: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error in Kafka publisher: {ex.Message}");
                throw;
            }
        }
    }
