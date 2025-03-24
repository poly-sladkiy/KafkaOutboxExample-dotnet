using Subscriber.Core.Application.Interfaces;
using Subscriber.Core.Application.Messaging;
using Subscriber.Core.Domain.Entities;
using Subscriber.Core.Domain.Interfaces;

namespace Subscriber.Core.Application.Services;

public class MessageHandlingService : IMessageHandlingService
    {
        private readonly IMessageProcessor _messageProcessor;
        private readonly IKafkaConsumerFactory _kafkaConsumerFactory;
        private CancellationTokenSource _cancellationTokenSource;
        private readonly List<string> _subscribedTopics = new List<string>();
        private Task _consumingTask;

        public MessageHandlingService(
            IMessageProcessor messageProcessor,
            IKafkaConsumerFactory kafkaConsumerFactory)
        {
            _messageProcessor = messageProcessor ?? throw new ArgumentNullException(nameof(messageProcessor));
            _kafkaConsumerFactory = kafkaConsumerFactory ?? throw new ArgumentNullException(nameof(kafkaConsumerFactory));
        }

        public void Subscribe(string topic)
        {
            if (string.IsNullOrEmpty(topic))
                throw new ArgumentException("Topic cannot be null or empty", nameof(topic));

            if (!_subscribedTopics.Contains(topic))
            {
                _subscribedTopics.Add(topic);
                Console.WriteLine($"Subscribed to topic: {topic}");
            }
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            if (_consumingTask != null)
            {
                throw new InvalidOperationException("Consumer is already running");
            }

            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            
            _consumingTask = Task.Run(async () =>
            {
                try
                {
                    using var consumer = _kafkaConsumerFactory.CreateConsumer(_subscribedTopics);
                    
                    while (!_cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        try
                        {
                            var consumeResult = consumer.Consume(_cancellationTokenSource.Token);
                            if (consumeResult != null)
                            {
                                var message = new Message(
                                    consumeResult.Topic,
                                    consumeResult.Message.Key,
                                    consumeResult.Message.Value);

                                await _messageProcessor.ProcessAsync(message);
                                
                                // Manually commit the offset
                                consumer.Commit(consumeResult);
                            }
                        }
                        catch (Exception ex) when (!(ex is OperationCanceledException))
                        {
                            Console.WriteLine($"Error consuming message: {ex.Message}");
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // Expected on cancellation
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Fatal error in consumer: {ex.Message}");
                }
            });
        }

        public async Task StopAsync()
        {
            if (_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Cancel();
                
                if (_consumingTask != null)
                {
                    try
                    {
                        await _consumingTask;
                    }
                    catch (OperationCanceledException)
                    {
                        // Expected
                    }
                    finally
                    {
                        _consumingTask = null;
                    }
                }

                _cancellationTokenSource.Dispose();
                _cancellationTokenSource = null;
            }
        }
    }
